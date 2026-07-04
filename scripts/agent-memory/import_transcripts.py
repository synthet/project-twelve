#!/usr/bin/env python3
"""Import Cursor agent transcripts into .agent-memory/raw-sessions/."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

_ROOT = Path(__file__).resolve().parents[2]
if str(_ROOT) not in sys.path:
    sys.path.insert(0, str(_ROOT))

import yaml  # noqa: E402

from scripts.agent_memory.limits import prune_old_sessions, session_timestamp_slug  # noqa: E402
from scripts.agent_memory.paths import ensure_dirs, find_repo_root, load_config  # noqa: E402
from scripts.agent_memory.schema import normalize_session, validate_session  # noqa: E402
from scripts.agent_memory.secrets import assert_no_secrets  # noqa: E402
from scripts.agent_memory.transcript_import import (  # noqa: E402
    discover_parent_transcripts,
    enrich_skeleton,
    list_all_turns,
    load_turn,
    parse_transcript,
    should_skip_turn,
    skeleton_from_turn,
    transcript_id_from_path,
)

MANIFEST_NAME = ".import-manifest.json"


def default_transcript_dir() -> Path:
    return Path.home() / ".cursor" / "projects" / "d-Projects-project-twelve" / "agent-transcripts"


def manifest_path(repo: Path) -> Path:
    return repo / ".agent-memory" / "raw-sessions" / MANIFEST_NAME


def load_manifest(repo: Path) -> dict[str, str]:
    path = manifest_path(repo)
    if not path.is_file():
        return {}
    with path.open(encoding="utf-8") as fh:
        return json.load(fh)


def save_manifest(repo: Path, manifest: dict[str, str]) -> None:
    path = manifest_path(repo)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def write_session(repo: Path, data: dict, manifest_key: str) -> Path:
    """Validate and write session YAML; update manifest."""
    config = load_config(repo)
    dirs = ensure_dirs(repo)
    data = normalize_session(data)
    errors = validate_session(data)
    if errors:
        raise ValueError("; ".join(errors))

    yaml_text = yaml.safe_dump(data, sort_keys=False, allow_unicode=True)
    assert_no_secrets(yaml_text, context="transcript import session")

    slug = session_timestamp_slug()
    ts = data.get("timestamp")
    if ts and isinstance(ts, str) and len(ts) >= 15:
        slug = ts.replace(":", "").replace("-", "-")
        if slug.endswith("Z"):
            slug = slug[:-1] + "Z"
    out_path = dirs["raw_sessions"] / f"{slug}.yaml"
    suffix = 0
    while out_path.exists():
        suffix += 1
        out_path = dirs["raw_sessions"] / f"{slug}_{suffix}.yaml"
    out_path.write_text(yaml_text, encoding="utf-8")

    manifest = load_manifest(repo)
    manifest[manifest_key] = out_path.name
    save_manifest(repo, manifest)
    prune_old_sessions(dirs["raw_sessions"], config.get("raw_session_retention_days", 90))
    return out_path


def resolve_transcript_path(transcript_dir: Path, transcript_id: str) -> Path | None:
    for path in discover_parent_transcripts(transcript_dir):
        if transcript_id_from_path(path) == transcript_id:
            return path
    return None


def cmd_list(args: argparse.Namespace) -> int:
    repo = args.repo_root or find_repo_root()
    manifest = load_manifest(repo)
    entries = list_all_turns(args.transcript_dir, repo_root=repo, manifest=manifest)
    if args.skip_imported:
        entries = [e for e in entries if not e["already_imported"]]
    print(json.dumps(entries, indent=2))
    return 0


def cmd_skeleton(args: argparse.Namespace) -> int:
    repo = args.repo_root or find_repo_root()
    path = resolve_transcript_path(args.transcript_dir, args.transcript_id)
    if path is None:
        print(f"Transcript not found: {args.transcript_id}", file=sys.stderr)
        return 1
    turn = load_turn(path, args.turn)
    if turn is None:
        print(f"Turn {args.turn} not found in {path}", file=sys.stderr)
        return 1
    sk = skeleton_from_turn(turn, path, repo_root=repo)
    if args.enrich:
        sk = enrich_skeleton(turn, sk)
    print(json.dumps(sk, indent=2))
    return 0


def cmd_import_all(args: argparse.Namespace) -> int:
    repo = args.repo_root or find_repo_root()
    manifest = load_manifest(repo)
    written: list[str] = []
    skipped: list[str] = []

    for path in discover_parent_transcripts(args.transcript_dir):
        tid = transcript_id_from_path(path)
        for turn in parse_transcript(path):
            key = f"{tid}:{turn.turn_index}"
            if key in manifest and not args.force:
                continue
            reason = should_skip_turn(turn)
            if reason and not args.force:
                skipped.append(f"{key} ({reason})")
                continue
            sk = skeleton_from_turn(turn, path, repo_root=repo)
            if args.enrich:
                sk = enrich_skeleton(turn, sk)
            if args.dry_run:
                written.append(f"[dry-run] {key}: {sk['task_summary'][:60]}")
                continue
            try:
                out = write_session(repo, sk, key)
                written.append(str(out))
            except ValueError as exc:
                print(f"Skip {key}: {exc}", file=sys.stderr)
                skipped.append(f"{key} (validation: {exc})")

    print(f"Written: {len(written)}")
    for w in written:
        print(w)
    if skipped:
        print(f"Skipped: {len(skipped)}", file=sys.stderr)
        for s in skipped[:20]:
            print(f"  {s}", file=sys.stderr)
        if len(skipped) > 20:
            print(f"  ... and {len(skipped) - 20} more", file=sys.stderr)
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Import agent transcripts to session logs")
    parser.add_argument("--repo-root", type=Path, default=None)
    parser.add_argument(
        "--transcript-dir",
        type=Path,
        default=None,
        help="Cursor agent-transcripts directory",
    )
    parser.add_argument("--force", action="store_true", help="Re-import even if manifest entry exists (import-all)")
    parser.add_argument("--dry-run", action="store_true")
    sub = parser.add_subparsers(dest="command")

    p_list = sub.add_parser("list", help="List all turns as JSON")
    p_list.add_argument("--skip-imported", action="store_true")
    p_list.set_defaults(func=cmd_list)

    p_sk = sub.add_parser("skeleton", help="Print skeleton JSON for one turn")
    p_sk.add_argument("transcript_id")
    p_sk.add_argument("--turn", type=int, required=True)
    p_sk.add_argument("--enrich", action="store_true")
    p_sk.set_defaults(func=cmd_skeleton)

    p_all = sub.add_parser("import-all", help="Import all unimported parent turns")
    p_all.add_argument("--enrich", action="store_true", default=True)
    p_all.add_argument("--no-enrich", action="store_false", dest="enrich")
    p_all.add_argument("--force", action="store_true", help="Re-import even if manifest entry exists")
    p_all.set_defaults(func=cmd_import_all)

    args = parser.parse_args()
    if args.transcript_dir is None:
        args.transcript_dir = default_transcript_dir()

    if args.command is None:
        # Legacy flags from plan: --list as top-level
        if "--list" in sys.argv:
            args.command = "list"
            args.skip_imported = "--skip-imported" in sys.argv
            return cmd_list(args)
        parser.print_help()
        return 1

    return args.func(args)


if __name__ == "__main__":
    raise SystemExit(main())
