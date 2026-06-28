---
type: reference
title: Vendor references (licensed art upstream)
description: Upstream packs, import paths, and integration guarantees for Pixel Hero Maker and TileEngine
resource: docs/wiki/vendor-references.md
tags:
  - assets
  - vendors
  - hippogames
  - third-party
timestamp: 2026-06-28T00:00:00Z
---

# Vendor references (licensed art upstream)

> **Status:** Reference documentation for vendor packs (Pixel Hero Maker, TileEngine).
> **Decisions:** Vendor names and upstream links are documentation; licensed files live in private submodule.
> **Invariants:** Simulation owns IDs; vendor sprites resolve at render/compose time only.

Both packs ship from the same publisher, **Hippo** (`hippogamesunity`), so they share a support
Discord and issue tracker.

## Characters — Pixel Hero Maker [Megapack]

| | |
|---|---|
| Asset Store | <https://assetstore.unity.com/packages/2d/characters/pixel-hero-maker-megapack-271664> (id `271664`) |
| itch.io | <https://hippogames.itch.io/pixel-hero-maker> |
| Base (non-mega) | <https://assetstore.unity.com/packages/2d/characters/pixel-hero-maker-250116> (id `250116`) |
| Publisher | Hippo (`hippogamesunity@gmail.com`) |

- Layered character generator: build characters in a `CharacterEditor` scene or at runtime via a
  `CharacterBuilder`, and export flipbook sprite sheets.
- Ships animation states: **Idle, Ready, Walking, Running, Crawling, Climbing, Jumping, Blocking,
  Pushing, Attacking (Slash / Jab / Shot), Death**.
- Demo gameplay scripts (`CharacterControls`, `CharacterController2D`, `CharacterAnimation`,
  `CharacterBuilder`, `Character`) are **stripped on spawn** by
  [`PlayerAvatarFactory`](../../Assets/Scripts/Integration/PlayerAvatarFactory.cs) so vendor input/physics
  never reach the sandbox; ProjectTwelve drives the avatar through `CharacterLocomotionDriver`.
- Imports under `Assets/_Licensed/PixelHeroes/` (the paid-asset guard blocks `Assets/PixelHeroes/` and
  `Assets/_Licensed/PixelHeroes/` from the public repo — see [Paid assets](../PAID_ASSETS.md)).
- Feeds `CharacterLayerCatalog` via the layer-folder importer; layer roots come from
  `Assets/_Licensed/config/visual-import.txt` (`hero_sprites_root`, `hero_extra_layer*`).

## Terrain — TileEngine (Fantasy Tile Engine)

| | |
|---|---|
| Wiki | <https://github.com/hippogamesunity/TileEngine/wiki> |
| Issues | <https://github.com/hippogamesunity/TileEngine/issues> |
| itch.io | <https://hippogames.itch.io/fantasy-tile-engine> |
| Discord | <https://discord.gg/4ht2AhW> |
| Publisher | Hippo (`hippogamesunity`) |

- Tilesets, props, and a Level Builder for fantasy 2D worlds.
- The **GitHub repo itself is empty** — it exists only to host the wiki and issue tracker (0 open
  issues at time of writing). The engine ships via the Asset Store / itch.io, not from git.
- Custom-sprite import conventions per the wiki: place sprites under the engine's `Tiles/` folder with
  **PPU 16, Point filter, Read/Write enabled, no compression**, then refresh the
  `Resources/SpriteCollection` asset. These match ProjectTwelve's pixel-art import policy in
  [Asset integration](15-assets-integration.md).
- WebGL sprite integration is marked "under development" upstream — relevant if WebGL builds are added.
- Tile source art feeds `AutotileCatalog` (`Ground` / `Cover` subfolders) via the autotile importer;
  root comes from `tile_sprites_root` in `visual-import.txt`.

## ProjectTwelve isolation guarantees

- Simulation owns tile/entity **IDs**; vendor sprites resolve only at render/compose time
  (see [Visual integration](visual-integration.md)).
- No vendor script types are referenced by ProjectTwelve code — the avatar is composed through
  project-owned components after demo scripts are stripped.
- Catalogs (`AutotileCatalog`, `CharacterLayerCatalog`, `MonsterVisualCatalog`) are generated into the
  private submodule; only ID→tileset mappings (`SandboxTileVisualCatalog`) live in the public repo.

## See also

- [Asset integration requirements](15-assets-integration.md)
- [Visual integration](visual-integration.md)
- [Visual setup](../VISUAL_SETUP.md)
- [Paid assets policy](../PAID_ASSETS.md)
