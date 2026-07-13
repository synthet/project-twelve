---
type: Specification
title: Tile Lighting
description: P2 tile-light propagation, dirty-window updates, sunlight, emissive sources, and rendering integration.
resource: wiki/06-lighting.md
tags: [docs, wiki, lighting, simulation, p2]
timestamp: 2026-07-12T00:00:00Z
okf_version: 0.1
---

# 06 — Lighting

> **Status:** Adopted for P2-LIGHT-001.
> **Decisions:** Custom single-channel **0–15 tile lightmap** with **BFS flood-fill** propagation;
> update **dirty regions** only. Not Unity 2D URP lights.
> **Invariants:** Light is derived state recomputed locally on tile/light-source changes.

## Why not Unity 2D lights

Unity's 2D (URP) lights treat blocks as geometry; they don't model "light seeps through open air
and is blocked/attenuated by solid tiles" the way *Terraria*/*Starbound* do. That behavior is a
**tile-based lightmap** you compute yourself. There is no built-in Unity feature for it.

## Model

- Maintain `SandboxTile.light` in the inclusive range 0–15. It is a derived cache and is never
  authoritative save data.
- **Sources:** strength-15 sunlight enters at the first open cell above the deterministic surface;
  `TileDefinition.LightEmission` supplies point sources. `core:gold_ore` emits 12 for the prototype.
- **Attenuation:** `TileDefinition.LightAttenuation` is the cost of entering a tile. Air costs 1;
  core opaque tiles cost 3. Registry validation rejects costs outside 1–15.

## BFS flood-fill propagation

Propagate from each source with a wavefront; a tile only updates if the incoming value beats its
current value. Take the **max** across overlapping sources (or additive blend per channel for
colored light).

```csharp
void PropagateLight(int sx, int sy, byte initial)
{
    var q = new Queue<Vector2Int>();
    light[sx, sy] = initial;
    q.Enqueue(new Vector2Int(sx, sy));

    while (q.Count > 0)
    {
        var p = q.Dequeue();
        byte cur = light[p.x, p.y];
        if (cur <= 1) continue;                       // nothing left to give

        foreach (var d in dirs)                       // 4-neighborhood (N,S,E,W)
        {
            int nx = p.x + d.x, ny = p.y + d.y;
            if (!InBounds(nx, ny)) continue;
            byte atten = IsOpaque(nx, ny) ? (byte)2 : (byte)1;
            byte next = (byte)Mathf.Max(0, cur - atten);
            if (next > light[nx, ny])
            {
                light[nx, ny] = next;
                q.Enqueue(new Vector2Int(nx, ny));
            }
        }
    }
}
```

This is the well-known Terraria-style approach (flow outward, attenuating; solids cost more).
Dijkstra (treating attenuation as edge cost) is an equivalent framing if you want weighted steps.

## Dirty-region updates (the important optimization)

Never relight the world. On a change, relight only a bounded region:

- **Tile removed/added:** the affected radius is at most the max light range (e.g. 15 tiles).
  Clear the lightmap in that window, then re-propagate from all sources/bright borders touching it.
- **Light source toggled/moved:** clear its old contribution radius and re-propagate.
- **Time of day:** sunlight changes are gradual; update surface-fed columns incrementally, or
  rescale the sky contribution and re-propagate affected open-air regions.

A correct, simple scheme: on edit, collect the radius-15 window, set available cells to 0, seed the
queue with intrinsic sources plus attenuated light entering from cells just outside the window,
and run the BFS above. Cross-chunk reads go through a non-generating world adapter; unavailable
chunks are boundaries rather than an invitation to generate speculative world data.

Chunk generation/load relights the new chunk plus a radius-15 halo. Loading either side of a seam
therefore re-evaluates the shared border, so final light is independent of chunk load order.

## Applying light to the screen

Two common paths (see [Rendering](04-rendering.md)):

- **Vertex colors (implemented):** each tile quad uses its cached light value, mapped from a 0.35
  darkness floor to full brightness. Per-corner neighbor sampling remains a polish follow-up.
- **Lightmap texture:** write `light` into a texture sampled by a full-screen or per-chunk shader.
  A blurred low-res lightmap gives the soft falloff look; some demos render open-air to a second
  camera, blur it, and composite to darken covered areas — but the underlying values still come
  from the tile fill above.

Colored light, time-of-day scaling, and smooth corner sampling are follow-up work.

## Persistence

Lighting writes mark only the affected chunk's render mesh dirty. They do not affect collision,
navigation versions, edit flags, or save dirtiness. New saves clear the cached `light` byte in tile
edit records; loaders ignore any legacy cached value and recompute after tile data is restored.

## Pitfalls

- **Relighting globally** on every edit — the whole point is local dirty regions.
- **Forgetting cross-chunk seeding** — a torch near a chunk edge must light the neighbor; seed the
  BFS from real neighbor values via `World`.
- **Light feedback loops** with the "update only if brighter" rule when *removing* light: removal
  needs a clear-then-refill pass, not just propagation, or stale brightness lingers.
- **Per-frame full recompute** for time-of-day — make it incremental.

## See also

- [Rendering](04-rendering.md) — how light values reach pixels.
- [Chunking](03-chunking.md) — the lighting dirty region is part of the tile-edit flow.
- [Data Models](02-data-models.md) — where `light` lives.
