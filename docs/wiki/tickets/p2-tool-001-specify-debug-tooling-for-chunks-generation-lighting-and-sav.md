---
type: Task
id: P2-TOOL-001
title: "[P2-TOOL-001] Specify debug tooling for chunks, generation, lighting, and saves."
description: Toggleable debug overlays, chunk inspector, generation tuning window, and console commands layered on the existing Runtime MCP surface.
status: in_progress
phase: "Phase P2 — Core systems alpha"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/39"
github_issue_status: created
resource: wiki/tickets/p2-tool-001-specify-debug-tooling-for-chunks-generation-lighting-and-sav.md
tags: [docs, wiki, ticket, tooling, debug, p2]
timestamp: 2026-07-01T00:00:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/spec-driven-development-tasks.md"
  - "docs/wiki/13-tooling-testing.md"
  - "docs/wiki/quality-gates.md"
---

# [P2-TOOL-001] Specify debug tooling for chunks, generation, lighting, and saves.

## Open knowledge summary

This ticket specifies the debug tooling that `docs/wiki/13-tooling-testing.md` says to build
early, because most sandbox bugs (wrong dirty flags, light leaks, fluid jitter, non-local
rebuilds) are invisible without overlays. Deliverables: toggleable in-game overlays (chunk
borders, tile solidity, light heatmap, fluid amounts + active set, collider rebuild regions), a
chunk inspector, a generation tuning editor window, save inspection commands, and extensions to
the already-implemented Runtime MCP surface (`Assets/Scripts/RuntimeMcp/GameplayMcpTools.cs`:
`player_state`, `world_info`, `tile_at`, `perf`, `player_move`, `world_set_tile`). Tooling reads
runtime state; it must not alter runtime contracts.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** [synthet/project-twelve#39](https://github.com/synthet/project-twelve/issues/39)
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a developer debugging P2 systems (lighting, fluids, generation, saves), I want overlays and
inspection tools that expose per-tile and per-chunk state at a glance so that I can see why a
system misbehaves instead of inferring it from symptoms.

## Requirements

### Functional requirements

1. In-game overlays, individually toggleable at runtime (hotkeys + console), rendering above the
   world: (a) chunk borders + load radius, (b) tile solidity, (c) light values as a heatmap
   (P2-LIGHT-001), (d) fluid amount + active-set membership (P2-FLUID-001), (e) collider rebuild
   flashes proving rebuilds are chunk-local, (f) dirty-flag state per chunk
   (render/collider/save).
2. Chunk inspector (editor window): select a chunk by coordinate or click; view its tile array
   (id/light/fluid/metadata), dirty flags, and save-diff status.
3. Generation tuning window: seed + pass parameters (P2-GEN-001 settings object) with live
   regenerate into a scratch world — never mutating a live save.
4. Console commands (minimum): toggle overlays, teleport, set/get tile, force-generate chunk,
   dump chunk data, save/load to a named slot, spawn item/enemy (as those systems land).
5. Runtime MCP extensions mirroring the above for agent-driven debugging: `chunk_info` (flags +
   diff status), `light_at`, `fluid_at`, overlay toggles. Loopback-only constraint and tool
   registration pattern per `McpDispatcher`/`McpTool`.
6. Read-only guarantee: debug reads never mutate simulation state; mutating commands (teleport,
   set tile) route through the same public APIs gameplay uses (`SandboxWorld.SetTile`), never
   private backdoors — tooling must not create a second edit path.

### Non-functional requirements

1. Zero cost when disabled: overlays and inspectors allocate nothing and draw nothing unless
   toggled; debug assemblies/classes are excluded or stripped from release builds.
2. Overlays render correctly across chunk borders and negative coordinates.
3. New MCP tools follow the existing dispatcher/serialization pattern and are covered by
   `RuntimeMcpDispatcherTests.cs`-style EditMode tests.

## Acceptance criteria

- Tools expose enough state to debug chunks, generation, lighting, and saves without modifying
  runtime contracts (the read-only guarantee holds in review).
- Editor smoke test: every overlay toggles on/off in Play mode without errors; screenshots of
  each overlay attached as exit evidence.
- Chunk inspector shows live tile/flag data for a selected chunk, including a negative-coordinate
  chunk.
- Generation tuning window regenerates a scratch world from a changed seed without touching the
  active session's save.
- New Runtime MCP tools respond correctly (EditMode dispatcher tests) and appear in the tool list.
- Profiler check: all overlays disabled ⇒ no measurable overhead vs a build without the tooling.
- The GitHub issue and this markdown ticket link to each other.
- Exit evidence records the commit, verification commands, and reviewer findings.

## Detailed technical specifications

### Scope

- Overlay renderer, chunk inspector window, generation tuning window, console commands, and MCP
  tool extensions listed above.
- Out of scope: network subscription overlays (P3), pathfinding open/closed-set overlay (lands
  with P2-AI-001 but may reuse this overlay framework), performance dashboards beyond the
  existing `perf` tool.

### Inputs and dependencies

- `Assets/Scripts/RuntimeMcp/` — `RuntimeMcpServer`, `McpDispatcher`, `GameplayMcpTools` patterns.
- `Assets/Scripts/Sandbox/SandboxWorld.cs`, `SandboxChunk.cs` — state to expose (flags, tiles).
- P2-LIGHT-001 / P2-FLUID-001 — data the heatmap/fluid overlays visualize (overlay framework can
  land first with borders/solidity/dirty-flag views).
- `Assets/Tests/EditMode/RuntimeMcpDispatcherTests.cs` — test pattern for new tools.

### Verification plan

- Editor smoke tests + screenshots of each overlay and window (exit evidence).
- EditMode tests for new MCP tools (dispatch, serialization, read-only behavior).
- Profiler comparison with tooling disabled (zero-cost requirement).

## Documentation impact

- `docs/wiki/13-tooling-testing.md` — overlay/command/window inventory updated to the specified
  contract; Runtime MCP tool list extended.
- `AGENTS.md` § In-game runtime MCP — new tool names.
- Update `docs/wiki/spec-driven-development-tasks.md` if task scope or sequencing changes.

## Exit evidence checklist

- [x] GitHub issue URL is recorded in this ticket.
- [x] GitHub issue links back to this markdown ticket.
- [x] Tooling inventory documented in `13-tooling-testing.md` before implementation
      (§ "Debug tooling contract (P2-TOOL-001)").
- [ ] Overlay screenshots and editor smoke-test notes attached — deferred to
      [P2-TOOL-002](p2-tool-002-implement-debug-overlays-inspector-windows-and-console.md) with the
      overlay implementation.
- [ ] MCP dispatcher EditMode tests pass for new tools (`RuntimeMcpChunkDebugToolsTests`; verified
      by the enforced CI EditMode run on the PR).
- [x] Follow-up tasks created: [P2-TOOL-002](p2-tool-002-implement-debug-overlays-inspector-windows-and-console.md)
      (overlays, windows, console, stripping/profiler evidence; GitHub issue pending
      `sync_wiki_tickets_to_github.py`); network/pathfinding overlays stay with P3 / P2-AI-001 per spec.
