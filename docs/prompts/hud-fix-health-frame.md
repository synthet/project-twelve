---
type: Prompt
title: HUD Fix — Health Panel Frame Regression
description: Single-issue prompt to repair the distorted health panel frame introduced during the HUD polish pass. Final HUD task; supersedes remaining consolidated-polish items that conflict.
resource: prompts/hud-fix-health-frame.md
tags: [prompt, agent, unity, hud, ui, fix, regression]
timestamp: 2026-07-11T18:00:00Z
okf_version: 0.1
---

# Single-issue fix: health panel frame regression

You are an agentic coding assistant working inside **ProjectTwelve**, a Unity 6 (`6000.5.1f1`) Terraria-like 2D sandbox prototype.

The previous HUD polish pass ([`docs/prompts/hud-polish-consolidated.md`](hud-polish-consolidated.md)) succeeded structurally: the health panel is now a single row (portrait + hearts) and the hotbar rails were removed. **Do not touch any of that.** It introduced exactly one regression, and fixing it is your entire scope.

## Project contract (minimal)

Before editing, read [`AGENTS.md`](../../AGENTS.md) and inspect:

- [`Assets/Scripts/Sandbox/UI/SandboxHudController.cs`](../../Assets/Scripts/Sandbox/UI/SandboxHudController.cs) — `BuildVitalsPanel()` builds the health panel at runtime
- [`Assets/Prefabs/UI/SandboxHUD.prefab`](../../Assets/Prefabs/UI/SandboxHUD.prefab) — `panelSprite` wires to `hud_panel_main.png`
- [`Assets/Sprites/UI/Generated/hud_panel_main.png`](../../Assets/Sprites/UI/Generated/hud_panel_main.png) and its `.meta` — 9-slice borders
- Hotbar slot frames ([`hud_slot_normal.png`](../../Assets/Sprites/UI/Generated/hud_slot_normal.png)) as the visual benchmark for intact slicing

## The regression

When the health panel was resized to a single row, its ornate frame sprite was visually mangled:

- corner ornaments look crushed/squished instead of preserved at native size;
- edge detail is muddy and has lost definition compared to the (intact) hotbar slot frames, which use the same art style and survived their pass correctly;
- there is a stray visual artifact near the bottom-right of the panel frame;
- a strip of empty framed space remains to the right of the hearts row — the frame is not wrapping its content tightly.

This is the signature of scaling a decorative frame non-uniformly, or 9-slicing it with border insets too narrow to contain the corner ornaments.

## The task

1. **Diagnose before editing.** Inspect the health panel frame's `Image` component (Image Type — Simple vs Sliced vs Tiled), the source sprite's 9-slice border values in the Sprite Editor, and the `RectTransform` dimensions. State what you find: is it unsliced and stretched, or sliced with bad borders?
2. **Fix the slicing.** Set the sprite's 9-slice borders so each corner region fully contains its corner ornament at native pixel size, and edges tile or stretch only in their safe repeating regions. Set the Image Type to Sliced (with Fill Center as the current design uses). If the frame art has large non-repeating edge medallions that 9-slice cannot preserve at this panel size, say so and propose the smallest alternative (e.g., a plainer frame variant from the same asset set) instead of shipping a distorted frame.
3. **Tighten the width.** Reduce the panel width so the frame wraps `[portrait] [hearts row]` with visually consistent padding on all sides — the trailing empty strip to the right of the hearts must be gone. Keep everything on one row; **do not reintroduce the numeric readout**.
4. **Remove the artifact.** Identify the stray element near the panel's bottom-right (leftover child object, overlapping ornament sprite, or a slicing seam) and eliminate it.
5. **Verify pixel integrity.** Filter Mode Point, no compression, integer `RectTransform` positions/sizes at the reference resolution. The repaired frame's corner ornaments must be pixel-crisp and comparable in quality to the hotbar slot frames — use them as the visual benchmark.

## Reporting

Output one report in this format:

```
Diagnosis: <what was actually wrong — slicing state, borders, scaling>
Fix applied: <what changed, with before/after border values and panel dimensions>
Artifact cause: <what the stray element was>
Verification: <how you confirmed corner ornaments are undistorted>
Deviations: <anything done differently than specified, and why>
```

## Rules

- **One commit.** Touch only the health panel frame sprite settings, its Image component, and its RectTransform/layout. Do not modify the hearts, portrait, hotbar, item label, debug box, or any code logic unless the artifact in step 4 is code-generated — in that case make the minimal code change and say so.
- Do not replace the art style; prefer repairing the existing frame asset's import/slice settings over substituting new art.
- Do not propose or perform any other HUD improvements. This is the **final HUD task**: when this fix is verified, the HUD polish effort is **closed**. State that explicitly at the end of your report.

## Testing

At minimum after changes:

```bash
Unity -batchmode -quit -projectPath . -logFile Logs/unity-validate.log
Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile Logs/unity-editmode-tests.log
python3 scripts/check_paid_assets.py --staged
```

Target: `SandboxHudTests` — update vitals width/height assertions if panel dimensions change.

Capture a before/after Game View screenshot showing corner ornaments comparable to hotbar slot frames.
