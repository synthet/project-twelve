---
type: Task
id: P2-TOOL-002
title: "[P2-TOOL-002] Implement debug overlays, inspector windows, and console per P2-TOOL-001 contract."
description: Overlay renderer with runtime toggles, chunk inspector and generation tuning windows, console commands, and zero-cost/stripping evidence.
status: open
phase: "Phase P2 — Core systems alpha"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/121"
github_issue_status: created
resource: wiki/tickets/p2-tool-002-implement-debug-overlays-inspector-windows-and-console.md
tags: [docs, wiki, ticket, tooling, debug, p2]
timestamp: 2026-07-16T00:00:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/13-tooling-testing.md"
  - "docs/wiki/tickets/p2-tool-001-specify-debug-tooling-for-chunks-generation-lighting-and-sav.md"
  - "docs/wiki/quality-gates.md"
---

# [P2-TOOL-002] Implement debug overlays, inspector windows, and console per P2-TOOL-001 contract.

## Open knowledge summary

Implementation follow-up to
[P2-TOOL-001](p2-tool-001-specify-debug-tooling-for-chunks-generation-lighting-and-sav.md), which
specified the debug tooling contract in `docs/wiki/13-tooling-testing.md` § "Debug tooling
contract" and landed the read-only Runtime MCP slice (`chunk_info`, `light_at`, `fluid_at`,
`SandboxWorld.TryGetChunkDebugState` / `TryGetExistingTile`, `SandboxFluidSimulator.IsAwake`).
This ticket builds the visual and interactive surfaces on those accessors.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** Pending creation via `scripts/sync_wiki_tickets_to_github.py`.
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a developer debugging P2 systems in Play Mode, I want the specified overlays, inspector
windows, and console commands actually rendered and interactive so that per-tile and per-chunk
state is visible at a glance.

## Requirements

### Functional requirements

1. Overlay renderer with individually toggleable overlays (hotkeys + console + MCP toggle tools):
   chunk borders + load radius, tile solidity, light heatmap, fluid amount + active set, collider
   rebuild flashes, per-chunk dirty flags — per the § "In-game overlays" table in
   `13-tooling-testing.md`.
2. Chunk inspector editor window (select by coordinate or click; tiles/flags/save-diff via
   `SandboxChunkDebugState`, including negative-coordinate chunks).
3. Generation tuning editor window (seed + pass parameters, live regenerate into a scratch world;
   never mutates a live save).
4. Console commands (minimum set from the contract): toggle overlays, teleport, set/get tile,
   force-generate chunk, dump chunk data, save/load named slot.
5. Runtime MCP overlay toggle/state tools mirroring the hotkey toggles.

### Non-functional requirements

1. Zero cost when disabled; debug classes excluded or stripped from release builds (profiler
   comparison as exit evidence).
2. Overlays render correctly across chunk borders and negative coordinates.
3. Read-only guarantee holds: overlays and inspectors consume snapshots, mutations route through
   `SandboxWorld.SetTile` / `TrySetDebugOverrideTile` only.

## Acceptance criteria

- Every overlay toggles on/off in Play Mode without errors; screenshots attached as exit evidence.
- Chunk inspector shows live data for a selected chunk, including a negative-coordinate chunk.
- Generation tuning window regenerates a scratch world from a changed seed without touching the
  active session's save.
- Profiler check: all overlays disabled ⇒ no measurable overhead vs a build without the tooling.
- MCP overlay toggle tools respond correctly (EditMode dispatcher tests) and appear in the tool
  list.
- The GitHub issue and this markdown ticket link to each other.

## Detailed technical specifications

### Scope

- Everything deferred from P2-TOOL-001: overlay renderer, editor windows, console, overlay MCP
  toggles, stripping/profiler evidence.
- Out of scope: network subscription overlays (P3) and pathfinding open/closed-set overlay
  (P2-AI-001), though both should reuse this overlay framework.

### Inputs and dependencies

- `SandboxWorld.TryGetChunkDebugState` / `TryGetExistingTile` (P2-TOOL-001 accessors).
- `SandboxFluidController.Simulator` / `SandboxFluidSimulator.IsAwake` for the fluid overlay.
- `Assets/Scripts/RuntimeMcp/` dispatcher pattern for the toggle tools.
- Light heatmap consumes P2-LIGHT-001 light values already stored on tiles.

### Verification plan

- Editor smoke tests + screenshots of each overlay and window.
- EditMode tests for MCP toggle tools and any pure overlay-state logic.
- Profiler comparison with tooling disabled.

## Documentation impact

- `docs/wiki/13-tooling-testing.md` — move overlay toggle tools from "specified, pending" to
  "implemented"; record hotkey map.
- `AGENTS.md` § In-game runtime MCP — add toggle tool names.

## Exit evidence checklist

- [ ] GitHub issue URL is recorded in this ticket.
- [ ] GitHub issue links back to this markdown ticket.
- [ ] Overlay screenshots and editor smoke-test notes attached.
- [ ] MCP dispatcher EditMode tests pass for toggle tools.
- [ ] Profiler zero-cost evidence recorded.
