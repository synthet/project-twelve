# 12 — Modding & Content Pipeline

> **Status:** Planning.
> **Decisions:** **Data-driven** content via **registries keyed by stable string IDs**; defs in
> ScriptableObjects/JSON; Addressables/AssetBundles for external assets.
> **Invariants:** No hard-coded content enums; string ID is the stable identity, runtime int is derived.

Make content data-driven **from the start**. Retrofitting data-driven content after hard-coding
tiles/items as enums and `switch` statements is expensive. Even the prototype should load tiles
from a registry.

## Registries

A registry per content kind: tiles, items, entities, biomes, recipes. Keyed by a **stable string
ID** (e.g. `"core:dirt"`, `"core:stone"`, `"mymod:ruby_ore"`). The registry resolves an ID to a
definition object carrying gameplay properties.

```csharp
// Definition, not behavior — pure data.
public sealed class TileDef
{
    public string id;          // stable: "core:dirt"
    public bool solid;         // → 05-collision-physics, 09-pathfinding
    public bool opaque;        // → 06-lighting attenuation
    public byte lightEmission; // → 06-lighting source
    public string atlasSprite; // → 04-rendering
    // hardness, drops, friction, fluid interaction, etc.
}
```

- `Tile.id` (the int in [Data Models](02-data-models.md)) is a **runtime index** assigned when the
  registry loads. The **string ID is the durable identity**; persist it (or a string→int map) in
  saves so reordering the registry doesn't corrupt worlds (see [Saving & Loading](11-saving-loading.md)).

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
