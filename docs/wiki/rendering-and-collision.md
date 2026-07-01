---
type: Specification
title: Rendering and Collision
description: Prototype rendering and collision rules — chunk-local colliders, player movement, and the chunk-local rebuild contract.
resource: wiki/rendering-and-collision.md
tags: [docs, wiki, rendering, collision, p1]
timestamp: 2026-07-01T05:03:48Z
okf_version: 0.1
---

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

## Prototype collision rules (P1-COLL-001)

This section is the authoritative specification of the **current** prototype collision
behavior for solid tiles and player movement. It documents what the code does today so the
behavior is testable and traceable; it is not a proposal. The design-level direction
(manual swept-AABB collision) lives in [Collision & Physics](05-collision-physics.md) and
remains a documented future option, not a regression against this prototype.

### Approach chosen for the prototype

The prototype uses the **chunk-local colliders + Unity Rigidbody** approach (approach #1 in
[Collision & Physics](05-collision-physics.md)), *not* the recommended manual swept-AABB
approach (#2). This is a deliberate prototype simplification: it reuses Unity's 2D physics
solver for the player while keeping collider rebuilds bounded to a single chunk. Manual tile
collision stays on the table for a later milestone if per-tile collider counts or tunneling
at high speed become a problem.

### Tile solidity contract

- A tile is **solid** when `SandboxTile.IsSolid` is true, defined as `id != SandboxTileIds.Air`
  (`Air == 0`). Every non-air tile id (Dirt, Grass, Stone, ore variants) is solid; there is no
  per-tile "passable but non-air" state in the prototype.
- Solidity is the single source of truth for both collider generation and the player's ground
  and wall probes. Pathfinding uses the same solid/empty grid (see
  [Pathfinding](09-pathfinding.md)).

### Terrain collider generation

Owned by `SandboxChunkRenderer.RebuildColliders`, invoked from `Rebuild(...)` on the same
chunk GameObject that carries the mesh:

- **Per-row run merging.** For each of the chunk's 32 rows, contiguous horizontal runs of
  solid tiles are merged into one `BoxCollider2D`. A row with two gaps produces up to three
  colliders; a fully solid row produces exactly one. This keeps collider count far below one
  box per tile without any cross-row merging.
- **Placement.** A run starting at local `x = runStart` with `runLength` tiles gets
  `offset = ((runStart + runLength/2) * tileSize, (y + 0.5) * tileSize)` and
  `size = (runLength * tileSize, tileSize)`, all in the chunk GameObject's local space. Tile
  `(x, y)` therefore spans world `[x, x+1) × [y, y+1)` at the default `tileSize = 1`.
- **Material.** Every terrain collider uses `SandboxPhysicsMaterials.ZeroFriction`
  (friction 0, bounciness 0) so the player does not stick to or bounce off walls.
- **Rebuild path.** Editing a tile sets the chunk's `NeedsColliderRebuild` flag; the renderer
  destroys the chunk's existing `BoxCollider2D`s and regenerates them during `Rebuild`, which
  then clears the flag. Collider cost per edit is therefore bounded to one chunk (32×32) and
  is independent of world size — the invariant this whole design protects. Border edits also
  dirty face-adjacent neighbors (see the [render rebuild contract](#chunk-local-render-rebuild-contract-p1-render-001)).

### Player movement model

Owned by `SandboxPlayerController` (`[RequireComponent(Rigidbody2D, BoxCollider2D)]`):

- **Gravity and integration** are handled by Unity's 2D physics (project gravity
  `(0, -9.81)`). The controller does not integrate velocity itself.
- **Horizontal movement.** `FixedUpdate` sets `linearVelocity.x = horizontalInput * moveSpeed`.
  When a wall is detected in the input direction the target x-velocity is clamped to `0`, so the
  player stops flush against walls instead of pushing into them.
- **Jump.** A jump request only takes effect when `IsGrounded`; it sets
  `linearVelocity.y = jumpVelocity`, preserving x-velocity. There is no double-jump, variable
  jump height, or coyote time in the prototype.
- **Automation surface.** `IsGrounded`, `Velocity`, `SetExternalMoveInput(direction, duration)`,
  `RequestJump()`, and `TeleportTo(worldPosition)` are public so tests and MCP automation can
  drive the player without synthetic input devices.

### Ground and wall probes

Collision *reads* are raycasts against terrain colliders, taken from the player collider bounds:

- **Grounded** = a downward ray of length `GroundProbeDistance` from either foot (bottom
  corners, inset by `FootInset`) hits a non-player collider whose `normal.y > GroundNormalThreshold`.
  Either foot grounds the player, which keeps footing on run-merged edges.
- **Wall-blocked** = a horizontal ray of length `WallProbeDistance` from the leading edge
  (at 25% of the collider height) hits a non-player collider whose `|normal.x| > WallNormalThreshold`.
- Probes ignore the player's own collider (`hit.collider != playerCollider`).

### Constants

| Constant | Value | Meaning |
|----------|-------|---------|
| `moveSpeed` | 7 units/s | Horizontal run speed. |
| `jumpVelocity` | 11 units/s | Upward velocity applied on a grounded jump. |
| `GroundProbeDistance` | 0.12 units | Downward ray length for the grounded check. |
| `GroundNormalThreshold` | 0.5 | Minimum `normal.y` for a hit to count as ground. |
| `WallProbeDistance` | 0.08 units | Horizontal ray length for the wall check. |
| `WallNormalThreshold` | 0.5 | Minimum `|normal.x|` for a hit to count as a wall. |
| `FootInset` | 0.05 units | Inset of foot rays from the collider corners. |
| `tileSize` | 1 unit | World size of one tile; solid tile spans `[x, x+1) × [y, y+1)`. |

### Invariants and edge cases

- **Per-axis feel comes from velocity clamping, not swept resolution.** The x-velocity is
  zeroed at walls while gravity keeps acting on y, giving clean wall-slides without sticking.
  True corner/swept resolution is a property of Unity's solver here, not custom code.
- **Tunneling.** The player's `Rigidbody2D` uses Unity's default *Discrete* collision
  detection. At prototype speeds (`moveSpeed 7`, `jumpVelocity 11`) against 1-unit tiles and a
  fixed timestep, per-step displacement is well under a tile, so the player does not pass
  through solid terrain. Faster entities, thinner-than-tile geometry, or large external
  impulses could tunnel; mitigations are *Continuous* collision detection, sub-stepping
  movement, or moving the player to manual swept-AABB collision. Any feature that raises speeds
  past roughly one tile per fixed step must revisit this.
- **Out of scope for the prototype:** one-way platforms, ladders, slabs, slopes, moving
  platforms, and crushing. These are layered rules for a later milestone.

### Play-mode QA checklist

Reproducible manual verification in a scene with a generated world and a spawned player:

- [ ] **Stand:** the player falls onto terrain and comes to rest with `IsGrounded` true and a
  stable y position (does not sink into or jitter on the ground).
- [ ] **Walk:** holding left/right moves the player at `moveSpeed` across a flat surface.
- [ ] **Wall-stop:** walking into a solid wall stops horizontal motion flush against it with no
  sticking or climbing.
- [ ] **Jump:** a grounded jump produces upward motion and returns to grounded; a jump input
  while airborne is ignored.
- [ ] **Dig-and-fall:** removing the tile directly under the player drops the player through the
  now-empty cell (collider rebuild took effect), and placing a tile under a falling player
  catches it.

Automated coverage of stand, jump, walk/wall-stop, and no-tunneling lives in the PlayMode
suite `Assets/Tests/PlayMode/SandboxCollisionPlayModeTests.cs`.

## Autotiled terrain visuals

Sandbox chunk meshes resolve autotile sprites through `SandboxTileVisualCatalog`, `AutotileResolver`, and `AutotileMaskBuilder`. See [Visual integration](visual-integration.md).

## Future Rendering Tasks

- Add texture atlas coordinates instead of color-only quads.
- Rebuild changed mesh regions within a chunk instead of the whole chunk mesh.
- Hide internal faces if the project moves from 2D quads to thicker geometry.
- Add a material/shader path that samples tile light or vertex colors.
