#!/usr/bin/env python3
"""Fetch and sync project-twelve with the Assets/_Licensed submodule."""

from __future__ import annotations

import argparse
import configparser
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_SUBMODULE_PATH = "Assets/_Licensed"


class GitError(Exception):
    """Raised when a git command fails."""


def run_git(
    *args: str,
    cwd: Path | None = None,
    check: bool = True,
    capture: bool = True,
) -> subprocess.CompletedProcess[str]:
    result = subprocess.run(
        ["git", *args],
        cwd=cwd or REPO_ROOT,
        capture_output=capture,
        text=True,
        check=False,
    )
    if check and result.returncode != 0:
        msg = (result.stderr or result.stdout or f"git {' '.join(args)}").strip()
        raise GitError(msg)
    return result


def git_lines(*args: str, cwd: Path | None = None, check: bool = True) -> list[str]:
    result = run_git(*args, cwd=cwd, check=check)
    return [line.strip() for line in result.stdout.splitlines() if line.strip()]


def resolve_submodule_path(override: str | None) -> str:
    if override:
        return override.replace("\\", "/").rstrip("/")
    gitmodules = REPO_ROOT / ".gitmodules"
    if not gitmodules.is_file():
        raise GitError("missing .gitmodules — not a submodule-aware repo")
    parser = configparser.ConfigParser()
    parser.read(gitmodules, encoding="utf-8")
    for section in parser.sections():
        if parser.has_option(section, "path"):
            return parser.get(section, "path").replace("\\", "/").rstrip("/")
    return DEFAULT_SUBMODULE_PATH


def submodule_dir(submodule_path: str) -> Path:
    return REPO_ROOT / submodule_path


def is_submodule_initialized(submodule_path: str) -> bool:
    path = submodule_dir(submodule_path)
    if not path.is_dir():
        return False
    try:
        git_lines("-C", submodule_path, "rev-parse", "HEAD")
        return True
    except GitError:
        return False


def working_tree_dirty() -> bool:
    result = run_git("status", "--porcelain", check=False)
    return bool(result.stdout.strip())


def local_sync(submodule_path: str) -> None:
    run_git("submodule", "sync", "--recursive")
    run_git("submodule", "update", "--init", "--recursive")
    print(f"local sync: submodules aligned with recorded gitlinks ({submodule_path})")


def fetch_remotes(submodule_path: str) -> None:
    print("fetching origin (main repo)...")
    run_git("fetch", "origin")
    run_git("submodule", "sync", "--recursive")
    run_git("submodule", "update", "--init", "--recursive")
    if submodule_dir(submodule_path).is_dir():
        print(f"fetching origin ({submodule_path})...")
        run_git("-C", submodule_path, "fetch", "origin")
    else:
        print(f"warning: {submodule_path} not present — submodule fetch skipped", file=sys.stderr)


def detect_upstream_ref() -> str | None:
    try:
        lines = git_lines("rev-parse", "--abbrev-ref", "--symbolic-full-name", "@{u}")
        return lines[0] if lines else None
    except GitError:
        return None


def apply_ff_only() -> None:
    if working_tree_dirty():
        raise GitError(
            "working tree has uncommitted changes — commit, stash, or discard before apply"
        )

    upstream = detect_upstream_ref()
    branch_lines = git_lines("branch", "--show-current")
    branch = branch_lines[0] if branch_lines else ""

    if upstream:
        print(f"applying fast-forward on {branch} from {upstream}...")
        run_git("pull", "--ff-only")
    else:
        fallback = f"origin/{branch}" if branch else "origin/master"
        print(f"no upstream configured — trying ff-only pull from {fallback}...")
        result = run_git("pull", "--ff-only", fallback, check=False)
        if result.returncode != 0:
            raise GitError(
                f"fast-forward pull failed ({fallback}). "
                "Set upstream: git branch --set-upstream-to=origin/<branch>"
            )

    run_git("submodule", "update", "--init", "--recursive")
    print("apply: main repo and submodules updated")


def parse_submodule_status(submodule_path: str) -> tuple[str, str, str] | None:
    """Return (prefix, sha, path) for the target submodule from git submodule status."""
    target = submodule_path.replace("\\", "/")
    for line in git_lines("submodule", "status", "--recursive"):
        if not line.strip():
            continue
        prefix = " "
        if line[0] in "-+U":
            prefix = line[0]
            body = line[1:].strip()
        else:
            body = line.strip()
        if " (" in body:
            body = body[: body.rindex(" (")]
        parts = body.split()
        if len(parts) < 2:
            continue
        sha, path = parts[0], parts[1]
        if path.replace("\\", "/") == target:
            return prefix, sha, path
    return None


def submodule_worktree_dirty(submodule_path: str) -> bool:
    if not is_submodule_initialized(submodule_path):
        return False
    wt = run_git("-C", submodule_path, "status", "--porcelain", check=False)
    return bool(wt.stdout.strip())


def verify_submodules(submodule_path: str) -> int:
    """Verify submodule alignment. Returns exit code (0 ok, 1 fail)."""
    status = parse_submodule_status(submodule_path)
    exit_code = 0

    if status is None:
        print(
            f"error: {submodule_path} not listed in git submodule status",
            file=sys.stderr,
        )
        return 1

    prefix, sha, path = status
    if prefix == "-":
        print(
            f"error: {path} is not initialized — run:\n"
            f"  git submodule update --init --recursive\n"
            f"See docs/PAID_ASSETS.md for access to project-twelve-assets.",
            file=sys.stderr,
        )
        return 1
    if prefix == "+":
        print(
            f"error: {path} checkout ({sha[:12]}...) differs from index gitlink — run:\n"
            f"  python scripts/fetch_remotes.py --local-sync",
            file=sys.stderr,
        )
        return 1
    if prefix.upper() == "U":
        print(f"error: {path} has unmerged submodule changes", file=sys.stderr)
        return 1

    if submodule_worktree_dirty(submodule_path):
        print(
            f"warning: {path} has uncommitted changes in the submodule working tree",
            file=sys.stderr,
        )

    print(f"verify: {path} aligned at {sha[:12]}")
    return exit_code


def warn_parent_gitlink_mismatch(submodule_path: str) -> int:
    """Warn when pushing submodule if parent gitlink != submodule HEAD. Always exit 0."""
    if not is_submodule_initialized(submodule_path):
        print(f"warning: {submodule_path} not initialized", file=sys.stderr)
        return 0

    head_lines = git_lines("-C", submodule_path, "rev-parse", "HEAD")
    head = head_lines[0]

    try:
        tree_lines = git_lines("ls-tree", "HEAD", submodule_path)
    except GitError:
        print(
            f"warning: could not read parent gitlink for {submodule_path}",
            file=sys.stderr,
        )
        return 0

    if not tree_lines:
        print(
            f"warning: {submodule_path} not recorded in parent HEAD — "
            "bump gitlink in project-twelve after pushing assets.",
            file=sys.stderr,
        )
        return 0

    parts = tree_lines[0].split()
    if len(parts) < 3:
        return 0
    recorded = parts[2]

    if recorded != head:
        print(
            f"warning: parent repo pins {recorded[:12]} but submodule HEAD is {head[:12]}\n"
            f"After pushing to project-twelve-assets, bump the gitlink in project-twelve:\n"
            f"  cd {REPO_ROOT}\n"
            f"  git add {submodule_path}\n"
            f"  git commit -m \"chore(assets): bump {submodule_path} submodule pointer\"",
            file=sys.stderr,
        )
    else:
        print(f"submodule pre-push: parent gitlink matches HEAD ({head[:12]})")
    return 0


def print_status(submodule_path: str) -> None:
    branch_lines = git_lines("branch", "--show-current")
    branch = branch_lines[0] if branch_lines else "(detached)"
    upstream = detect_upstream_ref()

    print(f"\nmain repo: branch={branch}", end="")
    if upstream:
        try:
            counts = git_lines("rev-list", "--left-right", "--count", f"HEAD...{upstream}")
            if counts:
                behind, ahead = counts[0].split()
                print(f", upstream={upstream}, behind={behind}, ahead={ahead}")
            else:
                print(f", upstream={upstream}")
        except GitError:
            print(f", upstream={upstream} (could not compare)")
    else:
        print(", upstream=(none)")

    status = parse_submodule_status(submodule_path)
    if status is None:
        print(f"submodule {submodule_path}: not tracked")
        return

    prefix, sha, path = status
    state = {
        " ": "aligned",
        "+": "out of sync with index",
        "-": "not initialized",
        "U": "unmerged",
    }.get(prefix, prefix)
    print(f"submodule {path}: {state} at {sha[:12]}")

    if is_submodule_initialized(submodule_path):
        sub_branch = git_lines("-C", submodule_path, "branch", "--show-current")
        sub_branch_name = sub_branch[0] if sub_branch else "(detached)"
        print(f"  checked out branch: {sub_branch_name}")
        try:
            remote_sha = git_lines(
                "-C",
                submodule_path,
                "rev-parse",
                "origin/HEAD",
            )
            if remote_sha:
                print(f"  origin/HEAD: {remote_sha[0][:12]}")
        except GitError:
            pass


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument(
        "--fetch-only",
        action="store_true",
        help="Fetch remotes and sync submodules to gitlink; do not pull main branch.",
    )
    mode.add_argument(
        "--local-sync",
        action="store_true",
        help="Local submodule sync only (hook mode; no network fetch or pull).",
    )
    mode.add_argument(
        "--verify",
        action="store_true",
        help="Verify submodule checkout matches index gitlink (hook mode).",
    )
    mode.add_argument(
        "--warn-parent-gitlink",
        action="store_true",
        help="Warn if parent gitlink != submodule HEAD (submodule pre-push hook).",
    )
    parser.add_argument(
        "--submodule-path",
        default=None,
        help=f"Submodule path override (default: from .gitmodules, usually {DEFAULT_SUBMODULE_PATH}).",
    )
    args = parser.parse_args()

    if not (REPO_ROOT / ".git").exists():
        print("error: not a git repository", file=sys.stderr)
        return 2

    try:
        submodule_path = resolve_submodule_path(args.submodule_path)
    except GitError as exc:
        print(f"error: {exc}", file=sys.stderr)
        return 2

    try:
        if args.local_sync:
            local_sync(submodule_path)
            return 0

        if args.verify:
            return verify_submodules(submodule_path)

        if args.warn_parent_gitlink:
            return warn_parent_gitlink_mismatch(submodule_path)

        fetch_remotes(submodule_path)

        if not args.fetch_only:
            apply_ff_only()

        print_status(submodule_path)
        return 0
    except GitError as exc:
        print(f"error: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
