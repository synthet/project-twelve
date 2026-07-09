#!/usr/bin/env python3
"""Commit, push, and publish the Assets/_Licensed submodule pointer in project-twelve.

Typical flow (assets-first):
  1. Edit licensed content under Assets/_Licensed/ (project-twelve-assets checkout).
  2. Commit and push in the submodule.
  3. Record the new gitlink in the parent repo (this script).

Examples:
  python scripts/publish_assets_submodule.py --status
  python scripts/publish_assets_submodule.py -m "docs: refine licensed asset inventory"
  python scripts/publish_assets_submodule.py --submodule-checkout main --pull-submodule
"""

from __future__ import annotations

import argparse
import configparser
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_SUBMODULE_PATH = "Assets/_Licensed"
DEFAULT_BRANCH = "main"


class GitError(Exception):
    """Raised when a git command fails."""


def run_git(
    *args: str,
    cwd: Path | None = None,
    check: bool = True,
) -> subprocess.CompletedProcess[str]:
    result = subprocess.run(
        ["git", *args],
        cwd=cwd or REPO_ROOT,
        capture_output=True,
        text=True,
        check=False,
    )
    if check and result.returncode != 0:
        msg = (result.stderr or result.stdout or f"git {' '.join(args)}").strip()
        raise GitError(msg)
    return result


def git_lines(*args: str, cwd: Path | None = None, check: bool = True) -> list[str]:
    return [
        line.strip()
        for line in run_git(*args, cwd=cwd, check=check).stdout.splitlines()
        if line.strip()
    ]


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


def submodule_root(submodule_path: str) -> Path:
    return REPO_ROOT / submodule_path


def is_submodule_initialized(submodule_path: str) -> bool:
    path = submodule_root(submodule_path)
    if not path.is_dir():
        return False
    try:
        git_lines("-C", submodule_path, "rev-parse", "HEAD")
        return True
    except GitError:
        return False


def porcelain_dirty(cwd: Path) -> bool:
    return bool(run_git("status", "--porcelain", cwd=cwd, check=False).stdout.strip())


def parent_gitlink(submodule_path: str) -> str | None:
    lines = git_lines("ls-tree", "HEAD", submodule_path, check=False)
    if not lines:
        return None
    parts = lines[0].split()
    if len(parts) < 3 or parts[0] != "160000":
        return None
    return parts[2]


def submodule_head(submodule_path: str) -> str:
    return git_lines("-C", submodule_path, "rev-parse", "HEAD")[0]


def print_status(submodule_path: str) -> None:
    if not is_submodule_initialized(submodule_path):
        print(f"submodule {submodule_path}: not initialized")
        return

    head = submodule_head(submodule_path)
    gitlink = parent_gitlink(submodule_path)
    branch = git_lines("-C", submodule_path, "branch", "--show-current")
    branch_name = branch[0] if branch else "(detached)"

    sub_dirty = porcelain_dirty(submodule_root(submodule_path))
    parent_dirty = porcelain_dirty(REPO_ROOT)

    print(f"submodule path: {submodule_path}")
    print(f"  branch: {branch_name}")
    print(f"  HEAD:   {head[:12]}")
    print(f"  dirty:  {sub_dirty}")

    print(f"parent gitlink: {gitlink[:12] if gitlink else '(none)'}")
    if gitlink and gitlink != head:
        print("  status: OUT OF SYNC — run publish to bump gitlink")
    elif gitlink:
        print("  status: aligned with submodule HEAD")
    else:
        print("  status: no gitlink recorded at parent HEAD")

    print(f"parent working tree dirty: {parent_dirty}")


def checkout_submodule_branch(submodule_path: str, branch: str, dry_run: bool) -> None:
    action = f"git -C {submodule_path} checkout {branch}"
    if dry_run:
        print(f"dry-run: {action}")
        return
    run_git("-C", submodule_path, "checkout", branch)


def pull_submodule_ff(submodule_path: str, dry_run: bool) -> None:
    action = f"git -C {submodule_path} pull --ff-only origin"
    if dry_run:
        print(f"dry-run: {action}")
        return
    branch = git_lines("-C", submodule_path, "branch", "--show-current")
    if not branch:
        raise GitError("submodule is detached — pass --submodule-checkout <branch> first")
    run_git("-C", submodule_path, "pull", "--ff-only", "origin", branch[0])


def commit_submodule_if_dirty(
    submodule_path: str,
    message: str,
    paths: list[str] | None,
    dry_run: bool,
) -> bool:
    root = submodule_root(submodule_path)
    if not porcelain_dirty(root):
        print("submodule: working tree clean — skip commit")
        return False

    if not message:
        raise GitError(
            "submodule has uncommitted changes; pass -m/--message with a commit subject"
        )

    if paths:
        for rel in paths:
            if dry_run:
                print(f"dry-run: git -C {submodule_path} add {rel}")
            else:
                run_git("-C", submodule_path, "add", rel)
    else:
        if dry_run:
            print(f"dry-run: git -C {submodule_path} add -A")
        else:
            run_git("-C", submodule_path, "add", "-A")

    if dry_run:
        print(f"dry-run: git -C {submodule_path} commit -m {message!r}")
        return True

    run_git("-C", submodule_path, "commit", "-m", message)
    print(f"submodule: committed ({submodule_head(submodule_path)[:12]})")
    return True


def push_submodule(submodule_path: str, dry_run: bool) -> None:
    branch = git_lines("-C", submodule_path, "branch", "--show-current")
    if not branch:
        raise GitError("cannot push — submodule HEAD is detached")
    remote_branch = branch[0]
    action = f"git -C {submodule_path} push origin {remote_branch}"
    if dry_run:
        print(f"dry-run: {action}")
        return
    run_git("-C", submodule_path, "push", "origin", remote_branch)
    print(f"submodule: pushed origin/{remote_branch}")


def bump_parent_gitlink(
    submodule_path: str,
    message: str,
    dry_run: bool,
) -> bool:
    head = submodule_head(submodule_path)
    gitlink = parent_gitlink(submodule_path)
    if gitlink == head:
        print(f"parent: gitlink already at {head[:12]} — skip bump")
        return False

    if dry_run:
        print(f"dry-run: git add {submodule_path}")
        print(f"dry-run: git commit -m {message!r}")
        return True

    run_git("add", submodule_path)
    staged = run_git("diff", "--cached", "--name-only", check=False).stdout.strip()
    if submodule_path not in staged.splitlines():
        raise GitError(f"git add {submodule_path} did not stage a gitlink change")

    run_git("commit", "-m", message)
    new_link = parent_gitlink(submodule_path)
    print(f"parent: recorded gitlink {new_link[:12] if new_link else head[:12]}")
    return True


def run_paid_assets_staged_check() -> None:
    script = REPO_ROOT / "scripts" / "check_paid_assets.py"
    if not script.is_file():
        print("warning: check_paid_assets.py missing — skip guard", file=sys.stderr)
        return
    result = subprocess.run(
        [sys.executable, str(script), "--staged"],
        cwd=REPO_ROOT,
        check=False,
    )
    if result.returncode != 0:
        raise GitError("check_paid_assets.py --staged failed — fix staged set before commit")


def push_parent(dry_run: bool) -> None:
    branch = git_lines("branch", "--show-current")
    if not branch:
        raise GitError("parent repo is detached — cannot push")
    action = f"git push origin {branch[0]}"
    if dry_run:
        print(f"dry-run: {action}")
        return
    run_git("push", "origin", branch[0])
    print(f"parent: pushed origin/{branch[0]}")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--status",
        action="store_true",
        help="Print submodule HEAD vs parent gitlink and exit.",
    )
    parser.add_argument(
        "-m",
        "--message",
        default="",
        help="Commit message for submodule commit (if dirty) and/or parent gitlink bump.",
    )
    parser.add_argument(
        "--submodule-path",
        default=None,
        help=f"Submodule path override (default: {DEFAULT_SUBMODULE_PATH}).",
    )
    parser.add_argument(
        "--submodule-checkout",
        default=None,
        metavar="BRANCH",
        help=f"Checkout this branch in the submodule before commit/pull (e.g. {DEFAULT_BRANCH}).",
    )
    parser.add_argument(
        "--pull-submodule",
        action="store_true",
        help="After checkout, git pull --ff-only in the submodule.",
    )
    parser.add_argument(
        "--submodule-paths",
        nargs="*",
        default=None,
        help="Limit submodule git add to these paths (default: all changes).",
    )
    parser.add_argument(
        "--skip-submodule-push",
        action="store_true",
        help="Commit in submodule but do not push to origin.",
    )
    parser.add_argument(
        "--skip-parent-commit",
        action="store_true",
        help="Push submodule only; do not record gitlink in parent.",
    )
    parser.add_argument(
        "--parent-message",
        default="",
        help="Parent gitlink commit message (default: chore(assets): bump <path> submodule pointer).",
    )
    parser.add_argument(
        "--push-parent",
        action="store_true",
        help="Push parent branch to origin after gitlink commit.",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Print actions without mutating repos.",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()

    if not (REPO_ROOT / ".git").is_dir():
        print("error: not a git repository", file=sys.stderr)
        return 2

    try:
        submodule_path = resolve_submodule_path(args.submodule_path)
    except GitError as exc:
        print(f"error: {exc}", file=sys.stderr)
        return 2

    if args.status:
        print_status(submodule_path)
        return 0

    if not is_submodule_initialized(submodule_path):
        print(
            f"error: {submodule_path} not initialized — run:\n"
            "  git submodule update --init --recursive",
            file=sys.stderr,
        )
        return 1

    parent_msg = args.parent_message or (
        f"chore(assets): bump {submodule_path} submodule pointer"
    )

    try:
        if args.submodule_checkout:
            checkout_submodule_branch(submodule_path, args.submodule_checkout, args.dry_run)

        if args.pull_submodule:
            pull_submodule_ff(submodule_path, args.dry_run)

        committed = commit_submodule_if_dirty(
            submodule_path,
            args.message,
            args.submodule_paths,
            args.dry_run,
        )

        if committed and not args.skip_submodule_push:
            push_submodule(submodule_path, args.dry_run)
        elif not args.skip_submodule_push and not args.dry_run:
            # Still push if ahead of remote even when nothing new to commit
            branch = git_lines("-C", submodule_path, "branch", "--show-current", check=False)
            if branch:
                ahead = run_git(
                    "-C",
                    submodule_path,
                    "rev-list",
                    f"origin/{branch[0]}..HEAD",
                    "--count",
                    check=False,
                )
                if ahead.stdout.strip() not in ("", "0"):
                    push_submodule(submodule_path, args.dry_run)

        if not args.skip_parent_commit:
            bumped = bump_parent_gitlink(submodule_path, parent_msg, args.dry_run)
            if bumped and not args.dry_run:
                run_paid_assets_staged_check()

        if args.push_parent:
            push_parent(args.dry_run)

        if not args.dry_run:
            print_status(submodule_path)
        return 0
    except GitError as exc:
        print(f"error: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
