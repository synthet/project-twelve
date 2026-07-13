---
type: Specification
title: HUD Redesign for Efficient PixelLab Generation
description: PixelLab capability research and a HUD asset redesign plan that fits the tool's size tiers, candidate grids, and style-consistency features.
resource: wiki/hud-redesign-pixellab.md
tags: [hud, ui, pixellab, assets, specification]
timestamp: 2026-07-12T00:00:00Z
okf_version: 0.1
---

# HUD redesign for efficient PixelLab generation

This plan follows the individual-element PixelLab pass recorded in
[`hud-conversation-summary.md`](hud-conversation-summary.md). That pass worked, but it fought
the tool: exact tiny bitmaps were requested from a generator with a 192 px minimum, panels were
re-rolled one at a time, and two jobs (portrait frame, item label) produced unusable component kits
or textures. This document records what the tool actually supports and redesigns the HUD asset
contract so every asset lands in a sweet spot of the API.

Authoritative capability sources: the live MCP tool schemas and
[`pixellab-api-v2.md`](pixellab-api-v2.md). Current account state at time of
writing: subscription active (Tier 1), 1,640 subscription generations remaining, **$0.00 USD
credits** — REST Pro endpoints that bill in USD are unavailable until that is confirmed or topped up.

## Capability research summary

### MCP `create_ui_asset` (panels, frames, bars)

- Output size **192–688 px per axis**; square capped at 512, wide/tall aspects up to 688.
  **It cannot generate small exact sizes.** Requesting a 56×56 portrait frame or 96×20 label is
  outside the envelope — this is why those jobs failed, not bad prompting.
- One call consumes **20–40 generations** and returns one image.
- `pieces` parameter: a validated shape template (`rounded_rect`, `circle`, `polygon`) placed on a
  **virtual canvas whose longer side is 0–512** (shorter side scaled to the output aspect). The
  model draws exactly the shapes you place. This turns the earlier "unwanted component kit" failure
  mode into a feature: one call can produce several deliberately placed elements at known
  coordinates, croppable deterministically afterwards.
- `elements` parameter: named scaffolds (`button`, `icon_button`, `toolbar`, `tab`, `panel`,
  `window`, `health_bar`, `avatar`, polygons) with auto-positioning.
- `color_palette` (free-text hint), `seed` (reproducibility), `no_background` (default true).

### MCP `create_1_direction_object` (icons, hearts, portrait, cursor)

- Square canvas **32–256 px**. Candidate grid depends on size: **≤42 px → 64 candidates,
  43–85 → 16, 86–170 → 4, larger → 1**, all from a single job entering `review` status.
- `style_images`: up to 8 base64 references (≤85 px output) — style-lock small icons to an
  already-approved asset.
- Review flow: `get_object` → inspect grid → `select_object_frames` (user-approved indices) or
  `dismiss_review`.

### MCP `create_map_object`

- 32–400 px free aspect, single result, `view: side`, outline/shading/detail controls; style
  matching via `background_image` (≤192×192) with inpainting masks. Results **expire in 8 hours**.
  Useful fallback for non-square one-offs.

### REST v2 Pro endpoints (currently blocked by $0 credits)

- `POST /generate-ui-v2`: the full web "Create UI Elements (Pro)" workflow — exact sizes from
  16 px, `concept_image` guidance, `color_palette`. Bills `usage.type: usd`.
- `POST /generate-with-style-v2`: square 16–512 with style refs; ≤32 px yields 64 candidates.
- `POST /resize`: aesthetic-preserving resize, 16×16 to a 200×200 area, `forced_palette`,
  `transparent_background`. Would replace hand-rolled normalization for awkward ratios.
- Before relying on any of these, confirm whether Tier 1 subscription generations cover them or
  whether they strictly bill USD credits (ask via `agent_help` or a minimal probe).

### Cost model

| Route | Cost | Yield |
|---|---|---|
| `create_ui_asset` | 20–40 gens | 1 panel image (multi-element via `pieces`) |
| `create_1_direction_object` ≤42 px | one job's gens | **64 candidates** |
| `create_1_direction_object` 43–85 px | one job's gens | 16 candidates |
| REST Pro (`generate-ui-v2`, `resize`) | USD credits — currently $0 | exact sizes, candidate grids |

Budget envelope for the full redesign below: roughly 5 UI-asset calls plus 6 object calls plus a
50% re-roll reserve — comfortably inside 1,640 remaining generations.

## Root causes of the rough first attempt

1. **Sizes below the tool floor.** 12×12 hearts, 96×20 label, 56×56 frame, 52×52 slots were asked
   from a 192-minimum generator, forcing lossy normalization and rejections.
2. **Un-templated panel calls.** Without `pieces`/`elements`, the model invents its own component
   layout (the rejected 512×512 sheet and the portrait-frame "kit").
3. **Exact-bitmap thinking for 9-sliced panels.** A 612×60 hotbar backing never needed to be a
   612×60 bitmap; slicing makes display size a Unity concern.
4. **One job per element with no candidate grids.** Small assets were rolled one at a time instead
   of harvesting 16–64 candidates from a single job.
5. **No style chain.** Panels and icons were generated independently, giving the current mixed
   steel-vs-gold look.

## Redesign principles

1. **Generation-friendly size grid.** Every non-sliced asset's display size × an integer factor
   (2×/4×) must land on a PixelLab tier boundary. Downscale is nearest-neighbor by that exact
   integer factor only.
2. **9-slice-first panels.** Panels ship as small sliceable bitmaps generated at 192+ and
   integer-downscaled; exact on-screen size lives in `RectTransform`, not the PNG. Exact-bitmap
   requirements remain only for Simple sprites (hearts, icons, portrait, cursor).
3. **One `pieces`-templated sheet per style family.** Place the panel, slot, selected slot, and
   label shapes on one 512×512 virtual canvas with author-known coordinates; crop
   deterministically. One call ≈ 4–5 style-consistent elements.
4. **Candidate grids for small art.** Generate icons/hearts at 43–85 px (16 candidates) in single
   jobs; promote user-approved indices via `select_object_frames`. In practice the ≤42 px
   64-candidate packs timed out server-side twice (2026-07-12) — treat 16-candidate packs as the
   reliable tier and pick sizes that keep the downscale factor integer.
5. **Explicit style chain.** Approve the hero sheet first; then lock `seed` and `color_palette`
   ("dark navy panel, polished silver frame, gold corner accents" — matches the palette in
   [`hud-assets.json`](../specs/hud-assets.json)). Caveat found in the live schema: `style_images`
   cannot be combined with an explicit `size` (the largest style image dictates output size), so
   candidate-grid jobs carry palette hints in the description instead of style refs.
6. **Deterministic normalization stays.** Extend
   [`../../scripts/normalize_pixellab_hud_assets.py`](../../scripts/normalize_pixellab_hud_assets.py)
   with: template-coordinate cropping (virtual-canvas → output-pixel mapping), pixel-scale
   detection asserting the expected integer factor, palette snapping to the manifest palette, and
   the existing largest-connected-component cleanup.

## Proposed asset contract v3

Display metric changes (look-and-feel): hearts grow 12→**16 px** with a 2 px gap (clearer
silhouette, room for half-heart pixels); slots move 52→**48 px** inner with a 4× clean generation
factor; the portrait becomes **40 px** in a 48 px frame. The vitals panel and hotbar widths follow
from slicing, so no bitmap dimension depends on them.

| Asset | Display (px) | Unity treatment | Generation route | Gen size | Post-process |
|---|---:|---|---|---|---|
| Panel/slot style sheet | — | source for crops below | `create_ui_asset` + `pieces`, 512×512, seed fixed | 512 | crop per template coords, ÷4 |
| Vitals/main panel | 64×64 bitmap, sliced 16 | Sliced | crop of sheet (256×256 region) | — | ÷4 → 64 |
| Hotbar backing | keep v2 612×60 bitmap, sliced 6 | Sliced | none (accepted v2 asset; slicing narrows it to the 560px display) | — | — |
| Slot normal | 48×48, sliced 12 | Sliced | crop of sheet (192×192 region) | — | ÷4 → 48 |
| Slot selected | 48×48 overlay, sliced 12 | Sliced | crop of sheet (gold variant piece) | — | ÷4 → 48 |
| Item label backing | 48×16, sliced 6 | Sliced | crop of sheet (192×64 piece) | — | ÷4 |
| Debug backing | keep current asset | Sliced | none (already accepted) | — | — |
| Heart full/half/empty | 16×16 | Simple | one `create_1_direction_object` job for the full-heart master, size 48; half/empty derived deterministically (v2 method) | 48 → 16 candidates | ÷3 |
| Tile icons (dirt, grass, stone, copper) | 32×32 | Simple | `create_1_direction_object`, size 64, `item_descriptions` | 64 → 16 candidates | ÷2 |
| Player portrait | 40×40 | Simple | `create_1_direction_object`, size 80 | 80 → 16 candidates | ÷2 |
| Cursor | 16×16 | Simple | keep current, or size-32 object job | 32 | ÷2 |
| Slot numbers / label font | text | TMP or `create_font` | optional; only if Pro font route is confirmed covered | — | — |

Every crop/downscale factor is integer; no aspect-distorting resize is permitted. Assets that fail
the factor check are re-rolled, not stretched.

## Generation batches

1. **Batch 0 — probe (no cost).** `get_balance` (done), and `agent_help` question on whether Tier 1
   generations cover `generate-ui-v2`/`resize`. Asked 2026-07-12: the PixelLab docs agent had **no
   information** on the billing split; unresolved, so the workflow remains MCP-only.
2. **Batch 1 — hero sheet.** One `create_ui_asset` 512×512 call with the `pieces` template
   (main panel, slot, selected slot, label bar) + palette hint + seed. Review; re-roll with the
   same template and a new seed if rejected. Gate: user approval of the sheet.
3. **Batch 2 — crops and slicing.** Deterministic crop/downscale into the five sliced bitmaps;
   verify slice borders survive at 1:1 pixel density.
4. **Batch 3 — small objects.** Hearts (3 jobs), icons (1 job), portrait (1 job), each with
   `style_images` cropped from the approved sheet. Present candidate grids; promote only
   user-selected indices.
5. **Batch 4 — Unity wiring.** Replace PNGs in `Assets/Sprites/UI/Generated/` preserving `.meta`
   GUIDs; update `SandboxHudPrefabBuilder`, `SandboxHUD.prefab`, and `SandboxHudTests` for the new
   metrics (heart 16 px, slot 48 px); Point filtering, no compression, no mipmaps unchanged.
6. **Batch 5 — verification.** EditMode `SandboxHudTests`, Play Mode screenshot at 1280×720 and one
   wide/narrow aspect, and the checklist from
   [`pixellab-api-v2.md`](pixellab-api-v2.md).

## Look-and-feel goals

- One material language everywhere: dark navy fill, silver frame, gold reserved for selection and
  corner accents (today the hotbar reads steel-industrial while the vitals panel reads
  gold-fantasy).
- Larger hearts with a readable half state at gameplay distance.
- Selected slot keeps the hard gold border contract (no glow), now generated in the same style
  family as the normal slot instead of separately.
- Portrait cleaned by construction (style-locked candidates) rather than by artifact removal.
- Label backing gains the panel's silver edge so floating text feels attached to the HUD.

## Open questions

1. Does the Tier 1 subscription cover REST Pro USD-billed endpoints, or are they hard-blocked at
   $0 credits? (Determines whether `resize` and exact-size `generate-ui-v2` are usable.)
2. Heart 12→16 and slot 52→48 change layout math in `SandboxHudController` and the manifest — a
   follow-up contract bump of [`hud-assets.json`](../specs/hud-assets.json) to v3 is required before wiring.
3. Whether to adopt a generated pixel font for slot numbers or keep the current code-drawn text.
