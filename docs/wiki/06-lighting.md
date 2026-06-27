# 06 — Lighting

> **Status:** Planning.
> **Decisions:** Custom **tile lightmap** with **BFS flood-fill** propagation; update **dirty
> regions** only. Not Unity 2D URP lights.
> **Invariants:** Light is derived state recomputed locally on tile/light-source changes.

## Why not Unity 2D lights

Unity's 2D (URP) lights treat blocks as geometry; they don't model "light seeps through open air
and is blocked/attenuated by solid tiles" the way *Terraria*/*Starbound* do. That behavior is a
**tile-based lightmap** you compute yourself. There is no built-in Unity feature for it.

## Model

- Maintain a per-tile light value `light[x,y]` (e.g. 0–15, or 0–255; or three channels for
  colored light). This is cached on the `Tile` (see [Data Models](02-data-models.md)) and/or in a
  per-chunk light layer.
- **Sources:** the sky (sunlight, scaled by time of day) feeds open-air tiles at the surface;
  emissive tiles (torches, lava, glowing ore) feed point light.
- **Attenuation:** light drops by 1 per step through air, and more per step through opaque solid
  tiles. The exact solid cost is a **tunable per-material constant** (the architecture blueprint
  uses solid −3, the prose design doc used −2); set it via the tile registry, not a magic number.

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

A correct, simple scheme: on edit, collect the window, set those cells to 0, seed the queue with
every cell on the window's border (and any source inside it) at its current value, and run the BFS
above. Cross-chunk windows read neighbors via `World.GetTile`.

## Applying light to the screen

Two common paths (see [Rendering](04-rendering.md)):

- **Vertex colors:** when building a chunk mesh, set each quad corner's color from `light` (sample
  neighboring tiles per corner for smooth gradients). The shader multiplies sprite color by light.
- **Lightmap texture:** write `light` into a texture sampled by a full-screen or per-chunk shader.
  A blurred low-res lightmap gives the soft falloff look; some demos render open-air to a second
  camera, blur it, and composite to darken covered areas — but the underlying values still come
  from the tile fill above.

Colored light: store/propagate R, G, B independently (or a `Color32`); same rules per channel.

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
