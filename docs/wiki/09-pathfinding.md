---
type: Technical Reference
title: "Pathfinding"
description: "ProjectTwelve Pathfinding reference — design notes, contracts, and decisions for the pathfinding area of the sandbox prototype."
resource: wiki/09-pathfinding.md
tags: [wiki, pathfinding]
timestamp: 2026-06-28T00:00:00Z
okf_version: 0.1
---

# 09 — Pathfinding

> **Status:** Planning.
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

## See also

- [Collision & Physics](05-collision-physics.md) — shared solid/empty grid.
- [Chunking](03-chunking.md) — nav-dirty marking on edits.
- [Procedural Generation](07-procedural-generation.md) — reachability validation reuses pathfinding.
