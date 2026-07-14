---
type: Architecture
title: Gameplay Systems
description: Simulation/presentation split for player, inventory, pickup, and enemy systems, including the P2 inventory and AI contracts.
resource: wiki/gameplay-systems.md
tags: [docs, wiki, gameplay, inventory, ai, p2]
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

### Inventory hotbar HUD

The prototype scene includes a screen-space HUD for the inventory-backed tile-edit loop. Its
bottom hotbar exposes all ten placeable ground materials in order: dirt, grass, stone, Bricks A–D,
Frozen, Magma, and Sand. Number keys select any slot; the mouse wheel cycles through populated
slots only. A populated selection changes the registered solid tile placed with right-click. An
empty inventory slot disables placement without affecting left-click removal. Each populated slot
shows its live finite inventory count; the prototype starts with 100 of every ground material.

The compact single-row vitals panel reads `SandboxPlayerVitals`, which initializes from the
`core:player` entity definition and exposes damage/heal events for later combat integration. Its
ten hearts encode the full 100-point health state without a redundant numeric readout. No normal
gameplay damage source is part of this prototype. The selected hotbar slot is lifted and marked in
gold, with its item name shown briefly in a compact backed label immediately above the slot.

Seed, player tile, and owning chunk are presented separately as lightweight development telemetry
using the same coordinate conversion as world streaming and debug tools. The telemetry has an
independent visibility API and is suppressed by default in non-development player builds.

The HUD is presentation over `SandboxInventory`; placement consumption, pickup merging, and save
persistence remain in simulation/data layers. Crafting, drag/drop inventory screens, mana, and
time-of-day remain outside this prototype and production UI flows belong to P4-UX-001.

The HUD frame and fallback theme use the repo-owned sprites in `Assets/Sprites/UI/Generated`.
In the live scene, tile icons resolve the isolated autotile sprite from each registered vendor
ground sheet so inventory presentation matches world rendering. Rebuilding the HUD through
**ProjectTwelve → UI → Rebuild Sandbox HUD** preserves the serialized fallback theme.

## Inventory and Items

P2-INV-001 adopts a fixed 40-slot ordered `SandboxInventory`; slots 0–9 form the hotbar. Each slot
stores a stable registry item string ID plus a positive count no greater than that item's
`ItemDefinition.MaxStack`. Tile items default to a stack cap of 999. Adds fill matching non-full
stacks first, then empty slots in ascending order. A full inventory returns the unaccepted amount,
so a world pickup remains rather than silently disappearing.

| Constant | Adopted value | Rule |
|----------|---------------|------|
| `SlotCount` / `HotbarSlotCount` | 40 / 10 | Fixed ordered storage; first row is the hotbar |
| `DefaultMaxStack` | 999 | Default for tile items; definitions may lower it |
| `EditRange` | 6 tiles | Measured from player position to target-cell center |
| `EditIntervalSeconds` | 0.10 s | Controller-bound request cadence |
| `PickupRadius` | 2.5 tiles | Drops magnetize only inside this radius |
| `DropLifetimeSeconds` | 120 s | Uncollected drops despawn after this lifetime |

Right-click placement is a synchronous transaction: validate range, selected placeable item,
target air, and player-body occlusion; route the mutation through `SandboxWorld.SetTile`; consume
exactly one item only after the mutation succeeds. Left-click breaking validates a solid tile with
positive hardness and a registry-defined drop, routes the edit to air through the same choke point,
then creates a `SandboxItemPickup` at the cell center. Pickups merge into matching stacks before
empty slots and retain any remainder while the inventory is full.

The pure `SandboxInventoryEditService` owns inventory/world transaction rules through
`ISandboxInventoryWorld`. `SandboxPlayerController` owns reach, body-occlusion, input cadence, and
pickup spawning, preserving the validation seam that P3-NET-001 can move server-side. Inventory is
serialized as ordered `(slot index, item string ID, count)` entries in `SandboxSaveData`; legacy
saves without that field retain the current inventory instead of fabricating data.

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
