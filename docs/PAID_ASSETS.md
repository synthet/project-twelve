---
type: guide
title: Paid and licensed assets (private submodule)
description: Workflow for managing and checking paid Asset Store content in project-twelve-assets
resource: docs/PAID_ASSETS.md
tags:
  - assets
  - licensing
  - git-submodule
  - security
timestamp: 2026-06-28T00:00:00Z
---

# Paid and licensed assets (private submodule)

> **Status:** Paid/licensed content segregated into private `project-twelve-assets` submodule.
> **Decisions:** Submodule pointer and blocker manifest (manifest, guard script) live in public repo; licensed files never do.
> **Invariants:** `Assets/_Licensed` is a git submodule; check_paid_assets.py guards the public tree.

Paid Unity Asset Store packages and third-party marketplace art **must not** be committed to the public [project-twelve](https://github.com/synthet/project-twelve) repository. Licensed content lives in the private submodule repo [project-twelve-assets](https://github.com/synthet/project-twelve-assets), mounted at `Assets/_Licensed/`.

## Submodule setup

```bash
# Full clone with licensed assets (requires private repo access)
git clone --recurse-submodules https://github.com/synthet/project-twelve.git

# Or initialize after clone
git submodule update --init --recursive
```

Import paths are read from `Assets/_Licensed/config/visual-import.txt` in the submodule. Optional machine override: copy [`config/visual-import.local-only.example.txt`](../config/visual-import.local-only.example.txt) to `config/visual-import.local-only.txt` (gitignored).

## What stays in the public repo

- Code under `Assets/Scripts/`
- Public settings such as `SandboxTileVisualCatalog` (tile ID → tileset name mapping)
- Submodule pointer (`.gitmodules` + gitlink at `Assets/_Licensed`)

## What stays in the assets submodule

- Licensed source art under `Assets/_Licensed/PixelHeroes/`
- Generated catalogs under `Assets/_Licensed/Settings/Visual/`
- Vendor import paths in `Assets/_Licensed/config/visual-import.txt`

## Regenerate visual catalogs

In Unity 6.0.5.1f1 with the submodule initialized:

1. **ProjectTwelve → Visual → Import Autotile Catalog from Local Source**
2. **ProjectTwelve → Visual → Import Character Layer Catalog from Local Source**
3. **ProjectTwelve → Visual → Import Monster Visual Catalog from Local Source**

Or batch mode:

```bash
Unity -batchmode -quit -projectPath . -executeMethod ProjectTwelve.Editor.Visual.VisualCatalogBatchImporter.ImportAllFromCommandLine -logFile Logs/unity-visual-import.log
```

Commit updated catalogs in **project-twelve-assets**, then bump the submodule pointer in the main repo.

Offline regeneration (no Unity Editor):

```bash
python3 scripts/generate_visual_catalogs.py
```

## Pre-commit / pre-push check

```bash
python3 scripts/check_paid_assets.py --staged   # before commit
python3 scripts/check_paid_assets.py --push     # before push
```

The guard blocks direct licensed imports into the public repo (`Assets/PixelHeroes/`, copied submodule files under `Assets/_Licensed/`, legacy catalog paths in main).

Optional local hook:

```bash
git config core.hooksPath .githooks
```

## Direct import mistake

If you import a paid pack into the public repo by accident:

1. Remove files from the index and disk.
2. Re-import under the assets submodule (or push to project-twelve-assets).
3. Run `python3 scripts/check_paid_assets.py --staged`.

See also [Visual setup](VISUAL_SETUP.md) and [Asset integration](wiki/15-assets-integration.md).
