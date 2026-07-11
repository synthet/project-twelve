---
type: Architecture
title: Procedural Generation
description: Deterministic multi-pass terrain generation — pass order, per-pass sub-seeds, and the hash value-noise that replaced engine-native perlin.
resource: wiki/07-procedural-generation.md
tags: [docs, wiki, generation, determinism, p2]
timestamp: 2026-07-11T00:00:00Z
okf_version: 0.1
---

# 07 — Procedural Generation

> **Status:** Planning.
> **Decisions:** **Deterministic, multi-pass** generation from a seed; passes run in a fixed order.
> **Invariants:** Same seed + settings ⇒ identical world. Generation is pure w.r.t. the seed.

Determinism is non-negotiable: it lets saves store only diffs (see [Saving & Loading](11-saving-loading.md)),
makes bugs reproducible, and lets the server and any late-joining client agree on untouched terrain.

## Deterministic noise (no engine-native perlin) — P2-GEN-001

Generation must be reproducible **outside** the engine so the offline tool
(`tools/world-viz`) can mirror it bit-for-bit for parity tests and debugging. Unity's
`Mathf.PerlinNoise` is native C++ whose gradient/permutation tables are not published and
could not be reproduced offline (Unity's own reference `Perlin.cs` and community ports all
disagree with it), so any JS port drifted from the engine and the parity gate stayed red.

Terrain noise is therefore **project-owned**, built on a shared deterministic 32-bit integer
hash (`SandboxHash.Hash` in C#, `tools/world-viz/src/core/hash.js` in JS — FNV-1a plus an
xorshift-multiply finalizer, the same mix the fluid sim uses). Pass 1 surface height is a
value noise: a quintic-smootherstep blend of per-lattice samples drawn from
`hash(seed, passId, latticeX)`, mapped to `[0, 1]` by `UnitFloat` (word ÷ 2³²). Because the
whole path is pure integer math plus explicit `float`/`Math.fround` steps, C# and JS agree
exactly — verified offline by **known-answer hash and surface-height vectors duplicated in
both test suites** (`SandboxHashTests` ↔ `hash.test.js`), independent of any Unity run.

Per-pass sub-seeds follow the `hash(seed, passId, coord)` convention (`SandboxGenPass` numbers
the passes); each pass draws from an independent, coordinate-addressable stream, never from
iteration order.

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
