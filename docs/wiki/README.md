---
type: Index
title: Open Knowledge Base — Index
description: The canonical wiki index for ProjectTwelve architecture, design decisions, and specification references.
resource: wiki/README.md
tags: [docs, wiki, index, architecture]
timestamp: 2026-06-27T00:00:00Z
okf_version: 0.1
---

# Open Knowledge Base — Index

This wiki is the public, human-readable reference for the Unity 2D sandbox prototype. It captures architectural intent, implementation boundaries, design decisions, and future work in a durable format that can be read by contributors, maintainers, tools, or assistants without depending on prompt-only context.

The wiki contains **three complementary page sets**. They overlap on purpose: use the prototype-aligned pages for current code work, the numbered subsystem reference for design rationale, and the visual presentation pages for sprite/avatar integration.

## Quick navigation

| Task | Start with | Then check |
|------|------------|------------|
| Understand the current prototype | [Project Brief](project-brief.md) | [Architecture Map](architecture-map.md) |
| Change world/chunk/tile behavior | [World and Chunk Data](world-and-chunk-data.md) | [02 Data Models](02-data-models.md), [03 Chunking](03-chunking.md) |
| Change rendering or collision | [Rendering and Collision](rendering-and-collision.md) | [04 Rendering](04-rendering.md), [05 Collision & Physics](05-collision-physics.md) |
| Change generation or saves | [Generation and Saving](generation-and-saving.md) | [07 Procedural Generation](07-procedural-generation.md), [11 Saving & Loading](11-saving-loading.md) |
| Plan future work | [Roadmap and Tasks](roadmap-and-tasks.md) | [Spec-Driven Development Tasks](spec-driven-development-tasks.md) |
| Integrate visuals | [Visual integration](visual-integration.md) | [Visual behavior spec](../VISUAL_BEHAVIOR_SPEC.md), [Visual setup](../VISUAL_SETUP.md) |

## Knowledge format

Each page should stay useful outside of any single chat or tool session:

- Start from project facts, decisions, and invariants rather than instructions to a specific assistant.
- Prefer stable headings, concise tables, diagrams, and cross-links over prompt text.
- Capture open tasks as implementation-ready work items with scope, acceptance criteria, and validation notes.
- Link back to canonical sources when a page summarizes a larger design document.
- Avoid private tool names, session-specific assumptions, or copy/paste prompt templates in the wiki.

## A. Prototype-aligned knowledge pages

Maps closely to the code that exists today (`Assets/Scripts/Sandbox*.cs`). Start here when working on the current prototype or checking whether an idea is already represented in implementation notes.

1. [Project Brief](project-brief.md)
2. [Architecture Map](architecture-map.md)
3. [World and Chunk Data](world-and-chunk-data.md)
4. [Rendering and Collision](rendering-and-collision.md)
5. [Lighting, Liquids, and Simulation](simulation-systems.md)
6. [Generation and Saving](generation-and-saving.md)
7. [Gameplay Systems](gameplay-systems.md)
8. [Multiplayer and Modding](multiplayer-and-modding.md)
9. [Roadmap and Tasks](roadmap-and-tasks.md)

## B. Detailed subsystem reference (numbered)

A deeper, decision-oriented reference. Each page opens with a **Status / Decisions / Invariants** block and ends with **See also** links, so a single page is enough to work one subsystem. Use this when you need the *why* and the pitfalls, not just the *what*.

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
| 15 | [Asset Integration Requirements](15-assets-integration.md) | Sprites, atlases, animations, rotations, and Unity/engine asset seams |
| 16 | [Architectural Risks](16-architectural-risks.md) | High-impact technical risks with mitigations, owners, decision deadlines |
| — | [Enemy Navigation & Spawn Rules](enemy-nav-and-spawn.md) | P2 spec for grid A* platformer pathfinding and enemy spawning rules |
| — | [Flexible HUD Framework](flexible-hud-framework.md) | uGUI layers, theming, scale, focus, inventory presentation, and migration contract |
| — | [Quality Gates](quality-gates.md) | Required checks: automated tests, link hygiene, deterministic verification, profiler targets |
| — | [Save/Load Format Spec](save-load-format.md) | P2-SAVE-001 spec: versioned header, dirty-chunk diffs, atomic writes, corruption fail-safe, migration |
| — | [Spec-Driven Development Tasks](spec-driven-development-tasks.md) | Phase-by-phase task backlog, acceptance criteria, and verification gates |
| — | [Architecture Blueprint](architecture-blueprint.md) | Text translation of the visual blueprint canvas (10 figures) |
| — | [Glossary](glossary.md) | Shared vocabulary |

## C. Visual presentation

Project-owned rendering, player avatar, creature, and effect systems. Local source art paths are configured per machine — see [Visual setup](../VISUAL_SETUP.md).

| Page | Scope |
|------|-------|
| [Visual integration](visual-integration.md) | Tile mapping, avatar factory, vendor parity, creature/effect touchpoints |
| [Visual catalog import pipeline](visual-catalog-import-pipeline.md) | Config precedence, deterministic autotile/character/monster catalog regeneration, and code-only behavior |
| [Licensed assets reference](licensed-assets-reference.md) | Public contracts vs private submodule inventory; regen checklist |
| [Visual behavior spec](../VISUAL_BEHAVIOR_SPEC.md) | Autotile masks, sprite sheets, animator API |
| [Asset integration requirements](15-assets-integration.md) | Sprites, atlases, animation schema (Set B reference) |

### HUD & PixelLab asset generation

| Page | Scope |
|------|-------|
| [HUD asset manifest](hud-assets-manifest.md) | Exact bitmap, Unity import, layout, and generation contract for the creative HUD |
| [HUD redesign for PixelLab](hud-redesign-pixellab.md) | PixelLab capability research and the v3 asset contract plan sized to the tool's generation tiers |
| [PixelLab API v2 reference](pixellab-api-v2.md) | Routing, security, polling, and integration rules for PixelLab MCP/REST |
| [HUD development conversation summary](hud-conversation-summary.md) | Chronological record of the HUD design, generation, wiring, and validation work |

### How the page sets correspond

| Topic | Set A (prototype) | Set B (reference) |
|-------|-------------------|-------------------|
| World/chunk/tile data | [World and Chunk Data](world-and-chunk-data.md) | [02 Data Models](02-data-models.md), [03 Chunking](03-chunking.md) |
| Rendering & collision | [Rendering and Collision](rendering-and-collision.md) | [04 Rendering](04-rendering.md), [05 Collision & Physics](05-collision-physics.md) |
| Lighting & liquids | [Simulation Systems](simulation-systems.md) | [06 Lighting](06-lighting.md), [08 Liquids](08-liquids.md) |
| Generation & saving | [Generation and Saving](generation-and-saving.md) | [07 Generation](07-procedural-generation.md), [11 Saving & Loading](11-saving-loading.md) |
| Gameplay / pathfinding | [Gameplay Systems](gameplay-systems.md) | [09 Pathfinding](09-pathfinding.md) |
| Multiplayer & modding | [Multiplayer and Modding](multiplayer-and-modding.md) | [10 Multiplayer](10-multiplayer.md), [12 Modding](12-modding.md) |
| Asset integration | — | [15 Asset Integration Requirements](15-assets-integration.md) |
| Visual presentation | [Visual integration](visual-integration.md) | [Visual setup](../VISUAL_SETUP.md) |
| Roadmap | [Roadmap and Tasks](roadmap-and-tasks.md) | [14 Roadmap](14-roadmap.md) |

## Cross-cutting invariants

These hold across the whole project; individual pages may add more.

1. **Chunk-local everything.** Rendering, colliders, lighting, and fluids are rebuilt per-chunk. No subsystem may take a global rebuild cost on a single tile edit.
2. **Separate dirty flags per subsystem.** A tile edit sets independent render/collider/light/fluid flags; subsystems consume them on their own cadence.
3. **Deterministic generation.** The world is reproducible from `seed` + generation settings; saves persist diffs from the generated baseline where practical.
4. **Server-authoritative.** In multiplayer the server owns canonical world state; clients request edits and receive deltas.
5. **Data-driven content.** Tiles/items/biomes/recipes come from registries keyed by stable string IDs, not hard-coded enums.
6. **No engine lock-in at the seams.** Tile-diff, chunk subscription, validation, and save logic stay independent of any specific networking package or render backend.

## Source of truth

The wiki summarizes and operationalizes the architecture in [`../terraria-like-unity-design.md`](../terraria-like-unity-design.md) (canonical product-level plan), with expanded context in [`../terraria-like-unity-design-detailed.md`](../terraria-like-unity-design-detailed.md). When a wiki page and the design document disagree, treat the design document as the product-level source of truth, then update the stale page or call out the follow-up explicitly. For precedence across all repository references, see [`../CANONICAL_SOURCES.md`](../CANONICAL_SOURCES.md).

## Maintenance notes

- Keep the prototype-aligned pages grounded in the code that exists today.
- Use the numbered reference pages for decisions, invariants, tradeoffs, and future-ready contracts.
- Add new pages to this index and to [`../INDEX.md`](../INDEX.md).
- Prefer cross-links over duplicating long sections between page sets.
