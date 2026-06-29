---
type: Source-of-Truth Map
title: Baseline Scope Statement
description: The single P0 baseline that fixes core genre, prototype target, non-goals, and shared terminology for ProjectTwelve, consolidating the product brief, overview, and glossary.
resource: wiki/baseline-scope.md
tags: [wiki, scope, baseline, p0, glossary]
timestamp: 2026-06-28T00:00:00Z
okf_version: 0.1
---

# Baseline Scope Statement

> **Status:** Baseline (P0). This page is the single agreed scope statement for the
> prototype milestone.
> **Owns:** Core genre, prototype target, non-goals, and the canonical term index.
> **Consolidates:** [Project Brief](project-brief.md), [Overview & Principles](00-overview.md),
> and the [Glossary](glossary.md). When those pages and this one disagree on *scope*, this
> page is authoritative; for subsystem detail defer to the page that owns the term.

This page exists so the GitHub issue, an implementation branch, a reviewer, and a tool can all
trace back to one unambiguous statement of *what we are building right now* — without
reconstructing it from three separate pages.

## Core genre

A **Unity 2D side-scrolling sandbox** in the lineage of *Terraria* and *Starbound*:

- Fully **destructible terrain** made of tiles.
- **Procedurally generated** worlds (biomes, caves, ores, structures) reproducible from a seed.
- **Block placement and removal** as the core verb.
- Built toward day/night and emissive **lighting**, flowing **liquids**, **inventory** and
  crafting, **enemies** with pathfinding, and **optional server-authoritative multiplayer** —
  each represented in documentation before it becomes production code.

The naive Unity approach — one giant `Tilemap` with one global `CompositeCollider2D` and Unity
2D lights — collapses under destructible, large-scale worlds. The architecture exists to avoid
that; see [Overview & Principles](00-overview.md#guiding-principles).

## Target prototype (vertical slice)

The current codebase stays intentionally small. The prototype proves the **world pipeline and
edit loop** and nothing more:

| The slice proves | Owning spec |
|------------------|-------------|
| Chunked world storage | [World and Chunk Data](world-and-chunk-data.md), [03 Chunking](03-chunking.md) |
| Procedural chunk generation (deterministic from seed) | [07 Procedural Generation](07-procedural-generation.md) |
| Chunk-local rendering rebuilds | [04 Rendering](04-rendering.md) |
| Chunk-local collision rebuilds | [05 Collision & Physics](05-collision-physics.md) |
| Basic platformer movement | [Gameplay Systems](gameplay-systems.md) |
| Mouse-driven tile placement and removal | [World and Chunk Data](world-and-chunk-data.md#tile-edit-choke-point-p1-edit-001) |

Everything outside this slice is **documented before it is implemented**. See
[Roadmap](14-roadmap.md) for sequencing and the full game scope.

## Non-goals (current barebone state)

- No hex-grid demo or unrelated sample gameplay.
- No generated demo-scene clutter.
- No committed IDE-generated Unity solution files.
- No production implementation for inventories, enemies, liquids, lighting, multiplayer, or
  modding until their interfaces are designed and accepted.

These are *current-state* non-goals, not permanent exclusions: liquids, lighting, inventory,
enemies, multiplayer, and modding are all in-scope for the full game and have planning pages.

## Invariants the scope depends on

The prototype and the full game share these cross-cutting rules
([source](README.md#cross-cutting-invariants)):

1. **Chunk-local cost.** Every per-tile operation is confined to the affected chunk(s); no
   subsystem takes a global rebuild cost on a single edit.
2. **Separate dirty flags per subsystem.** A tile edit sets independent
   render/collider/light/fluid/save flags, each drained on its own cadence.
3. **Determinism first.** Generation is reproducible from a seed; saves persist diffs from the
   generated baseline.
4. **Server owns truth.** Single-player is multiplayer with one local client; design
   server-authoritative from the start.
5. **Data over code.** Tiles/items/biomes/recipes come from registries keyed by stable string
   IDs, not new enum values and `switch` statements.

## Terminology

The [Glossary](glossary.md) is the canonical term index; each term links to the page that owns
it. The terms below are the minimum vocabulary needed to read this scope statement
unambiguously:

| Term | Meaning in scope | Owner |
|------|------------------|-------|
| **Chunk** | Fixed-size square of tiles (default 32×32); the unit of load/save/rebuild. | [03 Chunking](03-chunking.md) |
| **Tile** | The atomic world cell: id + light + fluid + metadata. | [02 Data Models](02-data-models.md) |
| **World / chunk / local coordinate** | Global tile index → which chunk (`floorDiv`) → index within the chunk (`[0, Size)`). | [02 Data Models](02-data-models.md) |
| **Dirty flag** | Per-chunk marker that a subsystem has pending work; flags are independent. | [03 Chunking](03-chunking.md) |
| **Seed** | Value that makes generation deterministic and reproducible. | [07 Procedural Generation](07-procedural-generation.md) |
| **Diff (chunk diff / edit log)** | Changes from the generated baseline that a save persists, instead of the full world. | [11 Saving & Loading](11-saving-loading.md) |
| **Registry** | Lookup from a stable string content ID to a definition (tile/item/biome/recipe). | [12 Modding & Content](12-modding.md) |
| **Server-authoritative** | The server owns canonical world state; clients request edits and receive deltas. | [10 Multiplayer](10-multiplayer.md) |

For the full vocabulary (atlas, attenuation, biome, flood-fill, lightmap, streaming, tile delta,
and more), see the [Glossary](glossary.md).

## See also

- [Project Brief](project-brief.md) — vision and prototype scope narrative.
- [Overview & Principles](00-overview.md) — goals, guiding principles, and risk map.
- [Glossary](glossary.md) — the full shared vocabulary.
- [Spec-Driven Development Tasks](spec-driven-development-tasks.md) — the phase-by-phase backlog
  this baseline gates.
