# 14 — Roadmap

> **Status:** Planning.
> **Decisions:** Build the world pipeline first; add simulation, then persistence, then multiplayer.
> **Invariants:** Don't start a phase before its prerequisites are stable and tested.

Estimates assume ~40 hrs/week. A solo dev trends to the upper range; a 2–3 person team can
parallelize world / networking / UI and compress it.

## Phases

| Phase | Duration | Goals |
|-------|----------|-------|
| **Prototype** (vertical slice) | 4–8 wk | Chunked tiles, player move/jump, basic world gen, place/break one block, rudimentary collision |
| **Core Systems Alpha** | 12–20 wk | Full gen (biomes, caves), lighting, simple liquids, enemy pathfinding, inventory UI, save/load, content registries |
| **Networking Alpha** | 6–10 wk | Server-authoritative movement and tile edits, chunk sync, LAN testing |
| **Feature Complete / Beta** | 12–16 wk | Crafting, content, mod pipeline, editor tools, multiplayer polish, optimization |
| **RC / Launch** | 8–12 wk | QA, profiling, platform porting, save migrations, cloud saves, launch polish |

## Milestone detail

| Milestone | Solo focus | Small-team additions | Timebox |
|-----------|-----------|----------------------|---------|
| Vertical Slice | Move/jump; chunk load/unload; place/break one block; test world gen | + short demo level | 4–6 wk |
| Core Systems Alpha | Full gen; collision; simple lighting; inventory/pickup; basic enemies | + AI pathfinding; water sim | 3–5 mo |
| Networking Alpha | Sync block changes; two local players | + LAN QA; basic server-client | ~2 mo |
| Feature Complete | Full lighting/liquids, combat, crafting UI, balancing | + multiplayer lobby; mod loading UI | 3–4 mo |
| Beta | Content completion; polish; optimization | + playtest loop; UX polish | 2–3 mo |
| RC | Final testing; platform porting; docs | + release/publishing | 1–2 mo |

## Implementation priorities

The concrete build order. Each step is testable before the next begins.

1. **Deterministic chunk coordinate system + tests.** World↔chunk↔local with negative-coordinate
   correctness (see [Data Models](02-data-models.md)). Everything sits on this.
2. **Chunk-local render rebuilds.** Tilemap-per-chunk; `NeedsRenderRebuild` consumed when visible
   (see [Rendering](04-rendering.md)).
3. **Manual tile collision** for a basic controller (see [Collision & Physics](05-collision-physics.md)).
4. **Tile-edit events + per-subsystem dirty flags** through the single `SetTile` choke point
   (see [Architecture](01-architecture.md)).
5. **Simple deterministic generator + save/load** (seed + dirty-chunk diffs)
   (see [Procedural Generation](07-procedural-generation.md), [Saving & Loading](11-saving-loading.md)).
6. **Lighting and liquids** — only after the world pipeline is stable
   (see [Lighting](06-lighting.md), [Liquids](08-liquids.md)).
7. **Multiplayer last** — only once single-player tile edits, persistence, and validation rules are
   clear (see [Multiplayer](10-multiplayer.md)).

> **Sequencing rationale:** lighting/fluids depend on a working chunk+edit pipeline; multiplayer
> depends on a clean authoritative `SetTile` and a chunk-diff format that save/load already exercises.
> Building them early means rework.

## See also

- [Overview & Principles](00-overview.md) — scope levels and risks.
- [Architecture](01-architecture.md) — the systems being sequenced.
