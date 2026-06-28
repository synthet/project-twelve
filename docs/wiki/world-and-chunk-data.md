---
type: Technical Reference
title: World and Chunk Data
description: Chunk data model, coordinate conversion contract, tile fields, the SetTile edit choke point, and chunk lifecycle rules for the sandbox world.
resource: wiki/world-and-chunk-data.md
tags: [wiki, world, chunks, coordinates, tiles]
timestamp: 2026-06-28T00:00:00Z
okf_version: 0.1
---

# World and Chunk Data

## Current Model

A world is a sparse dictionary of chunks keyed by chunk coordinate. Each chunk is 32×32 tiles. World tile coordinates are converted into chunk coordinates and local coordinates with floor division and positive modulo so negative world positions work predictably.

## Coordinate Conversion Contract (P1-WORLD-001)

World tile coordinates are integer `(x, y)` positions over the whole world. Every world coordinate
maps to exactly one chunk coordinate plus an in-chunk local coordinate, and that mapping is
invertible. `S` below is the chunk side length `SandboxChunk.Size` (currently `32`).

### Formulas

| Conversion | Helper | Formula |
| --- | --- | --- |
| World → chunk | `SandboxWorld.WorldToChunkCoord(x, y)` | `floor(x / S)`, `floor(y / S)` |
| World → local | `SandboxWorld.WorldToLocalCoord(x, y)` | `((x mod S) + S) mod S`, same for `y` |
| Chunk + local → world | `SandboxWorld.ChunkLocalToWorld(chunk, lx, ly)` | `chunk.x * S + lx`, `chunk.y * S + ly` |

Chunk selection uses **floor** division rather than C#'s truncating `/` so the chunk index stays
monotonic across the origin. Local selection uses a **positive** modulo so the result is always a
valid array index in `[0, S - 1]`, never negative.

### Invariants

- **Local range.** `WorldToLocalCoord` always returns coordinates in `[0, S - 1]`; the result is
  always in bounds for `SandboxChunk.IsLocalInBounds`.
- **Exact round trip.** For any world coordinate `p`,
  `ChunkLocalToWorld(WorldToChunkCoord(p), WorldToLocalCoord(p)) == p`.
- **Determinism.** The conversions depend only on their inputs and `S`; they are pure static
  functions with no per-instance or frame state.

### Boundary cases

| World `(x, y)` | Chunk | Local | Note |
| --- | --- | --- | --- |
| `(0, 0)` | `(0, 0)` | `(0, 0)` | Origin. |
| `(31, 31)` | `(0, 0)` | `(31, 31)` | Last tile of the origin chunk. |
| `(32, 0)` | `(1, 0)` | `(0, 0)` | First tile of the next chunk. |
| `(-1, -1)` | `(-1, -1)` | `(31, 31)` | One tile below/left of origin wraps to the high corner of the negative chunk. |
| `(-32, -32)` | `(-1, -1)` | `(0, 0)` | First tile of the `(-1, -1)` chunk. |
| `(-33, 64)` | `(-2, 2)` | `(31, 0)` | Mixed-sign coordinates. |

### Float position helpers and limits

`SandboxWorld.WorldPositionToTile` (`floor(worldPosition / tileSize)`) and `TileToWorldCenter`
(`(tile + 0.5) * tileSize`) convert between Unity world-space `Vector2` positions and integer tile
coordinates for input and rendering. They depend on the instance `tileSize`.

The integer conversions are exact only within the precision of `float`: `WorldToChunkCoord` divides
through `float`, so beyond roughly ±16 million tiles (the `float` integer-exact range) chunk
selection can lose precision. That far exceeds prototype world bounds and is recorded here as a
known limitation; a future task can switch to integer-only floor division if very large worlds are
required.

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

## Border Edit Propagation

`SandboxWorld.SetTile` dirties the face-adjacent neighbors of any edit that lands on a chunk
border so their render and collider state rebuilds alongside the owning chunk:

- An interior edit touches no neighbor; an edge edit touches one; a corner edit touches the two
  orthogonal chunks that share the edited tile's exposed faces.
- Only already-loaded neighbors are flagged. Unloaded neighbors are left to rebuild from current
  data when they next load, so editing never forces speculative chunk generation.

The neighbor set is exposed as the pure static `SandboxWorld.GetBorderNeighborChunks`, and the
dirtying step as `SandboxWorld.MarkBorderNeighborsDirty`, both covered by EditMode tests. This is
the groundwork that keeps cross-chunk rendering (face culling, lighting) and merged collision
consistent as those systems begin to sample neighbor tiles.

## Tile Edit Choke Point (P1-EDIT-001)

Every gameplay tile edit flows through the single `SandboxWorld.SetTile(x, y, tileId)` choke point.
Placing and breaking are the same operation: breaking writes `SandboxTileIds.Air`. Routing all
edits through one method keeps the data, dirty-flag, render, collision, and neighbor-propagation
side effects consistent regardless of caller (player input today, tools or networking later).

`SetTile` performs the Unity-facing steps — resolving and lazily generating the owning chunk and
ensuring it has a renderer — and delegates the pure edit logic to the static
`SandboxWorld.ApplyTileEdit`:

- **Tile data.** The new tile is written via `SandboxChunk.SetLocalTile`, replacing the previous
  tile in the owning chunk.
- **Dirty flags.** Writing the tile raises the owning chunk's `NeedsRenderRebuild` and
  `NeedsColliderRebuild` flags so both the mesh and the collider rebuild, and marks the chunk
  `IsDirty`/`HasEdits` so the edit is persisted on the next save.
- **Border neighbors.** `MarkBorderNeighborsDirty` dirties any already-loaded face-adjacent neighbor
  when the edit lands on a chunk border, matching the [Border Edit Propagation](#border-edit-propagation)
  rules above. Unloaded neighbors are left to rebuild when they next load.
- **Bounds.** An edit whose local coordinate is out of range is ignored and dirties nothing.

The deserialization path in `LoadFromPath` deliberately bypasses this choke point: it writes saved
tiles with `markDirty: false` because loading is not a gameplay edit and must not re-flag every
loaded chunk as edited. `ApplyTileEdit` is a pure function of the supplied chunk set and is covered
by the `SandboxWorld_Apply*Edit*` EditMode tests for central edits, border edits, breaking to air,
and out-of-bounds edits.

## Chunk Lifecycle Contract (P1-WORLD-002)

`SandboxWorld` streams chunks around the player and releases them when they leave range. The
selection policy is separated from Unity object creation so it can be unit tested:

- **Load window.** `SandboxWorld.GetChunksInLoadRange` enumerates a square of side
  `2 * loadRadius + 1` chunks centered on the player's chunk. `RefreshLoadedChunks` ensures a
  renderer (and, lazily, the chunk data) for each coordinate in that window. A negative radius is
  clamped to zero, loading only the center chunk.
- **Unload window.** `SandboxWorld.GetRenderersToUnload` returns the loaded coordinates whose
  Chebyshev (square) distance from the center exceeds `loadRadius + unloadPadding`. The padding
  makes the unload window strictly larger than the load window, creating hysteresis so edge chunks
  are not thrashed by small player movements. Negative radius or padding values are clamped to zero.
- **Dirty flags.** `NeedsRenderRebuild` and `NeedsColliderRebuild` are independent per-chunk flags.
  A newly loaded chunk requests both rebuilds; thereafter each subsystem clears its own flag when it
  rebuilds, and a tile edit re-raises both. Independent flags let render and collision work be
  scheduled separately as those passes diverge.

Both selection helpers are pure static functions covered by `SandboxWorld_LoadRange*`,
`SandboxWorld_Unload*`, and `SandboxChunk_*DirtyFlags*` EditMode tests.
