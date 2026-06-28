#!/usr/bin/env python3
"""Configure local git hooks for project-twelve and the Assets/_Licensed submodule."""

from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
GITHOOKS = REPO_ROOT / ".githooks"
SUBMODULE_HOOKS = GITHOOKS / "submodule"
DEFAULT_SUBMODULE_PATH = "Assets/_Licensed"


def run_git(*args: str, cwd: Path | None = None) -> tuple[int, str]:
    result = subprocess.run(
        ["git", *args],
        cwd=cwd or REPO_ROOT,
        capture_output=True,
        text=True,
        check=False,
    )
    msg = (result.stderr or result.stdout).strip()
    return result.returncode, msg


def get_config(key: str, cwd: Path | None = None) -> str | None:
    code, out = run_git("config", "--get", key, cwd=cwd)
    return out if code == 0 and out else None


def set_config(key: str, value: str, cwd: Path | None = None) -> None:
    code, msg = run_git("config", key, value, cwd=cwd)
    if code != 0:
        print(f"error: git config {key} {value}: {msg}", file=sys.stderr)
        sys.exit(1)


def submodule_initialized(path: str) -> bool:
    sub = REPO_ROOT / path
    if not sub.is_dir():
        return False
    code, _ = run_git("-C", path, "rev-parse", "HEAD")
    return code == 0


def resolve_submodule_path() -> str:
    gitmodules = REPO_ROOT / ".gitmodules"
    if not gitmodules.is_file():
        return DEFAULT_SUBMODULE_PATH
    for line in gitmodules.read_text(encoding="utf-8").splitlines():
        stripped = line.strip()
        if stripped.startswith("path = "):
            return stripped.split("=", 1)[1].strip().replace("\\", "/")
    return DEFAULT_SUBMODULE_PATH


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--submodule-path",
        default=None,
        help="Submodule path (default: from .gitmodules).",
    )
    parser.add_argument(
        "--check",
        action="store_true",
        help="Verify hooksPath is configured; exit 1 if not.",
    )
    args = parser.parse_args()

    if not (REPO_ROOT / ".git").exists():
        print("error: not a git repository", file=sys.stderr)
        return 2

    if not GITHOOKS.is_dir():
        print(f"error: missing {GITHOOKS.relative_to(REPO_ROOT)}", file=sys.stderr)
        return 2

    submodule_path = args.submodule_path or resolve_submodule_path()
    expected_parent = ".githooks"
    expected_sub = "../../.githooks/submodule"

    parent_hooks = get_config("core.hooksPath")
    sub_hooks = get_config("core.hooksPath", cwd=REPO_ROOT / submodule_path) if submodule_initialized(submodule_path) else None

    if args.check:
        ok = parent_hooks == expected_parent
        if submodule_initialized(submodule_path):
            ok = ok and sub_hooks == expected_sub
        if ok:
            print("githooks: configured")
            return 0
        print("githooks: not configured — run: python scripts/install_githooks.py", file=sys.stderr)
        if parent_hooks:
            print(f"  parent core.hooksPath={parent_hooks!r} (expected {expected_parent!r})", file=sys.stderr)
        if submodule_initialized(submodule_path) and sub_hooks != expected_sub:
            print(f"  submodule core.hooksPath={sub_hooks!r} (expected {expected_sub!r})", file=sys.stderr)
        return 1

    if parent_hooks != expected_parent:
        set_config("core.hooksPath", expected_parent)
        print(f"set core.hooksPath={expected_parent} (main repo)")
    else:
        print(f"core.hooksPath already {expected_parent} (main repo)")

    if submodule_initialized(submodule_path):
        sub_root = REPO_ROOT / submodule_path
        if sub_hooks != expected_sub:
            set_config("core.hooksPath", expected_sub, cwd=sub_root)
            print(f"set core.hooksPath={expected_sub} ({submodule_path})")
        else:
            print(f"core.hooksPath already {expected_sub} ({submodule_path})")
    else:
        print(
            f"skipped submodule hooks ({submodule_path} not initialized).\n"
            f"  After: git submodule update --init --recursive\n"
            f"  Re-run: python scripts/install_githooks.py",
            file=sys.stderr,
        )

    print("\nHooks enabled:")
    print("  pre-commit     — paid assets + submodule verify")
    print("  pre-push       — paid assets + submodule verify")
    print("  post-merge     — submodule local sync after pull/merge")
    print("  post-checkout  — submodule local sync on branch switch")
    print("  submodule/pre-push — warn if parent gitlink != submodule HEAD")
    return 0


if __name__ == "__main__":
    sys.exit(main())
