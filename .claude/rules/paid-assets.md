---
description: Never commit or push paid/licensed third-party Unity assets
alwaysApply: true
---

# Paid and licensed assets — private submodule

**Never commit, stage, or push paid, licensed, or Asset Store content into the public project-twelve repo.**

Licensed content lives in the private [project-twelve-assets](https://github.com/synthet/project-twelve-assets) repository, mounted as a git submodule at `Assets/_Licensed/`.

## Blocked paths (public repo)

Machine-local manifest: `config/paid-assets.local-only.txt` (gitignored; copy from `config/paid-assets.local-only.example.txt`).

- `Assets/PixelHeroes/` — direct imports into the public repo
- `Assets/_Licensed/*` copied as regular files (submodule gitlink at `Assets/_Licensed` is allowed)
- Legacy generated catalogs under `Assets/Settings/Visual/` in main

## Before every commit or push

1. Run `python3 scripts/check_paid_assets.py --staged` (commits) or `--push` (before push).
2. Inspect `git status` and `git diff --cached --name-only` for marketplace folders or paths in the manifest.
3. New paid packs go into **project-twelve-assets**, not the public repo.

## Agent actions

- Do **not** `git add` licensed art paths into the public repo.
- Do **not** use `git add -A` without verifying the staged set excludes paid assets.
- Vendor pack names and import paths **may** appear in public docs and code comments (the licensed
  files themselves live in the private `project-twelve-assets` submodule). Never commit the licensed
  **assets** — art, prefabs, generated catalogs — into the public repo.
- Submodule pointer updates (`.gitmodules`, `Assets/_Licensed` gitlink) **are** allowed in the public repo.

## Adding a new paid pack

1. Import into the assets submodule under `Assets/_Licensed/` (commit in project-twelve-assets).
2. Update `Assets/_Licensed/config/visual-import.txt` in the assets repo if paths change.
3. Regenerate catalogs (Unity import menus or `python3 scripts/generate_visual_catalogs.py`).
4. Bump the submodule pointer in the public repo.

See [`docs/PAID_ASSETS.md`](../../docs/PAID_ASSETS.md) for full workflow.
