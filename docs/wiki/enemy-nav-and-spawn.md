---
type: Specification
title: Enemy Navigation and Spawn Rules
description: P2 spec for grid A* platformer pathfinding over the shared solidity graph plus bounded, off-camera enemy spawning.
resource: wiki/enemy-nav-and-spawn.md
tags: [docs, wiki, ai, pathfinding, spawning, p2]
timestamp: 2026-07-03T14:53:07Z
okf_version: 0.1
---

# Enemy Navigation and Spawn Rules

> **Ticket:** [P2-AI-001](tickets/p2-ai-001-specify-enemy-spawn-and-pathfinding-rules.md) ·
> **Issue:** [synthet/project-twelve#32](https://github.com/synthet/project-twelve/issues/32) ·
> **Status:** Draft spec for review (values below are *proposed* — confirm before `/implement`).
> **Depends on:** [09 — Pathfinding](09-pathfinding.md) (movement model), P2-VISUAL-003 (monster visuals),
> P2-LIGHT-001 (light values — **not yet landed**; see [Light gate](#light-gate)).

## Summary

Enemies navigate the sandbox with **grid A\*** over the tile solidity graph and spawn on a bounded,
off-camera cadence. Walkability derives from the **same** `SandboxTile.IsSolid` data collision uses —
there is no second solidity grid. The search expands platformer edges (walk / jump / fall) sized to a
single "walker" archetype, is bounded by per-request and per-tick budgets, and recomputes lazily when
terrain edits mark a chunk nav-dirty. Spawning picks air-with-support cells inside the loaded-chunk set
but outside the camera, within a distance band from the player, gated by an underground condition, and
capped by a live-population limit. The pathfinding core is pure C# over a grid accessor so it is fully
EditMode-testable without scenes.

## Users / stakeholders

- **Gameplay developers** building enemy archetypes on a stable nav/spawn contract.
- **Content/level designers** who need believable enemy movement across player-dug terrain.
- **QA** validating the vertical-slice demo (P1-QA-001) where enemies chase the player.
- **Downstream tickets:** P2-VISUAL-003 (monster visuals consume the spawn hook), P4-CONTENT-001
  (combat/loot builds on this behavior).

## Non-goals

Explicitly **out of scope** for P2-AI-001 (recorded as follow-ups, not silently dropped):

- **Flying / swimming / climbing** movement models — walker archetype only.
- **Combat, damage, knockback, loot** — deferred to P4-CONTENT-001.
- **Boss / group / flocking behavior** and local avoidance between agents.
- **Waypoint / hierarchical nav-graph scaling** and third-party pathfinding packages
  (A\* Pathfinding Project, Unity NavMesh) — kept as scaling options in [09 — Pathfinding](09-pathfinding.md).
- **Real light propagation** — owned by P2-LIGHT-001; this spec consumes a hook and falls back to a
  depth gate (see [Light gate](#light-gate)).
- **Ladders / ropes / one-way platforms / doors** as nav edges — walk/jump/fall only for now.

## User stories

- As a **gameplay developer**, I want a pure-C# A\* that returns a step list my controller can follow
  without teleports, so enemies move believably across platforms and caves.
- As a **player**, I want enemies to re-route through a tunnel I just dug (and give up when I seal it),
  so the world feels reactive.
- As a **level designer**, I want enemies to refuse jumps taller than the archetype can clear, so paths
  never suggest impossible movement.
- As a **player**, I want enemies to appear from the dark nearby but never pop into existence on screen,
  so spawning feels dangerous rather than cheap.
- As **QA**, I want deterministic EditMode fixtures for movement and spawn selection, so regressions are
  caught without running a scene.

## Movement model (walker archetype)

Nodes are tile coordinates. A cell is a **standable** node when it is air (`!IsSolid`) with a solid
support tile directly below. Neighbor expansion produces these edges:

| Edge | Rule |
|------|------|
| **Walk** | To an adjacent standable cell at the same height (or step up/down 1 tile onto ground). |
| **Jump** | Up to `maxJumpHeight` tiles vertically and across a gap of up to `maxJumpGap` tiles, provided the arc's tiles are air (no ceiling clip). Modeled as reachable-arc edges, not free flight. |
| **Fall** | Off a ledge, straight down until the first support, up to `maxFallDistance` tiles. |

Heuristic: **octile** distance (admissible with diagonal jump/fall arcs). Tie-breaking is deterministic
(stable node ordering) so identical world + budgets yield identical paths — required for reliable fixtures.

## Constants (proposed — confirm in review)

Named constants live on the walker archetype / a `NavConfig`; no magic numbers in the search.

| Constant | Proposed | Rationale |
|----------|---------:|-----------|
| `maxJumpHeight` | `3` tiles | Clears a 2-tall wall + headroom; Terraria-like walker feel. |
| `maxJumpGap` | `4` tiles | Crosses a small pit at jump apex without looking superhuman. |
| `maxFallDistance` | `16` tiles | Fall damage is out of scope, but an unbounded fall edge bloats the graph; 16 keeps search bounded and paths sane. Documented as a *pathing* cap, not a survival limit. |
| `maxExpansionsPerRequest` | `2000` nodes | Covers agent-range routes; on exhaustion → local steering fallback, no frame stall. |
| `maxRequestsPerTick` | `4` | Staggers recompute across frames per [09 — Pathfinding](09-pathfinding.md). |
| `minSpawnDistance` | `24` tiles | Outside a typical ~48-tile-wide view so spawns are off-camera. |
| `maxSpawnDistance` | `64` tiles | Keeps spawns inside the loaded set and "nearby dangerous". |
| `spawnLightThreshold` | `5` (0–15) | Underground spawns require light ≤ 5. **Stubbed** until P2-LIGHT-001. |
| `populationCap` | `8` | Live enemies per active area (loaded set around the player). |
| `spawnInterval` | `1.0` s | Spawn-attempt cadence. |

## Dynamic terrain (nav invalidation)

- `SandboxChunk` gains **`NeedsNavRebuild`** alongside `NeedsRenderRebuild`/`NeedsColliderRebuild`.
- The `SetTile` flow sets `NeedsNavRebuild` on the edited chunk and its face-adjacent neighbors (reusing
  the existing `MarkBorderNeighborsDirty` path — no new divergent marking logic).
- Invalidation is **not** synchronous inside the edit. An agent holding a path that crosses a changed
  cell recomputes **lazily on its next step**. Sealing a route invalidates the in-flight path and
  triggers recompute; if no path is found within budget, the agent falls back to local steering.
- Enemies never path into or spawn in **unloaded** chunks. An agent whose target leaves the loaded set
  idles, then despawns after a grace period (documented rule, value TBD in review).

## Spawn rules

A candidate cell is eligible when **all** hold:

1. **Air with support** — the cell is `!IsSolid` with a solid tile directly below.
2. **Loaded, off-camera** — inside the loaded-chunk set and outside the camera view rect.
3. **Distance band** — Chebyshev/Euclidean distance from the player in `[minSpawnDistance, maxSpawnDistance]`.
4. **Underground condition** — see [Light gate](#light-gate).
5. **Under population cap** — live enemy count in the active area `< populationCap`.

Selection runs every `spawnInterval`, samples candidates, and spawns at most one per attempt.

### Light gate

P2-LIGHT-001 has **not landed**, so no real light values exist yet. This spec wires a
`spawnLightThreshold` hook but gates on **depth** in the interim:

- **Now:** eligible underground = tile `y` below `surfaceLevel - undergroundMargin`
  (reuse the terrain generator's surface height; `undergroundMargin` TBD in review).
- **Later:** when P2-LIGHT-001 lands, replace the depth check with `tile.light <= spawnLightThreshold`.
  A follow-up task must be filed to flip the gate. The hook shape stays stable so only the predicate changes.

## Integration seams

- **Solidity:** `SandboxTile.IsSolid` — single source of truth shared with collision.
- **Grid access + edit hook:** `SandboxWorld` (`SetTile`, loaded-chunk set); nav-dirty added to the
  existing dirty-marking path.
- **Visuals:** spawned enemies get presentation via the P2-VISUAL-003 contract
  (`MonsterSpawnHelper` / `MonsterVisualCatalog`) with locomotion through `MonsterLocomotionDriver`.
  **Behavior code must not reference vendor sprites directly.**
- **Pathfinding core:** pure C# over a grid snapshot/accessor — no `MonoBehaviour`, no scene deps.

## Acceptance criteria

- **Nav respects limits** — movement honors terrain, `maxJumpHeight`/`maxJumpGap`/`maxFallDistance`, and
  the loaded-world boundary per the constants table.
- **EditMode movement fixtures** (handcrafted grids): flat walk; jump up 1–N tiles; **refuse** a jump
  above `maxJumpHeight`; cross a gap ≤ `maxJumpGap`; fall from a ledge; a blocked route returns **no-path**
  within `maxExpansionsPerRequest`.
- **EditMode dynamic test** — carving a tunnel through a wall makes a previously no-path route succeed
  after the nav-dirty update; sealing it invalidates an in-flight path and triggers recompute.
- **EditMode spawn table** — candidate selection respects the distance band, air+support, the
  underground/light gate, and `populationCap` across a scenario table.
- **Play-mode chase** — an enemy chases the player across platforms and through a player-dug tunnel
  without walking through walls or hovering over gaps; spawns never appear inside the camera view.
- **Determinism** — identical world + budgets return an identical path (stable tie-breaking).
- **Linkage & evidence** — the GitHub issue and this ticket link to each other; exit evidence records the
  commit, verification commands, and reviewer findings.

## Open questions

1. **Constant values** — are the proposed `maxJump*` / spawn-band / `populationCap` values right for the
   intended difficulty, or are there specific gameplay targets? (Blocking sign-off, not implementation.)
2. **Fall model** — cap fall at `maxFallDistance = 16`, or allow unlimited fall now that fall damage is
   out of scope? (Affects graph size and path shape.)
3. **Despawn grace** — how long may an agent idle after its target leaves the loaded set before despawn?
4. **Underground margin** — `undergroundMargin` below surface for the interim depth gate; and confirm the
   surface-height source to reuse from the terrain generator.
5. **Spawn distance metric** — Chebyshev vs Euclidean for the distance band (affects corner spawns).

## Verification plan

- EditMode pathfinding fixtures across platforms, caves, and blocked routes (pure grid tests).
- EditMode spawn-rule table tests.
- Play-mode chase scenario captured for exit evidence.

Commands (per [CLAUDE.md](../../CLAUDE.md) § Commands):

```bash
Unity -batchmode -quit -projectPath . -runTests -testPlatform EditMode \
  -testResults TestResults/editmode.xml -logFile Logs/unity-editmode-tests.log
```

## Documentation impact (applied during `/implement`)

- [gameplay-systems.md](gameplay-systems.md) — replace the "Enemies and Pathfinding" stub with a link to
  this contract.
- [09 — Pathfinding](09-pathfinding.md) — record the adopted movement constants and search budgets.
- P2-VISUAL-003 ticket — confirm the spawn API cross-link stays accurate.
- File follow-ups: flip the light gate after P2-LIGHT-001; flying archetype; combat.

## See also

- [09 — Pathfinding](09-pathfinding.md) — A\* sketch, movement-edge guidance, scaling options.
- [Rendering and Collision](rendering-and-collision.md) — shared chunk-local rebuild + dirty-flag pattern.
- [World and Chunk Data](world-and-chunk-data.md) — chunk lifecycle and coordinate conversions.
