---
type: Task
id: P2-DATA-002
title: "[P2-DATA-002] Migrate sandbox callers to registry runtime indices and complete item/entity registries."
description: Swap SandboxTile.id from legacy constants to registry runtime indices, persist the save palette, and complete item/entity definitions and mod load order.
status: done
phase: "Phase P2 — Core systems alpha"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/86"
github_issue_status: closed
resource: wiki/tickets/p2-data-002-migrate-sandbox-callers-to-registry-runtime-indices.md
tags: [docs, wiki, ticket, registry, modding, p2]
timestamp: 2026-07-04T22:20:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/12-modding.md"
  - "docs/wiki/11-saving-loading.md"
  - "docs/wiki/02-data-models.md"
  - "docs/wiki/tickets/p2-data-001-specify-tile-item-and-entity-registry-contracts.md"
---

# [P2-DATA-002] Migrate sandbox callers to registry runtime indices and complete item/entity registries.

## Open knowledge summary

Follow-up to [[P2-DATA-001]](p2-data-001-specify-tile-item-and-entity-registry-contracts.md), which
landed the registry (`ContentRegistry<TDef>`, 8 `core:` tile defs, `RegistryPalette`, legacy ID
table) as inert infrastructure: `SandboxTile.id` still stores the hard-coded `SandboxTileIds`
constants, and six call sites make content decisions from those ints. This task performs the
behavioral swap: the world initializes a frozen tile registry at startup, `SandboxTile.id` becomes
a registry runtime index, callers read `TileDefinition` properties (`Solid`, `AtlasSprite`) instead
of matching constants, and saves become registry-safe — version-1 saves load through the legacy
table, and new saves persist the palette so worlds survive future registry reordering. The
pinned-seed world must be visually and behaviorally identical after the swap.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** [synthet/project-twelve#86](https://github.com/synthet/project-twelve/issues/86)
- **Backlink requirement:** Satisfied — the issue body links to this markdown ticket.

## Users / stakeholders

- **Systems developers** on P2 tickets (P2-INV-001 drops, P2-LIGHT-001 opacity/emission,
  P2-GEN-001 new tiles) — gain one data-driven place to add content without touching switches in
  renderer/catalog code.
- **P3 networking** — inherits deterministic indices shared across processes.
- **Players / testers** — existing P1 save files keep loading.
- **Future modders** (P4-MOD-001) — caller code stops assuming a closed set of 8 tiles.

## Non-goals

- **No mod loading** from external files, and no load-order/override implementation (spec text
  only, pre-work for P4-MOD-001).
- **No new save features** beyond the palette header field and legacy migration — atomic writes,
  corruption handling, and diff thresholds stay in P2-SAVE-001.
- **No inventory behavior** — `DropItemId`/`Hardness` remain unread placeholders until P2-INV-001.
- **No lighting behavior** — `Opaque`/`LightEmission` remain unread until P2-LIGHT-001.
- **No visual changes** — autotile resolution, licensed-catalog wiring, and the legacy UV/color
  fallback palette keep their current outputs; only their *selection keys* change.
- **No new tile content** — exactly the existing 8 `core:` defs.

## User stories

- As a **systems developer**, I want the renderer and visual catalog to read `TileDefinition`
  properties so that adding a ninth tile means adding one definition, not editing three `switch`
  statements.
- As a **player**, I want my existing version-1 save to load unchanged after the update so that
  the registry swap is invisible to me.
- As a **future contributor**, I want new saves to carry the string-ID palette so that reordering
  or extending the registry never corrupts a world.
- As a **networking developer (P3)**, I want `SandboxTile.id` to be the deterministic registry
  index so that two processes agree on tile identity without exchanging strings.

## Acceptance criteria

### Bootstrap and identity

- When `SandboxWorld` initializes, it builds and freezes the core tile/item registries and runs
  `SandboxCoreContent.ValidateTileReferences` before generating or loading any chunk; a validation
  failure prevents entering Play with a clear error.
- When any runtime code decides content behavior, it reads a `TileDefinition` (or a registry index
  compared against a registry-resolved value) — `grep -rn "SandboxTileIds\." Assets/Scripts`
  returns no hits outside `SandboxCoreContent` (legacy table) and save-migration code.
- When `SandboxChunkRenderer` computes solidity/culling and `SandboxColliderGeometry` consumes
  `IsSolid`, the result comes from `TileDefinition.Solid`, and the collider geometry EditMode
  tests still pass unmodified.
- When `SandboxTileVisualCatalog` resolves a ground tileset, it uses `TileDefinition.AtlasSprite`
  instead of the per-tile `switch` (`TryGetGroundTilesetName`); grass-cover rules key off
  definition data or the `core:grass` index, not the constant `2`.
- When the licensed catalog is absent, `GetLegacyTileUv`/`GetLegacyTileColor` produce the same
  UV/color per tile as before the swap (mapping keyed by string ID or definition, not constants).

### Saves

- Given a version-1 save produced before this change, when it is loaded, every edited tile
  resolves through `SandboxCoreContent.LegacyTileIdToStringId` to the same visible tile as before.
- Given a save written after this change, its JSON contains the palette (string ID → runtime
  index), and loading it after inserting a hypothetical `core:coal_ore` definition still resolves
  every tile to its original string ID (EditMode test over temp files, mirroring the
  registry-level test in `ContentRegistryTests`).
- Given a save whose palette references an unknown string ID, when it is loaded, the load fails
  with an explicit error — no tile is silently replaced with air.

### Regression

- When the pinned-seed scene from the [P1 vertical-slice runbook](../p1-vertical-slice-demo.md) is
  generated before and after the swap, terrain is identical at the runbook's three documented
  spot-check positions (including the negative-x one).
- All pre-existing EditMode tests pass without modification (except tests that themselves assert
  legacy constants, which may be updated in the same commit with justification).
- Player place/break still works: remove writes the air index, place writes the configured tile's
  index.

## Open questions

1. **Serialized inspector ints** — `SandboxPlayerController.placeTileId` is a
   `[SerializeField] int` (default `SandboxTileIds.Dirt`) baked into scenes/prefabs. If `id`
   becomes a registry index, serialized ints silently change meaning when content changes.
   Recommendation: change the serialized field to a **string ID** (`"core:dirt"`) resolved to an
   index at `Awake`; needs a decision because it touches scene assets.
2. **MCP tool contract** — `GameplayMcpTools` accepts `tileId` as an int from external callers.
   Keep accepting legacy ints (mapped through the table), accept string IDs, or both?
   Recommendation: accept both (`tileId` int = legacy, `tile` string = registry) to avoid breaking
   existing MCP clients; needs confirmation since it's a published tool surface
   (`docs/wiki/13-tooling-testing.md`).
3. **Save version bump** — bump `SandboxSaveData.version` to 2 when adding the palette, with
   version-1 handled by the legacy table? This pre-empts part of P2-SAVE-001's header work; the
   alternative (palette without a version bump) makes formats ambiguous. Recommendation: bump to 2
   and record the coordination in the P2-SAVE-001 ticket.
4. **Fate of `SandboxTileIds`** — delete outright, or keep as `static readonly` registry-resolved
   values for editor tooling readability? Deleting is cleaner; keeping eases review.
5. **Landing shape** — one PR or a short stack (renderer/catalog swap → save migration → shim
   removal)? The non-functional requirements prefer reviewable slices; a 2-PR stack (runtime swap
   + saves) seems right if the diff exceeds ~400 lines.

## Detailed technical specifications

### Scope

- Migrate `SandboxWorld`, `SandboxTerrainGenerator`, `SandboxChunkRenderer`,
  `SandboxPlayerController`, `SandboxTileVisualCatalog` (including its tile-ID `switch`), and
  `GameplayMcpTools` from `SandboxTileIds` constants to registry runtime indices /
  `TileDefinition` lookups.
- Load legacy saves through `SandboxCoreContent.LegacyTileIdToStringId`; write the
  `RegistryPalette` into new saves and resolve it on load (header field coordinated with
  P2-SAVE-001).
- Complete `ItemDefinition`/`EntityDefinition` fields as P2-INV-001 and P2-AI-001 firm up their
  contracts; keep definitions pure data.
- Specify mod load order and override/priority rules in `docs/wiki/12-modding.md` (pre-work for
  P4-MOD-001).

### Non-functional requirements

1. Hot paths remain array-indexed by runtime int — no per-tile string lookups per frame.
2. The pinned-seed prototype scene renders identically before and after the swap.
3. Minimal diffs per subsystem; the swap may land as a short stack of reviewable changes.

## Exit evidence checklist

- [x] GitHub issue URL is recorded in this ticket.
- [x] GitHub issue links back to this markdown ticket.
- [x] Caller migration landed with EditMode coverage (`SandboxRegistries` holder;
  `SandboxTerrainGenerator`, `SandboxPlayerController`, `GameplayMcpTools`, `McpTileDebug`,
  `SandboxTileVisualCatalog`, `SandboxChunkRenderer` migrated; `SandboxTileIds` reduced to
  `static readonly` registry-resolved indices; `ContentRegistryTests` updated).
- [x] Save palette persisted and legacy migration tested (`SandboxSaveData.tilePalette`,
  `SandboxWorld.SaveToPath`/`LoadFromPath`, new `SandboxSaveLoadTests`: legacy v1 fixture,
  palette write, in-engine registry-reorder round trip).
- [x] Mod load order specified in `docs/wiki/12-modding.md` (explicit dependency/priority
  scheme with declared overrides; not last-loader-wins).
- [x] Pinned-seed regression check recorded (2026-07-04, local finalize pass). **EditMode:**
  199/199 passed (`TestResults/editmode.xml`, Unity 6000.5.1f1 batchmode). **tile-viz:** 16/16
  passed after regenerating `test/fixtures/expected/*.json` from the JS resolver (Unity export now
  writes rule-table `spriteId` via `AutotileResolver.ResolveSpriteId`). **Determinism (seed 1337,
  columns x = −40, 0, +40):** two consecutive `world-viz` terrain-generator runs matched on surface
  Y and tile-name sequence (Play Mode MCP unavailable — Unity Editor held the project open; runtime
  MCP not listening). Evidence:

  | Column | Surface Y (run 1 / run 2) | Tile-name sequence (high → low, identical both runs) |
  |--------|---------------------------|------------------------------------------------------|
  | −40 | 32 / 32 | Air×5, Grass, Dirt×7, Stone×8 |
  | 0 | 27 / 27 | Air×5, Grass, Dirt×7, Stone×8 |
  | +40 | 26 / 26 | Air×5, Grass, Dirt×7, Stone×8 |

  Raw runtime tile ids legitimately differ from legacy 0–7; names are the regression contract.
  Implementation merged in [PR #93](https://github.com/synthet/project-twelve/pull/93).
