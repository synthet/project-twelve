---
type: Reference
title: Autotile Algorithm Reference
description: How ProjectTwelve resolves occupancy into ground and cover sprites — blob mask, direct/mirror resolution, weighted variants, cover rules, and the project normalization layer.
resource: wiki/autotile-algorithm.md
tags: [docs, wiki, autotile, visual, reference]
timestamp: 2026-07-07T00:00:00Z
okf_version: 0.1
---

# Autotile Algorithm Reference

How the autotiler turns plain tile occupancy into seamless terrain art.

**Document map**

| Doc | Role |
|-----|------|
| [`ground-autotile-32-rules.md`](ground-autotile-32-rules.md) | Canonical 32 ground masks and acceptance checks |
| This page | Algorithm: blob mask, resolution, cover rules, normalization (§8) |
| [`VISUAL_BEHAVIOR_SPEC.md`](../VISUAL_BEHAVIOR_SPEC.md) | Behavioral contract for implementers |
| [`autotile-next-actions-plan.md`](autotile-next-actions-plan.md) | Roadmap and fixture workflow (not behavior authority) |

Implemented in ProjectTwelve by:

| Concern | Source |
|---------|--------|
| Neighbor mask + normalization | `Assets/Scripts/Visual/Tiles/AutotileMaskBuilder.cs` |
| Mask → sprite resolution | `Assets/Scripts/Visual/Tiles/AutotileResolver.cs` |
| Rule tables | `Assets/Scripts/Visual/Tiles/AutotileRuleTables.cs` |
| Offline parity (resolver + rules) | `tools/tile-viz/src/visual/` |
| Exported rule tables | `tools/tile-viz/data/autotile-rules.{ground,cover}.json` |

## 1. What autotiling does

The world is authored as plain occupancy: a cell either holds a tile of a given material (dirt,
stone, brick, grass, …) or it is empty. It is **not** authored by choosing one of the 32
sub-sprites. Autotiling inspects each occupied cell's eight neighbors and selects the sub-sprite
whose artwork joins those neighbors seamlessly — an outer edge where the material stops, an
interior fill where it continues, and concave/convex corners where edges meet.

It is a **3×3, corner-aware, blob-style autotiler** with horizontal-flip reuse and weighted random
variants. The ~47 visually distinct neighbor configurations collapse onto 32 authored ground
sprites (many serve two configurations through mirroring).

## 2. Connectivity model

Connectivity is by **material group**, evaluated per cell: two neighbors connect only when they
share the same ground autotile group. Reads outside the world are treated as empty (no neighbor).
Placing or clearing one cell changes up to eight neighboring masks, so a cell **and its neighbors**
must be re-resolved on every edit.

## 3. The 3×3 neighbor mask

Each resolved cell builds a 3×3 mask (`mask[x, y]`; `x` = west→east, `y` = north→south, center is
always `1`):

```text
NW  N  NE
W   C  E
SW  S  SE
```

Cell values:

- `0` — neighbor does not connect (different material, empty, off-world, or disconnected by
  normalization).
- `1` — neighbor connects (same group present).
- `2` — cover only: a "ground-supported" neighbor (see [§7](#7-cover-tiles)).

### 3.1 Blob corner rule

This is **not** a naive 8-bit mask. A **diagonal counts as connected only when both of its adjacent
orthogonal neighbors are also connected.** For the top-left corner:

```text
NW connects  ⟺  N connects  AND  W connects  AND  the NW diagonal is present
```

The four orthogonal cells (N/S/E/W) each come from a single neighbor; the center is hard-wired to
`1`. This blob rule is what collapses the 256 raw 8-neighbor combinations down to the ~47 visually
distinct cases the 32-sprite sheet covers. See `AutotileMaskBuilder.BuildVisualGroundMask` /
`BuildConnectivityGroundMask` (vendor-aligned: connectivity equals same-material visual mask only).

## 4. Resolving a mask to a sprite

Resolution runs in ordered passes (`AutotileResolver.ResolveSprite`):

1. **Direct pass.** Collect all rules whose pattern equals the mask cell-by-cell. If any match,
   pick one (weighted — see §4.1) and return it unflipped (`flipX = false`).
2. **Mirror pass.** Only if the direct pass found nothing: match with columns mirrored
   (west↔east). On a hit, return the **same sprite id** with `flipX = true` so the renderer mirrors
   the artwork. Mirroring is a fallback, never a first choice — a non-mirrored sprite always wins
   when both could apply.
3. **Fallback.** If neither pass matches, the configuration is unresolvable. The base algorithm
   removes such a cell rather than drawing a wrong sprite; ProjectTwelve instead relies on the
   normalization layer ([§8](#8-project-normalization-layer)) to steer ambiguous geometry onto an
   authored rule, and falls back to sprite `20` only as a last resort.

### 4.1 Weighted variants

When several rules match one mask (variant sprites for a single configuration), one is chosen by
weight:

- A single match returns immediately.
- Weights bias selection: interior fill favors `9` over its variant `10` **4:1**.
- The flat-top variants `1` and `2` share a mask and alternate **50/50**.

These are the only multi-match configurations; every other mask is unique, so weights never change
any other cell.

## 5. Ground vs cover selection

The rule catalog is chosen purely by sprite count:

- **32 sprites → ground rules** — the full 3×3 blob set (edges, corners, inner corners, fills,
  strips, caps). Used for terrain bodies.
- **otherwise (6) → cover rules** — a thin horizontal overlay set that sits along the top surface
  of ground.

## 6. Layering

Cover always draws above ground, and props above cover, within a plane; higher planes draw over
lower ones. Cover is presentation-only decoration on the exposed top surface.

## 7. Cover tiles

Cover tilesets have **6 sprites** and use the cover rules. A cover cell survives only where there
is ground **at** the cell and **no** ground directly above it — i.e. an exposed top edge; otherwise
it is not drawn.

Cover is **material-agnostic** (vendor `PixelTileEngine LevelBuilder.SetCover`): the overlay applies
to *any* exposed-top ground cell regardless of its ground material — dirt, stone, or grass all take
the same surface cover. It is **not** bound to a "grass" tile. `ShouldRenderGrassCover` therefore
gates on `tileId != Air && !tileAbove.IsSolid`, and `TryGetCoverTileset` maps every solid ground
tile to the single configured cover tileset. Only vertical faces (a tile with solid ground directly
above it) go uncovered — so a stair-stepped slope covers the treads, never the risers.

The cover mask is horizontal: only the middle row (west / center / east) is meaningful. Its
neighbor values are built by `AutotileMaskBuilder.BuildCoverMask`, which reads each side neighbor by
**solidity alone**: open air is an end cap (`0`), an exposed-top ground cell continues the run (`1`),
and a ground cell with more ground stacked directly above it is a rising cliff step (`2`). This
mirrors vendor `SetCover`, whose bitmap connectivity gives `1` and whose `mask[1,0]`/`mask[1,2]`
overrides give `2` when ground is present both beside and above the neighbor.

### 7.1 Cover rule table

Verified against `tools/tile-viz/data/autotile-rules.cover.json` (middle row only; top/bottom rows
are always `0`):

| Sprite | West | Center | East | flipX | Meaning |
| ---: | :--: | :--: | :--: | :--: | --- |
| `0` | 0 | 1 | 0 | – | Isolated cover (both sides open) |
| `1` | 0 | 1 | 2 | ✓ | Flat end west, ground step east |
| `2` | 2 | 1 | 2 | – | Ground step both sides (nook) |
| `3` | 0 | 1 | 1 | ✓ | West end-cap, cover continues east |
| `4` | 1 | 1 | 1 | – | Middle of a cover run |
| `5` | 1 | 1 | 2 | ✓ | Cover west, ground step east |

`flipX` on the asymmetric rules (`1`, `3`, `5`) lets each also serve its mirror through the mirror
pass, covering all left/right variants with 6 sprites.

## 8. Project normalization layer (disabled at runtime)

The base PixelTileEngine autotiler has **no** normalization: it builds the blob mask, matches
directly (exact → row mirror → column mirror), and discards anything unresolvable.

ProjectTwelve **previously** added a thin normalization step between mask build and resolution
that re-selected among the same 32 authored sprites for side-view readability. That layer is
**intentionally disabled** in the current vendor-aligned build: `NormalizeGroundMask` returns
the connectivity mask unchanged and leaves all four flags false. Ground cells therefore resolve
from the **raw** same-material blob mask only (PixelTileEngine `GetMask` parity).

The `TryRemap*` helpers below are **retained** in C# and tile-viz JS for reference and direct
unit tests; they are **not** invoked from `NormalizeGroundMask` / `normalizeGroundMaskDetailed`.

| Helper (not active at runtime) | Former purpose |
|--------------------------------|----------------|
| `stairInterior` | Stair-step support cells with one upper diagonal open → interior fill instead of repeated diagonal corners down slopes. |
| `innerCavity` | Multi-wide window/hole lintels and inner vertical strips → underside `17` or inner-face `8`. |
| `cavityUnderside` | Bridge mask `000/111/000` over void → continuous underside `17` when arms continue both sides. |
| `materialBoundary` | Foreign-material lips → corner caps `16`/`24` instead of run-ends or west-open strips. |

**Vendor-aligned window/hole outcomes** (no normalizer; straight from blob rules):

| Topology | Mask (row-major) | Sprite |
|----------|------------------|--------|
| Open-sky horizontal bridge lintel | `000/111/000` | **25** |
| Flat ceiling span over void | `111/111/000` | **17** |
| Window top inner corner (one bottom diagonal open) | `111/111/110` / `111/111/011` | **18** (+`flipX`) |
| Inner vertical frame | east/west open + mass | **8** (+`flipX`) |

Fixtures: `open-sky-bridge-lintel`, `mountain-window-corner`, `dirt-window-inner-edges`.

Debug surfaces still expose `normalizationTrace` with all four entries in evaluation order
(`stairInterior`, `innerCavity`, `cavityUnderside`, `materialBoundary`); under vendor alignment
each reads `skipped`. Re-enable a helper only with a new fixture proving a regression and a
negative guard on unrelated topologies (see [`autotile-next-actions-plan.md`](autotile-next-actions-plan.md)).

## 9. Sprite role quick index

Roles for the 32 ground sprites (exact masks in
[`ground-autotile-32-rules.md`](ground-autotile-32-rules.md)):

- **Interior fills:** `9` (common), `10` (rare variant).
- **Straight edges:** top `1`/`2`, bottom `17`, left `8` (right via flip).
- **Convex (outer) corners:** TL `0`, BL `16`, TR `7`, BR `15` (`0`/`16` also serve the opposite
  side via flip).
- **Single inner corners:** `11` (TL), `18` (BL) — mirrors give TR/BR.
- **Paired inner corners:** `5` (top pair), `13` (bottom pair), `19` (right pair, left via flip).
- **Triple inner corners:** `4`, `12`. **Diagonal double:** `3`. **Four inner corners:** `26`.
- **Edge + single-notch:** `6`, `14`, `22`, `23`, `30`, `31`.
- **1-wide runs:** vertical middle `21`, horizontal middle `25`; vertical caps `28` (top),
  `29` (bottom); horizontal cap `24` (left, right via flip); shaft-with-side-border `27`.
- **Isolated:** `20`.

## 10. Invariants for implementers

- **Connectivity is group-equality**, per plane. Different materials never auto-join.
- **Corners are blob-style:** a diagonal connects only when both adjacent orthogonals connect. Do
  not treat the eight neighbors as independent bits.
- **Mirroring is a fallback, not a first choice:** the direct pass wins whenever it can.
- **Re-resolve neighbors on edit:** one placement changes up to eight neighboring masks.
- **The 32-sprite layout is fixed** across all ground materials (id → role is identical); the
  sprite count (`== 32`) selects ground rules over cover rules.
- **Runtime normalization is off** — `finalMask` equals `connectivityMask` unless a future
  fixture-backed normalizer is re-enabled. Helpers in §8 are reference-only.

## Parity

The ground (32) and cover (6) rule tables in `tools/tile-viz/data/` are exported from the C#
authoring path and match the authored PixelFantasy sheet layout cell-for-cell. Keep the C# resolver
(`AutotileResolver`), the exported JSON, and the tile-viz JS resolver in lockstep — see the parity
table in [`tools/tile-viz/README.md`](../../tools/tile-viz/README.md). When rule tables change,
regenerate the JSON from C# (EditMode export tests); never hand-edit it.

## See also

- [`ground-autotile-32-rules.md`](ground-autotile-32-rules.md) — canonical 32 ground masks and
  acceptance checks.
- [`VISUAL_BEHAVIOR_SPEC.md`](../VISUAL_BEHAVIOR_SPEC.md) — visual behavior contract.
- [`autotile-next-actions-plan.md`](autotile-next-actions-plan.md) — normalization roadmap and
  non-goals.
