---
type: System Concept
title: World and Chunk Data
description: Chunk, tile, coordinate, and mutation conventions for world data.
resource: ../terraria-like-unity-design.md
tags: [unity, chunks, tiles, data, okf]
timestamp: 2026-06-27T00:00:00Z
status: active
---

# World and Chunk Data

## Current Model

A world is a sparse dictionary of chunks keyed by chunk coordinate. Each chunk is 32×32 tiles. World tile coordinates are converted into chunk coordinates and local coordinates with floor division and positive modulo so negative world positions work predictably.

## Tile Fields

| Field | Purpose now | Future use |
| --- | --- | --- |
| `id` | Selects air, dirt, grass, or stone. | Registry-backed tile identity. |
| `light` | Simple brightness multiplier in rendering. | Propagated sunlight/emissive light value. |
| `fluid` | Reserved. | Liquid amount from 0.0 to 1.0. |
| `metadata` | Reserved. | Tile variant, damage, wall, frame, or compact state. |

## Expansion Rules

- Add new tile types through stable IDs or a registry, not scattered magic numbers.
- Keep chunk data serializable without requiring Unity scene objects.
- Mutating a tile must mark the owning chunk dirty for all affected subsystems.
- Neighboring chunks should be dirtied when edits affect shared borders, lighting, liquids, or merged collision.
