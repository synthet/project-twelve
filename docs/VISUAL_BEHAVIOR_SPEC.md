# Visual Behavior Specification

> **Authority:** ProjectTwelve-owned behavioral contract for autotiling, character composition, creature animation, and effects.
> **License boundary:** Licensed art stays local-only (`config/paid-assets.local-only.txt`). This document and `Assets/Scripts/Visual/` define **behavior** only.

---

## 1. Autotile neighbor masks

### Coordinate system

Masks are **3×3 int grids** indexed `[x, y]` where:

- `x`: 0 = west, 1 = center, 2 = east
- `y`: 0 = north (above), 1 = center, 2 = south (below)
- Center cell `[1,1]` is always `1` (the tile being resolved).

### Ground mask construction

For each offset `(dx, dy)` from the tile:

- Cardinal neighbors (N, S, E, W): `1` if the neighbor **shares the ground autotile group**, else `0`.
- Corner neighbors: `1` only if **both** adjacent cardinals are connected **and** the corner tile shares the group.

### Cover mask construction

Start from the cover connectivity mask (grass connects only to grass).

Then, for side cells at `[1,0]` and `[1,2]` (north row left/right of center):

- If a **solid ground body** exists at the diagonal ground position (west-of-north or east-of-north with matching ground group), set that side cell to `2` instead of `0` or `1`. This marks cliff/edge cover overlays.

Cover rules may use mask values `0`, `1`, and `2`.

### Autotile grouping (sandbox)

| Layer | Group rule |
|-------|------------|
| Ground | Same ground tileset **name** ⇒ connected |
| Cover | Same tile ID (grass) ⇒ connected |
| Cover visibility | Grass cover when tile above is **non-solid** |

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
Match(M, flipInput):
  for each x, y in 0..2:
    my = flipInput ? (2 - y) : y
    if P[x,y] != M[x, my]: return false
  return true
```

### Resolution order

1. Collect all rules where `Match(M, flipInput=false)`.
2. If none, collect rules where `Match(M, flipInput=true)` and set output `flipX = true`.
3. If multiple matches, pick by **weighted deterministic hash** (`AutotileResolver`).
4. Fallback sprite id `"20"` when no rule matches or sprite missing.
5. Ground tilesets use **32 sprites** ⇒ ground rule table. Other counts ⇒ cover rule table.

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

## 4. Character sprite sheet contract

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

## 5. Character locomotion animator API

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

## 6. Monster locomotion API

Bool parameters: `Idle`, `Ready`, `Walk`, `Run`, `Jump`, `Die`

Note: monster `Run()` maps to animator `Walk=true` in reference behavior.

Triggers: `Attack`, `Hit`, `Fire` (per creature animator).

---

## 7. Effects

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

## 8. Mount compositing

Given mount base texture and character layer pixels:

- Target frame positions on mount texture map to animation frame keys (Idle, Ready, Jab, Slash, Jump variants).
- For each target pixel block, copy non-transparent character layer pixels onto mount texture.
- Weapon/Arms/Bracers overlay rules depend on animation type (Idle/Ready vs combat vs Jump).

---

## 9. Import conventions (local licensed art)

| Asset type | PPU | Filter | Notes |
|------------|-----|--------|-------|
| Ground/cover tiles | 16 | Point | 16×16 grid slices |
| Character layers | 16 | Point | Read/Write enabled for runtime merge |
| Props | 16 | Point | Individual sprites |

Editor importers read paths from `Assets/_Licensed/config/visual-import.txt` (submodule) and populate catalogs under `Assets/_Licensed/Settings/Visual/`.

---

## See also

- [Visual setup](VISUAL_SETUP.md)
- [Paid and licensed assets](PAID_ASSETS.md)
- [Asset integration requirements](wiki/15-assets-integration.md)
