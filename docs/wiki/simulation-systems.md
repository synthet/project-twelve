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

Lighting is a single-channel 0–15 cache on `SandboxTile`. `SandboxLightSolver` performs four-way
BFS propagation, taking the maximum contribution from strength-15 surface sunlight and registry
`LightEmission` sources. Air attenuation is 1; each tile definition supplies its entry cost, with
core opaque tiles using 3.

Tile edits use clear-then-refill over a radius-15 window, seeded from sources inside and unaffected
light outside. The `SandboxWorldLightGrid` adapter refuses unavailable cells, so cross-chunk
propagation reaches already-generated neighbors without creating new chunks. Light writes mark
rendering only and are recomputed rather than persisted. The current renderer maps tile light to
flat vertex tint; colored light, time-of-day scaling, and smooth corner gradients are deferred.

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
