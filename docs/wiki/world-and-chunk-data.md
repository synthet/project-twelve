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
