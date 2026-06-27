---
type: System Concept
title: Lighting, Liquids, and Simulation
description: Future tile-based lighting, liquid simulation, and simulation budget guidance.
resource: ../terraria-like-unity-design.md
tags: [unity, lighting, liquids, simulation, okf]
timestamp: 2026-06-27T00:00:00Z
status: active
---

# Lighting, Liquids, and Simulation

## Lighting

Lighting should be tile-based, not a large field of Unity lights. Store light values in tiles or a chunk-local light buffer, propagate sunlight and emissive sources with breadth-first search, and rebuild only dirty light regions.

## Liquids

Liquids should use a grid simulation with active cells. Each liquid tile stores an amount, flows down first, sideways second, and upward only for pressure. Distant liquid chunks can be paused or approximated.

## Simulation Budget

Simulation systems should update by dirty region and distance from active players. Avoid full-world passes in `Update`.

## Source Alignment

This page documents future simulation direction only. Lighting and liquids should not be treated as production-complete until source files implement them.
