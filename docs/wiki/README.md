# LLM Wiki — Index

This wiki is the working knowledge base for the Unity 2D sandbox project, organized so an LLM (or
human) can locate architectural intent, implementation boundaries, and future work without
rereading the full design document each time.

It currently holds **two complementary page sets**. They overlap on purpose — read whichever fits
your task, and cross-reference the other.

## A. Prototype-aligned wiki

Maps closely to the code that exists today (`Assets/Scripts/Sandbox*.cs`). Start here when working
on the current prototype.

1. [Project Brief](project-brief.md)
2. [Architecture Map](architecture-map.md)
3. [World and Chunk Data](world-and-chunk-data.md)
4. [Rendering and Collision](rendering-and-collision.md)
5. [Lighting, Liquids, and Simulation](simulation-systems.md)
6. [Generation and Saving](generation-and-saving.md)
7. [Gameplay Systems](gameplay-systems.md)
8. [Multiplayer and Modding](multiplayer-and-modding.md)
9. [Roadmap and LLM Task Prompts](roadmap-and-llm-prompts.md)

## B. Detailed subsystem reference (numbered)

A deeper, decision-oriented reference. Each page opens with a **Status / Decisions / Invariants**
block and ends with **See also** links, so a single page is enough to work one subsystem. Use this
when you need the *why* and the pitfalls, not just the *what*.

| # | Page | Scope |
|---|------|-------|
| 00 | [Overview & Principles](00-overview.md) | Goals, scope, guiding principles |
| 01 | [Architecture](01-architecture.md) | System decomposition, ownership, data flow |
| 02 | [Data Models](02-data-models.md) | `Tile`, `Chunk`, `World`, `Entity`, `Inventory`, coordinates |
| 03 | [Chunking](03-chunking.md) | Chunk size, load/unload, dirty flags, streaming |
| 04 | [Rendering](04-rendering.md) | Tilemap vs custom mesh vs instancing |
| 05 | [Collision & Physics](05-collision-physics.md) | Why not one big CompositeCollider2D; tile collision |
| 06 | [Lighting](06-lighting.md) | Tile lightmap, BFS propagation, dirty regions |
| 07 | [Procedural Generation](07-procedural-generation.md) | Multi-pass terrain, caves, biomes, ores, structures |
| 08 | [Liquids](08-liquids.md) | Cellular-automaton fluid simulation |
| 09 | [Pathfinding](09-pathfinding.md) | Grid A*, dynamic terrain updates |
| 10 | [Multiplayer](10-multiplayer.md) | Server-authoritative sync, tile deltas |
| 11 | [Saving & Loading](11-saving-loading.md) | Chunk saves, diffs, versioning |
| 12 | [Modding & Content](12-modding.md) | Registries, data-driven content, Addressables |
| 13 | [Tooling, Testing & Profiling](13-tooling-testing.md) | Debug views, tests, profiler targets |
| 14 | [Roadmap](14-roadmap.md) | Milestones, sequencing, implementation priorities |
| — | [Architecture Blueprint](architecture-blueprint.md) | Text translation of the visual blueprint canvas (10 figures) |
| — | [Glossary](glossary.md) | Shared vocabulary |

### How the two sets correspond

| Topic | Set A (prototype) | Set B (reference) |
|-------|-------------------|-------------------|
| World/chunk/tile data | [World and Chunk Data](world-and-chunk-data.md) | [02 Data Models](02-data-models.md), [03 Chunking](03-chunking.md) |
| Rendering & collision | [Rendering and Collision](rendering-and-collision.md) | [04 Rendering](04-rendering.md), [05 Collision & Physics](05-collision-physics.md) |
| Lighting & liquids | [Simulation Systems](simulation-systems.md) | [06 Lighting](06-lighting.md), [08 Liquids](08-liquids.md) |
| Generation & saving | [Generation and Saving](generation-and-saving.md) | [07 Generation](07-procedural-generation.md), [11 Saving & Loading](11-saving-loading.md) |
| Gameplay / pathfinding | [Gameplay Systems](gameplay-systems.md) | [09 Pathfinding](09-pathfinding.md) |
| Multiplayer & modding | [Multiplayer and Modding](multiplayer-and-modding.md) | [10 Multiplayer](10-multiplayer.md), [12 Modding](12-modding.md) |
| Roadmap | [Roadmap and LLM Task Prompts](roadmap-and-llm-prompts.md) | [14 Roadmap](14-roadmap.md) |

## Cross-cutting invariants

These hold across the whole project; individual pages may add more.

1. **Chunk-local everything.** Rendering, colliders, lighting, and fluids are rebuilt per-chunk. No
   subsystem may take a global rebuild cost on a single tile edit.
2. **Separate dirty flags per subsystem.** A tile edit sets independent render/collider/light/fluid
   flags; subsystems consume them on their own cadence.
3. **Deterministic generation.** The world is reproducible from `seed` + generation settings; saves
   persist diffs from the generated baseline where practical.
4. **Server-authoritative.** In multiplayer the server owns canonical world state; clients request
   edits and receive deltas.
5. **Data-driven content.** Tiles/items/biomes/recipes come from registries keyed by stable string
   IDs, not hard-coded enums.
6. **No engine lock-in at the seams.** Tile-diff, chunk subscription, validation, and save logic stay
   independent of any specific networking package or render backend.

## Source of truth

Both page sets expand the architecture in [`../terraria-like-unity-design.md`](../terraria-like-unity-design.md)
(canonical plan), with a long-form companion in
[`../terraria-like-unity-design-detailed.md`](../terraria-like-unity-design-detailed.md). When a wiki
page and the design document disagree, treat the design document as the product-level source of
truth and the wiki as the implementation-facing index.
