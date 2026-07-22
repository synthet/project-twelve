"""Project self-verify command catalog and secret/licensed path gates."""

from __future__ import annotations

import re
from pathlib import Path

# Kept name for harness API compatibility with synthet-code-framework.
FRAMEWORK_VERIFY_COMMANDS: list[tuple[str, str]] = [
    ("sync_check", "python scripts/sync_assistant_trees.py --check"),
    ("frontmatter", "python scripts/ci/check_agent_frontmatter.py"),
    ("cli_skills", "python scripts/validate_cli_skills.py"),
    ("markdown_links", "python3 scripts/check_markdown_links.py"),
    ("paid_assets_staged", "python3 scripts/check_paid_assets.py --staged"),
    ("eval_fixtures", "python scripts/run_agent_eval_fixtures.py"),
]

CLAIM_PROOF_CATALOG: dict[str, dict[str, str]] = {
    "tests_pass": {
        "claim": "Tests pass",
        "proof": "python -m pytest tests -q",
        "not_enough": "Previous run or narrow subset for a broad claim",
    },
    "lint_clean": {
        "claim": "Lint/docs clean",
        "proof": "python3 scripts/check_markdown_links.py",
        "not_enough": "Formatting only",
    },
    "assets_synced": {
        "claim": "Generated assets in sync",
        "proof": "python scripts/sync_assistant_trees.py --check",
        "not_enough": "Manual copy or assumed sync",
    },
    "frontmatter_ok": {
        "claim": "Agent frontmatter valid",
        "proof": "python scripts/ci/check_agent_frontmatter.py",
        "not_enough": "Spot-check of one file",
    },
    "paid_assets_clean": {
        "claim": "No paid/licensed paths staged",
        "proof": "python3 scripts/check_paid_assets.py --staged",
        "not_enough": "Assumed clean without running the guard",
    },
    "ready_to_commit": {
        "claim": "Ready to commit",
        "proof": "git status --short && git diff --check",
        "not_enough": "Memory of earlier status",
    },
}

SECRET_PATH_PATTERNS = [
    re.compile(r"(?i)(^|/|\\)\.env(\.|$)"),
    re.compile(r"(?i)secrets\.json$"),
    re.compile(r"(?i)\.pem$"),
    re.compile(r"(?i)id_rsa"),
    re.compile(r"(?i)\.p12$"),
]

# Public-repo blocked licensed content (submodule content must not be staged as blobs).
LICENSED_PATH_PATTERNS = [
    re.compile(r"(?i)(^|/)Assets/_Licensed/"),
    re.compile(r"(?i)(^|/)Assets/PixelFantasy/"),
]


def is_secret_path(path: str | Path) -> bool:
    text = str(path).replace("\\", "/")
    return any(p.search(text) for p in SECRET_PATH_PATTERNS)


def is_licensed_path(path: str | Path) -> bool:
    """True when path is licensed content that must not enter the public repo as blobs."""
    text = str(path).replace("\\", "/")
    # Submodule gitlink at Assets/_Licensed (no trailing slash content) is allowed.
    if text.rstrip("/") == "Assets/_Licensed":
        return False
    return any(p.search(text) for p in LICENSED_PATH_PATTERNS)


def is_blocked_ship_path(path: str | Path) -> bool:
    return is_secret_path(path) or is_licensed_path(path)


def framework_verify_commands() -> list[dict[str, str]]:
    return [{"id": cid, "command": cmd} for cid, cmd in FRAMEWORK_VERIFY_COMMANDS]
