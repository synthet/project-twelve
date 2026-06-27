#!/usr/bin/env python3
"""Create GitHub issues from open-knowledge tickets and add them to a ProjectV2 board.

Usage:
  GITHUB_TOKEN=... python tools/github/create_project_issues.py \
    --repo OWNER/REPO --project-owner synthet --project-number 2
"""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
from pathlib import Path
from typing import Any
from urllib.error import HTTPError
from urllib.request import Request, urlopen

ROOT = Path(__file__).resolve().parents[2]
TICKETS = ROOT / "docs" / "wiki" / "tickets"
API = "https://api.github.com"
GRAPHQL = "https://api.github.com/graphql"


def request_json(method: str, url: str, token: str, payload: dict[str, Any] | None = None) -> Any:
    data = None if payload is None else json.dumps(payload).encode("utf-8")
    req = Request(
        url,
        data=data,
        method=method,
        headers={
            "Accept": "application/vnd.github+json",
            "Authorization": f"Bearer {token}",
            "Content-Type": "application/json",
            "X-GitHub-Api-Version": "2022-11-28",
        },
    )
    try:
        with urlopen(req) as response:
            body = response.read().decode("utf-8")
            return json.loads(body) if body else {}
    except HTTPError as exc:
        detail = exc.read().decode("utf-8")
        raise RuntimeError(f"GitHub API {method} {url} failed: {exc.code} {detail}") from exc


def graphql(token: str, query: str, variables: dict[str, Any]) -> dict[str, Any]:
    result = request_json("POST", GRAPHQL, token, {"query": query, "variables": variables})
    if result.get("errors"):
        raise RuntimeError(json.dumps(result["errors"], indent=2))
    return result["data"]


def parse_front_matter(text: str) -> tuple[dict[str, str], str]:
    if not text.startswith("---\n"):
        raise ValueError("ticket is missing YAML front matter")
    _, raw_meta, body = text.split("---\n", 2)
    meta: dict[str, str] = {}
    for line in raw_meta.splitlines():
        if not line or line.startswith("  - ") or ":" not in line:
            continue
        key, value = line.split(":", 1)
        meta[key.strip()] = value.strip().strip('"')
    return meta, body.strip()


def section(body: str, heading: str) -> str:
    pattern = re.compile(rf"^## {re.escape(heading)}\n(?P<content>.*?)(?=^## |\Z)", re.MULTILINE | re.DOTALL)
    match = pattern.search(body)
    return match.group("content").strip() if match else ""


def issue_body(ticket_path: Path, body: str, ticket_url: str) -> str:
    user_story = section(body, "User story")
    requirements = section(body, "Requirements")
    acceptance = section(body, "Acceptance criteria")
    specs = section(body, "Detailed technical specifications")
    verification = section(body, "Verification plan")
    return "\n\n".join(
        [
            f"## Markdown ticket\n[{ticket_path.as_posix()}]({ticket_url})",
            f"## User story\n{user_story}",
            f"## Requirements\n{requirements}",
            f"## Acceptance criteria\n{acceptance}",
            f"## Technical specifications\n{specs}",
            f"## Verification\n{verification}",
            "## Link maintenance\n- Keep this issue linked to the markdown ticket.\n- After creation, update the ticket front matter `github_issue` value with this issue URL.",
        ]
    )


def parse_tickets(repo: str, branch: str) -> list[dict[str, str]]:
    tasks: list[dict[str, str]] = []
    for ticket in sorted(TICKETS.glob("p*.md")):
        text = ticket.read_text(encoding="utf-8")
        meta, body = parse_front_matter(text)
        ticket_path = ticket.relative_to(ROOT)
        ticket_url = f"https://github.com/{repo}/blob/{branch}/{ticket_path.as_posix()}"
        phase_words = meta.get("phase", "Phase P0").split()
        phase_label = phase_words[1].lower() if len(phase_words) > 1 else "backlog"
        tasks.append(
            {
                "id": meta["id"],
                "title": meta["title"],
                "ticket": ticket_path.as_posix(),
                "body": issue_body(ticket_path, body, ticket_url),
                "phase_label": phase_label,
            }
        )
    return tasks


def existing_issue_titles(repo: str, token: str) -> set[str]:
    owner, name = repo.split("/", 1)
    titles: set[str] = set()
    page = 1
    while True:
        items = request_json(
            "GET",
            f"{API}/repos/{owner}/{name}/issues?state=all&per_page=100&page={page}",
            token,
        )
        if not items:
            return titles
        titles.update(item["title"] for item in items if "pull_request" not in item)
        page += 1


def project_id(project_owner: str, project_number: int, token: str) -> str:
    data = graphql(
        token,
        """
        query($login: String!, $number: Int!) {
          user(login: $login) { projectV2(number: $number) { id } }
        }
        """,
        {"login": project_owner, "number": project_number},
    )
    project = data.get("user", {}).get("projectV2")
    if not project:
        raise RuntimeError(f"Project not found: https://github.com/users/{project_owner}/projects/{project_number}")
    return project["id"]


def add_to_project(project: str, issue_node: str, token: str) -> None:
    graphql(
        token,
        """
        mutation($project: ID!, $content: ID!) {
          addProjectV2ItemById(input: {projectId: $project, contentId: $content}) { item { id } }
        }
        """,
        {"project": project, "content": issue_node},
    )


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True, help="Target GitHub repository as OWNER/REPO.")
    parser.add_argument("--project-owner", default="synthet")
    parser.add_argument("--project-number", type=int, default=2)
    parser.add_argument("--branch", default="main", help="Branch used to build markdown ticket URLs.")
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()

    if not re.fullmatch(r"[^/]+/[^/]+", args.repo):
        parser.error("--repo must be in OWNER/REPO format")

    tasks = parse_tickets(args.repo, args.branch)
    if args.dry_run:
        for task in tasks:
            print(f"{task['title']} -> {task['ticket']}")
        print(f"Prepared {len(tasks)} issues for https://github.com/users/{args.project_owner}/projects/{args.project_number}")
        return 0

    token = os.environ.get("GITHUB_TOKEN") or os.environ.get("GH_TOKEN")
    if not token:
        print("GITHUB_TOKEN or GH_TOKEN is required to create issues.", file=sys.stderr)
        return 2

    owner, repo_name = args.repo.split("/", 1)
    seen_titles = existing_issue_titles(args.repo, token)
    project = project_id(args.project_owner, args.project_number, token)

    created = 0
    skipped = 0
    for task in tasks:
        if task["title"] in seen_titles:
            print(f"skip existing: {task['title']}")
            skipped += 1
            continue
        issue = request_json(
            "POST",
            f"{API}/repos/{owner}/{repo_name}/issues",
            token,
            {
                "title": task["title"],
                "body": task["body"],
                "labels": ["spec-driven", "backlog", task["phase_label"]],
            },
        )
        add_to_project(project, issue["node_id"], token)
        print(f"created: {issue['html_url']} for {task['ticket']}")
        created += 1

    print(f"Created {created} issues, skipped {skipped} existing issues.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
