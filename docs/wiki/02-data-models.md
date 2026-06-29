---
type: Technical Reference
title: "Data Models"
description: "ProjectTwelve Data Models reference — design notes, contracts, and decisions for the data models area of the sandbox prototype."
resource: wiki/02-data-models.md
tags: [wiki, data]
timestamp: 2026-06-28T00:00:00Z
okf_version: 0.1
---

# 02 — Data Models

> **Status:** Planning.
> **Decisions:** `Tile` is a value struct; `Chunk` is fixed-size; `World` is the coordinate authority.
> **Invariants:** All world access goes through `World`; coordinate math is centralized and tested.

These are reference sketches, not final code. Field layout will be tuned for memory and
serialization (see [Saving & Loading](11-saving-loading.md)).

## Tile

A `Tile` is a small value type stored in dense arrays. Keep it compact — there are millions of them.

```csharp
public struct Tile
{
    public int  id;        // Registry ID: air, dirt, stone, ore, ... (see 12-modding)
    public byte light;     // Cached light level (0-15 or 0-255). Derived; see 06-lighting.
    public float fluid;    // 0.0-1.0 fill amount. See 08-liquids.
    public byte metadata;  // Frame/variant/wall/damage — compact per-tile state.
}
```

Design notes:

- `id == 0` is **air** by convention; `default(Tile)` is empty. `World.GetTile` returns `default`
  for unloaded/out-of-bounds coordinates, so callers must treat air and "not loaded" carefully
  (query chunk presence when the distinction matters).
- `light` and `fluid` are **derived/simulated** state, not authored content. On save you may
  persist them or recompute on load (recomputing light is cheap; fluid may need persistence).
- If memory pressure demands it, split rarely-used fields (e.g. `metadata`) into a sparse side
  table keyed by local index, keeping the hot array tiny.

## Chunk

A fixed-size square of tiles plus its dirty flags and per-chunk views.

```csharp
public sealed class Chunk
{
    public const int Size = 32;                       // see 03-chunking for size rationale
    public Vector2Int Coord { get; }                  // chunk-space coordinate
    public Tile[,] Tiles { get; } = new Tile[Size, Size];

    public bool NeedsRenderRebuild { get; set; }
    public bool NeedsColliderRebuild { get; set; }
    public bool IsDirtyForSave { get; set; }
    // Per-chunk views (render/collider/light/fluid) attached by their systems.
}
```

- `Size` is a compile-time constant so index math is cheap and the compiler can optimize loops.
- A chunk **never** reads its neighbors' arrays directly; edge cases (lighting, fluid, mesh
  borders) resolve neighbor tiles via `World.GetTile`.

## World

The orchestrator and the single authority on coordinates.

```csharp
public sealed class World
{
    private readonly Dictionary<Vector2Int, Chunk> chunks = new();

    public Tile GetTile(int x, int y)
    {
        Vector2Int cc = WorldToChunkCoord(x, y);
        if (!chunks.TryGetValue(cc, out Chunk chunk)) return default;
        Vector2Int l = WorldToLocalCoord(x, y);
        return chunk.Tiles[l.x, l.y];
    }

    public void SetTile(int x, int y, int tileId)
    {
        Vector2Int cc = WorldToChunkCoord(x, y);
        Chunk chunk = GetOrCreateChunk(cc);
        Vector2Int l = WorldToLocalCoord(x, y);
        chunk.Tiles[l.x, l.y].id = tileId;
        chunk.NeedsRenderRebuild = true;
        chunk.NeedsColliderRebuild = true;
        chunk.IsDirtyForSave = true;
        // + enqueue lighting/fluid updates and emit network delta — see 01-architecture.
    }
}
```

### Coordinate spaces (get this right once, test it hard)

There are three spaces. Conversions must handle **negative coordinates** correctly — the classic
bug is using `/` and `%`, which truncate toward zero and break for negatives.

| Space | Meaning | Example |
|-------|---------|---------|
| World | global tile index | `(x, y)` = (-5, 130) |
| Chunk | which chunk | `(cx, cy)` = `floorDiv(x, Size)` |
| Local | index within chunk `[0, Size)` | `lx = x - cx*Size` |

```csharp
static int FloorDiv(int a, int b) => (a >= 0) ? a / b : -(((-a) + b - 1) / b);

static Vector2Int WorldToChunkCoord(int x, int y) =>
    new(FloorDiv(x, Chunk.Size), FloorDiv(y, Chunk.Size));

static Vector2Int WorldToLocalCoord(int x, int y)
{
    int lx = x - WorldToChunkCoord(x, y).x * Chunk.Size; // always in [0, Size)
    int ly = y - WorldToChunkCoord(x, y).y * Chunk.Size;
    return new(lx, ly);
}
```

> **Invariant / first test to write:** for any `(x, y)` including negatives,
> `local ∈ [0, Size)²` and `chunkOrigin + local == (x, y)`. This is item #1 in the
> [implementation priorities](14-roadmap.md#implementation-priorities).

## Entity

```csharp
public abstract class Entity : MonoBehaviour
{
    public Vector2 position;
    public int health, maxHealth;
    public Inventory inventory = new();

    protected virtual void UpdateMovement() { /* move + tile collision — see 05 */ }
    protected virtual void OnTileCollision(Tile t) { /* standing/digging reactions */ }
}
```

Entities query the world grid for movement and interaction. They do not own world data; they read
it through `World`. Movement/collision detail lives in [Collision & Physics](05-collision-physics.md).

## Inventory

```csharp
public sealed class Inventory
{
    public Dictionary<int,int> items = new();        // itemId → quantity

    public void AddItem(int itemId, int qty = 1)
    {
        items.TryGetValue(itemId, out int have);
        items[itemId] = have + qty;
    }

    public bool RemoveItem(int itemId, int qty = 1)
    {
        if (!items.TryGetValue(itemId, out int have) || have < qty) return false;
        items[itemId] = have - qty;
        return true;
    }
}
```

Item IDs are registry IDs (see [Modding & Content](12-modding.md)). For UI with fixed slots,
back this with an ordered slot array instead of a dictionary; the dictionary form is the
simplest correct starting point.

## See also

- [Architecture](01-architecture.md) — how these types interact.
- [Chunking](03-chunking.md) — chunk lifecycle and dirty flags.
- [Saving & Loading](11-saving-loading.md) — serialization of these models.
