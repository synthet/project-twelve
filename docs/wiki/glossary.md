---
type: Technical Reference
title: Glossary
description: Canonical shared vocabulary for the wiki; each term links to the page that owns it.
resource: wiki/glossary.md
tags: [wiki, glossary, terminology]
timestamp: 2026-06-28T00:00:00Z
okf_version: 0.1
---

# Glossary

Shared vocabulary for the [wiki](README.md). Terms link to the page that owns them.

- **Active set** — the cells the [fluid](08-liquids.md) simulation visits this tick (changed cells +
  neighbors). Sleeping cells cost nothing.
- **Atlas** — a packed texture of tile sprites; UVs index into it during [rendering](04-rendering.md).
- **Attenuation** — light lost per step in the [lighting](06-lighting.md) BFS (more through solid/opaque tiles).
- **Biome** — a region with characteristic surface blocks, vegetation, and features, assigned during
  [generation](07-procedural-generation.md).
- **Chunk** — a fixed-size square of tiles (default 32×32) that owns its data and per-chunk views;
  the unit of load/save/rebuild. See [Chunking](03-chunking.md).
- **Chunk coordinate** — which chunk a tile is in: `floorDiv(worldCoord, Size)`. See [Data Models](02-data-models.md).
- **CompositeCollider2D** — Unity 2D collider that merges shapes; **must not** be used globally on a
  destructible world (re-merges everything per edit). See [Collision & Physics](05-collision-physics.md).
- **Dirty flag** — a per-chunk marker that a subsystem (render/collider/light/fluid/save) has pending
  work. Flags are independent. See [Chunking](03-chunking.md).
- **Dirty region** — the bounded area re-propagated after a [lighting](06-lighting.md) change, instead of
  the whole world.
- **Diff (chunk diff / edit log)** — the changes from the generated baseline that a [save](11-saving-loading.md)
  persists, rather than the full world.
- **Flood-fill (BFS)** — the wavefront algorithm propagating [light](06-lighting.md) outward from sources.
- **Lightmap** — the per-tile array of light values, derived state stored separate from authored world
  data. See [Lighting](06-lighting.md).
- **Local coordinate** — a tile's index within its chunk, always in `[0, Size)`. See [Data Models](02-data-models.md).
- **Manual tile collision** — resolving entity movement against the tile grid in code (swept AABB),
  avoiding Unity colliders for terrain. See [Collision & Physics](05-collision-physics.md).
- **Registry** — the lookup from a stable string **content ID** to a definition (tile/item/biome/recipe).
  See [Modding & Content](12-modding.md).
- **Seed** — the value that makes [generation](07-procedural-generation.md) deterministic and reproducible.
- **Server-authoritative** — the server owns canonical world state; clients request edits and receive
  deltas. See [Multiplayer](10-multiplayer.md).
- **Streaming** — loading/unloading chunks around active players. See [Chunking](03-chunking.md).
- **Tile** — the atomic world cell: id + light + fluid + metadata. See [Data Models](02-data-models.md).
- **Tile delta** — a single broadcast tile change (with sequence number) in [multiplayer](10-multiplayer.md).
- **Tilemap** — Unity's built-in grid renderer/editor; useful per-chunk but not the world data model.
  See [Rendering](04-rendering.md).
- **World coordinate** — a global tile index `(x, y)`, possibly negative. See [Data Models](02-data-models.md).

## See also

- [Wiki home / index](README.md)
- [Architecture](01-architecture.md)
