---
type: Plan
title: Visual Override Mode Plan
description: Shortcut and persistence plan for sandbox visual override editing.
resource: wiki/visual-override-plan.md
tags: [docs, visual, debug, shortcuts]
timestamp: 2026-07-13T02:18:45Z
---

# Visual Override Mode Plan

Visual Override Mode is a debug/editing mode for saving visual-only adjustments beside the normal sandbox world save. It must not change the established world save/load keys.

## Shortcut table

| Context | Key | Action |
|---------|-----|--------|
| Normal sandbox play | `F5` | Save `sandbox-world.json` through `SandboxWorld.SaveToPath`. |
| Normal sandbox play | `F6` | Load `sandbox-world.json` through `SandboxWorld.LoadFromPath` (not F9 — Unity Profiler RecordToggle). |
| Visual Override Mode | `F5` | Save `sandbox-world.json` and the visual override sidecar `sandbox-world.visual-overrides.json`. |
| Visual Override Mode | `F6` | Unchanged normal load of `sandbox-world.json`; visual override saving never uses `F6`. |
| Debug gate on (`debugOverrideModeEnabled`) | `F8` | Toggle Visual Override Mode (logs state to Console). |
| Visual Override Mode active | `Tab` | Switch Ground / Cover layer for the hovered cell. Cover edits apply only on **exposed grass** (air directly above). |
| Visual Override Mode active | `[` / `]` | Previous / next override sprite id (ground: 0–31; GrassA cover: 0–5). |
| Visual Override Mode active | `Shift` + `[` / `]` | Jump override sprite id by 8 on ground, or by the cover tileset size (6 for GrassA). |
| Visual Override Mode active | `X` / `Y` | Toggle override `flipX` / `flipY`. |
| Visual Override Mode active | `R` / `Shift` + `R` | Rotate override +90° / −90°. |
| Visual Override Mode active | `C` | Clear override on selected cell/layer. |
| Visual Override Mode active | `N` | Cycle override note preset. |
| Always | `F3` | Cycle ground autotile debug overlays (includes `VisualOverrideLabel`). |

F8 logs:

- Gate closed: `Visual Override Mode unavailable (enable debugOverrideModeEnabled in Editor or use a development build).`
- Toggle on: `Visual Override Mode: ON — Tab layer, [/] sprite, X/Y flip, R rotate, C clear, F5 save sidecar.`
- Toggle off: `Visual Override Mode: OFF`

## Regression guard

`SandboxSaveLoadShortcutRouter` owns shortcut routing as a pure input-level seam. EditMode tests assert that Visual Override Mode + `F5` resolves to sidecar save and never to `LoadWorld`, preserving the normal `F5`/`F6` behavior while preventing future save/load key conflicts.
