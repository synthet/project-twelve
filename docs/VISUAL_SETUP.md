---
type: Technical Reference
title: "Visual setup"
description: "ProjectTwelve Visual setup — technical reference for the visual setup documentation in the sandbox prototype."
resource: VISUAL_SETUP.md
tags: [docs, visual]
timestamp: 2026-06-28T00:00:00Z
okf_version: 0.1
---

# Visual setup (assets submodule)

ProjectTwelve renders terrain, player avatars, and monsters from catalogs under `Assets/_Licensed/Settings/Visual/` in the private assets submodule. Simulation code and public tile mappings stay in the main repo.

## One-time machine setup

1. Clone with submodules (requires access to [project-twelve-assets](https://github.com/synthet/project-twelve-assets)):

   ```bash
   git clone --recurse-submodules https://github.com/synthet/project-twelve.git
   ```

   Or, after a code-only clone:

   ```bash
   git submodule update --init --recursive
   ```

2. Open the **repository root** (the clone folder) in Unity 6.0.5.1f1. Import paths load from `Assets/_Licensed/config/visual-import.txt` automatically.

3. If catalogs are missing or stale, regenerate them:
   - **ProjectTwelve → Visual → Import Autotile Catalog from Local Source**
   - **ProjectTwelve → Visual → Import Character Layer Catalog from Local Source**
   - **ProjectTwelve → Visual → Import Monster Visual Catalog from Local Source**

   Or run `python3 scripts/generate_visual_catalogs.py` (no Editor required).

4. Confirm scene references (usually committed):
   - `SandboxTileVisualCatalog` → `AutotileCatalog` from submodule
   - Player `SandboxPlayerAvatarVisual` → `CharacterLayerCatalog` and character prefab from submodule

## Code-only checkout

Without the submodule, the sandbox prototype still runs with vertex-color terrain. Avatar and autotile visuals log warnings until `Assets/_Licensed` is initialized.

## Unity / MCP follow-up (when Editor is open)

1. Confirm **Edit → Project Settings → AI → Unity MCP** bridge is **Running** (optional).
2. Run the three import menu items if you changed licensed source art.
3. Enter Play mode and verify:
   - Terrain chunks render with autotiled ground/cover sprites.
   - Player avatar composes with randomized equipment.
   - A catalog monster spawns via `MonsterSpawnHelper.Spawn(catalog, "PurpleBat", position)`.

## Behavior reference

See [Visual behavior spec](VISUAL_BEHAVIOR_SPEC.md) for autotile masks, sprite sheet layout, and animator parameters in `Assets/Scripts/Visual/`.

## Paid asset hygiene

Never commit licensed files directly into the public repo. Run `python3 scripts/check_paid_assets.py --staged` before commits. See [Paid assets policy](PAID_ASSETS.md).
