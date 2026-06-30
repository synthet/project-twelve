#!/usr/bin/env python3
"""Fail if paid/local-only asset paths appear in git staged or push ranges."""

from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
MANIFEST = REPO_ROOT / "config" / "paid-assets.local-only.txt"
MANIFEST_EXAMPLE = REPO_ROOT / "config" / "paid-assets.local-only.example.txt"


def load_manifest() -> list[str]:
    manifest_path = MANIFEST if MANIFEST.is_file() else MANIFEST_EXAMPLE
    if not manifest_path.is_file():
        print(
            f"error: no manifest found. Copy {MANIFEST_EXAMPLE.name} to {MANIFEST.name}",
            file=sys.stderr,
        )
        sys.exit(2)

    paths: list[str] = []
    for line in manifest_path.read_text(encoding="utf-8").splitlines():
        stripped = line.strip()
        if not stripped or stripped.startswith("#"):
            continue
        normalized = stripped.replace("\\", "/").rstrip("/")
        paths.append(normalized)
    return paths


def run_git(*args: str) -> list[str]:
    result = subprocess.run(
        ["git", *args],
        cwd=REPO_ROOT,
        capture_output=True,
        text=True,
        check=False,
    )
    if result.returncode != 0:
        print(result.stderr.strip() or f"git {' '.join(args)} failed", file=sys.stderr)
        sys.exit(result.returncode)
    return [line.strip() for line in result.stdout.splitlines() if line.strip()]


def run_git_optional(*args: str) -> tuple[int, str]:
    """Run git without exiting on failure; return (returncode, stdout-stripped)."""
    result = subprocess.run(
        ["git", *args],
        cwd=REPO_ROOT,
        capture_output=True,
        text=True,
        check=False,
    )
    return result.returncode, result.stdout.strip()


# The empty tree's well-known SHA-1; diffing against it yields every tracked
# path reachable from HEAD, so the guard never silently misses a commit.
EMPTY_TREE_SHA = "4b825dc642cb6eb9a060e54bf8d69288fbee4904"


def resolve_push_base() -> str:
    """Pick a base ref to diff the push range against.

    Prefer the configured upstream; fall back to common remote default
    branches. If none resolve, diff against the empty tree so the whole
    history is scanned — a paid-asset guard must fail safe toward over-
    scanning, never toward skipping commits.
    """
    rc, upstream = run_git_optional(
        "rev-parse", "--abbrev-ref", "--symbolic-full-name", "@{u}"
    )
    if rc == 0 and upstream:
        return upstream
    for candidate in ("origin/master", "origin/main", "origin/HEAD"):
        rc, _ = run_git_optional("rev-parse", "--verify", f"{candidate}^{{commit}}")
        if rc == 0:
            return candidate
    return EMPTY_TREE_SHA


def path_is_blocked(file_path: str, blocked_roots: list[str]) -> str | None:
    normalized = file_path.replace("\\", "/")
    for root in blocked_roots:
        root_no_slash = root.rstrip("/")
        if normalized == root_no_slash or normalized == f"{root_no_slash}.meta":
            return root
        prefix = root_no_slash if root_no_slash.endswith("/") else f"{root_no_slash}/"
        if normalized.startswith(prefix):
            return root
    return None


def collect_violations(files: list[str], blocked_roots: list[str]) -> list[tuple[str, str]]:
    violations: list[tuple[str, str]] = []
    seen: set[str] = set()
    for file_path in files:
        normalized = file_path.replace("\\", "/")
        if normalized == "Assets/_Licensed":
            continue
        if normalized.startswith("Assets/_Licensed/"):
            if file_path not in seen:
                seen.add(file_path)
                violations.append((file_path, "Assets/_Licensed/ (submodule content belongs in project-twelve-assets)"))
            continue
        blocked = path_is_blocked(file_path, blocked_roots)
        if blocked and file_path not in seen:
            seen.add(file_path)
            violations.append((file_path, blocked))
    return sorted(violations, key=lambda item: item[0].lower())


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    group = parser.add_mutually_exclusive_group()
    group.add_argument(
        "--staged",
        action="store_true",
        help="Check index (staged) paths only — use before commit.",
    )
    group.add_argument(
        "--push",
        action="store_true",
        help="Check commits ahead of upstream — use before push.",
    )
    args = parser.parse_args()

    blocked_roots = load_manifest()
    if not blocked_roots:
        print("warning: paid-assets manifest is empty", file=sys.stderr)
        return 0

    if args.push:
        base = resolve_push_base()
        if base == EMPTY_TREE_SHA:
            # No merge-base exists with the empty tree, so use a two-dot diff
            # to scan every commit reachable from HEAD.
            files = run_git("diff", "--name-only", base, "HEAD")
        else:
            files = run_git("diff", "--name-only", f"{base}...HEAD")
    elif args.staged:
        files = run_git("diff", "--cached", "--name-only")
    else:
        files = run_git("diff", "--name-only", "HEAD")
        files.extend(run_git("diff", "--cached", "--name-only"))
        files = list(dict.fromkeys(files))

    violations = collect_violations(files, blocked_roots)
    if not violations:
        return 0

    print("Paid/local-only asset paths detected — do not commit or push:", file=sys.stderr)
    for file_path, root in violations:
        print(f"  {file_path}  (blocked by {root})", file=sys.stderr)
    print(
        f"\nRemove from index: git restore --staged -- <path>\n"
        f"Manifest: {MANIFEST.relative_to(REPO_ROOT).as_posix()}",
        file=sys.stderr,
    )
    return 1


if __name__ == "__main__":
    sys.exit(main())
