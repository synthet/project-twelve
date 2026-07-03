# 09 — Pathfinding

> **Status:** Specified for P2 walker archetype (P2-AI-001); implementation pending.
> **Decisions:** **Grid A\*** over the tile solidity graph; recompute on terrain change or path
> invalidation; per-chunk nav-dirty marking.
> **Invariants:** The nav graph is derived from the same solid/empty data used by collision.

## Graph

Treat the tile world as a grid graph: nodes are tile positions, edges connect tiles an entity can
move between. "Walkable" is derived from tile solidity (see [Collision & Physics](05-collision-physics.md))
— do **not** maintain a second, divergent notion of solidity.

For a side-scroller, a flat 4/8-neighborhood grid is the simplest correct start. Platformer
movement needs richer edges:

- **Walk** along solid ground.
- **Jump** up to N tiles high / across gaps (encode reachable arcs as edges or as a movement model
  the search queries).
- **Fall** down ledges.
- **Ladders / ropes / doors / one-way platforms** as special edges.

## A* sketch

```
A*(start, goal, isWalkable, neighbors):
    open = priority queue ordered by g + h
    g[start] = 0; push(start)
    while open not empty:
        n = pop lowest f
        if n == goal: return reconstruct(n)
        for m in neighbors(n):           // respects jump/fall/ladder rules
            if not isWalkable(m): continue
            tentative = g[n] + cost(n, m)
            if tentative < g[m]:
                g[m] = tentative; came_from[m] = n
                f = tentative + heuristic(m, goal)   // octile/Manhattan
                push(m)
    return no-path
```

A* over typical agent ranges is cheap; recomputing on demand is fine for most enemies.

## Dynamic terrain

When tiles change, paths can become invalid:

- Mark the **affected chunk(s) nav-dirty** as part of the tile-edit flow (see [Architecture](01-architecture.md)).
- Agents holding a path that crosses a changed cell **recompute** (or repair locally).
- For large worlds, keep nav data per chunk and only rebuild dirty chunks; recompute paths lazily
  (when an agent next needs a step, or its path is blocked).

## Scaling options

- **Per-tile A*** — fine for small/medium worlds and modest agent counts.
- **Waypoint / nav-graph** — for big worlds, search over a sparse graph of platforms, ledges, and
  ladders instead of every tile; far fewer nodes.
- **Third-party (A\* Pathfinding Project)** — supports grid graphs with runtime updates; flag nodes
  blocked/free on terrain change. Useful if you need flow fields, hierarchical graphs, or local
  avoidance.
- **Unity NavMesh** — poorly suited to a destructible voxel-like 2D world; avoid.

## Pitfalls

- **Two sources of truth** for solidity (nav vs collision) drifting apart — derive nav from collision data.
- **Recomputing every frame** for every agent — recompute on change/invalidation, stagger across frames.
- **Ignoring movement model** — flat grid A* will produce paths a jumping enemy can't follow.

## P2-AI-001 specification (walker archetype)

This section is the authoritative contract for the first enemy navigation and spawn pass
([P2-AI-001 ticket](tickets/p2-ai-001-specify-enemy-spawn-and-pathfinding-rules.md)). Visual
presentation is owned by [P2-VISUAL-003](tickets/p2-visual-003-specify-monster-visual-integration-for-enemies.md);
behavior code must not reference vendor sprites directly.

### Solidity source of truth

- **Walkable air cell:** `!SandboxTile.IsSolid` at the agent's foot tile (tile center or foot
  anchor — pick one and keep it consistent in tests).
- **Ground support:** the tile directly below the foot cell is solid (`SandboxTile.IsSolid`).
- Nav, collision, and spawn candidate checks all read the same world grid — no parallel nav mask.

### Movement model and constants

Prototype walker archetype values (tunable via named constants, not magic numbers in call sites):

| Constant | Value | Meaning |
|----------|-------|---------|
| `maxJumpHeight` | `3` | Max tiles the walker can jump **up** from standing |
| `maxJumpGap` | `4` | Max horizontal gap (tiles) the walker can jump **across** |
| `maxFallDistance` | `12` | Max ledge drop before the search refuses the edge; unlimited fall is **out of scope** for P2 (no fall damage yet) |
| `maxExpansionsPerRequest` | `2048` | A* node expansion cap per path request |
| `maxRequestsPerTick` | `4` | Path requests processed per simulation tick across all agents |
| `pathHeuristic` | Octile | `max(Δx, Δy) + (√2 − 1) × min(Δx, Δy)` on the tile grid |

**Edge types** expanded during neighbor generation:

1. **Walk** — move ±1 tile horizontally when foot cell is air and support below is solid; vertical
   step is 0.
2. **Jump arc** — from a supported foot cell, enumerate landing foot cells up to `maxJumpHeight`
   above and up to `maxJumpGap` away horizontally; each arc must pass a clearance probe (no solid
   tiles intersecting the body AABB along the arc). Cost = horizontal + vertical Manhattan distance.
3. **Fall** — from a supported foot cell at a ledge, step down tile-by-tile until support is found
   or `maxFallDistance` is exceeded. Cost = drop distance.

A returned path must be **followable** by the walker controller without teleports: each step is
walk, jump, or fall per the movement model.

**Budget exhaustion:** when `maxExpansionsPerRequest` is hit, return `no-path`. The agent falls
back to **local steering** (walk horizontally toward the target X, stop at ledges/gaps) until the
next scheduled recompute — never stall the frame.

**Determinism:** neighbor iteration order is fixed (e.g. left→right, down→up); open-set ties break
on lower `g`, then lower `h`, then lexicographic `(x, y)`.

### Dynamic terrain and nav-dirty

When `SandboxWorld.SetTile` mutates a cell:

1. Mark the containing chunk **nav-dirty** alongside existing render/collision dirty flags.
2. Do **not** synchronously recompute agent paths inside `SetTile`.
3. Agents holding a path that crosses a nav-dirty cell invalidate on their next step and enqueue a
   lazy recompute (subject to `maxRequestsPerTick`).

EditMode tests use a pure grid accessor implementing the same solidity rules without a Unity scene.

### Spawn rules (walker)

Spawn selection runs on a cadence (`spawnInterval` seconds) and only considers **loaded** chunks.

| Constant | Value | Meaning |
|----------|-------|---------|
| `minSpawnDistance` | `24` | Min Chebyshev distance (tiles) from player foot tile |
| `maxSpawnDistance` | `64` | Max Chebyshev distance from player foot tile |
| `spawnLightThreshold` | `3` | Underground spawn only when light ≤ this (0–15); until P2-LIGHT-001 lands, treat unlit cells as `0` and surface cells as `15` |
| `populationCap` | `8` | Max live walker enemies in the loaded-chunk set |
| `spawnInterval` | `6` | Seconds between spawn attempts per area controller |

**Candidate cell** must satisfy all of:

- Foot tile is air; tile below is solid (same support rule as movement).
- Inside the **loaded-chunk** set (never spawn in unloaded chunks).
- Outside the **camera view** rectangle (expanded by one tile padding).
- Chebyshev distance from player ∈ `[minSpawnDistance, maxSpawnDistance]`.
- If below the surface band (Y > surface + 2 tiles), light ≤ `spawnLightThreshold`.

On success, spawn via P2-VISUAL-003 `MonsterSpawnHelper` and drive locomotion through
`MonsterLocomotionDriver`. If the player target leaves the loaded set, idle agents **despawn** after
`despawnGraceSeconds` (default `10`) — document in gameplay systems.

### Out of scope (recorded follow-ups)

- Flying, swimming, ladders/ropes/one-way platforms as nav edges.
- Combat, damage, loot (P4-CONTENT-001).
- Waypoint graphs and third-party pathfinding packages (scaling options below remain valid).

### EditMode verification fixtures (required before close)

Pathfinding table tests on handcrafted grids:

- Flat walk; jump up 1–`maxJumpHeight`; refuse jump above limit.
- Cross gap ≤ `maxJumpGap`; fail on wider gap.
- Fall from ledge within `maxFallDistance`; fail beyond limit.
- Blocked route returns `no-path` within expansion budget.
- Dynamic: carve tunnel → path succeeds after nav-dirty; seal tunnel → in-flight path invalidates.

Spawn table tests:

- Respect distance band, air/support, light threshold, population cap, and camera exclusion.

Play-mode (exit evidence): one walker chases the player across platforms and through a dug tunnel;
no wall-walking; spawns never inside camera view.

## See also

- [Collision & Physics](05-collision-physics.md) — shared solid/empty grid.
- [Chunking](03-chunking.md) — nav-dirty marking on edits.
- [Procedural Generation](07-procedural-generation.md) — reachability validation reuses pathfinding.
