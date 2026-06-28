#!/usr/bin/env python3
"""Sync docs/wiki/tickets markdown files to GitHub issues and project board."""
from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
import time
from dataclasses import dataclass
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TICKETS_DIR = ROOT / "docs" / "wiki" / "tickets"
README_PATH = TICKETS_DIR / "README.md"
MANIFEST_PATH = ROOT / "scripts" / ".ticket-sync-manifest.json"

OWNER = "synthet"
REPO = "project-twelve"
DEFAULT_BRANCH = "master"
PROJECT_NUMBER = 2
PROJECT_ID = "PVT_kwHOAFXgIs4Bb1C5"

STATUS_FIELD_ID = "PVTSSF_lAHOAFXgIs4Bb1C5zhWingU"
PRIORITY_FIELD_ID = "PVTSSF_lAHOAFXgIs4Bb1C5zhWinmQ"

STATUS_BACKLOG = "f75ad846"
STATUS_DONE = "98236657"

PRIORITY_OPTION_IDS: dict[str, str] = {
    "P0": "79628723",
    "P1": "0a877460",
    "P2": "da944a9c",
}

API_DELAY_SECONDS = 0.5


@dataclass
class Ticket:
    path: Path
    ticket_id: str
    title: str
    status: str
    phase: str
    spec_references: list[str]
    user_story: str
    acceptance_criteria: str
    github_issue: str | None = None
    github_issue_status: str = "pending-creation"

    @property
    def filename(self) -> str:
        return self.path.name

    @property
    def phase_prefix(self) -> str:
        match = re.match(r"(P\d)", self.ticket_id)
        if not match:
            raise ValueError(f"Cannot derive phase prefix from ticket id {self.ticket_id!r}")
        return match.group(1)

    @property
    def ticket_id_label(self) -> str:
        return f"ticket-id:{self.ticket_id}"

    @property
    def phase_label(self) -> str:
        return f"phase:{self.phase_prefix}"

    @property
    def wiki_url(self) -> str:
        return (
            f"https://github.com/{OWNER}/{REPO}/blob/{DEFAULT_BRANCH}/"
            f"docs/wiki/tickets/{self.filename}"
        )

    @property
    def project_status_option(self) -> str:
        return STATUS_DONE if self.status == "implemented" else STATUS_BACKLOG

    @property
    def should_close_issue(self) -> bool:
        return self.status == "implemented"


def run_gh(args: list[str], *, check: bool = True) -> subprocess.CompletedProcess[str]:
    command = ["gh", *args]
    result = subprocess.run(
        command,
        cwd=ROOT,
        text=True,
        capture_output=True,
        encoding="utf-8",
    )
    if check and result.returncode != 0:
        raise RuntimeError(
            f"Command failed ({result.returncode}): {' '.join(command)}\n"
            f"stdout: {result.stdout.strip()}\n"
            f"stderr: {result.stderr.strip()}"
        )
    return result


def gh_json(args: list[str], *, json_fields: str | None = None) -> object:
    if json_fields is not None:
        result = run_gh([*args, "--json", json_fields])
    else:
        result = run_gh([*args, "--format", "json"])
    return json.loads(result.stdout or "null")


def load_manifest() -> dict[str, object]:
    if not MANIFEST_PATH.exists():
        return {"issues": {}, "project_items": {}}
    return json.loads(MANIFEST_PATH.read_text(encoding="utf-8"))


def save_manifest(data: dict[str, object]) -> None:
    MANIFEST_PATH.write_text(json.dumps(data, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def parse_front_matter(text: str) -> tuple[dict[str, object], str]:
    if not text.startswith("---"):
        raise ValueError("Ticket file missing YAML front matter")
    parts = text.split("---", 2)
    if len(parts) < 3:
        raise ValueError("Ticket file front matter is malformed")
    front_matter_raw = parts[1]
    body = parts[2]
    data: dict[str, object] = {}
    current_key: str | None = None
    list_values: list[str] = []

    for line in front_matter_raw.splitlines():
        stripped = line.strip()
        if not stripped:
            continue
        if stripped.startswith("- ") and current_key is not None:
            list_values.append(stripped[2:].strip().strip('"'))
            data[current_key] = list_values
            continue
        list_values = []
        if ":" not in line:
            continue
        key, value = line.split(":", 1)
        key = key.strip()
        value = value.strip()
        current_key = key
        if value == "":
            data[key] = []
            list_values = data[key]  # type: ignore[assignment]
            continue
        if value == "null":
            data[key] = None
        elif value.startswith('"') and value.endswith('"'):
            data[key] = value[1:-1]
        else:
            data[key] = value
    return data, body


def extract_section(body: str, heading: str) -> str:
    pattern = re.compile(rf"^## {re.escape(heading)}\s*$", re.MULTILINE)
    match = pattern.search(body)
    if not match:
        return ""
    start = match.end()
    next_heading = re.search(r"^## ", body[start:], re.MULTILINE)
    end = start + next_heading.start() if next_heading else len(body)
    return body[start:end].strip()


def load_tickets() -> list[Ticket]:
    tickets: list[Ticket] = []
    for path in sorted(TICKETS_DIR.glob("p*.md")):
        text = path.read_text(encoding="utf-8")
        front_matter, body = parse_front_matter(text)
        ticket_id = str(front_matter.get("id", "")).strip()
        if not ticket_id:
            raise ValueError(f"Missing id in {path}")
        tickets.append(
            Ticket(
                path=path,
                ticket_id=ticket_id,
                title=str(front_matter.get("title", "")).strip(),
                status=str(front_matter.get("status", "open")).strip(),
                phase=str(front_matter.get("phase", "")).strip(),
                spec_references=[str(item) for item in front_matter.get("spec_references", [])],
                user_story=extract_section(body, "User story"),
                acceptance_criteria=extract_section(body, "Acceptance criteria"),
                github_issue=front_matter.get("github_issue") if front_matter.get("github_issue") else None,
                github_issue_status=str(front_matter.get("github_issue_status", "pending-creation")),
            )
        )
    return tickets


def load_priority_field() -> dict[str, object]:
    field_data = gh_json(["project", "field-list", str(PROJECT_NUMBER), "--owner", OWNER])
    priority_field = next(
        (item for item in field_data["fields"] if item["name"] == "Priority"),
        None,
    )
    if priority_field is None:
        raise RuntimeError("Priority field not found on project")
    return priority_field


def sync_priority_options(priority_field: dict[str, object], write: bool) -> None:
    for option in priority_field.get("options", []):
        PRIORITY_OPTION_IDS[option["name"]] = option["id"]

    missing = [name for name in ("P3", "P4", "P5") if name not in PRIORITY_OPTION_IDS]
    if not missing:
        print("Priority field already includes P3, P4, P5.")
        return

    print(f"Priority field missing options: {', '.join(missing)}")
    if not write:
        for name in missing:
            PRIORITY_OPTION_IDS[name] = f"DRYRUN_{name}"
        return

    existing_options = priority_field.get("options", [])
    option_inputs = [
        {
            "id": option["id"],
            "name": option["name"],
            "color": "GRAY",
            "description": "",
        }
        for option in existing_options
    ]
    for name in missing:
        option_inputs.append({"name": name, "color": "GRAY", "description": ""})

    query = """
    mutation($fieldId: ID!, $options: [ProjectV2SingleSelectFieldOptionInput!]!) {
      updateProjectV2Field(input: {fieldId: $fieldId, singleSelectOptions: $options}) {
        projectV2Field {
          ... on ProjectV2SingleSelectField {
            options { id name }
          }
        }
      }
    }
    """
    payload = json.dumps(
        {
            "query": query,
            "variables": {"fieldId": PRIORITY_FIELD_ID, "options": option_inputs},
        }
    )
    payload_result = subprocess.run(
        ["gh", "api", "graphql", "--input", "-"],
        input=payload,
        text=True,
        capture_output=True,
        encoding="utf-8",
    )
    if payload_result.returncode != 0:
        raise RuntimeError(f"Failed to extend Priority field: {payload_result.stderr.strip()}")
    response = json.loads(payload_result.stdout)
    if response.get("errors"):
        raise RuntimeError(f"Failed to extend Priority field: {response['errors']}")
    for option in response["data"]["updateProjectV2Field"]["projectV2Field"]["options"]:
        PRIORITY_OPTION_IDS[option["name"]] = option["id"]
    time.sleep(API_DELAY_SECONDS)
    print("Extended Priority field with P3, P4, P5.")


def ensure_labels(tickets: list[Ticket], write: bool) -> None:
    needed = {"ticket"}
    for ticket in tickets:
        needed.add(ticket.ticket_id_label)
        needed.add(ticket.phase_label)

    if not write:
        print(f"Would ensure {len(needed)} labels")
        return

    existing_result = run_gh(["label", "list", "--repo", f"{OWNER}/{REPO}", "--limit", "500"], check=False)
    existing = set(existing_result.stdout.splitlines())
    for name in sorted(needed):
        if name in existing:
            continue
        print(f"Creating label: {name}")
        run_gh(["label", "create", name, "--repo", f"{OWNER}/{REPO}", "--force"], check=False)
        time.sleep(API_DELAY_SECONDS)


def find_issue_for_ticket(ticket: Ticket) -> dict[str, object] | None:
    issues = gh_json(
        [
            "issue",
            "list",
            "--repo",
            f"{OWNER}/{REPO}",
            "--label",
            ticket.ticket_id_label,
            "--state",
            "all",
            "--limit",
            "1",
        ],
        json_fields="number,url,state",
    )
    if isinstance(issues, list) and issues:
        return issues[0]
    return None


def build_issue_body(ticket: Ticket) -> str:
    spec_lines = "\n".join(f"- `{ref}`" for ref in ticket.spec_references) or "- _None listed_"
    acceptance = ticket.acceptance_criteria or "_See wiki ticket._"
    user_story = ticket.user_story or "_See wiki ticket._"
    return (
        f"## Wiki ticket (canonical spec)\n"
        f"{ticket.wiki_url}\n\n"
        f"## Phase\n"
        f"{ticket.phase}\n\n"
        f"## User story\n"
        f"{user_story}\n\n"
        f"## Acceptance criteria\n"
        f"{acceptance}\n\n"
        f"## Spec references\n"
        f"{spec_lines}\n"
    )


def create_issue(ticket: Ticket, write: bool) -> dict[str, object]:
    if write:
        existing = find_issue_for_ticket(ticket)
        if existing:
            print(f"{ticket.ticket_id}: reusing issue #{existing['number']}")
            return existing

    labels = ["ticket", ticket.ticket_id_label, ticket.phase_label]
    print(f"{ticket.ticket_id}: create issue")
    if not write:
        return {"number": 0, "url": f"https://github.com/{OWNER}/{REPO}/issues/DRYRUN", "state": "OPEN"}

    body = build_issue_body(ticket)
    result = run_gh(
        [
            "issue",
            "create",
            "--repo",
            f"{OWNER}/{REPO}",
            "--title",
            ticket.title,
            "--body",
            body,
            "--label",
            ",".join(labels),
        ]
    )
    issue_url = result.stdout.strip()
    issue_number = int(issue_url.rstrip("/").split("/")[-1])
    issue = {"number": issue_number, "url": issue_url, "state": "OPEN"}
    if ticket.should_close_issue:
        run_gh(
            [
                "issue",
                "close",
                str(issue_number),
                "--repo",
                f"{OWNER}/{REPO}",
                "--reason",
                "completed",
            ]
        )
        issue["state"] = "CLOSED"
        print(f"{ticket.ticket_id}: closed issue #{issue_number} (implemented)")
    time.sleep(API_DELAY_SECONDS)
    return issue


def list_project_items() -> dict[str, str]:
    items = gh_json(["project", "item-list", str(PROJECT_NUMBER), "--owner", OWNER, "--limit", "100"])
    mapping: dict[str, str] = {}
    for item in items.get("items", []):
        content = item.get("content") or {}
        url = content.get("url")
        item_id = item.get("id")
        if url and item_id:
            mapping[url] = item_id
    return mapping


def add_to_project(
    issue: dict[str, object],
    write: bool,
    project_items: dict[str, str],
) -> str | None:
    issue_url = str(issue["url"])
    if issue_url in project_items:
        print(f"Project already contains {issue_url}")
        return project_items[issue_url]

    print(f"Adding to project: {issue_url}")
    if not write:
        return "DRYRUN_ITEM_ID"

    result = run_gh(
        ["project", "item-add", str(PROJECT_NUMBER), "--owner", OWNER, "--url", issue_url],
        check=False,
    )
    if result.returncode == 0:
        created = json.loads(result.stdout or "{}")
        item_id = created.get("id")
    else:
        if "already exists" not in result.stderr.lower():
            raise RuntimeError(
                f"Failed to add project item for {issue_url}: {result.stderr.strip()}"
            )
        print(f"Project already contains {issue_url} (reported by API)")
        item_id = list_project_items().get(issue_url)
    if item_id:
        project_items[issue_url] = item_id
    time.sleep(API_DELAY_SECONDS)
    return item_id


def set_project_fields(ticket: Ticket, item_id: str | None, write: bool) -> None:
    if not item_id:
        return
    status_option = ticket.project_status_option
    priority_option = PRIORITY_OPTION_IDS.get(ticket.phase_prefix)
    if not priority_option:
        raise RuntimeError(f"No Priority option configured for {ticket.phase_prefix}")

    print(
        f"{ticket.ticket_id}: set Status={'Done' if ticket.status == 'implemented' else 'Backlog'}, "
        f"Priority={ticket.phase_prefix}"
    )
    if not write:
        return

    run_gh(
        [
            "project",
            "item-edit",
            "--id",
            item_id,
            "--project-id",
            PROJECT_ID,
            "--field-id",
            STATUS_FIELD_ID,
            "--single-select-option-id",
            status_option,
        ]
    )
    time.sleep(API_DELAY_SECONDS)
    run_gh(
        [
            "project",
            "item-edit",
            "--id",
            item_id,
            "--project-id",
            PROJECT_ID,
            "--field-id",
            PRIORITY_FIELD_ID,
            "--single-select-option-id",
            priority_option,
        ]
    )
    time.sleep(API_DELAY_SECONDS)


def update_ticket_front_matter(ticket: Ticket, issue_url: str, write: bool) -> None:
    text = ticket.path.read_text(encoding="utf-8")
    updated = text
    updated = re.sub(
        r"^github_issue: null\s*$",
        f'github_issue: "{issue_url}"',
        updated,
        count=1,
        flags=re.MULTILINE,
    )
    updated = re.sub(
        r"^github_issue_status: pending-creation\s*$",
        "github_issue_status: created",
        updated,
        count=1,
        flags=re.MULTILINE,
    )
    if updated == text:
        print(f"{ticket.ticket_id}: front matter already linked")
        return
    print(f"{ticket.ticket_id}: update front matter")
    if write:
        ticket.path.write_text(updated, encoding="utf-8")


def update_readme(tickets: list[Ticket], issue_by_id: dict[str, dict[str, object]], write: bool) -> None:
    text = README_PATH.read_text(encoding="utf-8")
    updated = text
    for ticket in tickets:
        issue = issue_by_id.get(ticket.ticket_id)
        if not issue:
            continue
        number = issue["number"]
        url = issue["url"]
        issue_cell = f"[#{number}]({url})"
        pattern = re.compile(
            rf"(\| {re.escape(ticket.ticket_id)} \| .+? \| {re.escape(ticket.status)} \| )Pending creation( \|)"
        )
        updated, count = pattern.subn(rf"\1{issue_cell}\2", updated, count=1)
        if count == 0:
            print(f"Warning: README row not updated for {ticket.ticket_id}")

    if updated == text:
        print("README already up to date")
        return
    print("Updating docs/wiki/tickets/README.md")
    if write:
        README_PATH.write_text(updated, encoding="utf-8")


def sync_tickets(write: bool) -> int:
    tickets = load_tickets()
    print(f"Loaded {len(tickets)} wiki tickets")

    if write:
        priority_field = load_priority_field()
        sync_priority_options(priority_field, write=True)
        ensure_labels(tickets, write=True)
        project_items = list_project_items()
    else:
        for name in ("P3", "P4", "P5"):
            PRIORITY_OPTION_IDS.setdefault(name, f"DRYRUN_{name}")
        ensure_labels(tickets, write=False)
        project_items = {}

    manifest = load_manifest()
    issue_records: dict[str, object] = manifest.setdefault("issues", {})
    project_records: dict[str, object] = manifest.setdefault("project_items", {})
    issue_by_id: dict[str, dict[str, object]] = {}

    for ticket in tickets:
        issue = create_issue(ticket, write)
        issue_by_id[ticket.ticket_id] = issue
        if write and issue.get("number"):
            issue_records[ticket.ticket_id] = issue

        item_id = add_to_project(issue, write, project_items)
        if write and item_id:
            project_records[ticket.ticket_id] = {"item_id": item_id, "issue_url": issue["url"]}

        set_project_fields(ticket, item_id, write)
        update_ticket_front_matter(ticket, str(issue["url"]), write)

    update_readme(tickets, issue_by_id, write)

    if write:
        save_manifest(manifest)
        print(f"Manifest written to {MANIFEST_PATH.relative_to(ROOT)}")

    return len(tickets)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument("--dry-run", action="store_true", help="Print actions without writing")
    group.add_argument("--write", action="store_true", help="Create/update GitHub and wiki files")
    args = parser.parse_args()

    try:
        count = sync_tickets(write=args.write)
    except RuntimeError as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1

    mode = "write" if args.write else "dry-run"
    print(f"Completed {mode} for {count} tickets.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
