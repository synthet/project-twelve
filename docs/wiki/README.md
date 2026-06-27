# Project Twelve — Engineering Wiki

This wiki is the navigable, LLM-friendly knowledge base for evolving **Project Twelve**
into a *Terraria*/*Starbound*-style 2D sandbox in Unity. It is derived from and
elaborates on the design documents:

- [Unity 2D Sandbox Architecture Plan](../terraria-like-unity-design.md) — canonical, concise plan.
- [Detailed Design Reference](../terraria-like-unity-design-detailed.md) — long-form companion.

The wiki reorganizes that material into focused, cross-linked pages. Each page is
self-contained and front-loaded with the decisions and invariants an agent needs
before touching code, so you can load a single page to work on one subsystem.

## How to use this wiki (for humans and agents)

- **Start at the relevant subsystem page**, not the design doc — pages here are scoped and link out.
- Every page opens with a **Status / Decisions / Invariants** block. Treat invariants as
  hard constraints; if a change violates one, update the page in the same commit.
- **See also** links at the bottom of each page wire the graph together. Follow them
  instead of re-deriving context.
- The [Glossary](glossary.md) defines shared terms (chunk, tile, lightmap, diff, etc.).

## Page index

| # | Page | Scope |
|---|------|-------|
| 00 | [Overview & Principles](00-overview.md) | Goals, scope, guiding principles, current repo state |
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

## Cross-cutting invariants

These hold across the whole project. Individual pages may add more.

1. **Chunk-local everything.** Rendering, colliders, lighting, and fluids are rebuilt
   per-chunk. No subsystem may take a global rebuild cost on a single tile edit.
   See [Chunking](03-chunking.md).
2. **Separate dirty flags per subsystem.** A tile edit sets independent
   render/collider/light/fluid dirty flags; subsystems consume them on their own cadence.
3. **Deterministic generation.** The world is reproducible from `seed` + generation
   settings; saves only persist diffs from the generated baseline where practical.
   See [Saving & Loading](11-saving-loading.md).
4. **Server-authoritative.** In multiplayer the server owns canonical world state; clients
   request edits and receive deltas. See [Multiplayer](10-multiplayer.md).
5. **Data-driven content.** Tiles/items/biomes/recipes come from registries keyed by stable
   string IDs, not hard-coded enums. See [Modding & Content](12-modding.md).
6. **No engine lock-in at the seams.** Tile-diff, chunk subscription, validation, and save
   logic stay independent of any specific networking package or render backend.

## Current repository state

The repo is a **barebone Unity project**, intentionally stripped of the earlier
hexagonal-grid demo. What exists today:

- `Assets/Scripts/PlayerController.cs` — minimal input-driven movement (placeholder).
- `Assets/Scene.unity` — default scene (Main Camera + Directional Light only).

None of the sandbox systems below are implemented yet; this wiki is the build plan.
Follow the [Roadmap](14-roadmap.md) for sequencing.
