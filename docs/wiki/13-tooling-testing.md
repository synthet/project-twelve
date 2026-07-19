---
type: Guide
title: Tooling, Testing & Profiling
description: Debug visualizations, editor tools, automated test priorities, and profiling targets for the sandbox.
resource: wiki/13-tooling-testing.md
tags: [docs, wiki, testing, tooling, profiling]
timestamp: 2026-07-16T00:00:00Z
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

## Debug tooling contract (P2-TOOL-001)

Most sandbox bugs are invisible without overlays. This section is the specified contract for
[P2-TOOL-001](tickets/p2-tool-001-specify-debug-tooling-for-chunks-generation-lighting-and-sav.md)
(issue [#39](https://github.com/synthet/project-twelve/issues/39)); it binds every debug surface —
overlays, windows, console, and Runtime MCP.

### Cross-cutting invariants

- **Read-only guarantee.** Debug reads never mutate simulation state and never generate chunks on
  query (`SandboxWorld.TryGetExistingTile` / `TryGetChunkDebugState`, not `GetTile`). Mutating
  commands (teleport, set tile) route through the same public APIs gameplay uses
  (`SandboxWorld.SetTile`, `TrySetDebugOverrideTile`) — tooling must not create a second edit path.
  Inspectors receive copy-by-value snapshots (`SandboxChunkDebugState`), never the live
  `SandboxChunk`.
- **Zero cost when disabled.** Overlays and inspectors allocate nothing and draw nothing unless
  toggled; debug assemblies/classes are excluded or stripped from release builds. MCP writes are
  already gated by `SandboxWorld.CanUseDebugOverrides` (Editor / development builds only).
- **Coordinate correctness.** Every overlay and inspector must render/report correctly across
  chunk borders and at negative chunk coordinates.

### In-game overlays (toggleable at runtime: hotkeys + console)

| Overlay | Shows | Verifies |
|---------|-------|----------|
| Chunk borders | Chunk grid + load/unload radius | Streaming window ([Architecture](01-architecture.md)) |
| Tile solidity | Which tiles count as solid | Collision & nav agree ([Collision](05-collision-physics.md)) |
| Light heatmap | Per-tile light values | [Lighting](06-lighting.md) propagation (P2-LIGHT-001) |
| Fluid | Amount + active-set membership | [Liquids](08-liquids.md) wake/sleep (P2-FLUID-001) |
| Collider rebuild flashes | Regions rebuilt this frame | Rebuilds are chunk-local, not global |
| Dirty flags | Per-chunk render/collider/save flags | Dirty-flag bookkeeping |
| Network chunk subscriptions | Applied-delta sequence | [Multiplayer](10-multiplayer.md) — lands in P3 |
| Pathfinding sets | Open/closed sets + final path | [Pathfinding](09-pathfinding.md) — lands with P2-AI-001 |

Each overlay toggles individually; toggles are also exposed as Runtime MCP tools once the overlay
framework lands (see below).

### Console commands (minimum set)

Toggle overlays, teleport, set/get tile, force-generate chunk, dump chunk data, save/load to a
named slot, spawn item/enemy (as those systems land).

### Editor windows

- **Chunk inspector:** select a chunk by coordinate or click; view its tile array
  (id/light/fluid/metadata), dirty flags, and save-diff status — including negative-coordinate
  chunks.
- **Generation tuning:** seed + pass parameters (P2-GEN-001 settings object) with live regenerate
  into a **scratch world** — never mutating a live save.
- Unity's **Tile Palette / Scene View** remains the tool for hand-authored set-pieces; most of the
  world is procedural and tested in Play Mode.

### Runtime MCP debug tools

While Play Mode or a desktop build is running, agents connect to `http://127.0.0.1:8765/mcp`
(`project-twelve-ingame-mcp`). Loopback-only; new tools follow the `McpDispatcher`/`McpTool`
registration pattern and are covered by `RuntimeMcpDispatcherTests.cs`-style EditMode tests.
See [AGENTS.md](../../AGENTS.md) § In-game runtime MCP for the full tool table.

- **Implemented reads:** `player_state`, `world_info`, `tile_at`, `tiles_area`, `perf`, autotile
  debug tools, and the P2-TOOL-001 extensions `chunk_info` (flags + save-diff status, never
  generates), `light_at`, `fluid_at` (amount + active-set membership via
  `SandboxFluidController.Simulator`).
- **Implemented writes (debug-override gated):** `player_move`, `player_jump`, `player_teleport`,
  `world_set_tile`.
- **Specified, pending overlay framework:** overlay toggle/state tools mirroring the hotkey
  toggles.

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
