---
type: Design
title: Flexible HUD Framework
description: Architecture and behavioral contract for ProjectTwelve runtime HUD and reusable UI controls.
resource: wiki/flexible-hud-framework.md
tags: [docs, wiki, ui, hud, inventory, architecture]
timestamp: 2026-07-20T16:23:11Z
okf_version: 0.1
---

# Flexible HUD Framework

A reusable uGUI HUD/UI framework for ProjectTwelve. It gives every screen one shared design-token
system, pixel-perfect scaling, layered rendering, a single focus/input model, and a clean boundary to
domain data — so screens compose primitives instead of re-inventing layout, styling, and interaction.

This document is the architecture/behavior contract for the UI **code** layer. It complements — and
does not replace — the HUD **asset/generation** contract in
[`hud-assets-manifest.md`](hud-assets-manifest.md) and the visual rules in
[`../VISUAL_BEHAVIOR_SPEC.md`](../VISUAL_BEHAVIOR_SPEC.md).

## Why this exists

The prototype ships a working HUD (`Assets/Scripts/Sandbox/UI/SandboxHudController.cs`, ~727 lines)
that builds its view tree in code with private factory helpers, a tested integer pixel-perfect scaler
(`SandboxHudPixelPerfectScaler.cs`), a hotbar model (`SandboxCreativeHotbarState.cs`), and a
registry-backed inventory (`Assets/Scripts/Sandbox/Inventory/SandboxInventory.cs`). Without a shared
control layer, each new screen (inventory, crafting, settings, multiplayer, pause, death — see
[P4-UX-001](tickets/p4-ux-001-specify-production-ui-flows-for-inventory-crafting-settings-.md)) would
re-implement all of the above. This framework extracts the reusable pieces and is the predecessor the
P4-UX-001 screen work builds on.

## Technology decision: extend uGUI

**Decision:** build on legacy uGUI. Recorded rationale (grounded in the repo):

- The existing HUD, scaler, and EditMode tests are all uGUI; there are **no `.uxml`/`.uss`** files in
  the project, so UI Toolkit would be greenfield with no reuse.
- Runtime pixel-art control (integer `CanvasScaler` factor, point filtering, 9-slice) is proven in
  `SandboxHudPixelPerfectScaler` and `SandboxHudPrefabBuilder`; UITK's runtime pixel-perfect and
  world-space story is weaker for a 2D pixel HUD.
- Drag-and-drop, runtime-created slots, dynamic tooltips, `Selectable`-based controller navigation, and
  `AddComponent`+`Awake()` EditMode testing are all straightforward in uGUI and match existing tests.
- URP 2D (17.5.0) and the code-created Input System (1.19.0) integrate with uGUI with no new packages.

**Tradeoffs:** uGUI is GameObject-heavy (mitigated by pooling + event-driven refresh, no per-frame
rebuilds) and has no style sheets (mitigated by the `UiTheme` token asset). UI Toolkit remains a
possible future choice for heavy **editor** tooling only; it is out of scope here.

## Layer architecture

The framework lives in a dedicated assembly `ProjectTwelve.UI` (`Assets/Scripts/UI/`). The dependency
direction is strict: **`ProjectTwelve.UI` never references the Sandbox domain.** Domain-specific
adapters live in `ProjectTwelve.Runtime` (which references `ProjectTwelve.UI`).

```
ProjectTwelve.Runtime  ──references──▶  ProjectTwelve.UI  ──references──▶  Unity.ugui, Unity.InputSystem
   (domain + adapters)                    (framework only, no domain types)
```

| Layer | Location | Key types |
|-------|----------|-----------|
| Foundation | `UI/Foundation/` | `HudRoot`, `UiLayer`, `UiScaleController`, `UiFocusController`, `ModalStack`, `TooltipService`, `UiFactory`, `IUiScreen` |
| Design system | `UI/Theme/` | `UiTheme` (ScriptableObject tokens + roles), `UiColorRole`, `UiSpriteRole`, `UiControlState`, `UiThemeProvider` |
| Primitive controls | `UI/Controls/` | `UiControl`, `UiFramedPanel`, `UiButton`, `UiLabel`, `UiScrollView`, `UiTooltipTarget`, `UiModalDialog` |
| Inventory (generic) | `UI/Inventory/` | `IInventoryViewModel`, `InventorySlotViewData`, `IInventoryCommandHandler`, `InventoryOperationResult`, `InventorySlotView`, `InventoryGridView` |
| Sandbox bridge | `Assets/Scripts/Sandbox/UI/Framework/` (Runtime) | `SandboxInventoryViewModel`, `SandboxInventoryCommandHandler`, `HudFrameworkDemo` |

## Concern separation

1. **Domain data** — `SandboxInventory`, `SandboxPlayerVitals`, `SandboxRegistries` (untouched).
2. **Presentation state** — selection, active tab, scroll position, dragged item, tooltip target, UI
   scale (owned by controls/`HudRoot`, never in domain code).
3. **View components** — the primitives/composites above.
4. **Input routing** — `ModalStack.BlocksGameplayInput` is the single source of truth for suppressing
   gameplay input; `UiFocusController` reconciles mouse hover vs directional focus (last-input wins).
5. **Styling/theming** — all values come from `UiTheme` tokens/roles; no literals in controls.
6. **Persistence** — UI scale via `UiScaleController` (panel position/size persistence is a follow-up).

The HUD never mutates world data from a visual control. Writes go through `IInventoryCommandHandler`;
reads come through `IInventoryViewModel`. Inventory/stacking rules live on the domain side
(`SandboxInventoryCommandHandler`), never in a view MonoBehaviour.

## Data flow

```
SandboxInventory ── Changed (coarse) ─▶ SandboxInventoryViewModel ─ SlotChanged(i) ─▶ InventoryGridView
                                          (diffs a snapshot;                            └─ InventorySlotView (pooled)
                                           one changed slot -> one event)
InventorySlotView ─ drop ─▶ InventoryGridView.HandleDrop ─▶ IInventoryCommandHandler ─▶ SandboxInventory
                                          (rejected result -> re-read source+dest = restore)
```

Because the domain inventory only raises a coarse "something changed" event, the adapter keeps a
snapshot and diffs it, emitting `SlotChanged(index)` only for slots that actually changed. This is what
lets the grid refresh **one** slot instead of re-reading all of them.

## Scaling model

- Reference resolution **1280×720**, reference PPU **100** (matches `SandboxHudPrefabBuilder`).
- `CanvasScaler` is always `ConstantPixelSize` with an **integer** `scaleFactor` — pixel-art borders
  never resample.
- `UiScaleController.ComputeMaxIntegerScale` floors the fit ratio; `ComputeEffectiveScale` clamps the
  user preference into `[1, maxFit]` (0 = auto = largest fit). A layout can never scale off-screen.
- 9-slice (`Image.Type.Sliced`) or flat tints for borders; sprites anchored by bounds, never stretched.
- When content no longer fits, `UiScrollView` (a `RectMask2D`-based scroll fallback) takes over.

## Layering model

`UiLayer` defines ordered bands (back→front): WorldIndicators, PersistentHud, Windows,
DropdownsContextMenus, DragPreview, Tooltips, Modal, Debug. `HudRoot` owns one root per band, so a
tooltip cannot render behind a panel and a dropdown cannot clip inside a scroll area — no scattered
per-prefab sorting constants.

## Input & focus model

- `ModalStack` reports `BlocksGameplayInput`; while true the player controller suppresses world input.
- `UiFocusController` traps focus inside a modal root (`PushTrap`/`PopTrap`), rejects focus on disabled
  controls, and shows the focus ring only for keyboard/controller (not mouse).
- Controls derive from `Selectable` (`UiButton`, `InventorySlotView`) for deterministic directional
  navigation. `InventoryGridView.Navigate` gives deterministic grid traversal.
- No new input actions are invented; the framework reuses the project's existing Input System idioms.

## Update flows for frequent events

| Event | Path | Cost |
|-------|------|------|
| Single inventory slot changes | domain `Changed` → adapter diff → `SlotChanged(i)` → `InventoryGridView.RefreshSlot(i)` | one slot rebind |
| Selection changes | `InventoryGridView.SetSelected` | two slot rebinds (old + new) |
| Resolution/scale changes | `UiScaleController.Update` (only when screen size changes) | one `scaleFactor` set + event |
| Theme swap | `UiThemeProvider.ThemeChanged` → bound controls re-skin | event-driven, no per-frame walk |

Slots are pooled (`InventoryGridView` reuses slot GameObjects across rebuilds); nothing rebuilds the
hierarchy per frame.

## Testing strategy

EditMode tests under `Assets/Tests/EditMode/UI/` (in the existing `ProjectTwelve.EditModeTests`
assembly) cover: integer scale math and user-scale clamping; grid index↔row/col mapping and
deterministic focus navigation; single-slot refresh from a coarse event; rejected-drop reporting and
source restoration; modal input-blocking and focus-trapping; theme-token fallback; tooltip timing; and
the Sandbox adapter's diffing + command handler (move/split/swap/clear/reject).

## Minimal vertical slice

`HudFrameworkDemo` (Runtime) + `HudFrameworkDemoBuilder` (Editor menu: *ProjectTwelve/UI/Rebuild HUD
Framework Demo*) assemble a framed window with a scrollable, configurable inventory grid bound to a real
`SandboxInventory`, item tooltips, a UI-scale stepper, and a modal confirmation dialog that blocks
gameplay input and traps focus. The existing HUD is untouched — this demonstrates the reusable pieces.

## Assets & licensing

The framework ships a public placeholder theme (`Assets/Settings/UI/PlaceholderUiTheme.asset`) with no
licensed binaries; every color/sprite role falls back to a built-in default, so controls render with no
art. Licensed pixel-art themes and PixelLab-generated bitmaps stay in the private `Assets/_Licensed/`
submodule per [`../PAID_ASSETS.md`](../PAID_ASSETS.md) and are assigned to a theme locally.

## Follow-ups

- Migrate `SandboxHudController` onto the framework primitives (separate PR).
- Build the P4-UX-001 screens (inventory/crafting/settings/multiplayer/pause/death) on the framework.
- Panel persistence + user-resizable/draggable windows; dropdown, context menu, tab strip, text input.
- Controller input bindings, localization string table, accessibility beyond focus order.
