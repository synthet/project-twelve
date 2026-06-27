# Architecture Blueprint (translated from the visual canvas)

> **Source:** the *Architecture Blueprint* artifact, delivered as a bundled HTML canvas
> (`Architecture_Blueprint.html`) and a rasterized PDF wireframe (`Architecture_Blueprint.pdf`).
> Both are the same single-page visual blueprint titled **"Unity · 2D Sandbox Engine —
> Terraria-like System Architecture."** This page is a faithful text translation of its ten
> figures so the content lives in the repo as searchable documentation.
>
> **Relationship to the rest of the wiki:** the blueprint is a one-page *map*; the numbered wiki
> pages are the *territory* (detail, pitfalls, invariants). Each figure below links to its page.

## Review notes

- The blueprint is **consistent** with [`terraria-like-unity-design.md`](../terraria-like-unity-design.md)
  and this wiki. It's a condensed visual index of the same architecture, organized into 6 sections /
  10 figures.
- It adds no contradicting decisions. One **numeric difference** worth flagging: the blueprint's
  lighting figure uses attenuation **air −1 / solid −3**, whereas the prose design doc used solid −2.
  These are tunable constants, not a design conflict — see [Lighting](06-lighting.md), which now notes
  attenuation is a per-material tunable.
- The PDF is a rasterized wireframe (image) of the HTML canvas; it carries no extra text. Content
  below was sourced from the HTML bundle's exact text layer.

---

## Section 01 · World Data & Rendering

### Fig.01 — Data model & streaming → [Data Models](02-data-models.md), [Chunking](03-chunking.md)

`World ▸ Chunk ▸ Tile` containment:

- **`class World`** — `Dictionary⟨Coord, Chunk⟩ chunks`, `byte worldVersion`, `int seed`;
  `GetTile(x,y) : Tile`, `SetTile(x,y,id)`, `Save()`, `Load()`. (1 ◆── many ▾)
- **`class Chunk` · 32×32** — `Tile[32,32] tiles`, `Vector2Int coord`, `bool needsRemesh`,
  `renderer`, `collider`, `RebuildMesh()`. (1 ◆── many ▾)
- **`struct Tile`** — `int id`, `Color32 color`, `byte light (0–15)`, `float fluid (0–1)`.
- **`Entity`** — `pos`, `health`, `Inventory ◇`; **`Inventory`** — `Dict⟨id, qty⟩`.

**Chunk streaming:** states are *player chunk* · *loaded (within radius)* · *edge (unloading)* ·
*dormant (diff on disk)*. Rule: activate chunks within the load radius, deactivate distant
GameObjects, and **persist only modified-chunk diffs**.

### Fig.02 — Rendering strategies → [Rendering](04-rendering.md)

| Strategy | + | − |
|----------|---|---|
| **Unity Tilemap** | built-in editor & batching; layers (bg/mid/fg) | slow bulk edits; no per-tile light |
| **Custom Mesh** | per-chunk quads; full control; shader lighting; merge quads | remesh on edit; more code |
| **GPU Instancing** | minimal draw calls; huge static worlds | complex setup; hard live edits |

**✓ Path:** prototype on Tilemap → migrate hot chunks to custom mesh for lighting & scale.

---

## Section 02 · Simulation Systems

### Fig.03 — Tile lighting · flood-fill (BFS) → [Lighting](06-lighting.md)

Lightmap as a BFS wavefront: *source → bright → dim*, *solid blocks*.

```text
PropagateLight(sx, sy, initial):
    light[sx,sy] = initial; enqueue(sx,sy)
    while queue:
        (x,y) = dequeue; cur = light[x,y]
        if cur ≤ 1: continue
        for d in 4-neighbours:
            att = solid(nx,ny) ? 3 : 1
            nv  = cur − att
            if nv > light[nx,ny]:
                light[nx,ny] = nv; enqueue(nx,ny)
```

- attenuation: **air −1 · solid −3** (tunable; design-doc prose used −2)
- overlap → **max-blend** (per-RGB channel for colored light)
- edit tile → **re-propagate the dirty region only**
- apply: `tileColor × (light / MAX)`
- URP 2D lights can't see through blocks — this lightmap is **code-driven**, stored **separate from
  world data**.

### Fig.04 — Liquids · cellular automaton → [Liquids](08-liquids.md)

1. **Flow down** — falls until the cell below is full.
2. **Sideways** — splits L + R when below is blocked.
3. **Pressure ↑** — an over-full cell rises in connected columns.

Store `float amount 0–1` per cell; iterate **5–10× per tick** (bottom-up); **update only wet cells**.

---

## Section 03 · Procedural Generation

### Fig.05 — Generation passes → [Procedural Generation](07-procedural-generation.md)

1. **Heightmap** — 1D Perlin/Simplex surface; dirt above, stone below.
2. **Caves** — cellular automata · Perlin worms · thresholded noise.
3. **Biomes** — per-region / per-layer; swap surface blocks & features.
4. **Ores** — depth-banded blobs / random-walk veins per resource.
5. **Structures** — dungeons, temples, trees & prefabs stamped last.

Vertical layers: **Sky · Surface · Underground · Cavern · Core**.

---

## Section 04 · Movement & Interaction

### Fig.06 — Collision · chunked vs manual → [Collision & Physics](05-collision-physics.md)

- **✕ Avoid** one global `CompositeCollider2D` — every edit re-merges all shapes · O(all tiles).
- **A · Chunk colliders** — Tilemap + Composite per chunk; rebuild only the edited chunk; cost
  confined to one region; Unity physics stays usable.
- **B · Manual AABB** — world → tile int math; swept box vs solid grid; no collider merging at all;
  tile-perfect & fastest.
- **✓ Hybrid** — Unity colliders for static geometry; code-driven AABB for the player & dynamic edits.

### Fig.07 — Pathfinding · grid A* → [Pathfinding](09-pathfinding.md)

Walkable grid graph: *start → goal → path*, *blocked*.

- nodes = `(x,y)` walkable tiles
- 4/8-neighbour + jump/gap cost
- edit tile → flag node, re-path
- waypoint graph for platformer AI
- *A\* Pathfinding Project* supports dynamic grid graphs with live node updates.

---

## Section 05 · Multiplayer & Persistence

### Fig.08 — Networking · server-authoritative sync → [Multiplayer](10-multiplayer.md)

Roles: *Client A* · **Server ✦ authoritative** · *Client B*.

1. **Client A ▸ Server** — break/place request `(x, y, tool)`.
2. **Server validates** — range · tool · cooldown · ownership.
3. **Server ▸ all clients** — `SetTile` diff broadcast (seq #).
4. **A · B apply diff** → remesh chunk → relight region.
5. ↺ movement: client prediction + server reconciliation (NetworkTransform).

Libraries: **Mirror** (free, UNET-based, client-server) · **Netcode (NGO)** (official, integrated,
evolving) · **Photon** (hosted relay, free CCU cap).

### Fig.09 — Persistence & modding → [Saving & Loading](11-saving-loading.md), [Modding & Content](12-modding.md)

**Save / load:** chunk diffs > full tile arrays · seed + edit-command log · binary/MessagePack > JSON ·
GZip compression · version header → fwd-compat · stream chunks near player.

**Content & mods:** registries by id (tile/item) · JSON · ScriptableObject defs ·
AssetBundles/Addressables · load order core → mods · override or extend defs · StreamingAssets for data.

---

## Section 06 · Roadmap & Milestones

### Fig.10 — Roadmap (solo / small team) → [Roadmap](14-roadmap.md)

| Phase | Duration | Scope |
|-------|----------|-------|
| **Prototype** | 4–8 wk | chunk system · move/jump · basic gen · place/destroy · rudimentary collision |
| **Alpha** | 12–20 wk | full gen (biomes · caves) · lighting · liquids · AI pathfind · inventory UI · save/load |
| **Beta** | 12–16 wk | character physics · multiplayer core · content · mod support · editor tools |
| **RC · Launch** | 8–12 wk | optimization · platform porting · cloud saves · wide testing · launch |

---

## See also

- [Wiki home / index](README.md) — the full page list.
- [Overview & Principles](00-overview.md) — narrative entry point.
- [`terraria-like-unity-design.md`](../terraria-like-unity-design.md) — canonical prose plan.
