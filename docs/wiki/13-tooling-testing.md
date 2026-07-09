---
type: Guide
title: Tooling, Testing & Profiling
description: Debug visualizations, editor tools, automated test priorities, and profiling targets for the sandbox.
resource: wiki/13-tooling-testing.md
tags: [docs, wiki, testing, tooling, profiling]
timestamp: 2026-07-01T00:00:00Z
okf_version: 0.1
---

# 13 — Tooling, Testing & Profiling

> **Status:** Planning.
> **Decisions:** Debug views early; unit-test pure algorithms; profile the known cliffs.
> **Invariants:** Pure data algorithms (coords, lighting, fluids, gen, serialization) are unit-tested.

## Manual checks

For reviewer-executed play-mode validation of the P1 vertical slice (movement, streaming,
determinism, editing, collision, visuals), follow the
[P1 Vertical-Slice Demo Runbook](p1-vertical-slice-demo.md). Merge-blocking manual checks and
profiler targets live in [Quality Gates](quality-gates.md).

## WebGL prototype viewer

The [WebGL Prototype Viewer Tool](webgl-prototype-viewer-tool.md) (`tools/webgl-viz`) turns the existing
`tools/world-viz` and `tools/tile-viz` JavaScript visualizers into a browser-hosted proof-of-concept
review surface. The intent is a lightweight world microscope for loading seeds, saves, tile-space
captures, and autotile reports with pan/zoom, tile inspection, overlay toggles, and shareable evidence
exports while keeping Unity as the authoritative runtime.

## Debug visualizations (build these early)

Most sandbox bugs are invisible without overlays. Add toggleable gizmos/overlays for:

- **Chunk borders** and load/unload radius.
- **Tile IDs / solidity** (which tiles count as solid — verifies collision & nav agree).
- **Light values** (heatmap over the grid) — verifies [Lighting](06-lighting.md) propagation.
- **Fluid amounts and the active set** — verifies [Liquids](08-liquids.md) wake/sleep.
- **Collider rebuild regions** — confirms rebuilds are chunk-local, not global.
- **Network chunk subscriptions** and applied-delta sequence — for [Multiplayer](10-multiplayer.md).
- **Pathfinding** open/closed sets and final path — for [Pathfinding](09-pathfinding.md).

Plus an in-game **console/cheats**: spawn items, toggle overlays, teleport to test far regions,
force-generate a chunk, dump a chunk's data.

**Runtime MCP (implemented):** while Play Mode or a desktop build is running, agents can connect to
`http://127.0.0.1:8765/mcp` (`project-twelve-ingame-mcp` in `.cursor/mcp.json`) to read debug state
(`player_state`, `world_info`, `tile_at`, `perf`) and drive the player (`player_move`, `player_jump`,
`player_teleport`, `world_set_tile`). Loopback-only; see [AGENTS.md](../../AGENTS.md) § In-game runtime MCP.

## Editor tools

- Use Unity's **Tile Palette / Scene View** for hand-authored set-pieces and prototyping; most of
  the world is procedural and tested in Play Mode.
- **Custom editor windows** for generation parameter tuning (seed, noise scales, biome weights)
  with live regenerate.
- A **chunk inspector** window to view a chunk's tiles/light/fluid arrays.

## Automated tests (Unity Test Runner / NUnit)

Prioritize **pure, deterministic** logic — it's where correctness bugs hide and where tests are cheap:

| Area | What to assert |
|------|----------------|
| Coordinate conversion | `local ∈ [0,Size)²` and round-trips for negative coords (see [Data Models](02-data-models.md)) |
| Chunk lookup / edit | `SetTile` then `GetTile` returns the value; dirty flags set |
| Lighting | Propagation matches expected falloff; dirty-region relight == full relight result |
| Fluids | **Mass conservation** within epsilon; settles; no infinite jitter |
| Generation | Same seed ⇒ identical world; on-demand chunk == full-world chunk |
| Save/load | Round-trip equality; old-version save migrates correctly |
| Network serialization | Tile-delta encode/decode round-trips; sequence ordering |

Keep these tests free of Unity scene dependencies where possible so they run fast in CI.

## Profiling targets (the known cliffs)

Use the Unity **Profiler**; watch specifically for:

- **Tilemap.SetTile / mesh rebuild** time while digging (chunk-local? budgeted per frame?).
- **Physics2D** steps, especially any `CompositeCollider2D` rebuild (must be chunk-local — see
  [Collision](05-collision-physics.md)).
- **Lighting** update cost on edits and time-of-day (dirty-region only?).
- **Fluid** iteration cost (active-set only?).
- **Draw calls** per chunk and GC **allocations** (avoid per-frame allocations in hot loops).

Test edge cases: max world size, dense dynamic objects, bulk edits (explosions), and — for
multiplayer — latency/packet loss. If targeting mobile/console, profile on-device.

## See also

- [Architecture](01-architecture.md) — per-frame work budgets.
- [Lighting](06-lighting.md), [Liquids](08-liquids.md), [Procedural Generation](07-procedural-generation.md) —
  the algorithms under test.
- [Roadmap](14-roadmap.md) — when to invest in tooling vs features.
