---
type: Task
id: P2-VISUAL-004
title: "[P2-VISUAL-004] Specify biome ground and cover tileset expansion."
description: Extend SandboxTileVisualCatalog and tile ID mapping to use remaining licensed autotile sets for biome-specific terrain.
status: open
phase: "Phase P2 — Core systems alpha"
github_project: "https://github.com/users/synthet/projects/2"
resource: wiki/tickets/p2-visual-004-specify-biome-tileset-expansion.md
tags: [docs, wiki, ticket, visual, terrain, biomes, p2]
timestamp: 2026-07-01T05:00:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/visual-integration.md"
  - "docs/VISUAL_BEHAVIOR_SPEC.md"
  - "docs/wiki/tickets/p2-gen-001-specify-biome-cave-structure-and-ore-generation-passes.md"
---

# [P2-VISUAL-004] Specify biome ground and cover tileset expansion.

## Open knowledge summary

This ticket extends `SandboxTileVisualCatalog` beyond the current seven-tile mapping (Dirt, Grass, Stone, four ores) to consume the remaining licensed autotile sets in `Assets/_Licensed`: Frozen, Magma, and Sand ground tilesets; Ice, Snow, Moss, Carpet, WoodenFloor, Lianas, and Icicles cover tilesets. It defines how new or extended `SandboxTileIds` (or biome metadata) map to tilesets and how P2-GEN-001 biome generation selects them per biome.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** Pending creation via `scripts/sync_wiki_tickets_to_github.py`.
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a developer working on the P2 milestone, I want biome-specific ground and cover tilesets specified and mapped so that procedural generation can place visually distinct terrain (desert, snow, volcanic) using licensed autotile art without breaking existing autotile invariants.

## Requirements

### Functional requirements

1. Document the unused licensed autotile inventory (ground: Frozen, Magma, Sand; cover: Ice, Snow, Moss, Carpet, WoodenFloor, Lianas, Icicles, and remaining Grass/Sand cover variants).
2. Extend `SandboxTileVisualCatalog` with mappings for new tile IDs or biome-driven tileset selection.
3. Define how P2-GEN-001 biome generation passes select tile IDs per biome (cross-link required).
4. Preserve autotile rule-matching invariants from `VISUAL_BEHAVIOR_SPEC.md` sections 1–2 (mask construction, connectivity groups, cover cliff rules).

### Non-functional requirements

1. Simulation continues to store tile IDs only; visuals resolve at render time.
2. Existing seven-tile mapping (Humus, Rocks, BricksA–D, GrassA) must remain backward-compatible for current seeds.
3. No licensed art blobs in the public repo; tileset sources remain in `Assets/_Licensed`.

## Acceptance criteria

- Unused tileset inventory documented in `docs/wiki/visual-integration.md`.
- `SandboxTileVisualCatalog` extended with biome tile mappings (or documented contract for biome metadata → tileset resolution).
- P2-GEN-001 cross-references this ticket for tile ID selection per biome.
- Autotile connectivity groups defined for each new ground/cover pairing.
- Play-mode verification: at least one seed produces visually distinct biome regions (e.g. sand desert, frozen surface).
- The GitHub issue and this markdown ticket link to each other.

## Detailed technical specifications

### Scope

- Tile ID → tileset mapping and documentation; biome placement rules are specified here but implemented in coordination with P2-GEN-001.
- Background wall layer (separate render pass) is out of scope — deferred to a future ticket.
- Bridge, ladder, and prop tiles are out of scope — see P2-VISUAL-005.

### Inputs and dependencies

- `Assets/Scripts/Sandbox/SandboxTileVisualCatalog.cs`
- `Assets/Scripts/Sandbox/SandboxTile.cs` / `SandboxTileIds`
- `Assets/_Licensed/Settings/Visual/AutotileCatalog.asset`
- Biome generation contract: `docs/wiki/tickets/p2-gen-001-specify-biome-cave-structure-and-ore-generation-passes.md`
- P2-VISUAL-001 catalog pipeline (autotile catalog regen).

### Suggested tileset mapping (initial)

| Biome | Ground tileset | Cover tileset |
|-------|----------------|---------------|
| Default (current) | Humus, Rocks, BricksA–D | GrassA |
| Desert | Sand | SandA or SandB |
| Snow / tundra | Frozen | SnowA or Ice |
| Volcanic | Magma | — |
| Forest (extended) | Humus | Moss or GrassB/C |

Final mapping subject to spec review and generation pass design.

### Verification plan

- EditMode: autotile resolver tests pass for new tileset names (no licensed PNGs in tests).
- Play-mode: generate world with biome-aware seed; confirm distinct ground/cover visuals per biome region.
- Regression: existing P1 tile mapping unchanged for default terrain.

## Documentation impact

- `docs/wiki/visual-integration.md` — default tile mapping table extended; unused tileset inventory.
- `docs/wiki/tickets/p2-gen-001-specify-biome-cave-structure-and-ore-generation-passes.md` — cross-link to this ticket.

## Exit evidence checklist

- [ ] GitHub issue URL is recorded in this ticket.
- [ ] GitHub issue links back to this markdown ticket.
- [ ] Spec references reviewed and updated if needed.
- [ ] Tileset inventory and mapping documented in `visual-integration.md`.
- [ ] P2-GEN-001 cross-link present.
- [ ] Play-mode biome visual verification notes attached.
