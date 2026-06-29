---
type: Technical Reference
title: "Chunking"
description: "ProjectTwelve Chunking reference — design notes, contracts, and decisions for the chunking area of the sandbox prototype."
resource: wiki/03-chunking.md
tags: [wiki, chunking]
timestamp: 2026-06-28T00:00:00Z
okf_version: 0.1
---

# 03 — Chunking

> **Status:** Planning.
> **Decisions:** Default **32×32** tiles/chunk; load around players; per-subsystem dirty flags.
> **Invariants:** No per-tile operation may incur a world-global cost; chunks save independently.

Chunking is the backbone of the whole architecture. It exists so that the cost of any operation
scales with the *edited region*, not the *world size*.

## Chunk size

Common choices: 16×16, 32×32, 64×64.

| Size | Pros | Cons |
|------|------|------|
| 16×16 | Fine-grained culling; cheap single-chunk rebuilds | More GameObjects/meshes/colliders; more bookkeeping |
| **32×32** | Balanced; good default | — |
| 64×64 | Fewer objects; fewer draw calls | Heavier rebuild when any tile in the chunk changes |

**Default: 32×32.** Revisit only with profiler data. `Chunk.Size` is a compile-time constant
(see [Data Models](02-data-models.md)); changing it is a deliberate, tested change, not a knob.

## Loading & unloading

- Keep chunks within a **load radius** of each active player loaded; unload beyond an
  **unload radius** (use hysteresis — unload radius > load radius — to avoid thrashing at the boundary).
- Run the load/unload pass on a fixed interval (e.g. a few times per second), not every frame.
- On unload: flush the chunk to the save layer if dirty, release its render/collider/light/fluid
  views, then drop it from the dictionary.
- On load: read from save if present, otherwise generate deterministically from the seed
  (see [Procedural Generation](07-procedural-generation.md)).

```csharp
void UpdateLoadedChunks(IEnumerable<Vector2Int> playerChunkCoords)
{
    var wanted = new HashSet<Vector2Int>();
    foreach (var pc in playerChunkCoords)
        for (int dx = -loadRadius; dx <= loadRadius; dx++)
        for (int dy = -loadRadius; dy <= loadRadius; dy++)
            wanted.Add(new Vector2Int(pc.x + dx, pc.y + dy));

    foreach (var coord in wanted)
        if (!chunks.ContainsKey(coord)) LoadOrGenerate(coord);

    foreach (var coord in chunks.Keys.ToList())
        if (!IsWithin(coord, playerChunkCoords, unloadRadius)) Unload(coord);
}
```

> Note: the Unity **Tilemap renderer's internal "chunking" only culls rendering**. It is not the
> same as our data chunks and does nothing for physics, lighting, fluids, or memory. We manage
> our own chunks; the Tilemap optimization is orthogonal.

## Dirty flags (the scheduling contract)

Each chunk carries **independent** dirty flags, one per subsystem:

- `NeedsRenderRebuild` → [Rendering](04-rendering.md) rebuilds the chunk view when visible.
- `NeedsColliderRebuild` → [Collision](05-collision-physics.md) rebuilds that chunk's collider.
- lighting dirty region → [Lighting](06-lighting.md) re-propagates locally.
- fluid wake set → [Liquids](08-liquids.md) re-activates touched cells.
- `IsDirtyForSave` → [Saving](11-saving-loading.md) persists on the next save tick.

Why separate flags? Because the subsystems run on different budgets and cadences. Coupling them
("dirty" as one boolean) forces, e.g., a collider rebuild every time only the light changed.

## Persistence & LOD

- **Save per chunk**, so only dirty chunks hit disk. See [Saving & Loading](11-saving-loading.md).
- Prefer **seed + diffs**: untouched chunks regenerate deterministically; only edits persist.
- Optional **LOD** for distant chunks: simulate less (pause fluids/lighting), render coarser, or
  don't render at all until nearer. Keep simulation correctness in mind — a paused fluid chunk
  must resume consistently.

## Pitfalls

- **Negative-coordinate math.** See the `FloorDiv` invariant in [Data Models](02-data-models.md).
- **Cross-chunk reads at borders.** Mesh edges, lighting, and fluid flow all need neighbor tiles;
  always go through `World.GetTile`, never reach into a neighbor's array.
- **Unbounded chunk dictionary.** Without unloading, exploration leaks memory. Unload is not optional.
- **Frame spikes from bulk edits.** Explosions/large brushes can dirty many chunks at once; cap
  rebuilds-per-frame and spread the work.

## See also

- [Architecture](01-architecture.md) — the tile-edit data flow that sets these flags.
- [Saving & Loading](11-saving-loading.md) — chunk persistence and diffs.
- [Rendering](04-rendering.md), [Collision](05-collision-physics.md) — flag consumers.
