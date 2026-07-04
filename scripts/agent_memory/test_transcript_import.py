"""Tests for transcript_import."""

from __future__ import annotations

import unittest
from pathlib import Path

from scripts.agent_memory.transcript_import import (
    discover_parent_transcripts,
    enrich_skeleton,
    extract_user_query,
    parse_transcript,
    should_skip_turn,
    skeleton_from_turn,
)

FIXTURE = Path(__file__).resolve().parent / "fixtures" / "sample-transcript.jsonl"
REPO = Path(__file__).resolve().parents[2]


class TranscriptImportTests(unittest.TestCase):
    def test_parse_two_turns(self) -> None:
        turns = parse_transcript(FIXTURE)
        self.assertEqual(len(turns), 2)
        self.assertEqual(turns[0].turn_index, 0)
        self.assertEqual(turns[1].turn_index, 1)
        self.assertEqual(turns[0].turn_ended, {"type": "turn_ended", "status": "success"})

    def test_extract_user_query(self) -> None:
        text = "<timestamp>x</timestamp>\n<user_query>\nFix autotile\n</user_query>"
        self.assertEqual(extract_user_query(text), "Fix autotile")

    def test_skeleton_harvests_files_and_commands(self) -> None:
        turns = parse_transcript(FIXTURE)
        sk = skeleton_from_turn(turns[0], FIXTURE, repo_root=REPO)
        self.assertIn("Assets/Scripts/Visual/Tiles/AutotileResolver.cs", sk["files_touched"])
        self.assertTrue(any("npm test" in c for c in sk["commands_run"]))
        self.assertTrue(sk["tests_run"])
        self.assertEqual(sk["task_summary"], "Fix autotile resolver in Assets/Scripts/Visual/Tiles/")

    def test_enrich_adds_autotile_candidate(self) -> None:
        turns = parse_transcript(FIXTURE)
        sk = skeleton_from_turn(turns[0], FIXTURE, repo_root=REPO)
        enriched = enrich_skeleton(turns[0], sk)
        texts = [c["text"] for c in enriched["memory_candidates"]]
        self.assertTrue(any("tile-viz" in t for t in texts))

    def test_skip_subagent_paths(self) -> None:
        transcript_dir = Path.home() / ".cursor" / "projects" / "d-Projects-project-twelve" / "agent-transcripts"
        if not transcript_dir.is_dir():
            self.skipTest("transcript dir not present")
        paths = discover_parent_transcripts(transcript_dir)
        for p in paths:
            self.assertNotIn("subagents", p.parts)

    def test_turn_has_activity(self) -> None:
        turns = parse_transcript(FIXTURE)
        self.assertIsNone(should_skip_turn(turns[0]))
        self.assertIsNone(should_skip_turn(turns[1]))


if __name__ == "__main__":
    unittest.main()
