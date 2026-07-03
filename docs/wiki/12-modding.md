---
type: Specification
title: Modding & Content Pipeline
description: Data-driven content registries — stable string IDs, freeze lifecycle, deterministic runtime indices, save palette, and mod loading boundaries.
resource: wiki/12-modding.md
tags: [docs, wiki, modding, registry, p2]
timestamp: 2026-07-03T00:00:00Z
okf_version: 0.1
---

# 12 — Modding & Content Pipeline

> **Status:** Registry contract specified (P2-DATA-001); mod loading still planning.
> **Decisions:** **Data-driven** content via **registries keyed by stable string IDs**; defs in
> ScriptableObjects/JSON; Addressables/AssetBundles for external assets.
> **Invariants:** No hard-coded content enums; string ID is the stable identity, runtime int is derived.

Make content data-driven **from the start**. Retrofitting data-driven content after hard-coding
tiles/items as enums and `switch` statements is expensive. Even the prototype should load tiles
from a registry.

## Registry contract (P2-DATA-001)

A registry per content kind: tiles, items, entities (biomes and recipes may reuse the same
mechanism later). Keyed by a **stable string ID**. The registry resolves an ID to a definition
object carrying gameplay properties. Implementation:
`Assets/Scripts/Sandbox/Registry/ContentRegistry.cs` plus the definition types and the
`SandboxCoreContent` bootstrap in the same folder.

### String IDs

- Grammar: `namespace:name`, both segments lowercase `[a-z0-9_]+` (e.g. `"core:dirt"`,
  `"mymod:ruby_ore"`). Registration rejects malformed IDs.
- `core:` is reserved for first-party content.
- The string ID is the **durable identity** — it is what saves, network palettes, recipes, and mods
  reference. It never changes once shipped; renames require an explicit alias/migration entry.

### Definitions are pure data

```csharp
// Definition, not behavior — pure data.
public sealed class TileDefinition
{
    public string Id;            // stable: "core:dirt"
    public bool Solid;           // → 05-collision-physics, 09-pathfinding
    public bool Opaque;          // → 06-lighting attenuation
    public byte LightEmission;   // → 06-lighting source, 0–15
    public string AtlasSprite;   // → 04-rendering visual key (ground tileset name today)
    public string DropItemId;    // → item string ID granted on break (placeholder for P2-INV-001)
    public float Hardness;       // → break time placeholder
}
```

`ItemDefinition` and `EntityDefinition` follow the same shape (see
`Assets/Scripts/Sandbox/Registry/`); their fields are minimal contracts to be extended by
P2-INV-001 and P2-AI-001.

### Lifecycle: register → freeze → index

```
1. Load core definitions       (built-in registries)
2. Load mod definitions        (later — P4-MOD-001, in a defined order)
3. Freeze()                    (assign runtime int indices; build lookup arrays)
4. Validate references         (fail before entering Play)
```

- **Before freeze:** `Register(def)` only. Duplicate string IDs throw; malformed IDs throw;
  index-based lookups throw.
- **After freeze:** the definition set is immutable — `Register` throws. Lookups are available:
  `Get(int runtimeIndex)` is an O(1) array read (hot path); `Get(string id)` throws on unknown IDs
  with the offending ID in the message; `TryGet(string id, out def)` for probing. Unknown IDs are
  **never** silently mapped to a default.

### Runtime index assignment (deterministic)

- The registry's declared **empty definition** (`core:air` for tiles) is pinned to **index 0**, so
  `default(Tile)` stays air per [Data Models](02-data-models.md).
- All other definitions are sorted by **ordinal string ID** and assigned indices `1..N-1`.
- Assignment therefore depends only on the frozen definition *set*, not registration order — two
  processes loading the same content agree on every index (required by P3 networking).
- Runtime indices are **not stable across content changes** (adding `core:coal_ore` shifts later
  indices). They must never be persisted bare — see the palette below.

### Save palette

Saves persist a **palette**: the string ID → runtime index map captured at save time, written into
the save header (field reserved by P2-SAVE-001; see [Saving & Loading](11-saving-loading.md)). On
load, the palette is resolved against the current frozen registry to produce an old-index → new-
index remap, so tiles survive registry reordering and insertions. A palette entry whose string ID
no longer resolves is an explicit load error (fail loudly), not a silent air-substitution.

`RegistryPalette` in `Assets/Scripts/Sandbox/Registry/` implements capture and resolution.

### Validation at load

- Duplicate string ID at `Register` → throws immediately (fail hard).
- Unresolved references after freeze — a tile def naming a missing drop item or an empty visual
  key — fail bootstrap validation before entering Play (`SandboxCoreContent.ValidateTileReferences`).
- Unknown-ID lookups throw with the ID named; `TryGet` is the only non-throwing probe.

### Prototype migration (legacy numeric IDs)

The hard-coded `SandboxTileIds` constants (`Air`=0 … `GoldOre`=7) survive as a **compatibility
shim**: `SandboxTile.id` still stores these legacy values until callers migrate to registry
runtime indices (follow-up P2-DATA-002). The legacy ↔ string mapping is fixed:

| Legacy int | String ID |
|-----------:|-----------|
| 0 | `core:air` |
| 1 | `core:dirt` |
| 2 | `core:grass` |
| 3 | `core:stone` |
| 4 | `core:copper_ore` |
| 5 | `core:iron_ore` |
| 6 | `core:silver_ore` |
| 7 | `core:gold_ore` |

`SandboxCoreContent.LegacyTileIdToStringId` encodes this table; existing P1 saves load through it.
New code must not add content `enum`s or `switch`es over tile IDs — bind to definitions instead.

- `Tile.id` (the int in [Data Models](02-data-models.md)) becomes a **runtime index** assigned at
  freeze once callers migrate; the string ID remains the durable identity persisted via the palette
  (see [Saving & Loading](11-saving-loading.md)).

## Definition formats

- **ScriptableObjects** for first-party content — Unity-native, inspector-editable.
- **JSON / CSV** for data non-programmers (and mods) can edit, loaded from `StreamingAssets`.
- Keep definitions declarative; behavior that can't be expressed as data hooks into typed systems
  via a small, documented extension surface — not arbitrary code from untrusted mods without review.

## Load order

```
1. Load core definitions      (built-in registries)
2. Load mod definitions        (in a defined order)
3. Mods add new IDs or override/extend existing ones
4. Freeze registries; assign runtime int IDs; build atlases
```

Conflicts: last-loader-wins for overrides, or an explicit dependency/priority scheme. Validate that
referenced IDs (recipe inputs, biome tiles) resolve after load.

## External assets

- **Addressables** (preferred) or **AssetBundles** let mods ship sprites/prefabs/audio loaded at
  runtime. Reference them by address from definitions.
- Atlas mod tiles into the rendering atlas at load (see [Rendering](04-rendering.md)).

## Pitfalls

- **Enum/`switch` content** — the anti-pattern this whole page exists to prevent.
- **Saving runtime int IDs without the string mapping** — worlds corrupt when the registry changes.
- **Loading arbitrary mod code** without a security/review story — treat third-party code as untrusted.
- **Unresolved references** after load — validate the graph before entering play.

## See also

- [Data Models](02-data-models.md) — `Tile.id` as a runtime index.
- [Saving & Loading](11-saving-loading.md) — persisting stable IDs.
- [Rendering](04-rendering.md), [Lighting](06-lighting.md), [Collision](05-collision-physics.md) —
  consumers of tile definition properties.
