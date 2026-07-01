---
type: Task
id: P2-VISUAL-005
title: "[P2-VISUAL-005] Specify tile engine props, bridges, ladders, and parallax backgrounds."
description: Integrate remaining PixelTileEngine content — props, bridges, ladders, and parallax backgrounds — via PropCatalog and presentation contracts.
status: open
phase: "Phase P2 — Core systems alpha"
github_project: "https://github.com/users/synthet/projects/2"
resource: wiki/tickets/p2-visual-005-specify-tile-engine-props-and-parallax-backgrounds.md
tags: [docs, wiki, ticket, visual, terrain, props, p2]
timestamp: 2026-07-01T05:00:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/visual-integration.md"
  - "docs/wiki/15-assets-integration.md"
  - "docs/wiki/tickets/p2-visual-001-specify-visual-catalog-import-pipeline-contract.md"
---

# [P2-VISUAL-005] Specify tile engine props, bridges, ladders, and parallax backgrounds.

## Open knowledge summary

This ticket integrates the remaining `PixelTileEngine` licensed content that has no current consumer in ProjectTwelve: approximately 97 decorative prop sprites, 5 bridge variants, ladder tiles, and 4 parallax background layers. It introduces a `PropCatalog` ScriptableObject and importer (mirroring `AutotileCatalogImporter`), documents bridge and ladder as distinct tile render rules, and specifies a `ParallaxBackgroundController` contract for scene-level backgrounds.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** Pending creation via `scripts/sync_wiki_tickets_to_github.py`.
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a developer working on the P2 milestone, I want props, bridges, ladders, and parallax backgrounds specified and catalogued so that the world can display decorative and structural tile-engine art without vendor demo scripts.

## Requirements

### Functional requirements

1. Document the licensed prop, bridge, ladder, and background inventory under `Assets/_Licensed/PixelFantasy/PixelTileEngine/Tiles/`.
2. Define a `PropCatalog` schema and `PropCatalogImporter` under `Assets/Scripts/Editor/Visual/` following the P2-VISUAL-001 catalog pipeline contract.
3. Specify props as non-colliding decoration initially (visual-only; collision deferred).
4. Specify bridge tiles with horizontal connectivity render rules (distinct from ground autotiles).
5. Specify ladder tiles as a distinct tile type; climb behavior and animation deferred to a future gameplay ticket.
6. Specify a `ParallaxBackgroundController` contract: biome-selectable layer sets from `Tiles/Backgraund/` (Layer0–3).

### Non-functional requirements

1. Props and backgrounds are presentation-only; simulation stores decoration IDs or metadata, not sprite paths.
2. Catalog generation follows `LocalImportConfig` and paid-asset policy (catalogs in submodule).
3. No vendor `TileMap` or `LevelBuilder` demo scripts at runtime.

## Acceptance criteria

- Prop/bridge/ladder/background inventory documented in `docs/wiki/visual-integration.md`.
- `PropCatalog` schema and importer spec documented (implementation may follow in same or subsequent PR).
- At least one prop placed and visible in play mode or a documented spawn API.
- Parallax background contract documented with layer scroll factors and biome selection.
- Bridge and ladder render rules documented (connectivity or tile-type contract).
- P2-VISUAL-001 cross-references this ticket for extended catalog pipeline.
- The GitHub issue and this markdown ticket link to each other.

## Detailed technical specifications

### Scope

- Specification and initial catalog/importer design; full world decoration placement system may be incremental.
- Destructible props (barrels, chests) remain in P2-VISUAL-003 / monster catalog — out of scope here.
- Climb animation and ladder physics deferred until player locomotion supports Climb state.

### Inputs and dependencies

- `Assets/_Licensed/PixelFantasy/PixelTileEngine/Tiles/Props/` (~97 sprites)
- `Assets/_Licensed/PixelFantasy/PixelTileEngine/Tiles/Bridge/` (5 variants)
- `Assets/_Licensed/PixelFantasy/PixelTileEngine/Tiles/Ladder/`
- `Assets/_Licensed/PixelFantasy/PixelTileEngine/Tiles/Backgraund/` (4 parallax layers)
- Catalog pipeline: `docs/wiki/tickets/p2-visual-001-specify-visual-catalog-import-pipeline-contract.md`
- Asset domain requirements: `docs/wiki/15-assets-integration.md`

### Suggested components

| Component | Role |
|-----------|------|
| `PropCatalog` | ScriptableObject listing prop sprite entries by stable ID |
| `PropCatalogImporter` | Editor menu + Python regen path |
| `PropRenderer` or tile-metadata channel | Places prop sprites in world (non-colliding) |
| `ParallaxBackgroundController` | Scene overlay; scrolls background layers by camera |
| Bridge/ladder tile types | Extend `SandboxTileVisualCatalog` or separate render pass |

### Verification plan

- Regenerate `PropCatalog` on submodule-enabled machine.
- Play-mode or scene test: one prop visible; parallax layers scroll with camera.
- EditMode: catalog importer produces expected entry count without licensed PNG fixtures in public tests.

## Documentation impact

- `docs/wiki/visual-integration.md` — props, bridges, ladders, backgrounds section.
- `docs/wiki/tickets/p2-visual-001-specify-visual-catalog-import-pipeline-contract.md` — note that `PropCatalog` follows same pipeline.

## Exit evidence checklist

- [ ] GitHub issue URL is recorded in this ticket.
- [ ] GitHub issue links back to this markdown ticket.
- [ ] Spec references reviewed and updated if needed.
- [ ] Inventory and contracts documented in `visual-integration.md`.
- [ ] P2-VISUAL-001 cross-link present.
- [ ] Play-mode or scene verification notes attached.
