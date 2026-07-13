---
type: Working Notes
title: HUD Development Conversation Summary
description: Chronological summary of the ProjectTwelve creative HUD design, implementation, PixelLab generation, asset wiring, and validation work.
resource: wiki/hud-conversation-summary.md
tags: [hud, ui, unity, pixellab, assets, summary]
timestamp: 2026-07-12T00:00:00Z
okf_version: 0.1
---

# HUD development conversation summary

## Goal

Create and polish a functional creative-mode HUD for ProjectTwelve, inspired by Stardew Valley, Terraria, Starbound, and Neverwinter Nights while retaining an original pixel-fantasy identity.

The final layout skeleton remained stable throughout the work:

- Player portrait and health hearts at the top-left.
- Seed, tile, and chunk telemetry at the top-right.
- Ten-slot creative hotbar at the bottom-center.

Current branch at the time of this summary: `master`.

## Functional HUD implementation

The HUD was implemented with Unity UGUI and a pixel-perfect 1280×720 reference canvas. The runtime view is built by `SandboxHudController` and wired through `SandboxHUD.prefab` and `SandboxHudPrefabBuilder`.

Functional behavior includes:

- Ten creative hotbar slots numbered `1`–`9` and `0`.
- Dirt, grass, stone, and copper ore in slots 1–4.
- Empty selection disables placement but preserves breaking and movement.
- Mouse-wheel cycling skips empty slots.
- Infinity quantities represent creative-mode placement.
- Selected-item label follows the active slot and hides after 1.75 seconds.
- Health is supplied by `SandboxPlayerVitals`, with ten health per heart and partial-heart fill support.
- Debug telemetry displays seed plus canonical tile and chunk coordinates.
- Corner panels and hotbar use fixed anchors and safe margins across representative aspect ratios.

## Visual polish decisions

Several screenshot-driven passes adjusted scale, alignment, padding, and ornament density. The final consolidated prompt is [`prompts/hud-polish-consolidated.md`](../prompts/hud-polish-consolidated.md), with [`prompts/hud-fix-health-frame.md`](../prompts/hud-fix-health-frame.md) taking precedence after the structural pass.

Final visual rules:

- Health is one row: portrait followed by ten hearts.
- Numeric health is omitted in the final design.
- The health frame wraps its content without a trailing empty strip.
- Hotbar height is slot-driven; long ornamental rails and center medallions are removed.
- Selected-slot emphasis uses hard gold borders/corner cues rather than glow.
- The item label remains close to the active slot on a dark backing.
- Debug telemetry is plain and subordinate rather than production-themed.
- Point filtering, no compression, no mipmaps, and integer reference positions are required.

## Asset specification and deterministic mockups

A machine-readable asset contract was created at [`specs/hud-assets.json`](../specs/hud-assets.json). It defines:

- Sixteen HUD assets.
- Exact source and display dimensions.
- Unity import settings and slice borders.
- Reference-resolution module positions.
- Shared palette and pixel-grid rules.
- Structured generation prompts for every asset.

The human-readable companion is [`hud-assets-manifest.md`](hud-assets-manifest.md).

Two deterministic Pillow workflows were added:

- [`../scripts/generate_hud_mockups.py`](../../scripts/generate_hud_mockups.py) creates specification fixtures and full HUD mockups.
- [`../scripts/normalize_pixellab_hud_assets.py`](../../scripts/normalize_pixellab_hud_assets.py) converts approved PixelLab drafts into exact Unity production dimensions.

Generated review images are stored under [`images/hud-mockups/`](../images/hud-mockups/).

## PixelLab MCP setup

PixelLab MCP initially returned `401` because the active Codex process lacked the Authorization header. The project now uses [`../scripts/start-pixellab-mcp.js`](../../scripts/start-pixellab-mcp.js), which:

- Loads `PIXELLAB_API_KEY` from the gitignored project `.env` or parent environment.
- Passes the credential to `mcp-remote` through a process-only environment variable.
- Keeps the token out of tracked TOML, command-line arguments, prompts, and documentation.

The project and user-level PixelLab MCP entries were aligned to the launcher. No credential value is recorded in this summary.

## PixelLab generation history

### Rejected component-sheet attempt

The first PixelLab attempt requested a single 512×512 HUD component sheet. It consumed 40 trial generations but produced a fantasy menu with baked `LOAD GAME`, `OPTIONS`, and `EXIT` text, character art, and environment art.

It was rejected and preserved only for provenance:

- [`images/hud-mockups/pixellab-hud-component-sheet-v1-rejected.png`](../images/hud-mockups/pixellab-hud-component-sheet-v1-rejected.png)
- [`images/hud-mockups/pixellab-hud-component-sheet-v1-rejected-preview.png`](../images/hud-mockups/pixellab-hud-component-sheet-v1-rejected-preview.png)

No production sprites were replaced from that sheet.

### Individual-element retry

After the subscription allowance refreshed, assets were retried as individual PixelLab jobs.

Accepted UI generations:

- Health-panel frame.
- Hotbar backing.
- Normal slot.
- Selected slot.
- Debug backing.

Accepted object candidates:

- Player portrait.
- Empty, half, and full hearts.
- Dirt, grass, stone, and copper-ore icons.
- Cursor.

The recommended object candidates were promoted and tagged `project-twelve-hud-v2`.

Rejected individual UI generations:

- Portrait frame: generated another multi-component UI kit.
- Selected-item label: generated an unintended checker texture.

The existing production portrait frame and code-backed item label were retained.

## PixelLab/API documentation learned

PixelLab's Create UI Elements (Pro) documentation showed that the web workflow supports transparent output, palette guidance, concept images, exact size tiers, and candidate grids. PixelLab confirmed that this full Pro workflow is not currently exposed by the MCP `create_ui_asset` tool.

A ProjectTwelve-oriented API reference was saved at [`skills/pixellab-api-v2.md`](pixellab-api-v2.md). It documents:

- Bearer authentication and capability-URL handling.
- Asynchronous job polling.
- Common 401/402/422/429 failures.
- Exact-size and style-reference generation.
- Object review and promotion.
- MCP versus API v2 capability boundaries.
- HUD-specific generation and Unity-import rules.

## Current production wiring

Approved PixelLab images were normalized and wired into the HUD.

Replaced in place while preserving existing `.meta` GUIDs:

- `hud_panel_main.png` — 210×70 health frame, 14 px slice borders.
- `hud_panel_info.png` — 160×62 debug backing, 2 px slice borders.
- `hud_slot_normal.png` — 52×52, 8 px slice borders.
- `hud_slot_selected.png` — 54×54, 8 px slice borders.
- `hud_heart_full.png` — 12×12 accepted PixelLab master. `hud_heart_half.png` and `hud_heart_empty.png` are deterministically derived from its exact silhouette so all health states share one outline and pixel grid.
- `hud_cursor.png` — 16×16 optional cursor asset.

New generated production assets:

- `hud_hotbar_backing.png` — 612×60, 6 px slice borders.
- `hud_player_portrait.png` — 38×38.
- `hud_tile_dirt.png`, `hud_tile_grass.png`, `hud_tile_stone.png`, and `hud_tile_copper_ore.png` — 32×32.

The selected portrait contained disconnected generation artifacts. The normalizer now retains only the largest alpha-connected component and recenters the clean portrait.

`SandboxHudController` gained serialized hotbar and debug sprites. `SandboxHUD.prefab`, `SandboxHudPrefabBuilder`, and `SandboxHudTests` were updated with the new references.

## Validation evidence

Verified:

- Fourteen normalized production images are valid RGBA sprites with exact specified dimensions and visible alpha bounds.
- Runtime C# project builds with zero errors.
- Editor C# project builds with zero errors.
- Prefab and builder references use the intended generated sprite paths/GUIDs.
- Existing and new Unity `.meta` files specify Point filtering, no mipmaps, no compression, and the intended PPU/slice settings.
- Documentation link checks passed before this final summary.
- Paid-assets checks passed; generated public art remains outside `Assets/_Licensed/`.

Unknown or blocked:

- Unity batch validation exited before import with return code 1 and no compiler diagnostic in the log.
- A required unsandboxed Unity retry was rejected because the Codex approval service had reached its usage limit.
- Runtime and editor assemblies compile through `dotnet`, but the generated EditMode `.csproj` fails on a pre-existing NUnit reference issue with missing `Test`/`TestAttribute` symbols. This is not evidence that the Unity Test Runner fails.
- A fresh Game-view screenshot of the newly wired production sprites has not yet been captured.

## Required next verification

1. Open the project in Unity 6000.5.1f1 and allow all new sprites to import.
2. Confirm the Console has no import or compile errors.
3. Run `SandboxHudTests` through the Unity EditMode Test Runner.
4. Enter Play Mode and capture a 1280×720 or 1920×1080 Game-view screenshot.
5. Verify health-frame corners, hotbar backing, slot selection, heart readability, portrait cleanup, tile icons, and debug contrast.
6. Check 16:10, narrow, and ultrawide Game views before considering the PixelLab wiring complete.

## Worktree note

The HUD, PixelLab MCP, generated skill/documentation, and asset changes are currently uncommitted. `Assets/_Licensed` also has pre-existing unrelated submodule state and must not be staged or modified as part of the public HUD work.
