# 00 — Overview & Principles

> **Status:** Planning. No sandbox systems implemented yet.
> **Decisions:** 2D side-scrolling sandbox; chunked world; optional multiplayer.
> **Invariants:** See [cross-cutting invariants](README.md#cross-cutting-invariants).

## What we are building

A 2D side-scrolling sandbox in the lineage of *Terraria* and *Starbound*:

- Fully **destructible terrain** made of tiles.
- **Procedurally generated** worlds with biomes, caves, ores, and structures.
- **Day/night** and emissive **lighting** via a tile lightmap.
- **Liquids** (water, lava) that flow.
- **Inventory**, crafting, enemies with pathfinding.
- **Optional multiplayer** (server-authoritative).

Unity *can* do this, but only with a deliberate architecture. The naive approach — one giant
Tilemap with one CompositeCollider2D and Unity's 2D lights — collapses under destructible,
large-scale worlds. The whole design exists to avoid that.

## Scope levels

The same architecture serves two milestones; pick the smallest that proves the next risk.

- **Prototype / vertical slice:** chunked tiles, player movement, basic generation, place/break,
  collision, minimal lighting. Goal: prove the world pipeline and edit loop.
- **Full game:** liquids, rich lighting, enemy pathfinding, save/load, multiplayer, data-driven
  content, mod support, tooling, and performance test coverage.

See [Roadmap](14-roadmap.md) for sequencing and timeboxes.

## Guiding principles

1. **Chunk-local cost.** Every per-tile operation must be confined to the affected chunk(s).
   This is the single most important rule; it drives [chunking](03-chunking.md),
   [rendering](04-rendering.md), [physics](05-collision-physics.md), and [lighting](06-lighting.md).
2. **Separate concerns behind dirty flags.** Editing a tile flags render/collider/light/fluid
   work independently; each subsystem drains its queue on its own schedule and budget.
3. **Determinism first.** Generation is reproducible from a seed. This shrinks saves
   (store diffs) and makes bugs reproducible.
4. **Server owns truth.** Single-player is multiplayer with one local client; designing
   server-authoritative from the start avoids a painful retrofit.
5. **Data over code.** New blocks/items/biomes are data, loaded from registries, not new enum
   values and `switch` statements.
6. **Measure before optimizing, but design for the known cliffs.** The cliffs (global collider
   rebuilds, full-world relight, unbounded saves) are documented; avoid them up front and
   profile the rest.

## Top technical risks (and where they're addressed)

| Risk | Mitigation | Page |
|------|-----------|------|
| Large destructible tilemaps | Chunked data + chunk-local rebuilds | [03](03-chunking.md), [04](04-rendering.md) |
| Expensive collider rebuilds | Per-chunk colliders or manual tile collision | [05](05-collision-physics.md) |
| Terraria-style lighting | Custom tile lightmap + BFS, not Unity 2D lights | [06](06-lighting.md) |
| Fluid simulation cost | Active-cell CA, pause distant chunks | [08](08-liquids.md) |
| Save-file scale | Chunk diffs + compression + versioning | [11](11-saving-loading.md) |
| Multiplayer consistency/cheating | Server-authoritative tile deltas | [10](10-multiplayer.md) |

## See also

- [Architecture](01-architecture.md) — how the pieces fit together.
- [Quality Gates](quality-gates.md) — required checks before merge.
- [Tooling, Testing & Profiling](13-tooling-testing.md) — testing strategy and debug tools.
- [Roadmap](14-roadmap.md) — what to build first.
- [Glossary](glossary.md).
