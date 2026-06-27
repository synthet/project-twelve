# Rendering and Collision

## Rendering Direction

The current renderer builds one mesh per loaded chunk. Each solid tile contributes a quad with vertex colors derived from tile type and light value. This matches the design document's recommendation to avoid one global tilemap or collider for a large destructible world.

## Chunk-Local Render Rebuild Contract (P1-RENDER-001)

Render and collider rebuilds are bounded to the chunks a player can currently see and has
actually edited. The selection is owned by the pure `SandboxWorld.GetChunksNeedingRebuild`
helper, which keeps the policy independent of Unity lifecycle and unit-testable.

**Inputs:**

- The set of currently visible chunk coordinates (the renderer-backed chunks).
- The loaded chunk lookup, which carries each chunk's `NeedsRenderRebuild` / `NeedsColliderRebuild` flags.

**Selection rules:**

- A chunk is rebuilt only when it is visible **and** at least one of its own dirty flags is set.
- A visible but clean chunk is skipped.
- A dirty chunk that is loaded but not visible is skipped; it rebuilds when it next becomes visible (its dirty flags are preserved).
- A visible coordinate with no loaded chunk is skipped defensively rather than forcing generation.

**Invariants:**

- A rebuild never touches an unrelated chunk: editing one chunk only rebuilds that chunk and, via border propagation, its face-adjacent neighbors (see `MarkBorderNeighborsDirty`).
- `Rebuild` clears both dirty flags, so a chunk that has not changed since its last rebuild is not rebuilt again.

These properties are covered by the `SandboxWorld_RebuildSelection*` tests in
`Assets/Tests/EditMode/SandboxCoreTests.cs`.

## Collision Direction

The barebone implementation uses chunk-local `BoxCollider2D` components for solid tiles. Each
chunk merges horizontal runs of solid tiles per row into a single `BoxCollider2D`, and the
colliders are rebuilt alongside the mesh through the same dirty-flag path. This is simple and
transparent for a prototype. If tile counts grow, replace run-merged colliders with larger
merged rectangles or manual tile collision.

## Future Rendering Tasks

- Add texture atlas coordinates instead of color-only quads.
- Rebuild changed mesh regions within a chunk instead of the whole chunk mesh.
- Hide internal faces if the project moves from 2D quads to thicker geometry.
- Add a material/shader path that samples tile light or vertex colors.
