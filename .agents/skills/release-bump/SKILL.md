---
name: release-bump
description: Bump the project version with a semver rubric, promote the Unreleased changelog section, and verify before committing. Use when the user asks to cut a release, bump the version, or tag a new version.
---

# Release bump

Repeatable release procedure: decide the semver level, update the version and changelog,
verify, and prepare (not push) the release commit.

## 1. Detect the version source

Find where the version lives, in this order:

1. Root `VERSION` file or `CHANGELOG.md` (Keep a Changelog) — **preferred when present**
2. `tools/tile-viz/package.json` or `tools/world-viz/package.json` (offline tooling only)
3. `pyproject.toml` / `setup.cfg` (Python), if added later

**ProjectTwelve note:** There is no root `CHANGELOG.md` or `VERSION` file yet. For game releases,
Unity uses `ProjectSettings/ProjectSettings.asset` (bundle version) — that is a human Editor workflow,
not an agent semver bump. If no semver source exists, **ask the user** whether to:

- Create `VERSION` + `CHANGELOG.md` at repo root for agent-infra / tooling releases, or
- Tag a specific subsystem only (e.g. `tile-viz@x.y.z`), or
- Skip versioning until a release process is defined.

Do not invent a version file without explicit approval.

## 2. Choose the bump (semver rubric)

Review changes since the last release (`git log <last-tag>..HEAD` and the `Unreleased` section of
`CHANGELOG.md` when it exists):

- **major** — any breaking change: removed/renamed public API, config key, schema field, MCP tool
  contract, or autotile/visual behavior that breaks parity fixtures without migration.
- **minor** — new backward-compatible capability (new command, skill, MCP tool, gameplay feature).
- **patch** — bug fixes, docs, internal refactors with no contract change.

State the chosen level and the one or two changes that justify it.

## 3. Update files

1. Bump the version in the detected source.
2. When `CHANGELOG.md` exists: rename `## [Unreleased]` to `## [X.Y.Z] — YYYY-MM-DD`,
   keeping its subsections, and add a fresh empty `## [Unreleased]` above it.

## 4. Verify

Run checks from **AGENTS.md** for the changed areas:

```bash
python scripts/sync_assistant_trees.py --check
python3 scripts/check_markdown_links.py
python3 scripts/check_paid_assets.py --staged
# Unity / offline tests as applicable — see AGENTS.md test vocabulary
```

Do not proceed with failing checks.

## 5. Commit and tag — only when the user asks

Suggest a Conventional Commit (`chore(release): vX.Y.Z`) and the matching annotated tag
(`git tag -a vX.Y.Z`). Do not commit, tag, or push without an explicit request.
