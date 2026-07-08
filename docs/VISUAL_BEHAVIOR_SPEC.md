---
type: Specification
title: Visual Behavior Specification
description: Behavioral contract for autotile masks, rule matching, character composition, creature animation, and effects.
resource: VISUAL_BEHAVIOR_SPEC.md
tags: [docs, visual, autotile, rendering]
timestamp: 2026-07-07T00:00:00Z
---

# Visual Behavior Specification

> **Authority:** ProjectTwelve-owned behavioral contract for autotiling, character composition, creature animation, and effects.
> **License boundary:** Licensed art stays local-only (`config/paid-assets.local-only.txt`). This document and `Assets/Scripts/Visual/` define **behavior** only.

**Document map (autotile)**

| Doc | Role |
|-----|------|
| This page | Behavioral contract — what implementers must guarantee |
| [`wiki/autotile-algorithm.md`](wiki/autotile-algorithm.md) | Algorithm detail, normalization predicates (§8) |
| [`wiki/ground-autotile-32-rules.md`](wiki/ground-autotile-32-rules.md) | 32 ground masks, mirroring, acceptance checks |
| [`wiki/autotile-next-actions-plan.md`](wiki/autotile-next-actions-plan.md) | Roadmap and fixture workflow |

---

## 1. Autotile neighbor masks

### Coordinate system

Masks are **3×3 int grids** indexed `[x, y]` where:

- `x`: 0 = west, 1 = center, 2 = east
- `y`: 0 = north (above), 1 = center, 2 = south (below)
- Center cell `[1,1]` is always `1` (the tile being resolved).

### Ground mask construction

Build at least two conceptual masks before rule matching:

| Mask | Predicate | Purpose |
|------|-----------|---------|
| `visualMask` | Same ground autotile group only | Visual connectivity — foreign stone/dirt never connects on sides or north |
| `solidMask` | Any solid tile (`isSolid`) | Physical support and exposure context |
| `connectivityMask` | Same as `visualMask` (vendor `GetMask`) | Resolver input before normalization pass-through |
| `finalMask` | `NormalizeGroundMask(connectivityMask, …)` | Mask passed to `AutotileResolver` (currently **equals** `connectivityMask`; normalization disabled) |

For each offset `(dx, dy)` from the tile when building `visualMask` / `connectivityMask`:

- Cardinal neighbors (N, S, E, W): `1` if the neighbor **shares the ground autotile group**, else `0`.
- Corner neighbors: `1` only if **both** adjacent cardinals are connected **and** the corner tile shares the group.

**Vendor strict** — foreign stone/dirt never connects on any row or column. `solidMask` (any `isSolid`) is debug/exposure context only; it does **not** alter `connectivityMask`.

**Vendor-aligned resolution** — `NormalizeGroundMask` is currently a **pass-through**: `finalMask` equals `connectivityMask`, all normalization flags are false, and `AutotileResolver` matches the raw blob mask (exact → mirror → fallback). Window/hole cells therefore read vendor sprites directly:

- Open-sky bridge lintel `000/111/000` → **25**
- Flat ceiling span `111/111/000` → **17**
- Window top inner corners `111/111/110` / `111/111/011` → **18** (+`flipX`)
- Inner vertical frames → **8** (+`flipX`) when topology matches

Former project normalizers (`stairInterior`, `innerCavity`, `cavityUnderside`, `materialBoundary`) are retained in code for reference/tests but **not** applied at runtime. See [`wiki/autotile-algorithm.md`](wiki/autotile-algorithm.md) §8. Fixtures: `open-sky-bridge-lintel`, `mountain-window-corner`, `dirt-window-inner-edges`.

There is deliberately **no generic** underside or external-cliff face remapping beyond the raw vendor rules:

- **External undersides / cave ceilings** resolve through the authored underside family (`14`–`17`, `31`, bottom caps `29`/`16`) from their raw masks.
- **External vertical cliff faces** resolve through face sprites (`8` + `flipX`, cap `22`, outside corners `0`/`16`) when topology matches; pillars use `28`/`21`/`29`.

Callers may pass `isSolid(x,y)` for `solidMask` debug output and retained normalizer unit tests; it does not change vendor mask connectivity.

**Exposure floor** — Play Mode procedural fill below the saved world bottom (`y < autotileExposureFloorY`, default `0` on `SandboxWorld`) must not count as solid for `solidMask` / `isSolid`. Tile-viz treats off-space coordinates as air. Implemented in `AutotileExposure.CreateIsSolid`.

Debug reports (tile-viz autotile JSON, runtime MCP `tile_autotile`) expose `visualMask`, `solidMask`, `connectivityMask`, `finalMask`/`mask`, and normalization flags (`stairInterior`, `innerCavity`, `cavityUnderside`, `materialBoundary`).

### Material-boundary anti-regressions

Fixture-backed guarantees at dirt/stone boundaries:

- `24 + flipX` must not collapse to bridge sprite `25` (one-sided lips only).
- Pillar strip `21` must not be replaced by side-cap `22` without matching topology.
- Stone neighbors must not count as same-material dirt connectivity on W/E/N.
- Foreign materials never connect in the ground blob mask (vendor `GetMask` parity).
- Foreign solid below must not automatically trigger air-underside behavior.
- Foreign solid beside dirt must not force interior fill `9`/`10` at re-entrant corners.

### Cover mask construction

Cover is **material-agnostic** (vendor `LevelBuilder.SetCover`): the surface overlay applies to any
exposed-top ground cell, independent of ground material. Each west/east cardinal at `[0,1]` and
`[2,1]` is read by **solidity alone**:

- Neighbor is **air** (not solid) ⇒ `0` (end cap).
- Neighbor is solid with **more ground stacked directly above it** ⇒ `2` (rising cliff step).
- Neighbor is solid with **air above it** (an exposed top like the center) ⇒ `1` (run continues).

Cover rules may use mask values `0`, `1`, and `2`.

### Autotile grouping (sandbox)

| Layer | Group rule |
|-------|------------|
| Ground | Same ground tileset **name** ⇒ connected |
| Cover | Any exposed-top ground beside (solid, air above) ⇒ connected (material-agnostic) |
| Cover visibility | Cover on any solid ground tile when the tile above is **non-solid** |

---

## 2. Autotile rule matching

### Rule structure

Each rule contains:

- `spriteId` — string name matching a sprite in the tileset (e.g. `"20"` fallback fill)
- `pattern` — 3×3 int grid (same indexing as neighbor mask)
- `weight` — non-negative; minimum effective weight is `1` when picking variants
- `authoredFlipX` — metadata only; matching uses the flip parameter below

### Match algorithm

Given neighbor mask `M` and rule pattern `P` (both 3×3):

```
MatchRows(M, flipInput):
  for each x, y in 0..2:
    my = flipInput ? (2 - y) : y
    if P[x,y] != M[x, my]: return false
  return true

MatchColumns(M, flipColumns):
  for each x, y in 0..2:
    mx = flipColumns ? (2 - x) : x
    if P[x,y] != M[mx, y]: return false
  return true
```

### Resolution order

1. Collect all rules where `MatchRows(M, flipInput=false)`.
2. If none, collect rules where `MatchRows(M, flipInput=true)`; return the matched rule's `spriteId` with `flipX = true` (vendor PixelFantasy behavior).
3. If none, collect rules where `MatchColumns(M, flipColumns=true)`; return the matched rule's `spriteId` with `flipX = true`.
4. Cover tilesets (6 sprites) use the same flip-pass contract.
5. Optional authored partner substitution (`AutotileGroundSpritePartners`) is **disabled** until fixture-validated; see [`wiki/ground-autotile-32-rules.md`](wiki/ground-autotile-32-rules.md) § Mirroring policy.
6. If multiple matches, pick by **weighted deterministic hash** (`AutotileResolver`).
7. Fallback sprite id `"20"` for 32-sprite ground tilesets; `"0"` for cover tilesets when no rule matches or sprite missing.
8. Ground tilesets use **32 sprites** ⇒ ground rule table. Other counts ⇒ cover rule table.
9. **Single-sprite tilesets** (e.g. Rocks when the catalog lists one sprite) skip rule matching and always use the lone sprite. Standard ground sheets such as BricksA, BricksB, BricksC, BricksD, and Humus use **32 sprites** and the ground rule table.

PixelFantasy 128×64 ground sheets may contain artist-authored left/right variants on the sprite sheet, but the resolver must not substitute a different sprite ID unless mask topology proves equivalence. Prefer `flipX = true` on the matched rule. Rule table: [`wiki/ground-autotile-32-rules.md`](wiki/ground-autotile-32-rules.md).

Rule tables are stored in `AutotileRuleTables`.

---

## 3. Deterministic weighted pick

When multiple rules match:

```
totalWeight = sum(max(1, rule.weight) for each match)
pick = HashMask(M, flipX) % totalWeight
walk cumulative weights to select rule
```

Hash combines flip flag and all mask cell values (order: x outer, y inner).

---

## 4. Visual override debug annotations

Visual overrides are **debug annotations** layered onto terrain presentation after the normal autotile decision has been made. They are intentionally non-authoritative:

- They do **not** write tile IDs, tile definitions, save data, or generated chunks.
- They do **not** alter collision, solidity, lighting, navigation, terrain generation, fluid simulation, or runtime world queries.
- They do **not** change the mask or resolver contract described above; the normal ground/cover autotile report remains the source of truth for diagnosing resolver drift.
- They may temporarily substitute the sprite/flip emitted by the renderer or tile-viz compositor so agents and humans can mark suspect cells, test a visual hypothesis, or attach a compact reproduction to an RCA.

Treat any `*.visual-overrides.json` file as diagnostic evidence beside a capture, not as content data. If a visual override makes a scene look correct, the follow-up fix still belongs in the canonical tile mapping, mask builder, resolver, import data, or mesh compositor as appropriate.

---

## 5. Character sprite sheet contract

| Property | Value |
|----------|-------|
| Sheet size | 576 × 928 pixels |
| Cell size | 64 × 64 per animation frame |
| PPU | 16 |
| Pivot | (0.5, 0.125) — feet-aligned |
| Icon corner | Top-left 32×32 cleared after merge |
| Layout | One row per animation clip; clip name = `{State}_{frameIndex}` |

### Animation rows (top to bottom in sheet)

Roll, Death, Block, Fire, Shot, Slash, Jab, Push, Jump, Climb, Crawl, Run, Ready, Idle.

### Equipment layer strings

Format: `NAME` or `NAME#RRGGBB/H:S:V` where H ∈ [-180,180], S and V ∈ [-100,100].

Layers (merge order): Back, Shield, Body, Arms, Head, Ears, Armor, Bracers, Eyes, Hair, Cape, Helmet, Weapon, Mask, Horns, Firearm.

Special cases:

- Lizard head clears Hair, Helmet, Mask
- Firearm mode shifts head layers up one row and uses detached 64×64 firearm texture
- Shield overlays last non-weapon layer in row 2 of each frame block

---

## 6. Character locomotion animator API

Shared bool parameters (exactly one true for locomotion):

`Idle`, `Ready`, `Walk`, `Run`, `Crouch`, `Crawl`, `Jump`, `Fall`, `Land`, `Block`, `Climb`, `Die`

Trigger parameters for actions:

`Roll`, `Slash`, `Jab`, `Push`, `Shot`, `Hit`

`Action` bool — set true while action clip plays (see `ActionLockBehaviour`).

### Sandbox-used methods

| Method | Animator effect | Optional VFX |
|--------|-----------------|--------------|
| Idle | Idle=true | — |
| Run | Run=true | Run dust if state changed |
| Jump | Jump=true | Jump dust |
| Fall | Fall=true | — |
| Land | Land=true | Fall dust |

---

## 7. Monster locomotion API

Bool parameters: `Idle`, `Ready`, `Walk`, `Run`, `Jump`, `Die`

Note: monster `Run()` maps to animator `Walk=true` in reference behavior.

Triggers: `Attack`, `Hit`, `Fire` (per creature animator).

---

## 8. Effects

`EffectCatalog` ScriptableObject holds:

- Sprite effect prefab (Animator + SpriteRenderer)
- Optional audio clips

`CreateSpriteEffect(creature, clipName, direction)`:

- Instantiate prefab at creature position
- Sorting order = body + 1
- Scale X by facing sign
- Play animator state `clipName`
- Destroy after ~0.25s

Known clip names: `Run`, `Jump`, `Fall`, `Dash`, `Brake`.

---

## 9. Mount compositing

Given mount base texture and character layer pixels:

- Target frame positions on mount texture map to animation frame keys (Idle, Ready, Jab, Slash, Jump variants).
- For each target pixel block, copy non-transparent character layer pixels onto mount texture.
- Weapon/Arms/Bracers overlay rules depend on animation type (Idle/Ready vs combat vs Jump).

---

## 10. Import conventions (local licensed art)

| Asset type | PPU | Filter | Notes |
|------------|-----|--------|-------|
| Ground/cover tiles | 16 | Point | 16×16 grid slices |
| Character layers | 16 | Point | Read/Write enabled for runtime merge |
| Props | 16 | Point | Individual sprites |

Editor importers read paths from `Assets/_Licensed/config/visual-import.txt` (submodule) and populate catalogs under `Assets/_Licensed/Settings/Visual/`.

---

## 11. Play Mode ground autotile debug overlay

`SandboxWorld` exposes `GroundAutotileDebugMode` (inspector + **F3** cycle):

| Mode | Purpose |
|------|---------|
| `Off` | No overlay |
| `SpriteIdLabel` | Ground sprite id digits + flip notch on every solid tile |
| `ResolveDetail` | Ground sprite id + flip notch + compact 3×3 mask; green/red tint vs baseline |
| `GroundCoverSplit` | Ground marker + cover marker (top half) with sprite id labels |
| `VisualOverrideLabel` | Saved visual override ground/cover labels from `sandbox-world.visual-overrides.json`; cyan = valid, yellow = auto snapshot drift, magenta = missing sprite |
| `CoordinateLabel` | World tile X (top) and Y (bottom) on solid cells |

F3 logs the active mode name, index, and a one-line summary to the Unity Console.

While any non-`Off` F3 mode is active, moving the mouse over the game view shows a top-left HUD with world tile `(x, y)` plus owning chunk `(cx, cy)` and in-chunk local `(lx, ly)` for the tile under the cursor (same coordinate layout as MCP `tile_at`). This complements `CoordinateLabel`, which paints coordinates on every solid cell in the world.

When enabled, each loaded chunk draws a child `GroundAutotileDebug` mesh over solid cells using the same resolve path as chunk rendering (`AutotileGroundResolve` for ground; cover uses the same mask path as `SandboxChunkRenderer.AddCoverTile`). `ResolveDetail` reads `StreamingAssets/AutotileBaselines/sandbox-scene-mountain-autotile.json`. `VisualOverrideLabel` reads the active `AutotileVisualOverrideMap` loaded from the world sidecar or set at runtime.

### Drift RCA tooling

See [wiki/autotile-drift-rca.md](wiki/autotile-drift-rca.md) for the layered playbook (world tile diff → autotile baseline diff → PNG compare). Offline scripts live under `tools/tile-viz/scripts/`; Play Mode MCP tools: `world_export_tile_space`, `autotile_diff_baseline`.

### Mesh compositing vs tile-viz blit

Unity chunk rendering uses `AutotileSpriteMeshBuilder.AppendGroundAutotileSprite` for ground layers: **always** a fixed 16×16 UV quad (`AppendFixedCellQuad`) to match tile-viz `blitSprite`, regardless of sprite mesh bounds. Cover layers still use `AppendSprite`. Horizontal flip mirrors within the cell width.

#### Visual override transform contract

Visual overrides that need sprite-space transforms must use the same UV/sample-corner mapping in Unity and tile-viz. The canonical operation order is:

1. Start with untransformed sprite UVs/cell sample coordinates.
2. Apply `flipX` as a horizontal mirror inside the sprite rect.
3. Apply `flipY` as a vertical mirror inside the sprite rect.
4. Apply `rotationDegrees` clockwise around the sprite rect center.

`rotationDegrees` accepts only quarter-turn values: `0`, `90`, `180`, and `270`. Callers may pass negative equivalent quarter-turns; implementations normalize by modulo 360, so `-90` is `270`, `-180` is `180`, and `-270` is `90`. Any non-quarter-turn input is invalid and must fail fast rather than being rounded.

The operation order is intentionally not commutative. For asymmetric sprites, `flipX=true, flipY=false, rotationDegrees=90` is distinct from rotating first and then flipping. Tests must use asymmetric fixtures so each transform combination maps to a distinct output.

If Play Mode labels match tile-viz but art still diverges on asymmetric sprites, route autotile quads through `AppendFixedCellQuad` (full 16×16 cell UV span) as a parity fallback. No remap is applied unless the Phase 3 gate fails (labels match, pixels differ).

Ground autotile sheets must be 128×64 with 32 sprites named `0`…`31` in canonical row-major layout. Validate via **ProjectTwelve → Visual → Validate Ground Autotile Sheets** (`AutotileGroundSheetValidator`); import fails on layout errors via `AutotileGroundSheetLayout`.

---

## See also

- [Visual setup](VISUAL_SETUP.md)
- [Paid and licensed assets](PAID_ASSETS.md)
- [Asset integration requirements](wiki/15-assets-integration.md)
