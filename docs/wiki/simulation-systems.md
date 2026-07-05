---
type: Reference
title: Lighting, Liquids, and Simulation
description: Simulation subsystem contracts — tile lighting, the adopted P2-FLUID-001 liquid CA, and the shared active-set/budget discipline.
resource: wiki/simulation-systems.md
tags: [docs, wiki, simulation, fluids, p2]
timestamp: 2026-07-04T00:00:00Z
okf_version: 0.1
---

# Lighting, Liquids, and Simulation

## Lighting

Lighting should be tile-based, not a large field of Unity lights. Store light values in tiles or a chunk-local light buffer, propagate sunlight and emissive sources with breadth-first search, and rebuild only dirty light regions.

## Liquids

Liquids use a grid cellular automaton over the existing `SandboxTile.fluid` amount (0.0–1.0),
simulating **only active cells**. Water is the P2 scope (P2-FLUID-001); the full contract, tick
procedure, and constants live in [08 — Liquids](08-liquids.md) § "P2-FLUID-001 specification". In
brief:

- **Flow order** per active cell (bottom-up): down (gravity) → sideways (equalize) → up (pressure),
  using the finite-water stable-state transfer so mass is conserved by construction and columns
  equalize through U-tubes.
- **Active set:** a cell sleeps when its net transfer drops below `SettleEpsilon`; tile edits and
  neighbour changes re-wake cells via the `SetTile` flow, so still water costs ~zero.
- **Budget:** at most `MaxActiveCellsPerTick` cells per tick, overflow deferred to the next tick;
  target under the [quality-gates](quality-gates.md) simulation budget.
- **Determinism:** bottom-up rows with a seeded left↔right scan direction ⇒ same world + seed ⇒
  identical field.
- **Chunks & saves:** unloaded chunks are impassable (simulation pauses at the border and re-wakes
  on reload); `fluid` amounts persist for dirty chunks once [P2-SAVE-001](tickets/p2-save-001-specify-save-load-format-using-seed-plus-dirty-chunk-diffs.md) lands.

Implementation: `Assets/Scripts/Sandbox/Fluid/` (`SandboxFluidSimulator`, `SandboxFluidConstants`,
`ISandboxFluidGrid`, `SandboxWorldFluidGrid`, `SandboxFluidController`).

## Simulation Budget

Simulation systems should update by dirty region and distance from active players. Avoid full-world passes in `Update`. Both the liquid CA and pathfinding share this discipline: an **active/dirty set** plus a **per-tick work cap** (`MaxActiveCellsPerTick` for fluids, `MaxRequestsPerTick`/`MaxExpansionsPerRequest` for nav) so a spike of work is spread across frames rather than stalling one.
