---
name: repo-sync
description: Sync project-twelve with the Assets/_Licensed submodule via fetch_remotes.py, git hooks, and /fetch-remotes. Use at session start, before picking backlog work, or when the user asks to sync repos or get latest.
---

# Repo sync (project-twelve + assets submodule)

> **ProjectTwelve:** Main repo [project-twelve](https://github.com/synthet/project-twelve); licensed art submodule [project-twelve-assets](https://github.com/synthet/project-twelve-assets) at `Assets/_Licensed/`. The clone folder is the git and Unity project root (no nested `ProjectTwelve/` wrapper).

## When to use

- Session start or before picking backlog work (`/fetch-remotes`).
- User says “sync repo”, “get latest”, or “update submodule”.
- Submodule checkout looks wrong after a pull or branch switch (hooks should auto-fix; run `--local-sync` if needed).
- Before committing parent-repo changes that touch the submodule gitlink.

## Default sync (network + apply)

```bash
python scripts/fetch_remotes.py
```

This **fetches** both remotes, **fast-forward pulls** the current main-repo branch, and checks out the submodule at the **commit pinned by the parent gitlink** (not the submodule’s latest `main` tip).

Fetch only (no pull / no working-tree update on main branch):

```bash
python scripts/fetch_remotes.py --fetch-only
```

## Submodule semantics

- The parent repo records a **gitlink** (SHA) for `Assets/_Licensed`.
- After sync, the submodule checkout must match that gitlink.
- To advance assets: commit in **project-twelve-assets**, push, then bump the gitlink in **project-twelve** (see [`docs/PAID_ASSETS.md`](../../../docs/PAID_ASSETS.md)).

## Git hooks (one-time setup)

Humans opt in locally (agents do **not** run install automatically):

```bash
python scripts/install_githooks.py
```

| Hook | Repo | Behavior |
|------|------|----------|
| `pre-commit` | parent | Paid-asset guard + `--verify` |
| `pre-push` | parent | Paid-asset guard + `--verify` |
| `post-merge` | parent | `--local-sync` after pull/merge |
| `post-checkout` | parent | `--local-sync` on branch switch |
| `submodule/pre-push` | assets | Warn if parent gitlink ≠ submodule `HEAD` |

Hooks enforce **local alignment** only; they do not fetch remotes on every commit. Use `/fetch-remotes` for network sync.

Manual hook equivalents:

```bash
python scripts/fetch_remotes.py --local-sync   # align checkout to gitlink
python scripts/fetch_remotes.py --verify       # fail if misaligned
```

## Assets-first workflow

1. Change art/catalogs/docs in `Assets/_Licensed/`; commit and push to **project-twelve-assets**.
2. Submodule `pre-push` may **warn** that the parent gitlink is stale — expected until step 3.
3. In the main repo: bump the gitlink — use **`python scripts/publish_assets_submodule.py`** (skill: [`assets-submodule-publish`](../assets-submodule-publish/SKILL.md)) or manually `git add Assets/_Licensed` and commit.

```bash
python scripts/publish_assets_submodule.py --status
python scripts/publish_assets_submodule.py -m "docs: …" --submodule-checkout main --pull-submodule
```

## Safety

- Never stage licensed blobs under wrong paths in the public repo — see [`docs/PAID_ASSETS.md`](../../../docs/PAID_ASSETS.md) and `scripts/check_paid_assets.py`.
- If submodule init fails (no private repo access), follow submodule setup in PAID_ASSETS.md.
- Do not use `git submodule update --remote` unless explicitly maintaining a pointer bump.

## Slash command

Prefer `/fetch-remotes` (optional `[--fetch-only]`) for agent-driven sync.
