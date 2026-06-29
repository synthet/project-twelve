---
type: Technical Reference
title: "Procedural Generation"
description: "ProjectTwelve Procedural Generation reference — design notes, contracts, and decisions for the procedural generation area of the sandbox prototype."
resource: wiki/07-procedural-generation.md
tags: [wiki, procedural]
timestamp: 2026-06-28T00:00:00Z
okf_version: 0.1
---

# 07 — Procedural Generation

> **Status:** Planning.
> **Decisions:** **Deterministic, multi-pass** generation from a seed; passes run in a fixed order.
> **Invariants:** Same seed + settings ⇒ identical world. Generation is pure w.r.t. the seed.

Determinism is non-negotiable: it lets saves store only diffs (see [Saving & Loading](11-saving-loading.md)),
makes bugs reproducible, and lets the server and any late-joining client agree on untouched terrain.

## Pass order

Run passes in a fixed sequence; later passes read earlier results.

1. **Surface heightmap** — 1D Perlin/Simplex per column for ground/ceiling shape (mountains, valleys).
2. **Layer fill** — fill below the surface with dirt → stone → cavern → core bands.
3. **Caves** — carve voids (see methods below).
4. **Biomes** — assign regions and rewrite surface/stone/vegetation accordingly.
5. **Ores & resources** — scatter veins by depth/biome.
6. **Structures & features** — trees, lakes, dungeons, temples, ore pockets, biome set-pieces.
7. **Validation** — spawn safety, structure overlap, reachability of key areas.

```text
for x in width:
    surfaceY = baseHeight + (int)(noise1(x) * scale)
    for y in 0..surfaceY:        set dirt
    for y in surfaceY..bottom:   set stone

for x, y in underground range:
    if perlin2(x, y) > caveThreshold: set air      // caves

spawnLiquids(waterLevel); spawnLava(lavaDepth)
for ore in ores: spawnBlobs(ore, density, size, depthRange)

for region in world:
    biome = pickBiome(region)
    applyBiome(region, biome)                       // surfaces, vegetation, features
```

## Determinism mechanics

- Seed a **single PRNG** from the world seed; derive **per-pass / per-feature sub-seeds**
  (e.g. hash(seed, passId, chunkCoord)) so passes are independent and order-stable.
- Generation must be **chunk-addressable**: generating a chunk on demand (during streaming) must
  produce the same tiles as generating the whole world. Achieve this by seeding noise/PRNG from
  coordinates, not from iteration order. Structures that span chunks need a deterministic anchor
  rule (e.g. structure owned by the chunk containing its origin).
- No reliance on `Time`, frame order, floating-point platform differences in gameplay-critical
  paths, or unordered container iteration.

## Caves

Mix methods — Perlin alone is too smooth:

- **Cellular automata:** random underground fill, then birth/death smoothing iterations → organic caverns.
- **Perlin worms:** carve meandering tunnels by walking a noise-driven heading.
- **Overlapping/thresholded Perlin:** threshold a 2D noise field for blobby cave networks.

Combine: CA for big caverns, worms for connecting tunnels, plus hand-tuned bands for special layers.

## Biomes

- Assign by horizontal region and/or noise, optionally split by **vertical layers** (Sky, Surface,
  Underground, Cavern, Core) each with its own biome roll.
- Biome controls surface block (grass/sand/snow), vegetation, cave scale, hazards, and special
  features. Apply as a rewrite pass over already-generated terrain.

## Ores & structures

- **Ores:** per type, choose depth band, vein count, and size; place via random walk or small
  circular blobs, or roll per-stone-tile with depth/biome-weighted probability.
- **Structures:** place prefab/template rooms after terrain (dungeons, temples, houses); carve
  rooms, add doors/loot/spawns; flag special regions (e.g. corruption) and run effect passes there.

## Validation pass

Cheap guards that prevent broken worlds:

- Spawn point is safe (solid ground, not inside rock or liquid).
- Critical structures don't overlap destructively.
- Key areas are reachable (sanity flood-fill / pathfinding probe — see [Pathfinding](09-pathfinding.md)).

## Pitfalls

- **Order-dependent randomness** that breaks on-demand chunk generation — seed from coordinates.
- **Cross-chunk structures** placed twice or clipped — use a single deterministic owning chunk.
- **Over-smooth caves** from Perlin-only — mix methods.
- **Float nondeterminism** across platforms in gameplay-critical generation — prefer integer/fixed
  logic or pin the noise implementation.

## See also

- [Saving & Loading](11-saving-loading.md) — why determinism enables diff-only saves.
- [Chunking](03-chunking.md) — on-demand chunk generation during streaming.
- [Liquids](08-liquids.md) — lakes/lava placed during generation then simulated.
