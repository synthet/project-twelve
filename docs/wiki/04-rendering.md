---
type: Technical Reference
title: "Rendering"
description: "ProjectTwelve Rendering reference — design notes, contracts, and decisions for the rendering area of the sandbox prototype."
resource: wiki/04-rendering.md
tags: [wiki, rendering]
timestamp: 2026-06-28T00:00:00Z
okf_version: 0.1
---

# 04 — Rendering

> **Status:** Planning.
> **Decisions:** Start with **Unity Tilemap per chunk**; migrate to **custom chunk meshes** if
> lighting/atlas/update performance demands it.
> **Invariants:** Rendering is chunk-local; a tile edit rebuilds at most the affected chunk view(s).

## Options

| Option | Strengths | Risks / costs |
|--------|-----------|---------------|
| **Unity Tilemap** (per chunk/layer) | Built-in palette/editor, batching, easy layering, fast to prototype | `SetTile` storms get expensive; no Terraria-style per-tile light falloff out of the box |
| **Custom chunk mesh** | Full control: vertex colors for light, atlas/UV control, custom shaders, partial rebuilds, quad merging | You write the mesh builder, atlas, and update tooling |
| **GPU instancing / procedural draw** | Very low draw-call overhead for huge static regions | Complex data upload; dynamic per-tile edits are awkward |

## Recommendation & migration path

1. **Prototype on Tilemaps.** One `Tilemap` per chunk (or a small number of layered Tilemaps:
   background wall / foreground tile / overlay). Iterate gameplay fast.
2. **Move to custom meshes when** any of these bite:
   - lighting needs per-vertex tile colors blended smoothly,
   - texture atlas / animation control exceeds what Tile assets give you,
   - `SetTile`/collider churn shows up in the profiler during digging.
3. **Consider instancing only** for very large, mostly-static vistas (parallax backdrops, deep
   cavern fill) — not the editable foreground.

Keep the render layer behind a small interface (`IChunkRenderView`) so the Tilemap→mesh swap is
local to one subsystem and doesn't ripple into world/data code (engine-agnostic seam, see
[Architecture](01-architecture.md)).

## Custom mesh sketch

For each chunk, build a mesh of quads for non-air tiles. UVs index a texture atlas; vertex colors
carry lighting (see [Lighting](06-lighting.md)).

```
for each tile (lx, ly) in chunk:
    if tile.id == AIR: continue
    add quad at local position (lx, ly)
    set UVs from atlas rect of tile.id (+ metadata frame/variant)
    set vertex color from light[lx, ly]   // sampled per corner for smooth gradients
```

Optimizations, in rough priority order:

- **Per-chunk rebuild only** (never the whole world).
- **Greedy quad merging** of runs of identical tiles to cut vertex count.
- **Partial mesh update** for single-tile edits if the builder supports it; otherwise rebuild the
  one chunk's mesh — still cheap at 32×32.
- **Layer/material batching** so chunks sharing an atlas batch into few draw calls.

## Layers

Typical 2D sandbox layering, back to front:

1. Sky / parallax background.
2. **Wall** layer (background tiles behind the player).
3. **Foreground** tile layer (collidable terrain).
4. Liquids overlay (see [Liquids](08-liquids.md)).
5. Entities, particles, UI.

Lighting modulates layers 2–4. Each layer can be its own Tilemap or mesh per chunk.

## Pitfalls

- **One global Tilemap for the whole world.** Defeats chunk-local rebuilds and culling; don't.
- **Rebuilding visible chunks every frame.** Rebuild only on `NeedsRenderRebuild`; clear the flag
  after.
- **Atlas bleeding.** Add padding/extrusion to atlas tiles to avoid seams at chunk/quad edges.
- **Mixing data chunks with Tilemap's render chunks.** They're unrelated; see
  [Chunking](03-chunking.md).

## See also

- [Chunking](03-chunking.md) — `NeedsRenderRebuild` and the rebuild cadence.
- [Lighting](06-lighting.md) — how light reaches the renderer (vertex colors / lightmap texture).
- [Collision & Physics](05-collision-physics.md) — the parallel concern for the same tiles.
