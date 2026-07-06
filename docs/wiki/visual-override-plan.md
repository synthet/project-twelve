---
type: Plan
title: Visual Override Mode Plan
description: Shortcut and persistence plan for sandbox visual override editing.
resource: visual-override-plan.md
tags: [docs, visual, debug, shortcuts]
timestamp: 2026-07-06T00:00:00Z
---

# Visual Override Mode Plan

Visual Override Mode is a debug/editing mode for saving visual-only adjustments beside the normal sandbox world save. It must not change the established world save/load keys.

## Shortcut table

| Context | Key | Action |
|---------|-----|--------|
| Normal sandbox play | `F5` | Save `sandbox-world.json` through `SandboxWorld.SaveToPath`. |
| Normal sandbox play | `F9` | Load `sandbox-world.json` through `SandboxWorld.LoadFromPath`. |
| Visual Override Mode | `F5` | Save `sandbox-world.json` and the visual override sidecar `sandbox-world.visual-overrides.json`. |
| Visual Override Mode | `F9` | Unchanged normal load of `sandbox-world.json`; visual override saving never uses `F9`. |

## Regression guard

`SandboxSaveLoadShortcutRouter` owns shortcut routing as a pure input-level seam. EditMode tests assert that Visual Override Mode + `F5` resolves to sidecar save and never to `LoadWorld`, preserving the normal `F5`/`F9` behavior while preventing future save/load key conflicts.
