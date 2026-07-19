from __future__ import annotations

import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path


SCRIPT_PATH = Path(__file__).with_name("pick_next_task.py")
SPEC = importlib.util.spec_from_file_location("pick_next_task", SCRIPT_PATH)
assert SPEC is not None and SPEC.loader is not None
pick = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = pick
SPEC.loader.exec_module(pick)


def write_ticket(
    directory: Path,
    name: str,
    *,
    ticket_id: str,
    status: str,
    phase: str,
    issue: str | None = None,
    tags: list[str] | None = None,
) -> Path:
    tag_line = f"tags: [{', '.join(tags)}]\n" if tags else ""
    issue_line = f'github_issue: "{issue}"\n' if issue else "github_issue: null\n"
    body = (
        "---\n"
        f"id: {ticket_id}\n"
        f'title: "[{ticket_id}] Example"\n'
        f"status: {status}\n"
        f'phase: "{phase}"\n'
        f"{issue_line}"
        f"{tag_line}"
        "---\n\n"
        f"# {ticket_id}\n"
    )
    path = directory / name
    path.write_text(body, encoding="utf-8")
    return path


class PickNextTaskTests(unittest.TestCase):
    def test_rank_open_by_phase_then_id(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            tickets = Path(temp_dir)
            write_ticket(
                tickets,
                "p2-b.md",
                ticket_id="P2-ZZZ-001",
                status="open",
                phase="Phase P2 — Core",
                issue="https://github.com/synthet/project-twelve/issues/20",
            )
            write_ticket(
                tickets,
                "p1-a.md",
                ticket_id="P1-AAA-001",
                status="open",
                phase="Phase P1 — Prototype",
                issue="https://github.com/synthet/project-twelve/issues/10",
            )
            write_ticket(
                tickets,
                "p0-done.md",
                ticket_id="P0-SPEC-001",
                status="done",
                phase="Phase P0 — Discovery",
                issue="https://github.com/synthet/project-twelve/issues/1",
            )
            write_ticket(
                tickets,
                "p1-b.md",
                ticket_id="P1-BBB-001",
                status="open",
                phase="Phase P1 — Prototype",
                issue="https://github.com/synthet/project-twelve/issues/11",
            )
            ranked = pick.filter_open(pick.list_tickets(tickets))
            self.assertEqual(
                ["P1-AAA-001", "P1-BBB-001", "P2-ZZZ-001"],
                [t.id for t in ranked],
            )
            md = pick.format_markdown(ranked)
            self.assertIn("P1-AAA-001", md)
            self.assertIn("/task-claim 10", md)

    def test_phase_and_tag_filters(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            tickets = Path(temp_dir)
            write_ticket(
                tickets,
                "ux.md",
                ticket_id="P4-UX-001",
                status="open",
                phase="Phase P4 — Feature complete",
                issue="https://github.com/synthet/project-twelve/issues/48",
                tags=["ux", "ui", "p4"],
            )
            write_ticket(
                tickets,
                "net.md",
                ticket_id="P3-NET-001",
                status="open",
                phase="Phase P3 — Networking",
                issue="https://github.com/synthet/project-twelve/issues/40",
                tags=["net", "p3"],
            )
            by_phase = pick.filter_open(pick.list_tickets(tickets), phase="P4")
            self.assertEqual(["P4-UX-001"], [t.id for t in by_phase])
            by_tag = pick.filter_open(pick.list_tickets(tickets), tag="net")
            self.assertEqual(["P3-NET-001"], [t.id for t in by_tag])

    def test_unclaimable_without_issue(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            tickets = Path(temp_dir)
            write_ticket(
                tickets,
                "orphan.md",
                ticket_id="P2-ORPHAN-001",
                status="open",
                phase="Phase P2 — Core",
                issue=None,
            )
            ranked = pick.filter_open(pick.list_tickets(tickets))
            self.assertEqual(1, len(ranked))
            self.assertFalse(ranked[0].claimable)
            md = pick.format_markdown(ranked)
            self.assertIn("not claimable", md.lower())
            self.assertIn("sync_wiki_tickets_to_github.py", md)

    def test_json_payload(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            tickets = Path(temp_dir)
            write_ticket(
                tickets,
                "only.md",
                ticket_id="P5-DOC-001",
                status="open",
                phase="Phase P5 — Launch",
                issue="https://github.com/synthet/project-twelve/issues/99",
            )
            ranked = pick.filter_open(pick.list_tickets(tickets))
            payload = pick.format_json(ranked)
            self.assertIn('"id": "P5-DOC-001"', payload)
            self.assertIn('"count_open": 1', payload)


if __name__ == "__main__":
    unittest.main()
