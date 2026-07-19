#!/usr/bin/env python3
"""Recommend the next open wiki ticket (read-only).

Deterministic harness for the pick-next-task skill. Scans docs/wiki/tickets/,
filters status=open, ranks by phase then ticket id, prints recommendation.

Examples:
  python scripts/pick_next_task.py
  python scripts/pick_next_task.py --phase P2
  python scripts/pick_next_task.py --tag ux --json
  python scripts/pick_next_task.py --fetch-only
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from dataclasses import asdict, dataclass
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
TICKETS_DIR = REPO_ROOT / "docs" / "wiki" / "tickets"
PHASE_RE = re.compile(r"\bP([0-5])\b", re.IGNORECASE)
ISSUE_RE = re.compile(r"/issues/(\d+)")


@dataclass
class Ticket:
    path: str
    id: str
    title: str
    status: str
    phase: str
    phase_rank: int
    github_issue: str | None
    issue_number: int | None
    tags: list[str]
    claimable: bool

    @property
    def sort_key(self) -> tuple[int, str]:
        return (self.phase_rank, self.id.lower())


def parse_frontmatter(text: str) -> dict[str, object]:
    if not text.startswith("---"):
        return {}
    end = text.find("\n---", 3)
    if end < 0:
        return {}
    block = text[3:end].strip()
    try:
        import yaml

        data = yaml.safe_load(block) or {}
        return data if isinstance(data, dict) else {}
    except Exception:
        # Minimal fallback without PyYAML for simple scalars / lists
        data: dict[str, object] = {}
        for line in block.splitlines():
            if ":" not in line or line.strip().startswith("-"):
                continue
            key, _, raw = line.partition(":")
            key = key.strip()
            value = raw.strip().strip('"').strip("'")
            if value.startswith("[") and value.endswith("]"):
                inner = value[1:-1].strip()
                data[key] = [p.strip().strip('"').strip("'") for p in inner.split(",") if p.strip()]
            else:
                data[key] = value
        return data


def phase_rank(phase: object) -> int:
    text = str(phase or "")
    match = PHASE_RE.search(text)
    if match:
        return int(match.group(1))
    # Ticket id fallback e.g. P2-QA-001
    return 99


def issue_number(url: object) -> int | None:
    if not url:
        return None
    match = ISSUE_RE.search(str(url))
    return int(match.group(1)) if match else None


def normalize_tags(raw: object) -> list[str]:
    if raw is None:
        return []
    if isinstance(raw, list):
        return [str(t).strip().lower() for t in raw if str(t).strip()]
    if isinstance(raw, str):
        return [t.strip().lower() for t in raw.split(",") if t.strip()]
    return []


def load_ticket(path: Path, *, tickets_root: Path | None = None) -> Ticket | None:
    root = tickets_root or TICKETS_DIR
    if path.name.lower() == "readme.md":
        return None
    text = path.read_text(encoding="utf-8", errors="replace")
    fm = parse_frontmatter(text)
    ticket_id = str(fm.get("id") or path.stem).strip()
    title = str(fm.get("title") or ticket_id).strip()
    status = str(fm.get("status") or "").strip().lower()
    phase = str(fm.get("phase") or "").strip()
    gh = fm.get("github_issue")
    gh_url = None if gh in (None, "", "null") else str(gh).strip()
    tags = normalize_tags(fm.get("tags"))
    num = issue_number(gh_url)
    try:
        rel = str(path.resolve().relative_to(REPO_ROOT.resolve())).replace("\\", "/")
    except ValueError:
        try:
            rel = str(path.resolve().relative_to(root.resolve())).replace("\\", "/")
        except ValueError:
            rel = str(path).replace("\\", "/")
    return Ticket(
        path=rel,
        id=ticket_id,
        title=title,
        status=status,
        phase=phase or f"Phase from {ticket_id}",
        phase_rank=phase_rank(phase) if phase else phase_rank(ticket_id),
        github_issue=gh_url,
        issue_number=num,
        tags=tags,
        claimable=bool(gh_url and num),
    )


def list_tickets(tickets_dir: Path | None = None) -> list[Ticket]:
    directory = tickets_dir or TICKETS_DIR
    tickets: list[Ticket] = []
    if not directory.is_dir():
        return tickets
    for path in sorted(directory.glob("*.md")):
        ticket = load_ticket(path, tickets_root=directory)
        if ticket is not None:
            tickets.append(ticket)
    return tickets


def filter_open(
    tickets: list[Ticket],
    *,
    phase: str | None = None,
    tag: str | None = None,
) -> list[Ticket]:
    phase_key = phase.upper() if phase else None
    tag_key = tag.lower() if tag else None
    out: list[Ticket] = []
    for ticket in tickets:
        if ticket.status != "open":
            continue
        if phase_key:
            if f"P{ticket.phase_rank}" != phase_key and phase_key not in ticket.phase.upper():
                # Also match explicit Pn in id
                if not ticket.id.upper().startswith(phase_key + "-"):
                    continue
        if tag_key and tag_key not in ticket.tags:
            continue
        out.append(ticket)
    out.sort(key=lambda t: t.sort_key)
    return out


def format_markdown(
    ranked: list[Ticket],
    *,
    phase: str | None = None,
    tag: str | None = None,
    alternates: int = 3,
) -> str:
    filters = []
    if phase:
        filters.append(f"phase={phase}")
    if tag:
        filters.append(f"tag={tag}")
    filter_note = f" ({', '.join(filters)})" if filters else ""

    if not ranked:
        return (
            f"No open tickets{filter_note}. Do not invent work. "
            "File a ticket (backlog-queue) or relax filters."
        )

    rec = ranked[0]
    alts = ranked[1 : 1 + alternates]
    lines = [
        f"## Recommended{filter_note}",
        f"- **ID:** {rec.id}",
        f"- **Title:** {rec.title}",
        f"- **Status:** {rec.status}",
        f"- **Phase:** {rec.phase} (rank P{rec.phase_rank})",
        f"- **Issue:** {rec.github_issue or '(missing — not claimable)'}",
        f"- **Path:** `{rec.path}`",
        f"- **Claimable:** {'yes' if rec.claimable else 'no'}",
        "",
        "**Why:** lowest open phase, then ticket id (lexical).",
    ]
    if not rec.claimable:
        lines.append(
            "**Note:** missing `github_issue` — run "
            "`python scripts/sync_wiki_tickets_to_github.py` before claiming."
        )
    if alts:
        lines.append("")
        lines.append("## Alternates")
        for alt in alts:
            issue = f"#{alt.issue_number}" if alt.issue_number else "no-issue"
            lines.append(
                f"- {alt.id} — {alt.title} ({issue}; claimable={alt.claimable})"
            )
    if rec.claimable and rec.issue_number:
        lines.append("")
        lines.append(f"**Next step:** Run `/task-claim {rec.issue_number}`.")
    return "\n".join(lines)


def format_json(
    ranked: list[Ticket],
    *,
    phase: str | None = None,
    tag: str | None = None,
    alternates: int = 3,
) -> str:
    payload = {
        "filters": {"phase": phase, "tag": tag},
        "recommended": asdict(ranked[0]) if ranked else None,
        "alternates": [asdict(t) for t in ranked[1 : 1 + alternates]],
        "count_open": len(ranked),
    }
    return json.dumps(payload, indent=2)


def run_fetch_only() -> int:
    script = REPO_ROOT / "scripts" / "fetch_remotes.py"
    completed = subprocess.run(
        [sys.executable, str(script), "--fetch-only"],
        cwd=REPO_ROOT,
        check=False,
    )
    return int(completed.returncode)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--phase", default=None, help="Filter to phase e.g. P1 or P2")
    parser.add_argument("--tag", default=None, help="Filter to frontmatter tag")
    parser.add_argument("--json", action="store_true", help="Emit JSON instead of markdown")
    parser.add_argument(
        "--alternates",
        type=int,
        default=3,
        help="Number of alternate recommendations (default 3)",
    )
    parser.add_argument(
        "--fetch-only",
        action="store_true",
        help="Run scripts/fetch_remotes.py --fetch-only before ranking",
    )
    parser.add_argument(
        "--tickets-dir",
        type=Path,
        default=None,
        help="Override tickets directory (tests)",
    )
    args = parser.parse_args(argv)

    if args.fetch_only:
        rc = run_fetch_only()
        if rc != 0:
            print("warning: fetch-only failed; ranking local tickets anyway", file=sys.stderr)

    tickets = list_tickets(args.tickets_dir)
    ranked = filter_open(tickets, phase=args.phase, tag=args.tag)
    if args.json:
        print(format_json(ranked, phase=args.phase, tag=args.tag, alternates=args.alternates))
    else:
        print(format_markdown(ranked, phase=args.phase, tag=args.tag, alternates=args.alternates))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
