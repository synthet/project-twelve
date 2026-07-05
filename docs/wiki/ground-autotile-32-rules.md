---
type: Reference
title: Ground Autotile Rule Spec — PixelFantasy 32-Tile Ground Sheets
description: Canonical 32 ground autotile masks, mirrored scenarios, and acceptance checks for PixelFantasy 128×64 terrain sheets.
resource: wiki/ground-autotile-32-rules.md
tags: [docs, wiki, autotile, visual, p1, reference]
timestamp: 2026-07-05T06:00:00Z
okf_version: 0.1
---

# Ground Autotile Rule Spec — PixelFantasy 32-Tile Ground Sheets

This spec documents the 32 ground autotile scenarios used by the PixelFantasy PixelTileEngine terrain sheets.

## Mask convention

Each ground tile uses a 3×3 neighbor mask:

```text
NW  N  NE
W   C  E
SW  S  SE
```

`C` is the current tile and should always be `1`.

For ground rules:

- `1` = same connected ground group / same terrain material for rule matching
- `0` = air, empty, or intentionally treated as disconnected by normalization
- `flipX` = horizontal mirror of the scenario

The 32 sprites are indexed row-major on a 128×64 sheet of 16×16 tiles:

```text
0  1  2  3  4  5  6  7
8  9  10 11 12 13 14 15
16 17 18 19 20 21 22 23
24 25 26 27 28 29 30 31
```

## Rule table

| ID | Mask | Canonical scenario | Mirrored scenario |
|---:|---|---|---|
| 0 | `000 / 011 / 011` | NW outside corner: exposed top and west, terrain continues east and downward | NE outside corner |
| 1 | `000 / 111 / 111` | Flat top/surface tile, variant A | No mirror needed |
| 2 | `000 / 111 / 111` | Flat top/surface tile, variant B | No mirror needed |
| 3 | `110 / 111 / 011` | Diagonal saddle: NE and SW corners open | NW and SE corners open |
| 4 | `010 / 111 / 110` | Diagonal transition with SE open; full horizontal body and north/south spine | SW-open mirror |
| 5 | `010 / 111 / 111` | Embedded upper edge: north connected only at center, full body below | No mirror needed |
| 6 | `000 / 111 / 010` | Top-exposed horizontal run with only center support below | No mirror needed |
| 7 | `000 / 110 / 010` | Top-exposed lower/side cap: west and south connected, east open | East/south connected mirror |
| 8 | `011 / 011 / 011` | West-open vertical face: solid column/body to the east | East-open vertical face |
| 9 | `111 / 111 / 111` | Fully surrounded fill/interior, common variant, weight 4 | No mirror needed |
| 10 | `111 / 111 / 111` | Fully surrounded fill/interior, rare variant, weight 1 | No mirror needed |
| 11 | `011 / 111 / 111` | Filled body with NW diagonal open | NE diagonal open |
| 12 | `110 / 111 / 010` | Diagonal side/corner transition: NE, SW, SE open | Mirrored diagonal transition |
| 13 | `111 / 111 / 010` | Bottom-diagonal exposed body: only center below remains connected | No mirror needed |
| 14 | `010 / 111 / 000` | Flat underside / ceiling edge with open bottom row | No mirror needed |
| 15 | `010 / 110 / 000` | Underside outside corner: north and west connected, east/bottom open | East-side underside corner |
| 16 | `011 / 011 / 000` | Lower west-open cap / underside-left corner; terrain continues north/east | Lower east-open cap |
| 17 | `111 / 111 / 000` | Full flat underside / cave ceiling with complete mass above | No mirror needed |
| 18 | `111 / 111 / 011` | Filled body with SW diagonal open | SE diagonal open |
| 19 | `110 / 111 / 110` | Side-body transition with east diagonals open | West-diagonal mirror |
| 20 | `000 / 010 / 000` | Isolated single block | No mirror needed |
| 21 | `010 / 010 / 010` | Vertical pillar / vertical face middle segment | No mirror needed |
| 22 | `010 / 011 / 011` | West-open vertical cap/body with north/east/south continuity | East-open mirror |
| 23 | `000 / 111 / 110` | Top-exposed run with SE diagonal open | SW diagonal open |
| 24 | `000 / 011 / 000` | One-sided horizontal lip/cap: connected only east | Connected only west |
| 25 | `000 / 111 / 000` | One-tile-high horizontal bridge/run | No mirror needed |
| 26 | `010 / 111 / 010` | Four-way plus junction: N/S/W/E connected, diagonals open | No mirror needed |
| 27 | `010 / 110 / 010` | T-junction/side pillar: N/W/S connected, east open | N/E/S connected mirror |
| 28 | `000 / 010 / 010` | Vertical top cap: connected only downward | No mirror needed |
| 29 | `010 / 010 / 000` | Vertical bottom cap: connected only upward | No mirror needed |
| 30 | `011 / 011 / 010` | West-open vertical lower transition: north/east/south connected, bottom diagonals open | East-open mirror |
| 31 | `110 / 111 / 000` | Underside diagonal corner: mass above/side, bottom open, NE open | NW-open mirror |

## Duplicate-mask randomization

Two sprite pairs intentionally share the same mask:

- IDs `1` and `2`: flat top/surface variants.
- IDs `9` and `10`: fully surrounded fill variants. ID `9` has weight 4 and ID `10` has weight 1.

## Intended resolve order

For a solid ground cell:

1. Build a raw 3×3 same-material/same-group connectivity mask. For the **south row only**,
   a foreign solid below counts as connected (blend-below), so buried tiles resting on
   another material resolve as interior/top families instead of underside roots.
2. Apply project-specific normalizations:
   - stair-step interior support normalization
   - material-boundary corner normalization
   - grass/cover cliff handling

   Undersides/ceilings and vertical faces are **not** remapped: they resolve through the
   authored underside family (`14`–`17`, `31`) and face sprites (`8`/`22` + `flipX`) from
   their raw masks.
3. Resolve against the exact 32 ground rules.
4. If no direct match, try the same rule pattern with row flip (`flipInput=true`); return the **same sprite ID** with `flipX = true` when matched.
5. If still none, try column flip (`flipColumns=true`); return the **same sprite ID** with `flipX = true` when matched.

Vendor PixelFantasy `TileMeta.Match` mirrors the mask lookup (`height - 1 - y` for row flip; west/east column swap for column flip) and does **not** substitute a different sprite ID for mirrored ground cases.

See also [`VISUAL_BEHAVIOR_SPEC.md`](../VISUAL_BEHAVIOR_SPEC.md).

## Mirroring policy

The vendor rule system resolves mirrored cases by returning the same sprite ID with `flipX = true`.
Do not automatically substitute a different sprite ID unless a fixture proves that the authored opposite sprite is visually and topologically equivalent.

**Safe categories (not left/right mirroring):**

- `1` / `2`: random flat-top variants (same mask, weighted pick)
- `9` / `10`: random fill variants (same mask, weighted pick)
- `28` and `29`: vertical top/bottom caps — north/south classification, not horizontal mirroring

**Unsafe / provisional visual candidates (do not use as resolver truth without fixture proof):**

| Pair | Why provisional |
|------|-----------------|
| `0 ↔ 7` | Sheet corners; masks differ from simple column mirror |
| `8 ↔ 15` | Side faces; verify mask before substituting |
| `16 ↔ 23` | Lower caps; not proven as exact column-mirror masks |
| `21 ↔ 22` | `21` is pure vertical pillar; `22` has east-side mass |
| `24 ↔ 25` | `24` = one-sided lip (`000/011/000`); `25` = full bridge (`000/111/000`) |
| `30 ↔ 31` | Context-dependent underside transitions |

`AutotileGroundSpritePartners` (C#) and `groundSpritePartners.js` (tile-viz) are **disabled** until a pair passes side-by-side fixture validation.

## Acceptance checks

Use the debug overlay in `SpriteIdLabel` mode.

Important visual sanity checks:

- flat exposed surfaces should mostly resolve to `1` or `2`
- filled interiors should mostly resolve to `9` or `10`
- isolated blocks should resolve to `20`
- one-tile horizontal bridges should resolve to `25`
- vertical pillars should read `28 / 21 / 29`
- cliff faces with mass on one side should read `8` (+`flipX`) with `22`/`0`/`16` caps — never a repeated `21` strip
- cave ceilings / hanging undersides should use the authored underside family (`14`–`17`, `31`), not flipped top sprites
- side-connected cliff runs may use side-cap variants such as `7`, `8`, `22`, `24`, `27`, `30`, depending on the normalized topology
- dirt/stone re-entrant material lips should not collapse into fill `9/10`
- window/hole inner-cavity lintels should resolve `17` (underside), not diagonal-body `18`, when a south-row diagonal opens into the carved void (`dirt-window-inner-edges` fixture)
- inner vertical window frames should resolve `8` (+`flipX` when chirality requires), not outside-corner `0`, when halo-correct neighbor context is present
- top-left and top-right corners should be visually paired; avoid accidental mirrored dirt-noise artifacts if an authored opposite sprite exists
