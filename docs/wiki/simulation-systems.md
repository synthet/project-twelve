---
type: Technical Reference
title: "Lighting, Liquids, and Simulation"
description: "ProjectTwelve Lighting, Liquids, and Simulation reference — design notes, contracts, and decisions for the lighting, liquids, and simulation area of the sandbox prototype."
resource: wiki/simulation-systems.md
tags: [wiki, lighting]
timestamp: 2026-06-28T00:00:00Z
okf_version: 0.1
---

# Lighting, Liquids, and Simulation

## Lighting

Lighting should be tile-based, not a large field of Unity lights. Store light values in tiles or a chunk-local light buffer, propagate sunlight and emissive sources with breadth-first search, and rebuild only dirty light regions.

## Liquids

Liquids should use a grid simulation with active cells. Each liquid tile stores an amount, flows down first, sideways second, and upward only for pressure. Distant liquid chunks can be paused or approximated.

## Simulation Budget

Simulation systems should update by dirty region and distance from active players. Avoid full-world passes in `Update`.
