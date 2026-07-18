---
name: commit-and-push
description: Use when the user asks to commit and push, publish, ship, or prepare a release commit. Guides staging only intended changes, writing a Conventional Commit, committing, and pushing to origin after verifying status and diff.
capability: "commit-and-push agent asset workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: medium
---

# Commit and push

Ship local work to the remote. Pair with [`release-bump`](../release-bump/SKILL.md) when
`VERSION` and `CHANGELOG.md` were already promoted.

## When to use

- User explicitly asks to **commit**, **commit and push**, or **ship** changes.
- After `/release-bump` when the user approves the release commit.

**Do not** commit or push without an explicit user request.

## Workflow

### 1. Inspect (parallel)

```bash
git status --short
git diff
git diff --cached
git log -5 --oneline
git status -sb
```

Confirm what will ship. Exclude secrets (`.env`, `secrets.json`), licensed art under `Assets/_Licensed` or paths in `config/paid-assets.local-only.txt`, build artifacts, and unrelated WIP unless the user included them.

### 2. Version and changelog (if releasing)

| Source | Path |
|--------|------|
| Semver | [`VERSION`](../../../VERSION) at repo root |
| Changelog | [`CHANGELOG.md`](../../../CHANGELOG.md) (Keep a Changelog) |

If `CHANGELOG.md` still has all changes under `## [Unreleased]` and no `VERSION` bump, run
[`release-bump`](../release-bump/SKILL.md) first or ask the user which version to ship.

### 3. Stage

Stage only paths the user intends to ship — prefer explicit `git add <paths>` over blind
`git add -A` when unrelated dirty files exist.

Run before commit:

```bash
python3 scripts/check_paid_assets.py --staged
```

Always include when present: `VERSION`, `CHANGELOG.md`, and any files referenced in the changelog.

### 4. Commit message

Follow [`commit-conventions`](../commit-conventions/SKILL.md).

| Situation | Subject |
|-----------|---------|
| Release after `release-bump` | `chore(release): vX.Y.Z` |
| Feature work | `feat(scope): imperative summary` |
| Docs/skills only | `docs(scope): …` or `feat(agents): …` |

Body (optional second `-m`): one or two sentences on **why**, not a file list.

**PowerShell** — use multiple `-m` flags (heredocs fail):

```powershell
git commit -m "chore(release): v0.1.0" -m "First framework release with CLI install tiers, agent-environment refs, and CI validation gates."
```

### 5. Push

```bash
git push -u origin HEAD
```

Use the current branch name from `git status -sb`. Do not force-push `main`/`master` unless the user explicitly requests it.

### 6. Verify

```bash
git status -sb
git log -1 --oneline
```

Report the commit hash and whether the branch is up to date with origin.

## Git safety (hard rules)

- Never modify `.git/config` or add non-standard git extensions.
- Never `--no-verify` / `--no-gpg-sign` unless the user asked.
- Never `git commit --amend` after a failed hook — fix and create a **new** commit.
- Amend only when the user asked, HEAD is your commit, and it was not pushed.
- Never commit files that likely contain secrets.

## Optional tag

Suggest `git tag -a vX.Y.Z -m "…"` only when the user asks to tag. Do not tag or push tags unless requested.

## ProjectTwelve verify (before agent-infra or release commit)

When shipping agent scaffolding or release changes, run the self-verify set from [`AGENTS.md`](../../../AGENTS.md):

```bash
python scripts/sync_assistant_trees.py --check
python scripts/ci/check_agent_frontmatter.py
python scripts/validate_cli_skills.py
python3 scripts/check_markdown_links.py
python3 scripts/check_paid_assets.py --staged
```

If `.claude/` assets changed, run `python scripts/sync_assistant_trees.py` before staging so the Cursor and Codex mirrors stay in sync.

For Unity C# or autotile changes, also run targeted EditMode tests and `cd tools/tile-viz && npm test` per [`unity-tests`](../unity-tests/SKILL.md).
