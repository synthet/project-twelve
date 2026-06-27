# 05 — Collision & Physics

> **Status:** Planning.
> **Decisions:** **No single global `CompositeCollider2D`.** Use chunk-local colliders and/or
> manual tile collision for terrain movement.
> **Invariants:** Collider rebuilds are chunk-local; terrain collision cost is independent of world size.

## The cliff to avoid

A single `CompositeCollider2D` spanning the whole tilemap is a trap: **every tile edit forces it
to re-merge all physics shapes**, which is O(world) and stalls on digging. It is, in the words of
the design doc, a "sledgehammer." Do not use one global composite for a large, destructible world.

## Approaches

1. **Chunk-local colliders.** Each chunk owns its own `TilemapCollider2D` (+ optional
   `CompositeCollider2D`). A tile edit rebuilds only that chunk's collider — bounded cost.
   Good when you want Unity physics (rigidbodies, projectiles) to interact with terrain.
2. **Manual tile collision (recommended for the player/NPCs).** Don't give terrain colliders at
   all for character movement. Move entities in code and resolve against the tile grid with
   swept AABB tests. This is integer/float math against a few nearby tiles — extremely fast and
   perfectly tile-accurate, with no collider rebuilds ever.
3. **Hybrid (recommended overall).** Manual tile collision for terrain movement (players, walking
   NPCs); Unity physics + **chunk-local** colliders for things that genuinely benefit from the
   physics engine (thrown objects, ragdolls, projectiles bouncing off walls).

## Manual tile collision sketch

Treat each solid tile as a unit AABB. For a moving entity, sweep its box against the tiles it
would overlap and clamp movement per axis.

```
// Resolve one axis at a time (x then y) to get clean wall-slide / ground behavior.
move entity by intended dx:
    compute the tile range the entity's box would span after moving
    for each solid tile in range:
        if box overlaps tile:
            snap entity to the tile edge, set dx = 0 (and grounded/wall flags)
repeat for dy
```

Notes:

- Query solidity via `World.GetTile(x,y)` + the tile's registry `solid` flag (see
  [Modding & Content](12-modding.md)). Cache the few tiles around the entity per step.
- Resolving **per axis** gives correct corner behavior (no sticking, clean wall-slides).
- One-way platforms, ladders, slabs, and slopes are extra rules layered on the same scan.
- Disable rigidbody gravity for these entities and integrate velocity yourself for full control.

## Chunk-local collider sketch

```
on Chunk.NeedsColliderRebuild:
    rebuild this chunk's TilemapCollider2D / composite from its Tiles[,]
    clear NeedsColliderRebuild
```

Because it's one chunk (e.g. 32×32), the merge is small and bounded. Spread rebuilds across frames
if many chunks dirty at once (explosions) — see the per-frame budget note in
[Architecture](01-architecture.md).

## Pitfalls

- **Per-tile `BoxCollider2D`s** at scale: thousands of colliders tank performance. Only viable for
  tiny worlds.
- **Tunneling** at high speed with manual collision: use swept tests (or sub-step movement), not a
  single end-point overlap check.
- **Mixing authorities**: if both manual collision and Unity physics act on the same entity vs the
  same terrain, you get fighting. Pick one authority per entity-vs-terrain interaction.

## See also

- [Chunking](03-chunking.md) — `NeedsColliderRebuild` lifecycle.
- [Data Models](02-data-models.md) — tile solidity / coordinate conversion.
- [Pathfinding](09-pathfinding.md) — uses the same solid/empty grid.
