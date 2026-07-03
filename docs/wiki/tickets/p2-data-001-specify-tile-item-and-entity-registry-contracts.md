---
type: Task
id: P2-DATA-001
title: "[P2-DATA-001] Specify tile, item, and entity registry contracts."
description: String-ID registries for tiles/items/entities with runtime index assignment, save palette mapping, and duplicate/missing-ID validation.
status: done
phase: "Phase P2 — Core systems alpha"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/33"
github_issue_status: created
resource: wiki/tickets/p2-data-001-specify-tile-item-and-entity-registry-contracts.md
tags: [docs, wiki, ticket, registry, modding, p2]
timestamp: 2026-07-03T00:00:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/spec-driven-development-tasks.md"
  - "docs/wiki/12-modding.md"
  - "docs/wiki/02-data-models.md"
  - "docs/wiki/11-saving-loading.md"
  - "docs/wiki/world-and-chunk-data.md"
---

# [P2-DATA-001] Specify tile, item, and entity registry contracts.

## Open knowledge summary

This ticket specifies the data-driven registry layer that replaces the hard-coded constants in
`SandboxTileIds` (`Air`=0 … `GoldOre`=7 in `Assets/Scripts/Sandbox/SandboxTile.cs`). Per
`docs/wiki/12-modding.md`, content identity becomes a **stable string ID** (`"core:dirt"`); the
int stored in `SandboxTile.id` becomes a **runtime index assigned at registry load**. The ticket
defines the registry API, definition schema, load/freeze lifecycle, save palette mapping, and
validation rules that every downstream system (rendering, collision, lighting, saves, networking,
mods) will consume.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** [synthet/project-twelve#33](https://github.com/synthet/project-twelve/issues/33)
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a systems developer on the P2 milestone, I want registry contracts for tiles, items, and
entities specified before inventory, generation, and save work lands, so that all of those systems
bind to stable string IDs instead of magic ints and worlds survive content reordering.

## Requirements

### Functional requirements

1. Specify one registry per content kind (tiles, items, entities; biomes and recipes may reuse the
   same mechanism later) keyed by stable string ID with the `namespace:name` convention
   (`core:` reserved for first-party content).
2. Definitions are pure data. The tile definition must carry at least: `id` (string), `solid`
   (collision/pathfinding), `opaque` (lighting attenuation), `lightEmission` (0–15),
   `atlasSprite`/visual key, and drop/hardness placeholders — mirroring the `TileDef` sketch in
   `docs/wiki/12-modding.md`.
3. Lifecycle: load core definitions → (later: load mods in defined order) → **freeze** → assign
   runtime int indices → build atlases/lookup tables. After freeze, registration attempts throw.
4. `default(SandboxTile)`/runtime index 0 remains **air**; the empty-tile invariant from
   `docs/wiki/02-data-models.md` is preserved.
5. Saves must never persist bare runtime indices: specify a **palette** (string ID → runtime index
   map) written into the save header so tiles survive registry reordering
   (`docs/wiki/11-saving-loading.md`); coordinate the header field with P2-SAVE-001.
6. Validation at load: duplicate string IDs fail hard; unresolved references (e.g. a def naming a
   missing sprite key or drop item) fail before entering Play; lookups of unknown IDs return an
   explicit error, not a silent default.
7. Migration rule for the current prototype: the existing numeric IDs 0–7 map 1:1 onto
   `core:air`, `core:dirt`, `core:grass`, `core:stone`, `core:copper_ore`, `core:iron_ore`,
   `core:silver_ore`, `core:gold_ore`, and the P1 world remains loadable.

### Non-functional requirements

1. Registry lookups on hot paths (per-tile render/collision/lighting queries) are array-indexed by
   runtime int — no dictionary or string hashing per tile per frame.
2. No content `enum`/`switch` statements in new code; `SandboxTileIds` survives only as a
   compatibility shim mapping to registry lookups until callers migrate.
3. Registry code stays engine-agnostic at the seam: definitions may load from ScriptableObjects or
   JSON without changing the consumer-facing API.
4. Deterministic runtime index assignment for a fixed definition set (stable sort by string ID),
   so two processes loading the same content agree on indices (needed by P3 networking).

## Acceptance criteria

- IDs, serialization names, mod-safety rules, and migration behavior are documented in the spec
  page before implementation starts.
- Registry validation tests exist for: duplicate ID registration (throws), unknown-ID lookup
  (explicit error), post-freeze registration (throws), unresolved reference detection.
- EditMode test proves runtime index assignment is deterministic and that index 0 is air.
- EditMode test proves a save palette round-trip: persist palette, reorder definitions, reload,
  tiles resolve to the same string IDs.
- Current prototype scene renders identically before and after the registry swap (same seed,
  spot-checked positions).
- The GitHub issue and this markdown ticket link to each other.
- Exit evidence records the commit, verification commands, and reviewer findings.

## Detailed technical specifications

### Scope

- Specification plus the tile registry implementation with the 8 `core:` defs; item and entity
  registries may land as contracts with minimal defs (item defs are consumed by P2-INV-001).
- Out of scope: mod loading from external files (P4-MOD-001), Addressables/atlas pipeline changes,
  recipe/biome registries (P4-CONTENT-001 / P2-GEN-001 may extend the mechanism).

### Inputs and dependencies

- `Assets/Scripts/Sandbox/SandboxTile.cs` — `SandboxTile` struct and `SandboxTileIds` constants.
- `Assets/Scripts/Sandbox/SandboxTileVisualCatalog.cs` — current tile→sprite mapping to be keyed
  by registry ID.
- Consumers to keep in sync: `SandboxColliderGeometry.cs` (solidity), future lighting
  (P2-LIGHT-001: `opaque`, `lightEmission`), saves (P2-SAVE-001: palette), inventory (P2-INV-001:
  item defs and drops).

### Contract sketch

```csharp
public interface IRegistry<TDef> where TDef : class
{
    void Register(TDef def);          // pre-freeze only; duplicate string ID throws
    void Freeze();                    // assigns runtime indices; builds lookup arrays
    TDef Get(int runtimeIndex);       // O(1) array lookup, hot path
    TDef Get(string id);              // startup/tools path; unknown ID throws
    bool TryGet(string id, out TDef def);
    IReadOnlyList<TDef> All { get; }  // stable order after freeze
}
```

### Verification plan

- EditMode: registry validation tests (duplicates, unknown IDs, freeze semantics, deterministic
  indices, palette round-trip) with no Unity scene dependencies.
- Play-mode regression: pinned-seed world before/after swap looks identical (manual spot-check or
  golden fixture from P1-GEN-001 tests).
- `Unity -batchmode -quit -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile Logs/unity-editmode-tests.log`

## Documentation impact

- `docs/wiki/12-modding.md` — promote registry sketch to the specified contract; record the
  `namespace:name` convention and freeze lifecycle.
- `docs/wiki/world-and-chunk-data.md` and `docs/wiki/02-data-models.md` — note `Tile.id` is a
  runtime registry index.
- P2-SAVE-001 ticket — palette header field cross-reference.
- Update `docs/wiki/spec-driven-development-tasks.md` if task scope or sequencing changes.

## Exit evidence

- **Spec:** `docs/wiki/12-modding.md` § "Registry contract (P2-DATA-001)" documents the
  `namespace:name` ID grammar, pure-data definitions, register→freeze→validate lifecycle,
  deterministic runtime index assignment (empty def pinned to 0, ordinal string-ID sort),
  save palette semantics, load validation rules, and the legacy 0–7 migration table.
- **Implementation:** `Assets/Scripts/Sandbox/Registry/` — `ContentRegistry<TDef>`
  (engine-agnostic registry with freeze semantics), `TileDefinition`/`ItemDefinition`/
  `EntityDefinition` (pure data), `RegistryPalette` (capture + fail-loud remap), and
  `SandboxCoreContent` (8 `core:` tile defs, item/entity contracts with minimal defs, legacy ID
  table, cross-registry reference validation). `SandboxTileIds` is documented as the
  compatibility shim; caller migration to runtime indices is deferred to P2-DATA-002, so
  runtime behavior (and therefore the pinned-seed prototype scene) is unchanged by this change
  set.
- **Automated tests:** EditMode `Assets/Tests/EditMode/ContentRegistryTests.cs` covers duplicate
  ID registration (throws), malformed IDs, unknown-ID lookup (explicit error), post-freeze
  registration (throws), deterministic indices across registration orders, air at runtime index
  0, palette round-trip across a reordered/extended registry, palette unknown-ID and
  duplicate/sparse-index failures, legacy table ↔ `SandboxTileIds` 1:1 mapping, solidity parity
  with `SandboxTile.IsSolid`, and unresolved drop-item/visual-key detection.
- **Verification commands:**
  - `Unity -batchmode -quit -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile Logs/unity-editmode-tests.log`
    (pending — authored in a headless container without Unity 6.0.5.1f1; run on a
    Unity-capable machine before merge).
  - `python3 scripts/check_markdown_links.py`
  - `python scripts/ci/okf_lint_changed.py --base origin/master --head HEAD --profile project --fail-on error`
  - `python3 scripts/check_paid_assets.py --staged`

### Checklist

- [x] GitHub issue URL is recorded in this ticket.
- [x] GitHub issue links back to this markdown ticket.
- [x] Registry contract documented in `docs/wiki/12-modding.md` before implementation.
- [ ] Validation + determinism + palette EditMode tests pass (authored; EditMode run pending a
      Unity-capable environment).
- [x] Prototype regression check recorded: no runtime caller changed in this change set —
      `SandboxTile.id` still stores legacy constants, so the pinned-seed world is bit-identical;
      the behavioral swap happens in P2-DATA-002.
- [x] Follow-up task created for caller migration, item/entity registry completion, and mod load
      order: [[P2-DATA-002]](p2-data-002-migrate-sandbox-callers-to-registry-runtime-indices.md).
