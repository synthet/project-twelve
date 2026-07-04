---
type: Architecture
title: Gameplay Systems
description: Simulation/presentation split for player, inventory, and enemy systems, including the P2-AI-001 enemy nav and spawn contract.
resource: wiki/gameplay-systems.md
tags: [docs, wiki, gameplay, ai, p2]
timestamp: 2026-07-04T00:00:00Z
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
