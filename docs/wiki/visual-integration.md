---
type: Architecture
title: Visual Integration
description: Sandbox tile mapping, player avatar composition, creature visuals, and vendor parity for project-owned presentation code.
resource: wiki/visual-integration.md
tags: [docs, wiki, visual, rendering, characters]
timestamp: 2026-06-30T12:00:00Z
---

# Visual integration

> **Status:** Terrain autotiling and player avatar presentation use project-owned code under `Assets/Scripts/Visual/`.
> **Decisions:** Sandbox simulation owns tile IDs; visuals resolve at render/compose time only.
> **Invariants:** World state stores tile IDs; `SandboxTileVisualCatalog` maps IDs to autotile tileset names.

## Overview

| System | ProjectTwelve touchpoints |
|--------|-------------------------|
| Terrain autotiles | `AutotileCatalog`, `AutotileResolver`, `AutotileMaskBuilder`, `SandboxTileVisualCatalog`, `SandboxChunkRenderer` |
| Player avatar | `CharacterComposer`, `LayeredCharacterVisual`, `CharacterLocomotionDriver`, `PlayerAvatarFactory`, `SandboxPlayerAvatarVisual` |
| Creatures | `MonsterVisualCatalog`, `MonsterLocomotionDriver`, `MonsterSpawnHelper`, `MountCompositor` |
| Sprite effects | `EffectCatalog`, `SpriteEffectInstance` |

Licensed source art lives in the private **project-twelve-assets** git submodule at `Assets/_Licensed/`. Import paths: `Assets/_Licensed/config/visual-import.txt` (optional override: gitignored `config/visual-import.local-only.txt`).

## Vendor parity (Pixel Heroes Hub)

ProjectTwelve does **not** reference vendor demo scripts at runtime. Behavioral contracts are documented in [Visual behavior spec](../VISUAL_BEHAVIOR_SPEC.md) and aligned with the public [Pixel Heroes Hub wiki](https://github.com/hippogamesunity/PixelHeroesHub/wiki).

| Vendor concept | ProjectTwelve replacement | Status |
|----------------|---------------------------|--------|
| CharacterBuilder (runtime layer merge) | `CharacterComposer.Rebuild()` | Implemented |
| SpriteLibrary / SpriteLibraryAsset | Runtime `SpriteLibraryAsset` from merged texture | Implemented |
| CharacterAnimation (clip API) | `CharacterLocomotionDriver` + prefab `Animator` | Partial — sandbox uses Idle/Run/Jump/Fall/Land only |
| CharacterControls | Stripped; `SandboxPlayerController` owns physics | By design |
| Detached firearms | `FirearmVisual` + `ApplyFirearm()` | Partial — not wired from `PlayerAvatarFactory` |
| Sprite effects (dust, muzzle) | `EffectCatalog.CreateSpriteEffect` | Partial — driver hooks exist; scene catalog optional |
| Sprite sheet format (576×928, 64×64 cells) | `CharacterSheetLayout` | Implemented |
| Building characters at runtime | Equipment strings on `CharacterComposer` + `Rebuild()` | Implemented |

## Implementation status matrix

| Capability | Status | Notes |
|------------|--------|-------|
| Autotiled terrain in chunk meshes | Implemented | P1-RENDER-001 |
| Random avatar spawn in play mode | Implemented | Requires submodule + catalog |
| Foot alignment to collider | Implemented | `SandboxPlayerAvatarVisual` |
| Locomotion from controller velocity | Partial | 5 of 12+ animator states wired |
| Walk vs Run threshold | Deferred | P2-VISUAL-002 |
| Combat triggers (Slash, Shot, etc.) | Deferred | Until combat simulation exists |
| Detached firearm + muzzle socket | Deferred | P2-VISUAL-002 |
| Run/Jump/Land dust VFX | Deferred | Needs `EffectCatalog` in scene |
| Horns layer in merge order | Deferred | Spec lists Horns; merge omits it |
| Monster spawn + locomotion demo | Partial | `MonsterSpawnHelper` works; no AI wiring |
| EditMode visual invariant tests | Open | P1-VISUAL-002 |
| Catalog import pipeline spec | Open | P2-VISUAL-001 |

## Data flow (terrain)

```mermaid
flowchart LR
  World[SandboxWorld tile IDs]
  Catalog[SandboxTileVisualCatalog]
  AC[AutotileCatalog]
  Mask[AutotileMaskBuilder]
  Resolver[AutotileResolver]
  Renderer[SandboxChunkRenderer mesh]

  World --> Catalog
  Catalog --> AC
  World --> Mask
  AC --> Resolver
  Mask --> Resolver
  Resolver --> Renderer
```

## Data flow (player avatar)

```mermaid
flowchart TB
  Factory[PlayerAvatarFactory]
  Composer[CharacterComposer]
  Merge[LayerTextureComposer]
  Library[SpriteLibraryAsset]
  Visual[LayeredCharacterVisual]
  Driver[CharacterLocomotionDriver]
  Anim[SandboxPlayerAvatarAnimation]
  Controller[SandboxPlayerController]

  Factory --> Composer
  Composer --> Merge
  Merge --> Library
  Library --> Visual
  Visual --> Driver
  Controller --> Anim
  Anim --> Driver
```

1. `PlayerAvatarFactory` instantiates the licensed character prefab, strips vendor demo scripts, and attaches project-owned components.
2. `CharacterComposer` merges equipment layer textures into a 576×928 sheet and builds a runtime `SpriteLibraryAsset`.
3. `LayeredCharacterVisual.ApplySpriteLibrary` assigns the library to the body `SpriteLibrary` component.
4. `SandboxPlayerAvatarAnimation` reads `SandboxPlayerController` velocity and calls `ISandboxPlayerLocomotion` methods on `CharacterLocomotionDriver`.

## Default tile mapping

| Sandbox tile ID | Ground tileset | Cover tileset |
|-----------------|----------------|---------------|
| Dirt | Humus | — |
| Grass | Humus | GrassA |
| Stone | Rocks | — |
| CopperOre | BricksA | — |
| IronOre | BricksB | — |
| SilverOre | BricksC | — |
| GoldOre | BricksD | — |

## Local setup

See [Visual setup](../VISUAL_SETUP.md) for machine configuration and import menu steps.

## Key files

| File | Role |
|------|------|
| `Assets/Scripts/Visual/Tiles/AutotileCatalog.cs` | Ground/cover tileset catalog |
| `Assets/Scripts/Visual/Tiles/AutotileResolver.cs` | Deterministic autotile resolution |
| `Assets/Scripts/Visual/Characters/CharacterComposer.cs` | Runtime hero layer merge |
| `Assets/Scripts/Visual/Characters/CharacterSheetLayout.cs` | Sprite sheet dimensions and clip row order |
| `Assets/Scripts/Visual/Characters/CharacterLocomotionDriver.cs` | Animator bool/trigger API |
| `Assets/Scripts/Integration/PlayerAvatarFactory.cs` | Avatar spawn |
| `Assets/Scripts/Integration/LocalImportConfig.cs` | Submodule/override import path reader |
| `Assets/Scripts/Sandbox/SandboxPlayerAvatarVisual.cs` | Scene hook for avatar spawn |
| `Assets/Scripts/Sandbox/SandboxPlayerAvatarAnimation.cs` | Controller-to-locomotion bridge |
| `Assets/Scripts/Visual/Creatures/MonsterSpawnHelper.cs` | Catalog monster spawn |

## Backlog tickets

| ID | Scope |
|----|-------|
| P1-VISUAL-001 | Sandbox player avatar visual integration and QA checklist |
| P1-VISUAL-002 | EditMode tests for visual invariants |
| P2-VISUAL-001 | Visual catalog import pipeline contract |
| P2-VISUAL-002 | Extended character presentation (VFX, firearms, locomotion) |
| P2-VISUAL-003 | Monster visual integration for enemies |

## See also

- [Visual behavior spec](../VISUAL_BEHAVIOR_SPEC.md)
- [Rendering and Collision](rendering-and-collision.md)
- [Asset integration requirements](15-assets-integration.md)
- [Gameplay Systems](gameplay-systems.md) — player simulation vs presentation boundary
