> **Claude Code:** Same intent as Cursor `/fetch-remotes`. When customizing, keep in sync with `.cursor/commands/fetch-remotes.md`.

# /fetch-remotes — Sync main repo and assets submodule

Use at session start, before picking backlog work, or when the user asks to sync repos or get latest from GitHub.

## Usage

```
/fetch-remotes [--fetch-only]
```

## Inputs

- Optional `--fetch-only`: fetch remotes and sync submodule to gitlink without fast-forward pulling the main branch.

## Action

Run the steps in order. Stop and report on any failure.

### 1. Sync remotes

From repo root:

```bash
python scripts/fetch_remotes.py
```

Or with fetch-only:

```bash
python scripts/fetch_remotes.py --fetch-only
```

### 2. Summarize status

Report from script output:

- Main repo branch, upstream, commits behind/ahead
- Submodule alignment (gitlink vs checkout)
- Any warnings (dirty submodule working tree, submodule not initialized)

### 3. Handle blockers

If apply fails (dirty working tree, non-ff pull, missing submodule access):

- Report the error verbatim
- Suggest the next manual step (stash/commit, resolve divergence, or `docs/PAID_ASSETS.md` submodule setup)
- Do **not** force-merge or `--force` pull

## Done when

- Script exits 0, or the user has a clear written blocker and next step.

## Related

- Skill: `.claude/skills/repo-sync/SKILL.md` (hooks, assets-first workflow, semantics)
- One-time hook setup (human only): `python scripts/install_githooks.py`
