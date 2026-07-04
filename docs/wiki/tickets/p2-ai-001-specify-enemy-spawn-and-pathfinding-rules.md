---
type: Task
id: P2-AI-001
title: "[P2-AI-001] Specify enemy spawn and pathfinding rules."
description: Grid A* navigation with jump/fall edges derived from collision solidity, plus spawn rules bounded by distance, light, and loaded chunks.
status: in_progress
phase: "Phase P2 — Core systems alpha"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/32"
github_issue_status: created
resource: wiki/tickets/p2-ai-001-specify-enemy-spawn-and-pathfinding-rules.md
tags: [docs, wiki, ticket, ai, pathfinding, p2]
timestamp: 2026-07-04T16:46:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/spec-driven-development-tasks.md"
  - "docs/wiki/09-pathfinding.md"
  - "docs/wiki/gameplay-systems.md"
  - "docs/wiki/tickets/p2-visual-003-specify-monster-visual-integration-for-enemies.md"
---

# [P2-AI-001] Specify enemy spawn and pathfinding rules.

## Open knowledge summary

This ticket specifies enemy navigation and spawning for the P2 alpha: grid A* over the tile
solidity graph per `docs/wiki/09-pathfinding.md` — with platformer movement edges (walk, jump up
to N tiles, fall from ledges) — and spawn rules that respect terrain, light, distance bands, and
the loaded-chunk boundary. Navigation derives walkability from the **same** solidity data
collision uses (`SandboxTile.IsSolid`), never a second grid. Enemy visuals are owned by
P2-VISUAL-003 (`MonsterSpawnHelper`/`MonsterVisualCatalog`); this ticket owns behavior.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** [synthet/project-twelve#32](https://github.com/synthet/project-twelve/issues/32)
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a gameplay developer on the P2 milestone, I want spawn and pathfinding rules specified against
the shared solidity grid so that enemies navigate platforms, caves, and player-modified terrain
believably, and spawning feels dangerous underground without popping in on screen.

## Requirements

### Functional requirements

1. Nav graph: nodes are tile positions; walkable/support cells derive from `SandboxTile.IsSolid`
   (single source of truth with collision, per `09-pathfinding.md`). Edges encode the movement
   model: **walk** along solid ground, **jump** up to `maxJumpHeight` tiles and across
   `maxJumpGap` tiles, **fall** from ledges up to `maxFallDistance` (unlimited fall is allowed
   only if fall damage is out of scope — record the choice).
2. A*: octile/Manhattan heuristic; neighbor expansion respects the movement-model edges; a
   returned path is followable by the enemy's controller without teleports.
3. Dynamic terrain: tile edits mark affected chunks **nav-dirty** as part of the `SetTile` flow
   (alongside render/collider flags); an agent whose path crosses a changed cell recomputes
   lazily on its next step, not synchronously inside the edit.
4. Path search is bounded: max node expansions per request and max requests per tick (named
   constants); on budget exhaustion the agent falls back to local steering (walk toward target,
   stop at ledges) rather than stalling the frame.
5. Spawn rules (all named constants): candidate cells must be air with solid support, inside the
   loaded-chunk set but **outside the camera view**, within a `[minSpawnDistance, maxSpawnDistance]`
   band from the player; underground spawns require light below `spawnLightThreshold` (consumes
   P2-LIGHT-001 values); per-area population cap and spawn-attempt cadence are specified.
6. Enemies never path into or spawn in unloaded chunks; agents whose target leaves the loaded set
   idle/despawn per a documented rule.
7. Spawned enemies get visuals via the P2-VISUAL-003 contract (`MonsterSpawnHelper`), with
   locomotion driven through `MonsterLocomotionDriver` — behavior code must not reference vendor
   sprites directly.

### Non-functional requirements

1. Pathfinding core is pure C# over a grid snapshot/accessor (EditMode-testable, no scenes).
2. No per-frame full recompute: recompute on invalidation or arrival, staggered across frames.
3. Determinism where cheap: given identical world + budgets, the same request returns the same
   path (stable tie-breaking), keeping fixtures reliable.

## Acceptance criteria

- Navigation respects terrain, jump/fall limits, and loaded-world boundaries per the constants
  table in the spec.
- EditMode fixtures (handcrafted grids): flat walk; jump up 1–N tiles; refuse jump above
  `maxJumpHeight`; cross a gap ≤ `maxJumpGap`; fall from a ledge; blocked route returns no-path
  within the expansion budget.
- EditMode dynamic test: carving a tunnel through a wall makes a previously no-path route
  succeed after the nav-dirty update; sealing it invalidates an in-flight path and triggers
  recompute.
- EditMode spawn tests: candidate selection respects distance band, support/air, light threshold,
  and population cap over a table of scenarios.
- Play-mode: an enemy chases the player across platforms and through a player-dug tunnel without
  walking through walls or hovering over gaps; spawns never appear inside the camera view.
- The GitHub issue and this markdown ticket link to each other.
- Exit evidence records the commit, verification commands, and reviewer findings.

## Detailed technical specifications

### Scope

- Nav-graph derivation, A* with platformer edges, nav-dirty invalidation, spawn-candidate
  selection, and one walker enemy archetype wired to the monster visual contract.
- Out of scope: flying/swimming movement models, combat/damage (P4-CONTENT-001), boss behavior,
  waypoint-graph scaling and third-party pathfinding packages (recorded as scaling options).

### Inputs and dependencies

- `Assets/Scripts/Sandbox/SandboxTile.cs` — `IsSolid` (shared solidity source).
- `Assets/Scripts/Sandbox/SandboxWorld.cs` — grid access + nav-dirty hook in `SetTile`.
- P2-LIGHT-001 — light values for spawn threshold (if not landed, spec the hook and gate on
  depth instead, with a recorded follow-up).
- P2-VISUAL-003 — `MonsterSpawnHelper`, `MonsterVisualCatalog`, `MonsterLocomotionDriver`.
- `docs/wiki/09-pathfinding.md` — A* sketch, movement-edge guidance, scaling pitfalls.

### Constants table (values finalized in the spec page)

| Constant | Meaning |
|----------|---------|
| `maxJumpHeight` / `maxJumpGap` | Jump reach in tiles for the walker archetype |
| `maxFallDistance` | Ledge-drop limit (or unlimited, documented) |
| `maxExpansionsPerRequest`, `maxRequestsPerTick` | Search budgets |
| `minSpawnDistance`, `maxSpawnDistance` | Spawn band radii (tiles) |
| `spawnLightThreshold` | Max light (0–15) for underground spawns |
| `populationCap`, `spawnInterval` | Live-enemy cap per area and attempt cadence |

### Verification plan

- EditMode pathfinding fixtures across platforms, caves, and blocked routes (pure grid tests).
- EditMode spawn-rule table tests.
- Play-mode chase scenario per acceptance criteria, captured for exit evidence.

## Documentation impact

- `docs/wiki/gameplay-systems.md` — enemies/pathfinding section updated to the specified contract.
- `docs/wiki/09-pathfinding.md` — record adopted movement-model constants and budgets.
- P2-VISUAL-003 ticket — confirm the spawn API cross-link stays accurate.
- Update `docs/wiki/spec-driven-development-tasks.md` if task scope or sequencing changes.

## Exit evidence checklist

- [x] GitHub issue URL is recorded in this ticket.
- [x] GitHub issue links back to this markdown ticket.
- [x] Nav/spawn contract documented in `gameplay-systems.md` and `09-pathfinding.md` before
      implementation (P2-AI-001 specification section added 2026-07-03).
- [x] Implementation landed under `Assets/Scripts/Sandbox/Nav/` (pathfinder, scheduler, spawn
      rules, world adapter, `SandboxEnemySpawner`/`SandboxEnemyAgent`) with nav-dirty wiring in
      `SandboxChunk`/`SandboxWorld` (2026-07-04; adopted decisions recorded in
      `09-pathfinding.md` § "Implementation notes").
- [x] Pathfinding and spawn EditMode fixtures pass. EditMode run 2026-07-04: 199/199 passed — see `TestResults/editmode.xml`, `Logs/unity-editmode-tests.log` (Unity 6000.5.1f1).
- [ ] Play-mode chase capture attached. *(Requires the Unity Editor; see acceptance criteria.)*
- [ ] Follow-up tasks created for flying archetypes, combat, and nav scaling. *(Combat/loot is
      already tracked by P4-CONTENT-001 (#44); flying/swimming archetypes and waypoint-graph
      scaling still need tickets — deferred to maintainer triage.)*
