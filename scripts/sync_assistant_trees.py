#!/usr/bin/env python3
"""Generate Cursor and Codex assistant assets from the canonical Claude tree (.claude/).

Single source of truth: author rules/commands/skills/agents under `.claude/`, then run this script
to regenerate `.cursor/`, Codex-native `.agents/skills/`, and `.codex/agents/*.toml`. Idempotent.

Mappings:
  .claude/commands/*.md        -> .cursor/commands/*.md       (verbatim)
  .claude/skills/<n>/**        -> .cursor/skills/<n>/**       (verbatim, whole dir)
  .claude/agents/*.md          -> .cursor/agents/*.md         (verbatim)
  .claude/rules/*.md           -> .cursor/rules/*.mdc         (extension change; content kept)
  .claude/skills/<n>/**        -> .agents/skills/<n>/**       (Codex repo skills, verbatim)
  .claude/agents/*.md          -> .codex/agents/<n>.toml      (Codex custom agents)
  AGENTS.md                    -> .agents/AGENTS.md           (Antigravity/Gemini)

`.cursor/mcp.example.json`, `.codex/config.toml`, and other hand-authored files are left untouched.
"""

from __future__ import annotations

import argparse
import json
import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CLAUDE = ROOT / ".claude"
CURSOR = ROOT / ".cursor"
CODEX_SKILLS = ROOT / ".agents" / "skills"
CODEX_AGENTS = ROOT / ".codex" / "agents"

# (subdir, dest_subdir, mode) — mode: "tree" copies whole dir, "rules" converts .md->.mdc
SUBDIRS = [
    ("commands", "commands", "files"),
    ("skills", "skills", "tree"),
    ("agents", "agents", "files"),
    ("rules", "rules", "rules"),
]

_FRONTMATTER_RE = re.compile(r"\A---\s*\n(.*?)\n---\s*\n?(.*)\Z", re.DOTALL)
_KEY_RE = re.compile(r"^([A-Za-z][A-Za-z0-9_-]*):\s*(.*)$")


def _reset_dir(path: Path) -> None:
    root = ROOT.resolve()
    resolved = path.resolve()
    if resolved == root or root not in resolved.parents:
        raise ValueError(f"refusing to reset path outside the repository: {resolved}")
    if path.exists():
        # Windows can raise FileNotFoundError mid-delete when antivirus/indexers touch files.
        for _ in range(3):
            try:
                shutil.rmtree(path)
                break
            except FileNotFoundError:
                if not path.exists():
                    break
        else:
            shutil.rmtree(path, ignore_errors=True)
            if path.exists():
                raise
    path.mkdir(parents=True, exist_ok=True)


def _ignore_python_cache(dirpath: str, names: list[str]) -> set[str]:
    """Skip bytecode caches when mirroring skill trees."""
    ignored = {n for n in names if n == "__pycache__" or n.endswith(".pyc")}
    return ignored


def _copytree_skill(src: Path, dst: Path) -> None:
    shutil.copytree(src, dst, ignore=_ignore_python_cache)


def _normalise(text: str) -> str:
    return text.replace("\r\n", "\n").replace("\r", "\n")


def _parse_agent(md: Path) -> tuple[str, str, str]:
    text = _normalise(md.read_text(encoding="utf-8"))
    match = _FRONTMATTER_RE.match(text)
    if not match:
        raise ValueError(f"{md}: missing or unterminated YAML frontmatter")
    metadata: dict[str, str] = {}
    for line in match.group(1).splitlines():
        key_match = _KEY_RE.match(line)
        if key_match:
            metadata[key_match.group(1)] = key_match.group(2).strip().strip("\"'")
    name = metadata.get("name", "")
    description = metadata.get("description", "")
    if not name or not description:
        raise ValueError(f"{md}: agent frontmatter requires name and description")
    if name != md.stem:
        raise ValueError(f"{md}: agent name {name!r} must match file stem {md.stem!r}")
    body = match.group(2).strip()
    if '"""' in body:
        raise ValueError(f'{md}: agent body cannot contain TOML delimiter """')
    return name, description, body


def _render_codex_agent(md: Path) -> str:
    name, description, body = _parse_agent(md)
    return (
        f"name = {json.dumps(name, ensure_ascii=False)}\n"
        f"description = {json.dumps(description, ensure_ascii=False)}\n"
        'developer_instructions = """\n'
        f"{body}\n"
        '"""\n'
    )


def _sync_codex_agents() -> None:
    agents_src = CLAUDE / "agents"
    _reset_dir(CODEX_AGENTS)
    if not agents_src.is_dir():
        return
    for md in sorted(agents_src.glob("*.md")):
        (CODEX_AGENTS / f"{md.stem}.toml").write_text(
            _render_codex_agent(md), encoding="utf-8", newline="\n"
        )


def _diff_codex_agents() -> list[str]:
    agents_src = CLAUDE / "agents"
    if not agents_src.is_dir():
        return []
    expected = {
        f"{md.stem}.toml": _render_codex_agent(md) for md in agents_src.glob("*.md")
    }
    actual = (
        {path.name: path for path in CODEX_AGENTS.glob("*.toml")}
        if CODEX_AGENTS.is_dir()
        else {}
    )
    out: list[str] = []
    for name in sorted(expected.keys() - actual.keys()):
        out.append(f"missing in .codex/agents: {name}")
    for name in sorted(actual.keys() - expected.keys()):
        out.append(f"stale in .codex/agents: {name}")
    for name in sorted(expected.keys() & actual.keys()):
        if _normalise(actual[name].read_text(encoding="utf-8")) != expected[name]:
            out.append(f"differs: .codex/agents/{name}")
    return out


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
                if child.name == "__pycache__":
                    continue
                if child.is_dir():
                    _copytree_skill(child, dst / child.name)
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
                if child.name == "__pycache__":
                    continue
                if child.is_dir():
                    _copytree_skill(child, CODEX_SKILLS / child.name)
                else:
                    shutil.copy2(child, CODEX_SKILLS / child.name)
            print("synced skills -> .agents/skills")

    if check:
        changes.extend(_diff_codex_agents())
    else:
        _sync_codex_agents()
        print("synced agents -> .codex/agents")

    # Sync AGENTS.md to .agents/AGENTS.md for Gemini/Antigravity
    agents_md = ROOT / "AGENTS.md"
    target_agents_md = ROOT / ".agents" / "AGENTS.md"
    if agents_md.is_file():
        if check:
            if not target_agents_md.exists():
                changes.append("missing in .agents: AGENTS.md")
            elif target_agents_md.read_text(encoding="utf-8") != agents_md.read_text(
                encoding="utf-8"
            ):
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
        print("in sync: .cursor, .agents/skills, .codex/agents, .agents/AGENTS.md")
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

    def _files(root: Path) -> set[Path]:
        if not root.is_dir():
            return set()
        return {
            p.relative_to(root)
            for p in root.rglob("*")
            if p.is_file()
            and "__pycache__" not in p.parts
            and not p.name.endswith(".pyc")
        }

    src_files = _files(src)
    dst_files = _files(dst)
    for rel in sorted(src_files - dst_files):
        out.append(f"missing in {label}: {rel.as_posix()}")
    for rel in sorted(dst_files - src_files):
        out.append(f"stale in {label}: {rel.as_posix()}")
    for rel in sorted(src_files & dst_files):
        if (src / rel).read_bytes() != (dst / rel).read_bytes():
            out.append(f"differs: {label}/{rel.as_posix()}")
    return out


def main() -> int:
    ap = argparse.ArgumentParser(
        description="Sync .cursor/, .agents/skills/, and .codex/agents/ from .claude/"
    )
    ap.add_argument("--check", action="store_true", help="Report drift without writing (CI gate)")
    args = ap.parse_args()
    return sync(check=args.check)


if __name__ == "__main__":
    raise SystemExit(main())
