"""Parse Cursor agent transcript JSONL into session log skeletons."""

from __future__ import annotations

import json
import re
from dataclasses import dataclass, field
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

FILE_TOOLS = frozenset({"Read", "Write", "StrReplace", "Delete", "EditNotebook"})
TEST_COMMAND_RE = re.compile(
    r"(npm test|node --test|-runTests|pytest|ruff check|npm run test)",
    re.I,
)
USER_QUERY_RE = re.compile(r"<user_query>\s*(.*?)\s*</user_query>", re.S)
TIMESTAMP_RE = re.compile(r"<timestamp>\s*(.*?)\s*</timestamp>", re.S)
CURSOR_COMMAND_RE = re.compile(r"Cursor Command:\s*([^\s—-]+)", re.I)
SUMMARY_MAX = 120
OUTCOME_MAX = 500

# Heuristic memory-candidate patterns: (regex on combined text, text, category, confidence)
_CANDIDATE_RULES: list[tuple[re.Pattern[str], str, str, str]] = [
    (
        re.compile(r"tile-viz|Autotile|autotile", re.I),
        "Autotile changes require Unity EditMode tests and `cd tools/tile-viz && npm test` for C#/JS parity",
        "working_rule",
        "high",
    ),
    (
        re.compile(r"world-viz", re.I),
        "Terrain generation changes require Unity EditMode tests and `cd tools/world-viz && npm test`",
        "working_rule",
        "high",
    ),
    (
        re.compile(r"_Licensed|paid.?asset|PAID_ASSETS|check_paid_assets", re.I),
        "Licensed art lives in Assets/_Licensed submodule; never commit paid blobs to the public repo",
        "working_rule",
        "high",
    ),
    (
        re.compile(r"sprite bounds|bounds.*pivot|anchor.*bounds", re.I),
        "Autotile and sprite anchoring must use sprite bounds, not pivots (VISUAL_BEHAVIOR_SPEC)",
        "working_rule",
        "high",
    ),
    (
        re.compile(r"OKF|okf_lint|frontmatter", re.I),
        "Docs under docs/ need OKF frontmatter; missing type blocks CI merge",
        "recurring_issue",
        "high",
    ),
    (
        re.compile(r"sync_assistant_trees|\.cursor/.*generated", re.I),
        "Edit .claude/ sources then run python scripts/sync_assistant_trees.py; do not hand-edit .cursor/",
        "working_rule",
        "high",
    ),
    (
        re.compile(r"PlayerAvatarFactory|CharacterComposer|vendor script", re.I),
        "Licensed prefabs must strip vendor scripts; use project-owned drivers (PlayerAvatarFactory pattern)",
        "successful_pattern",
        "medium",
    ),
    (
        re.compile(r"Runtime MCP|8765|Play Mode", re.I),
        "Runtime MCP tools require Play Mode; endpoint is loopback-only on port 8765",
        "stable_fact",
        "medium",
    ),
    (
        re.compile(r"wiki/tickets|backlog-workflow|00-backlog", re.I),
        "Canonical task queue is docs/wiki/tickets/ linked to GitHub issues",
        "stable_fact",
        "medium",
    ),
    (
        re.compile(r"generate_visual_catalogs|AutotileCatalog|CharacterLayerCatalog", re.I),
        "Visual catalogs regenerate via Unity menus or scripts/generate_visual_catalogs.py",
        "working_rule",
        "medium",
    ),
]


@dataclass
class TurnSegment:
    """One user turn through turn_ended."""

    turn_index: int
    transcript_id: str = ""
    lines: list[dict[str, Any]] = field(default_factory=list)
    turn_ended: dict[str, Any] | None = None

    @property
    def manifest_key(self) -> str:
        return f"{self.transcript_id}:{self.turn_index}"

    def combined_text(self) -> str:
        parts: list[str] = []
        for line in self.lines:
            if line.get("role") == "user":
                parts.extend(_text_blocks(line))
            elif line.get("role") == "assistant":
                parts.extend(_text_blocks(line))
        return "\n".join(parts)

    def has_activity(self) -> bool:
        if self.turn_ended and self.turn_ended.get("status") == "error":
            return True
        for line in self.lines:
            if line.get("role") != "assistant":
                continue
            for block in line.get("message", {}).get("content") or []:
                if block.get("type") == "tool_use":
                    return True
            for block in _text_blocks(line):
                if block.strip() and block.strip() != "[REDACTED]":
                    return True
        return bool(extract_user_query(self.first_user_text))

    @property
    def first_user_text(self) -> str:
        for line in self.lines:
            if line.get("role") == "user":
                texts = _text_blocks(line)
                if texts:
                    return texts[0]
        return ""


def _text_blocks(line: dict[str, Any]) -> list[str]:
    content = line.get("message", {}).get("content") or []
    out: list[str] = []
    for block in content:
        if block.get("type") == "text" and block.get("text"):
            out.append(block["text"])
    return out


def _tool_uses(line: dict[str, Any]) -> list[dict[str, Any]]:
    content = line.get("message", {}).get("content") or []
    return [b for b in content if b.get("type") == "tool_use"]


def discover_parent_transcripts(transcript_dir: Path) -> list[Path]:
    """Return parent transcript JSONL paths, excluding subagents/, sorted by mtime."""
    if not transcript_dir.is_dir():
        return []
    paths = [
        p
        for p in transcript_dir.glob("**/*.jsonl")
        if "subagents" not in p.parts
    ]
    return sorted(paths, key=lambda p: p.stat().st_mtime)


def transcript_id_from_path(path: Path) -> str:
    return path.stem


def parse_transcript(path: Path) -> list[TurnSegment]:
    """Split JSONL into turns ending at turn_ended markers."""
    tid = transcript_id_from_path(path)
    turns: list[TurnSegment] = []
    current: TurnSegment | None = None
    turn_index = 0

    with path.open(encoding="utf-8") as fh:
        for raw in fh:
            raw = raw.strip()
            if not raw:
                continue
            try:
                obj = json.loads(raw)
            except json.JSONDecode(re.JSONDecodeError):
                continue

            if obj.get("type") == "turn_ended":
                if current is None:
                    current = TurnSegment(
                        turn_index=turn_index, transcript_id=tid
                    )
                current.turn_ended = obj
                current.turn_index = turn_index
                turns.append(current)
                turn_index += 1
                current = None
                continue

            role = obj.get("role")
            if role not in ("user", "assistant"):
                continue

            if role == "user" and current is None:
                current = TurnSegment(turn_index=turn_index, transcript_id=tid)

            if current is not None:
                current.lines.append(obj)

    if current is not None and current.lines:
        current.turn_index = turn_index
        turns.append(current)

    return turns


def extract_user_query(text: str) -> str:
    """Strip tags and return a one-line task summary."""
    m = USER_QUERY_RE.search(text)
    body = m.group(1).strip() if m else text
    body = TIMESTAMP_RE.sub("", body).strip()
    body = re.sub(r"<cursor_commands>.*?</cursor_commands>", "", body, flags=re.S).strip()
    body = re.sub(r"\s+", " ", body)
    if not body:
        cmd = CURSOR_COMMAND_RE.search(text)
        if cmd:
            return f"/{cmd.group(1).strip()}"
        return ""
    if len(body) > SUMMARY_MAX:
        return body[: SUMMARY_MAX - 3] + "..."
    return body


def parse_timestamp_to_iso(text: str) -> str | None:
    """Best-effort parse human timestamp to ISO-8601 UTC."""
    m = TIMESTAMP_RE.search(text)
    if not m:
        return None
    raw = m.group(1).strip()
    for fmt in (
        "%A, %b %d, %Y, %I:%M %p (UTC%z)",
        "%A, %B %d, %Y, %I:%M %p (UTC%z)",
    ):
        try:
            dt = datetime.strptime(raw.replace("(UTC-5)", "-0500").replace("(UTC+0)", "+0000"), fmt)
            if dt.tzinfo is None:
                dt = dt.replace(tzinfo=timezone.utc)
            return dt.astimezone(timezone.utc).strftime("%Y-%m-%dT%H%M%SZ")
        except ValueError:
            continue
    return None


def _normalize_repo_path(path: str, repo_root: Path | None) -> str:
    p = path.replace("\\", "/")
    if repo_root:
        try:
            rel = Path(path).resolve().relative_to(repo_root.resolve())
            return str(rel).replace("\\", "/")
        except ValueError:
            pass
    for marker in ("Assets/", "docs/", "scripts/", "tools/"):
        idx = p.find(marker)
        if idx >= 0:
            return p[idx:]
    return p


def _harvest_tools(turn: TurnSegment, repo_root: Path | None) -> tuple[list[str], list[str], list[str]]:
    files: list[str] = []
    commands: list[str] = []
    decisions: list[str] = []
    seen_files: set[str] = set()
    seen_cmds: set[str] = set()

    for line in turn.lines:
        if line.get("role") != "assistant":
            continue
        for tool in _tool_uses(line):
            name = tool.get("name") or ""
            inp = tool.get("input") or {}
            if name in FILE_TOOLS and inp.get("path"):
                norm = _normalize_repo_path(str(inp["path"]), repo_root)
                if norm not in seen_files:
                    seen_files.add(norm)
                    files.append(norm)
            if name == "Shell" and inp.get("command"):
                cmd = str(inp["command"]).strip()
                if cmd not in seen_cmds:
                    seen_cmds.add(cmd)
                    commands.append(cmd)
            if name == "CreatePlan" and inp.get("name"):
                title = str(inp.get("name", "")).strip()
                if title:
                    decisions.append(f"Plan: {title}")
            if name == "AskQuestion" and inp.get("title"):
                decisions.append(f"Asked: {inp.get('title')}")

    return files, commands, decisions


def _extract_tests(commands: list[str]) -> list[str]:
    return [c for c in commands if TEST_COMMAND_RE.search(c)]


def _final_outcome(turn: TurnSegment) -> str:
    if turn.turn_ended:
        status = turn.turn_ended.get("status")
        if status == "error":
            err = turn.turn_ended.get("error") or "error"
            return f"Turn ended with error: {err}"[:OUTCOME_MAX]
    last_text = ""
    for line in reversed(turn.lines):
        if line.get("role") != "assistant":
            continue
        texts = _text_blocks(line)
        for t in reversed(texts):
            if t.strip() and t.strip() != "[REDACTED]":
                last_text = t.strip()
                break
        if last_text:
            break
    if last_text:
        last_text = re.sub(r"\[REDACTED\]", "", last_text).strip()
        if len(last_text) > OUTCOME_MAX:
            return last_text[: OUTCOME_MAX - 3] + "..."
        return last_text
    if turn.turn_ended:
        return f"Completed ({turn.turn_ended.get('status', 'unknown')})"
    return "No assistant summary captured"


def _errors(turn: TurnSegment) -> list[str]:
    errs: list[str] = []
    if turn.turn_ended and turn.turn_ended.get("status") == "error":
        msg = turn.turn_ended.get("error") or "turn ended with error"
        errs.append(str(msg))
    combined = turn.combined_text().lower()
    if "user aborted" in combined:
        errs.append("User aborted request")
    return errs


def _turn_timestamp(turn: TurnSegment, path: Path) -> str:
    for line in turn.lines:
        if line.get("role") == "user":
            for text in _text_blocks(line):
                iso = parse_timestamp_to_iso(text)
                if iso:
                    return iso
    mtime = datetime.fromtimestamp(path.stat().st_mtime, tz=timezone.utc)
    return mtime.strftime("%Y-%m-%dT%H%M%SZ")


def skeleton_from_turn(
    turn: TurnSegment,
    path: Path,
    *,
    repo_root: Path | None = None,
) -> dict[str, Any]:
    """Build mechanical session dict (memory_candidates empty)."""
    summary = extract_user_query(turn.first_user_text)
    if not summary.strip():
        summary = f"Transcript {turn.transcript_id} turn {turn.turn_index}"

    files, commands, decisions = _harvest_tools(turn, repo_root)
    tests = _extract_tests(commands)

    test_results = ""
    if tests:
        if turn.turn_ended and turn.turn_ended.get("status") == "success":
            test_results = "Tests/commands ran; turn completed successfully"
        elif turn.turn_ended and turn.turn_ended.get("status") == "error":
            test_results = "Turn ended with error after test commands"

    return {
        "timestamp": _turn_timestamp(turn, path),
        "task_summary": summary,
        "files_touched": files,
        "commands_run": commands,
        "tests_run": tests,
        "test_results": test_results,
        "key_decisions": decisions,
        "errors_or_blockers": _errors(turn),
        "final_outcome": _final_outcome(turn),
        "memory_candidates": [],
        "source_transcript": {
            "transcript_id": turn.transcript_id,
            "turn_index": turn.turn_index,
            "path": str(path),
        },
    }


def suggest_memory_candidates(turn: TurnSegment, skeleton: dict[str, Any]) -> list[dict[str, str]]:
    """Heuristic durable learnings from turn text and touched files."""
    combined = skeleton.get("task_summary", "") + "\n"
    combined += skeleton.get("final_outcome", "") + "\n"
    combined += turn.combined_text() + "\n"
    combined += "\n".join(skeleton.get("files_touched") or [])
    combined += "\n".join(skeleton.get("key_decisions") or [])

    seen: set[str] = set()
    candidates: list[dict[str, str]] = []
    for pattern, text, category, confidence in _CANDIDATE_RULES:
        if pattern.search(combined):
            key = normalize_text(text)
            if key not in seen:
                seen.add(key)
                candidates.append(
                    {
                        "text": text,
                        "category": category,
                        "confidence": confidence,
                        "source_hint": turn.manifest_key,
                    }
                )
    return candidates[:5]


def normalize_text(text: str) -> str:
    return re.sub(r"[^a-z0-9]+", " ", text.lower()).strip()


def enrich_skeleton(turn: TurnSegment, skeleton: dict[str, Any]) -> dict[str, Any]:
    """Add curated memory_candidates to a skeleton."""
    out = dict(skeleton)
    out["memory_candidates"] = suggest_memory_candidates(turn, skeleton)
    return out


def should_skip_turn(turn: TurnSegment) -> str | None:
    """Return skip reason or None if turn should be logged."""
    if not turn.has_activity():
        return "no activity"
    summary = extract_user_query(turn.first_user_text).lower()
    if summary in ("implement the plan as specified, it is attached for your reference. do not edit the plan file itself.",):
        if not _harvest_tools(turn, None)[0] and not _harvest_tools(turn, None)[1]:
            if turn.turn_ended and turn.turn_ended.get("status") == "error":
                return None
            return "duplicate implement-plan stub"
    return None


def list_all_turns(
    transcript_dir: Path,
    *,
    repo_root: Path | None = None,
    manifest: dict[str, str] | None = None,
) -> list[dict[str, Any]]:
    """List metadata for every turn in parent transcripts."""
    manifest = manifest or {}
    entries: list[dict[str, Any]] = []
    for path in discover_parent_transcripts(transcript_dir):
        tid = transcript_id_from_path(path)
        for turn in parse_transcript(path):
            key = f"{tid}:{turn.turn_index}"
            skip = should_skip_turn(turn)
            summary = extract_user_query(turn.first_user_text) or f"Turn {turn.turn_index}"
            entries.append(
                {
                    "transcript_id": tid,
                    "turn_index": turn.turn_index,
                    "manifest_key": key,
                    "summary_preview": summary[:80],
                    "timestamp": _turn_timestamp(turn, path),
                    "path": str(path),
                    "already_imported": key in manifest,
                    "skip_reason": skip,
                }
            )
    entries.sort(key=lambda e: e["timestamp"])
    return entries


def load_turn(path: Path, turn_index: int) -> TurnSegment | None:
    for turn in parse_transcript(path):
        if turn.turn_index == turn_index:
            return turn
    return None
