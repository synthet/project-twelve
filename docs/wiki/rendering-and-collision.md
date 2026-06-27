---
type: System Concept
title: Rendering and Collision
description: Chunk-local mesh rendering and collision strategy for destructible terrain.
resource: ../terraria-like-unity-design.md
tags: [unity, rendering, collision, chunks, okf]
timestamp: 2026-06-27T00:00:00Z
---

# Rendering and Collision

## Rendering Direction

The current renderer builds one mesh per loaded chunk. Each solid tile contributes a quad with vertex colors derived from tile type and light value. This matches the design document's recommendation to avoid one global tilemap or collider for a large destructible world.

## Collision Direction

The barebone implementation uses chunk-local `BoxCollider2D` components for solid tiles. This is simple and transparent for a prototype. If tile counts grow, replace per-tile colliders with merged rectangles or manual tile collision.

## Future Rendering Tasks

- Add texture atlas coordinates instead of color-only quads.
- Rebuild only changed chunks and eventually changed mesh regions.
- Hide internal faces if the project moves from 2D quads to thicker geometry.
- Add a material/shader path that samples tile light or vertex colors.
