#!/usr/bin/env python3
"""Generate Cursor and Codex assistant assets from the canonical Claude tree (.claude/).

Single source of truth: author rules/commands/skills/agents under `.claude/`, then run this script
to regenerate `.cursor/` and Codex-native `.agents/skills/`. Idempotent: re-running produces no
diff.

Mappings:
  .claude/commands/*.md        -> .cursor/commands/*.md       (verbatim)
  .claude/skills/<n>/**        -> .cursor/skills/<n>/**       (verbatim, whole dir)
  .claude/agents/*.md          -> .cursor/agents/*.md         (verbatim)
  .claude/rules/*.md           -> .cursor/rules/*.mdc         (extension change; content kept)
  .claude/skills/<n>/**        -> .agents/skills/<n>/**       (Codex repo skills, verbatim)

`.cursor/mcp.example.json`, `.codex/config.toml`, and other hand-authored files are left untouched.
"""

from __future__ import annotations

import argparse
import shutil
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CLAUDE = ROOT / ".claude"
CURSOR = ROOT / ".cursor"
CODEX_SKILLS = ROOT / ".agents" / "skills"

# (subdir, dest_subdir, mode) — mode: "tree" copies whole dir, "rules" converts .md->.mdc
SUBDIRS = [
    ("commands", "commands", "files"),
    ("skills", "skills", "tree"),
    ("agents", "agents", "files"),
    ("rules", "rules", "rules"),
]


def _reset_dir(path: Path) -> None:
    if path.exists():
        shutil.rmtree(path)
    path.mkdir(parents=True, exist_ok=True)


def sync(check: bool = False) -> int:
    changes: list[str] = []
    for src_name, dst_name, mode in SUBDIRS:
        src = CLAUDE / src_name
        dst = CURSOR / dst_name
        if not src.is_dir():
            continue
        if check:
            changes.extend(_diff(src, dst, mode))
            continue
        _reset_dir(dst)
        if mode == "tree":
            for child in sorted(src.iterdir()):
                if child.is_dir():
                    shutil.copytree(child, dst / child.name)
                else:
                    shutil.copy2(child, dst / child.name)
        elif mode == "rules":
            for md in sorted(src.glob("*.md")):
                (dst / f"{md.stem}.mdc").write_text(
                    md.read_text(encoding="utf-8"), encoding="utf-8"
                )
        else:  # files
            for md in sorted(src.glob("*.md")):
                shutil.copy2(md, dst / md.name)
        print(f"synced {src_name} -> .cursor/{dst_name}")

    claude_skills = CLAUDE / "skills"
    if claude_skills.is_dir():
        if check:
            changes.extend(_diff_tree(claude_skills, CODEX_SKILLS, ".agents/skills"))
        else:
            _reset_dir(CODEX_SKILLS)
            for child in sorted(claude_skills.iterdir()):
                if child.is_dir():
                    shutil.copytree(child, CODEX_SKILLS / child.name)
                else:
                    shutil.copy2(child, CODEX_SKILLS / child.name)
            print("synced skills -> .agents/skills")

    # Sync AGENTS.md to .agents/AGENTS.md for Gemini/Antigravity
    agents_md = ROOT / "AGENTS.md"
    target_agents_md = ROOT / ".agents" / "AGENTS.md"
    if agents_md.is_file():
        if check:
            if not target_agents_md.exists():
                changes.append("missing in .agents: AGENTS.md")
            elif target_agents_md.read_text(encoding="utf-8") != agents_md.read_text(encoding="utf-8"):
                changes.append("differs: .agents/AGENTS.md")
        else:
            target_agents_md.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(agents_md, target_agents_md)
            print("synced AGENTS.md -> .agents/AGENTS.md")

    if check:
        if changes:
            print("OUT OF SYNC:")
            for c in changes:
                print(f"  {c}")
            return 1
        print("in sync")
    return 0


def _expected_name(p: Path, mode: str) -> str:
    return f"{p.stem}.mdc" if mode == "rules" else p.name


def _diff(src: Path, dst: Path, mode: str) -> list[str]:
    out: list[str] = []
    if mode == "tree":
        return _diff_tree(src, dst, f".cursor/{dst.name}")
    pat = "*.md"
    expected_names = {_expected_name(f, mode) for f in src.glob(pat)}
    actual_names = (
        {f.name for f in dst.iterdir() if f.is_file()} if dst.is_dir() else set()
    )
    for name in sorted(actual_names - expected_names):
        out.append(f"stale in .cursor: {dst.name}/{name}")
    for f in sorted(src.glob(pat)):
        target = dst / _expected_name(f, mode)
        if not target.exists():
            out.append(f"missing in .cursor: {dst.name}/{target.name}")
        elif target.read_text(encoding="utf-8") != f.read_text(encoding="utf-8"):
            out.append(f"differs: {dst.name}/{target.name}")
    return out


def _diff_tree(src: Path, dst: Path, label: str) -> list[str]:
    out: list[str] = []
    src_files = {p.relative_to(src) for p in src.rglob("*") if p.is_file()}
    dst_files = (
        {p.relative_to(dst) for p in dst.rglob("*") if p.is_file()}
        if dst.is_dir()
        else set()
    )
    for rel in sorted(src_files - dst_files):
        out.append(f"missing in {label}: {rel.as_posix()}")
    for rel in sorted(dst_files - src_files):
        out.append(f"stale in {label}: {rel.as_posix()}")
    for rel in sorted(src_files & dst_files):
        if (src / rel).read_bytes() != (dst / rel).read_bytes():
            out.append(f"differs: {label}/{rel.as_posix()}")
    return out


def main() -> int:
    ap = argparse.ArgumentParser(description="Sync .cursor/ and .agents/skills/ from .claude/")
    ap.add_argument("--check", action="store_true", help="Report drift without writing (CI gate)")
    args = ap.parse_args()
    return sync(check=args.check)


if __name__ == "__main__":
    raise SystemExit(main())
