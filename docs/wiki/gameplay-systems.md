---
type: Architecture
title: Gameplay Systems
description: Simulation/presentation split for player, inventory, and enemy systems, including the P2-AI-001 enemy nav and spawn contract.
resource: wiki/gameplay-systems.md
tags: [docs, wiki, gameplay, ai, p2]
timestamp: 2026-07-11T16:52:00Z
okf_version: 0.1
---

# Gameplay Systems

## Player

The player controller owns movement input and translates tile edit inputs into world requests. Validation such as edit range belongs near the player/controller boundary, while authoritative world mutation belongs in `SandboxWorld` or a future server layer.

### Player presentation

Simulation and visuals are intentionally separated:

| Layer | Component | Responsibility |
|-------|-----------|----------------|
| Simulation | `SandboxPlayerController` | Physics, grounded state, velocity, tile edit requests |
| Presentation | `SandboxPlayerAvatarVisual` | Spawns composed avatar via `PlayerAvatarFactory` |
| Animation bridge | `SandboxPlayerAvatarAnimation` | Maps controller state to `ISandboxPlayerLocomotion` |
| Locomotion API | `CharacterLocomotionDriver` | Drives prefab `Animator` bools/triggers |

Gameplay code must not depend on `SpriteRenderer` bounds or vendor animation scripts. Equipment appearance is data on `CharacterComposer` (layer strings) resolved at compose time — see [Visual integration](visual-integration.md).

### Creative sandbox HUD

The prototype scene includes a screen-space HUD for the current creative tile-edit loop. Its
bottom hotbar has ten slots: dirt, grass, stone, and copper ore occupy slots 1–4, while slots 5–10
are intentionally empty. Number keys select any slot; the mouse wheel cycles through populated
slots only. A populated selection changes the registered solid tile placed with right-click. An
empty selection disables placement without affecting left-click removal. The infinity marker is a
creative-mode affordance, not an inventory quantity.

The compact top-left vitals panel reads `SandboxPlayerVitals`, which initializes from the
`core:player` entity definition and exposes damage/heal events for later combat integration. No
normal gameplay damage source is part of this prototype. The selected hotbar slot is lifted and
marked in gold, with its item name shown as a compact label immediately above the slot.

Seed, player tile, and owning chunk are presented separately as lightweight development telemetry
using the same coordinate conversion as world streaming and debug tools. The telemetry has an
independent visibility API and is suppressed by default in non-development player builds.

The HUD is presentation over existing prototype state. It does not implement item consumption,
pickups, persistence, crafting, mana, or time-of-day; those remain owned by P2-INV-001 and the
production UI flows in P4-UX-001.

The HUD uses the repo-owned original sprites in `Assets/Sprites/UI/Generated`. Rebuilding the HUD
through **ProjectTwelve → UI → Rebuild Sandbox HUD** preserves this theme; there is no vendor-style
switcher or dependency on an external extracted-graphics directory.

## Inventory and Items

Future inventory should be data-driven and registry-backed. Tile placement should consume item stacks after validation. Tool strength, range, cooldown, and tile damage should be item/tool properties rather than hard-coded player logic.

## Enemies and Pathfinding

Enemy behavior for the P2 alpha is specified in [09 — Pathfinding](09-pathfinding.md) §
"P2-AI-001 specification (walker archetype)" and implemented against the shared solidity grid
(`SandboxTile.IsSolid`).

| Layer | Component | Responsibility |
|-------|-----------|----------------|
| Simulation | `SandboxEnemyAgent` | Path follow, local steering fallback, despawn rules |
| Navigation | `SandboxNavPathfinder` (+ `SandboxNavRequestScheduler`) | A* over walk/jump/fall edges with expansion and per-tick budgets |
| Spawn | `SandboxEnemySpawner` / `SandboxSpawnRules` | Candidate selection per distance/light/population rules |
| Presentation | `MonsterSpawnHelper` / `MonsterLocomotionDriver` | Visual spawn and animation (P2-VISUAL-003) |

All navigation code lives under `Assets/Scripts/Sandbox/Nav/`; the pure core (pathfinder, spawn
rules, scheduler) is EditMode-tested against grid fixtures without a scene.

**Nav-dirty:** `SandboxWorld.SetTile` marks affected chunks nav-dirty (`SandboxChunk.NavVersion`,
a monotonic counter; border edits also bump face neighbors); agents snapshot crossed-chunk
versions and recompute lazily on the next step when their path crosses changed cells.

**Despawn:** when the chase target's chunk leaves the loaded set, an agent idles and then despawns
after `despawnGraceSeconds` (10 s). Enemies never path into or spawn in unloaded chunks.

**Spawn constraints:** candidates must be air-with-support inside loaded chunks, outside the camera
view, within `[minSpawnDistance, maxSpawnDistance]` of the player, and respect underground light
thresholds. Never spawn in unloaded chunks.

**Out of scope for P2-AI-001:** combat/damage, flying/swimming archetypes, waypoint-graph scaling.
See the pathfinding page for constants and EditMode fixture requirements.
