#!/usr/bin/env python3
"""Lightweight Markdown link checker for repository-relative links."""
from __future__ import annotations

import re
import sys
from pathlib import Path
from urllib.parse import unquote, urlparse

ROOT = Path(__file__).resolve().parents[1]
LINK_PATTERN = re.compile(r"(?<!!)\[[^\]]+\]\(([^)]+)\)")
SKIP_PREFIXES = ("http://", "https://", "mailto:", "tel:", "#")


def markdown_files() -> list[Path]:
    return sorted(
        path
        for path in ROOT.rglob("*.md")
        if ".git" not in path.parts and "Library" not in path.parts
    )


def normalize_target(raw: str) -> str:
    target = raw.strip().split()[0].strip('"\'')
    parsed = urlparse(target)
    if parsed.scheme or target.startswith(SKIP_PREFIXES):
        return ""
    return unquote(parsed.path)


def main() -> int:
    failures: list[str] = []
    for md_file in markdown_files():
        text = md_file.read_text(encoding="utf-8")
        for match in LINK_PATTERN.finditer(text):
            raw = match.group(1) or ""
            target = normalize_target(raw)
            if not target:
                continue
            resolved = (md_file.parent / target).resolve()
            try:
                resolved.relative_to(ROOT)
            except ValueError:
                failures.append(f"{md_file.relative_to(ROOT)}: link escapes repo: {raw}")
                continue
            if not resolved.exists():
                failures.append(f"{md_file.relative_to(ROOT)}: missing link target: {raw}")

    if failures:
        print("Markdown link check failed:")
        for failure in failures:
            print(f"- {failure}")
        return 1

    print(f"Checked {len(markdown_files())} Markdown files; all local links resolved.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
