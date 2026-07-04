---
type: Reference
title: Lighting, Liquids, and Simulation
description: Overview of tile-based lighting, active-cell liquid simulation, and dirty-region simulation budgeting for the sandbox.
resource: wiki/simulation-systems.md
tags: [docs, wiki, simulation, lighting, fluids]
timestamp: 2026-07-04T00:00:00Z
okf_version: 0.1
---

# Lighting, Liquids, and Simulation

## Lighting

Lighting should be tile-based, not a large field of Unity lights. Store light values in tiles or a chunk-local light buffer, propagate sunlight and emissive sources with breadth-first search, and rebuild only dirty light regions.

## Liquids

Liquids use a grid simulation with active cells. Each liquid tile stores an amount (`SandboxTile.fluid`), flows down first, sideways second, and upward only for pressure. Distant liquid chunks can be paused or approximated.

The adopted water contract — a compressible-water cellular automaton with an active set, an order-independent mass-conserving tick, wake-on-edit, chunk pause/resume, a per-tick active-cell budget, and save persistence — is specified in [08 — Liquids § P2-FLUID-001 specification](08-liquids.md#p2-fluid-001-specification-water) and implemented as `SandboxFluidSimulator` / `SandboxFluidConstants` under `Assets/Scripts/Sandbox/Fluid/`.

## Simulation Budget

Simulation systems should update by dirty region and distance from active players. Avoid full-world passes in `Update`.
