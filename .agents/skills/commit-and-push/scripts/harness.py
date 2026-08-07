#!/usr/bin/env python3
"""Compiled harness for commit-and-push.

Inspect status/diff, flag secret and licensed paths, optionally run verify.
Defaults to dry-run. Commit/push only with --execute plus explicit flags,
and only when the user requested shipping.
"""

from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path

_ROOT = Path(__file__).resolve().parents[4]
if str(_ROOT) not in sys.path:
    sys.path.insert(0, str(_ROOT))

from scripts.skill_harness.io_util import emit, find_repo_root  # noqa: E402
from scripts.skill_harness.verify_catalog import (  # noqa: E402
    framework_verify_commands,
    is_blocked_ship_path,
    is_licensed_path,
    is_secret_path,
)


def _git(repo: Path, *args: str) -> str:
    proc = subprocess.run(
        ["git", *args],
        cwd=repo,
        capture_output=True,
        text=True,
        check=False,
    )
    return (proc.stdout or "").strip()


def _run_paid_assets_guard(repo: Path) -> tuple[int, str]:
    proc = subprocess.run(
        [sys.executable, "scripts/check_paid_assets.py", "--staged"],
        cwd=repo,
        capture_output=True,
        text=True,
        check=False,
    )
    out = ((proc.stdout or "") + (proc.stderr or "")).strip()
    return proc.returncode, out


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=None)
    parser.add_argument("--release", action="store_true", help="Include project verify commands")
    parser.add_argument(
        "--run-verify",
        action="store_true",
        help="Actually run verify commands (with --release)",
    )
    parser.add_argument(
        "--execute",
        action="store_true",
        help="Allow mutating git (still requires --commit / --push)",
    )
    parser.add_argument("--commit", action="store_true", help="Create commit (needs --execute -m)")
    parser.add_argument("-m", "--message", default=None, help="Commit message")
    parser.add_argument("--push", action="store_true", help="Push HEAD (needs --execute)")
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args(argv)

    repo = find_repo_root(args.repo)
    status = _git(repo, "status", "--short")
    branch = _git(repo, "status", "-sb")
    recent = _git(repo, "log", "-5", "--oneline")
    paths = [line[3:].strip() for line in status.splitlines() if line.strip()]
    secret_hits = [p for p in paths if is_secret_path(p)]
    licensed_hits = [p for p in paths if is_licensed_path(p)]
    blocked_hits = [p for p in paths if is_blocked_ship_path(p)]

    result: dict = {
        "repo": str(repo),
        "branch_status": branch,
        "status_short": status,
        "recent_commits": recent.splitlines(),
        "paths": paths,
        "secret_paths": secret_hits,
        "licensed_paths": licensed_hits,
        "blocked_paths": blocked_hits,
        "verify_commands": framework_verify_commands() if args.release else [],
        "verify_runs": [],
        "paid_assets_guard": None,
        "committed": False,
        "pushed": False,
        "notes": [
            "LLM slot: draft Conventional Commit subject/body (imperative, why not file list).",
            "Human: do not --execute/--commit/--push unless the user explicitly asked to ship.",
            "Default is dry-run inspect only.",
            "Prefer explicit path staging when unrelated dirty files exist.",
        ],
    }

    if secret_hits:
        result["notes"].append(
            f"Blocked secret-looking paths: {secret_hits}. Exclude before staging."
        )
    if licensed_hits:
        result["notes"].append(
            f"Blocked licensed paths: {licensed_hits}. "
            "Commit licensed art in project-twelve-assets; only bump the Assets/_Licensed gitlink here."
        )

    if args.release and args.run_verify:
        for entry in framework_verify_commands():
            proc = subprocess.run(
                entry["command"],
                shell=True,
                cwd=repo,
                capture_output=True,
                text=True,
            )
            result["verify_runs"].append(
                {
                    "id": entry["id"],
                    "command": entry["command"],
                    "exit_code": proc.returncode,
                    "ok": proc.returncode == 0,
                }
            )
        if any(not r["ok"] for r in result["verify_runs"]):
            result["notes"].append("Verify failed; refusing commit/push.")
            emit(result, as_json=args.json)
            return 1

    if args.commit or args.push:
        if not args.execute:
            result["notes"].append(
                "Refusing mutate: pass --execute only after explicit user request."
            )
            emit(result, as_json=args.json)
            return 1
        if blocked_hits:
            emit(result, as_json=args.json)
            return 1

    if args.commit:
        if not args.message:
            result["notes"].append("Commit requires -m/--message.")
            emit(result, as_json=args.json)
            return 1
        add = subprocess.run(["git", "add", "-A"], cwd=repo, capture_output=True, text=True)
        if add.returncode != 0:
            result["notes"].append(add.stderr)
            emit(result, as_json=args.json)
            return 1
        # Re-check staged for secrets / licensed blobs
        staged = _git(repo, "diff", "--cached", "--name-only")
        staged_blocked = [p for p in staged.splitlines() if is_blocked_ship_path(p)]
        if staged_blocked:
            subprocess.run(["git", "reset", "HEAD"], cwd=repo, capture_output=True)
            result["blocked_paths"] = staged_blocked
            result["secret_paths"] = [p for p in staged_blocked if is_secret_path(p)]
            result["licensed_paths"] = [p for p in staged_blocked if is_licensed_path(p)]
            result["notes"].append("Reset staging: secret or licensed paths present.")
            emit(result, as_json=args.json)
            return 1

        guard_code, guard_out = _run_paid_assets_guard(repo)
        result["paid_assets_guard"] = {"exit_code": guard_code, "output": guard_out}
        if guard_code != 0:
            subprocess.run(["git", "reset", "HEAD"], cwd=repo, capture_output=True)
            result["notes"].append(
                "Reset staging: check_paid_assets.py --staged failed. "
                + (guard_out or "")
            )
            emit(result, as_json=args.json)
            return 1

        commit = subprocess.run(
            ["git", "commit", "-m", args.message],
            cwd=repo,
            capture_output=True,
            text=True,
        )
        result["committed"] = commit.returncode == 0
        if commit.returncode != 0:
            result["notes"].append(commit.stderr or commit.stdout)
            emit(result, as_json=args.json)
            return 1

    if args.push:
        push = subprocess.run(
            ["git", "push", "-u", "origin", "HEAD"],
            cwd=repo,
            capture_output=True,
            text=True,
        )
        result["pushed"] = push.returncode == 0
        if push.returncode != 0:
            result["notes"].append(push.stderr or push.stdout)
            emit(result, as_json=args.json)
            return 1

    emit(result, as_json=args.json)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
