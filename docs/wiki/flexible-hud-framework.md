---
type: Design
title: Flexible HUD Framework
description: Architecture and behavioral contract for ProjectTwelve runtime HUD and reusable UI controls.
resource: wiki/flexible-hud-framework.md
tags: [docs, wiki, ui, hud, inventory, architecture]
timestamp: 2026-07-19T22:57:02Z
okf_version: 0.1
---

# Flexible HUD Framework

## Status and scope

This document defines the architecture for evolving the prototype HUD into a reusable runtime UI
framework. It is an incremental design for the existing Unity uGUI foundation, not approval to
replace the HUD or implement every production screen in one change.

[P4-UX-001](tickets/p4-ux-001-specify-production-ui-flows-for-inventory-crafting-settings-.md)
is claimed and linked to issue #48. The first vertical slice described below is implemented; the
full production-screen inventory, interaction flows, localization structure, controller bindings,
and screenshot certification remain in that ticket.

### Implemented vertical slice (2026-07-19)

- `SandboxUiRoot` supplies the safe-area hierarchy, ordered popup/drag/tooltip/modal layers, one
  Input System `EventSystem`, screen stack, modal focus trap, and gameplay-input gate.
- `SandboxUiTheme` supplies public-code-only Hearthland and Forged Night runtime token sets;
  reusable uGUI panels, labels, buttons, scroll views, slots, tooltips, and modal dialogs consume
  those tokens.
- `SandboxInventoryViewAdapter` caches all 40 inventory slots and turns the domain's coarse change
  event into targeted slot refreshes without moving inventory rules into UI code.
- Pressing `I` opens a 5x8 read-only Backpack demonstration window. `Esc` closes the top screen;
  the window demonstrates deterministic grid focus, theme and scale controls, delayed tooltips,
  and a focus-trapped modal. Gameplay movement, hotbar input, and world editing are suppressed
  while a blocking screen is open.
- Production drag/drop remains disabled until a domain-owned move/split/merge command service
  exists. Presentation-only drag cancellation and rejected-drop restoration are covered by pure
  EditMode tests.

The intended visual direction is **Stardew-inspired but ProjectTwelve-owned**:

- warm parchment content surfaces with dark walnut and copper keylines;
- strong tab silhouettes, large readable labels, and regular inventory cells;
- gold selection/focus cues and compact contextual tooltips;
- persistent HUD elements anchored to screen edges while blocking windows remain centered;
- original ProjectTwelve sprites, icons, typography, proportions, and names - no copied game art.

The proposed warm theme is named **Hearthland**. The existing dark silver/gold theme remains a
second theme so runtime theme switching has a real compatibility target.

## Baseline findings

The following were the confirmed repository facts before the first vertical slice. They explain
the migration constraints and are retained as design evidence.

| Area | Current state | Evidence |
|------|---------------|----------|
| Runtime UI technology | Unity uGUI with `Canvas`, `CanvasScaler`, `Image`, and legacy `Text` | [`SandboxHUD.prefab`](../../Assets/Prefabs/UI/SandboxHUD.prefab), [`SandboxHudController.cs`](../../Assets/Scripts/Sandbox/UI/SandboxHudController.cs), [`Packages/manifest.json`](../../Packages/manifest.json) |
| Construction | One controller builds vitals, a ten-slot hotbar, selected-item label, and debug telemetry at runtime | [`SandboxHudController.cs`](../../Assets/Scripts/Sandbox/UI/SandboxHudController.cs) |
| Authoring | An editor command rebuilds the prefab and scene, then assigns concrete sprite/font paths | [`SandboxHudPrefabBuilder.cs`](../../Assets/Scripts/Editor/SandboxHudPrefabBuilder.cs) |
| Scaling | A 1280x720 reference canvas uses an integer `scaleFactor`; screen-size changes are polled and reapplied | [`SandboxHudPixelPerfectScaler.cs`](../../Assets/Scripts/Sandbox/UI/SandboxHudPixelPerfectScaler.cs) |
| Existing theme | Repo-owned generated HUD sprites are serialized in the prefab; one font path points into the licensed submodule | [`SandboxHUD.prefab`](../../Assets/Prefabs/UI/SandboxHUD.prefab), [`SandboxHudPrefabBuilder.cs`](../../Assets/Scripts/Editor/SandboxHudPrefabBuilder.cs), [`hud-assets-manifest.md`](hud-assets-manifest.md) |
| Inventory | Fixed ordered 40-slot inventory; slots 0-9 are the hotbar; only a coarse parameterless `Changed` event is exposed | [`SandboxInventory.cs`](../../Assets/Scripts/Sandbox/Inventory/SandboxInventory.cs), [`SandboxInventoryConstants.cs`](../../Assets/Scripts/Sandbox/Inventory/SandboxInventoryConstants.cs) |
| Inventory mutation | Placement and breaking use a domain service; the HUD does not own consumption or world-edit rules | [`SandboxInventoryEditService.cs`](../../Assets/Scripts/Sandbox/Inventory/SandboxInventoryEditService.cs), [`SandboxPlayerController.cs`](../../Assets/Scripts/Sandbox/SandboxPlayerController.cs) |
| Hotbar state | A separate pure presentation state owns ten display slots and selection/cycling | [`SandboxCreativeHotbarState.cs`](../../Assets/Scripts/Sandbox/UI/SandboxCreativeHotbarState.cs) |
| Input | Gameplay, pointer, and HUD code create or poll actions directly; there is no shared UI/gameplay context owner | [`SandboxPlayerController.cs`](../../Assets/Scripts/Sandbox/SandboxPlayerController.cs), [`SandboxScreenPointer.cs`](../../Assets/Scripts/Sandbox/SandboxScreenPointer.cs), [`SandboxHudController.cs`](../../Assets/Scripts/Sandbox/UI/SandboxHudController.cs) |
| Focus/navigation | No production `EventSystem` or UI input module is serialized in the main scene or HUD prefab | [`Scene.unity`](../../Assets/Scene.unity), [`SandboxHUD.prefab`](../../Assets/Prefabs/UI/SandboxHUD.prefab) |
| Resolution | Desktop defaults to 1920x1080, allows window resize/fullscreen switching, and uses the single build scene | [`ProjectSettings.asset`](../../ProjectSettings/ProjectSettings.asset), [`EditorBuildSettings.asset`](../../ProjectSettings/EditorBuildSettings.asset) |
| Tests | Existing EditMode coverage checks heart fill, prefab references, integer scale, label expiry, selection geometry, and representative aspect-ratio bounds | [`SandboxHudTests.cs`](../../Assets/Tests/EditMode/SandboxHudTests.cs) |
| Backlog | Production UI flows, input matrix, focus targets, and screen behavior belong to P4-UX-001 | [`P4-UX-001`](tickets/p4-ux-001-specify-production-ui-flows-for-inventory-crafting-settings-.md), [`00-backlog-workflow.md`](../project/00-backlog-workflow.md) |

### Existing strengths to preserve

- The HUD already observes inventory and vitals instead of owning simulation rules.
- Point-filtered sprites, nine-sliced frames, and integer placement are established conventions.
- The hotbar state and scale calculation already have pure logic that is inexpensive to test.
- Debug telemetry can be hidden independently and is suppressed in release builds.
- The existing generated sprites are public-repo assets and can remain the fallback theme.

### Current pain points

1. `SandboxHudController` combines hierarchy construction, layout constants, input polling, theme
   references, inventory binding, and presentation updates. None of its panel/slot/text creation is
   reusable by another screen.
2. Styling is split between static colors and dimensions in the controller, serialized sprite
   fields in the prefab, and asset paths in the editor builder.
3. The coarse inventory `Changed` event makes the HUD re-read every visible slot even when one slot
   changed. That is acceptable for ten slots but does not meet the production grid contract.
4. Direct gameplay input polling has no blocking seam, so a future modal cannot reliably prevent
   movement, jumping, or tile edits.
5. There is no focus owner, modal stack, tooltip layer, drag layer, context-menu layer, safe-area
   root, or persisted layout model.
6. The current scale controller has no user scale, safe-area handling, or controlled fractional
   mode. Its integer-only rule should remain the default until a theme proves a coarser pixel module.
7. The builder's hardcoded licensed font path is a migration risk. New public framework assets must
   use a public placeholder/fallback plus local theme overrides, in line with
   [`PAID_ASSETS.md`](../PAID_ASSETS.md).

### Migration and replacement risk

A UI Toolkit rewrite would discard working uGUI layout, prefab wiring, pixel-scaling tests, and
runtime-created hotbar behavior before it provides a production screen. A hybrid would also require
two focus systems, two theme systems, and explicit render-order coordination. The lowest-risk path
is to extract uGUI primitives behind stable presenter/view boundaries, migrate the existing HUD one
piece at a time, and revisit UI Toolkit only if a concrete screen exposes a measured uGUI limit.

## Technology decision

**Decision: use uGUI for the runtime framework and do not introduce a hybrid in the first slice.**

| Criterion | uGUI | UI Toolkit | Decision impact |
|-----------|------|------------|-----------------|
| Current compatibility | Already in prefab, scene, package set, and tests | Would be a parallel foundation | Strongly favors uGUI |
| Pixel-art rendering | Existing point sprites, sliced images, and scaler are proven | Possible, but would require a new scaling/import validation pass | Favors uGUI |
| Runtime item slots | Straightforward pooled `RectTransform`/`Image` controls | Possible with `VisualElement` reuse | Neutral after migration cost |
| Drag/drop | Event interfaces and raycasting fit inventory cells | Pointer manipulators are capable | Neutral |
| Controller navigation | Built-in `Selectable` navigation plus `EventSystem` | Requires a new project-specific focus contract | Favors uGUI for the slice |
| Nested scroll views | Mature but must avoid mask/layout rebuild excess | Strong retained-mode layout | Slight UI Toolkit advantage |
| Theming | ScriptableObject tokens and sprite references are sufficient | USS is stronger for cascading style | UI Toolkit advantage, not enough to justify replacement |
| Editor authoring | Prefabs and custom builder already exist | UI Builder is attractive | Neutral after current investment |
| Automated testing | Existing prefab/EditMode pattern | Would need new panel/event test harness | Favors uGUI |

Tradeoff: uGUI does not provide CSS-like cascading themes or automatic retained-mode binding. The
framework addresses this with one explicit theme asset, small stateful primitives, event-driven
presenters, and pooled inventory slots. It should not build a custom general-purpose UI engine.

## Recommended architecture

### Component hierarchy

```text
SandboxUiRoot (one screen-space Canvas)
|-- SafeArea
|   |-- PersistentHudLayer
|   |   |-- migrated VitalsPanel
|   |   `-- migrated HotbarView
|   `-- WindowLayer
|       `-- InventoryDemoWindow (first vertical slice)
|-- PopupLayer
|   |-- DropdownLayer
|   `-- ContextMenuLayer
|-- DragLayer
|   `-- DraggedItemPreview
|-- TooltipLayer
|   `-- SandboxUiTooltip
|-- ModalLayer
|   |-- ModalBlocker
|   `-- SandboxUiModalDialog
`-- DebugLayer (development only)

Services on SandboxUiRoot
|-- SandboxUiScaleController
|-- SandboxUiThemeController
|-- SandboxUiFocusController
|-- SandboxUiScreenStack
|-- SandboxUiTooltipService
`-- SandboxUiDragController
```

The root owns layer order, global theme/scale, focus, and blocking state. Screens own only their
local presentation state. Domain state remains outside the UI tree.

### State and data flow

```text
SandboxInventory / SandboxPlayerVitals / settings services
                 |
                 | queries + domain events
                 v
        view adapters / presenters
                 |
                 | immutable view data + targeted change events
                 v
       uGUI primitive/composite controls
                 |
                 | user intent (select, move, confirm, cancel)
                 v
       command interfaces / domain services
                 |
                 `---- result event ----> presenter refresh or feedback
```

Rules:

- Controls never receive mutable inventory collections.
- Presenters do not call `SetSlot`, `Add`, or `TryConsumeAt` to implement UI gestures.
- A failed drag leaves the domain unchanged; the presenter clears the preview and redraws the
  source slot from the adapter.
- Theme and scale changes invalidate presentation only, not domain data.
- Frequent changes are event-driven; no static control receives its own `Update` loop.

### Foundation responsibilities

| Component | Responsibility |
|-----------|----------------|
| `SandboxUiRoot` | Own the canvas, named layers, safe area, service references, and gameplay-input blocking aggregate |
| `SandboxUiScreenStack` | Push/pop standard and modal screens, preserve dismissal order, expose the top dismissible screen |
| `SandboxUiScaleController` | Apply validated scale steps, snap positions, publish scale/resolution changes |
| `SandboxUiThemeController` | Apply one `SandboxUiTheme` to registered controls and publish theme changes |
| `SandboxUiFocusController` | Own current input mode, initial focus, focus restoration, modal trapping, and pointer-vs-controller indicator policy |
| `SandboxUiTooltipService` | Delay, position, clamp, populate, and dismiss the one shared tooltip |
| `SandboxUiDragController` | Own the one drag transaction and preview; ask a command handler to commit or cancel |

### Design system and theme model

`SandboxUiTheme` should be a ScriptableObject containing tokens and public-safe asset references.
Controls register with `SandboxUiThemeController` and update only when the active theme changes.
No control should load sprites by string at runtime.

Proposed base tokens in 1280x720 logical canvas units:

| Token | Default | Purpose |
|-------|---------|---------|
| `spacingUnit` | 4 | All spacing is a multiple of four |
| `paddingSmall/Medium/Large` | 4 / 8 / 12 | Control and panel insets |
| `frameThickness` | 8 | Nine-slice protected corner/frame region |
| `rowHeight` | 28 | Lists and settings rows |
| `buttonHeight` | 32 | Standard action button |
| `iconSmall/Medium/Large` | 16 / 24 / 32 | Icon roles, not arbitrary per-screen sizes |
| `inventorySlotSize` | 48 | Matches the established hotbar cell |
| `minimumHitTarget` | 40 | Interaction target may exceed decorative sprite bounds |
| `fontSmall/Body/Heading` | 12 / 14 / 18 | Theme may substitute a compatible public font |
| `tooltipDelaySeconds` | 0.35 | Mouse hover delay; keyboard/controller focus may be immediate |
| `scrollStep` | 28 | One standard row per discrete input |
| `fast/normalAnimation` | 0.08 / 0.14 | Focus/press feedback only; no decorative over-animation |

Proposed Hearthland palette (original ProjectTwelve values, not sampled game assets):

| Role | Color | Use |
|------|-------|-----|
| `surface` | `#F3D39A` | Main parchment fill |
| `surfaceRaised` | `#F8E4B7` | Rows, text fields, selected content |
| `frameOuter` | `#6E351D` | Dark walnut silhouette |
| `frameInner` | `#C76B2A` | Copper inner keyline |
| `accent` | `#F2B83E` | Selection and primary focus |
| `text` | `#3A2118` | Body text on parchment |
| `textMuted` | `#79533B` | Secondary labels and disabled text |
| `positive` | `#5E8B43` | Valid/craftable/confirmed state |
| `warning` | `#C77926` | Attention state |
| `destructive` | `#A64235` | Destructive actions and errors |
| `focusHighContrast` | `#FFF2A6` | Outer focus cue independent of selection |

The current dark silver/gold sprites become a fallback theme with the same semantic roles. A theme
declares its sprite pixel module and allowed scale quantum. It must not contain important labels
baked into textures.

### Primitive controls

The first framework should provide only primitives with an immediate vertical-slice consumer:

- `SandboxUiPanel`: plain or framed nine-sliced panel with padding token.
- `SandboxUiLabel`: body/heading/muted roles, wrapping, ellipsis, semantic text.
- `SandboxUiButton`: normal, hovered, pressed, focused, disabled, and destructive variants.
- `SandboxUiScrollView`: viewport, content, scrollbar, discrete-row controller navigation.
- `SandboxUiItemSlot`: icon, count, empty/selected/focused/locked states and tooltip target.
- `SandboxUiTooltipTarget`: supplies structured tooltip data to the shared service.
- `SandboxUiModalDialog`: title, wrapped body, actions, initial focus, cancel policy.

Composite controls are built from these primitives:

- configurable `SandboxUiInventoryGrid`;
- hotbar view using item slots without inventory mutation rules;
- labeled settings row and dropdown;
- tab strip and tabbed window;
- explicitly resizable/draggable window.

Dropdowns, text input, context menus, progress bars, rebind hints, and tabs belong in follow-up
slices when a real screen consumes them. Their visual/state contracts should reuse the same tokens.

## Core interfaces and data contracts

These are proposed signatures, not implemented repository facts. Names may be adjusted during the
claimed ticket, but their responsibilities should remain narrow.

```csharp
public interface ISandboxUiScreen
{
    bool BlocksGameplayInput { get; }
    bool IsDismissible { get; }
    Selectable InitialFocus { get; }
    void Show();
    void Hide();
}

public readonly struct SandboxInventorySlotViewData
{
    public SandboxInventorySlotViewData(
        int index,
        string itemId,
        Sprite icon,
        int count,
        int maximumCount,
        bool isSelected,
        bool isLocked,
        SandboxTooltipData tooltip)
    {
        Index = index;
        ItemId = itemId;
        Icon = icon;
        Count = count;
        MaximumCount = maximumCount;
        IsSelected = isSelected;
        IsLocked = isLocked;
        Tooltip = tooltip;
    }

    public int Index { get; }
    public string ItemId { get; }
    public Sprite Icon { get; }
    public int Count { get; }
    public int MaximumCount { get; }
    public bool IsEmpty => string.IsNullOrEmpty(ItemId) || Count <= 0;
    public bool IsSelected { get; }
    public bool IsLocked { get; }
    public SandboxTooltipData Tooltip { get; }
}

public readonly struct SandboxTooltipData
{
    public SandboxTooltipData(string titleKey, string categoryKey, string bodyKey)
    {
        TitleKey = titleKey;
        CategoryKey = categoryKey;
        BodyKey = bodyKey;
    }

    public string TitleKey { get; }
    public string CategoryKey { get; }
    public string BodyKey { get; }
}

public interface ISandboxInventoryViewModel
{
    int SlotCount { get; }
    SandboxInventorySlotViewData GetSlot(int index);
    event Action<int> SlotChanged;
}

public interface ISandboxInventoryCommandHandler
{
    SandboxInventoryOperationResult TryMove(int sourceIndex, int destinationIndex, int amount);
}

public enum SandboxInventoryOperationResult
{
    Success,
    InvalidSource,
    InvalidDestination,
    InvalidAmount,
    Rejected
}
```

The fixed integer index is the stable slot identifier for the current ordered inventory. The view
adapter can subscribe to `SandboxInventory.Changed`, compare its 40 cached slots with the current
model, and emit `SlotChanged(index)` only for differences. This meets targeted refresh behavior
without modifying domain event semantics in the UI slice.

Move/swap/split/merge/quick-transfer behavior does not exist in the current domain API. A command
handler must be implemented in the inventory domain before production drag/drop is enabled. UI
code must not emulate those rules with direct `SetSlot` calls.

## Layout and scaling contract

### Global scale

Keep the established 1280x720 logical reference and point filtering. Replace arbitrary scale with
a theme-declared quantum:

```text
fit = min(viewportWidth / 1280, viewportHeight / 720)
quantum = 1 / theme.pixelModule
maximumCrispScale = floor(fit / quantum) * quantum
effectiveScale = clamp(userScale, theme.minimumScale, maximumCrispScale)
```

- The legacy theme declares a one-pixel module and therefore remains integer-only.
- Hearthland art should use a consistent two-pixel drawing module, allowing validated 0.5 scale
  steps without splitting a design module across physical pixels.
- Every theme is visually certified at each declared scale step; unsupported arbitrary fractions
  are rejected rather than blurred.
- Rect positions and sizes are snapped to the reciprocal scale grid before rendering.
- Text point sizes resolve to whole physical pixels. Text may use a separate readability floor,
  but must remain aligned with its decorated control.
- User scale is clamped after a resolution change and the unclamped preference is retained so it
  can be restored when the viewport grows again.

### Safe area and overflow

`SafeArea` derives normalized anchors from `Screen.safeArea`. Persistent HUD and standard windows
are clamped inside it with 8 logical pixels of edge padding. Popup, drag, tooltip, and modal layers
use the full root but clamp visible content to the safe area.

At a small window, the framework keeps minimum hit targets and text size, switches eligible
windows to a scroll view, and clamps panel size. It does not shrink controls below their minimum or
stretch panel corners.

### Explicitly resizable panels

Resizing is an opt-in component, not a property of every panel.

- Size snaps to the 4-unit spacing grid.
- Minimum size is the larger of the panel's content minimum and theme token.
- Maximum size is safe-area size minus 16 logical pixels.
- Eight mouse resize regions support edges and corners; decorative handles may be smaller than
  their invisible hit targets.
- Keyboard/controller users open a window-layout mode and adjust size by one grid unit per action;
  settings may also expose width/height presets.
- Position is clamped after every resize and resolution change.
- Optional aspect constraints are declared per window.
- Reset restores the screen's authored default rect.

Persist `{schemaVersion, screenId, anchor, normalizedPosition, logicalSize, userScale}` in a
UI-specific JSON settings file under `Application.persistentDataPath`. Do not mix layout preference
with world save data. Unknown or invalid records fall back to authored defaults.

## Input and focus model

The production root adds exactly one `EventSystem` with `InputSystemUIInputModule`. It owns a UI
action map through an approved project action asset; individual controls do not create actions.
Action names and bindings must be specified in P4-UX-001 before implementation.

`SandboxUiScreenStack.GameplayInputBlocked` becomes the single presentation-owned blocking signal.
`SandboxPlayerController` and pointer-driven tile editing consult an injected/read-only gate before
processing gameplay input. UI controls request actions through domain interfaces; they never call
world mutation directly.

Focus rules:

1. Opening a screen stores the prior focused control and selects `InitialFocus`.
2. Opening a modal pushes it above all standard windows and traps navigation within its selectables.
3. Cancel closes the topmost dismissible popup/modal/window, in that order.
4. Closing the final blocking screen restores gameplay input and the prior valid focus.
5. Disabled controls are excluded from explicit navigation links.
6. Inventory-grid navigation is deterministic: left/right stay in the row, up/down preserve the
   column where possible, and incomplete final rows clamp to the last valid cell.
7. Mouse movement switches the visible focus cue to hover; directional/submit/cancel input restores
   the focus cue. Hover does not clear controller selection.
8. Drag operations are cancellable. Cancel or invalid drop discards preview state and redraws both
   endpoints from the view model.

## Layering and screen management

Layer order is declared once by `SandboxUiRoot`, never with unrelated sorting constants:

1. world-space indicators (separate world-space canvas);
2. persistent HUD;
3. standard windows;
4. dropdowns/context menus;
5. dragged-item preview;
6. tooltips;
7. modal dialogs/blocker;
8. development-only debug UI.

Dropdowns and context menus are reparented to `PopupLayer`, so scroll masks cannot clip them.
Tooltips are reparented to `TooltipLayer` and cannot appear behind a window. A modal blocker catches
pointer events outside the dialog.

## Inventory integration

`SandboxInventoryViewAdapter` exposes only view data resolved from the inventory and item registry.
It caches each slot, diffs on the existing coarse event, and updates only changed `SandboxUiItemSlot`
instances. The grid pools slot views across rebuilds and only rebuilds hierarchy when dimensions or
slot count change.

The grid accepts columns, rows, slot size, spacing, and a view-model range. Index mapping is:

```text
row = index / columns
column = index % columns
index = row * columns + column
```

The current production-shaped demonstration is a 5x8 grid over the 40-slot inventory, with a 10-slot
hotbar view sharing the same adapter. Selection, focus, hover, drag-source, drag-target, empty,
locked, and disabled visuals are independent states so selection is not confused with controller
focus.

## Text and localization readiness

- User-facing strings come from a text catalog/key service introduced with the consuming screen;
  controls receive resolved strings, not hardcoded English in decorative textures.
- Labels declare wrap, ellipsis, or dynamic-height behavior explicitly.
- Tooltips have a maximum width and scroll long bodies rather than overflowing the viewport.
- Item names and descriptions are view data; controls do not derive display text from registry IDs.
- Input fields declare validation and placeholder behavior; numeric validation belongs in the
  presenter or settings service, not the visual component.

## Performance contract

- No per-frame hierarchy reconstruction or sprite lookup by string.
- One root-level input/focus update is preferable to one `Update` per static control.
- Inventory slot views are pooled; a single changed slot refreshes one view.
- Theme/scale changes may batch a full visible-tree refresh because they are infrequent.
- Uncommon screens are created lazily and disabled screens do not block raycasts.
- Avoid nested masks unless a real overflow requirement needs them.
- Profile the 40-slot grid and tooltip path before introducing virtualization; 40 cells alone do
  not justify a general virtualized collection framework.

Expected frequent flows:

| Event | Update path |
|-------|-------------|
| Health change | `SandboxPlayerVitals.Changed` -> vitals presenter -> changed heart fills only |
| Hotbar selection | selection state event -> prior/new slot visual + item label |
| One inventory slot changes | inventory event -> adapter diff -> one `SlotChanged(index)` -> one item slot |
| Resolution change | scale/safe-area controller -> root layout/clamping once |

## Developer diagnostics

An editor/development-only inspector overlay may report UI scale, logical/actual resolution, safe
area, focused control, input mode, open screen stack, modal depth, panel bounds, pixel misalignment,
overflow, and active drag endpoints. Release builds must not create this hierarchy. Editor-only
helpers use `#if UNITY_EDITOR`; runtime development diagnostics use `Debug.isDebugBuild` or the
existing development-build convention.

## Proposed file structure

This is the minimal target structure. Directories should be created only as their first consumer is
implemented.

```text
Assets/Scripts/Sandbox/UI/
  Foundation/
    SandboxUiRoot.cs
    SandboxUiScaleController.cs
    SandboxUiThemeController.cs
    SandboxUiScreenStack.cs
    SandboxUiFocusController.cs
  Theme/
    SandboxUiTheme.cs
  Controls/
    SandboxUiPanel.cs
    SandboxUiLabel.cs
    SandboxUiButton.cs
    SandboxUiScrollView.cs
    SandboxUiTooltipTarget.cs
    SandboxUiModalDialog.cs
  Inventory/
    SandboxInventorySlotViewData.cs
    SandboxInventoryViewAdapter.cs
    SandboxUiItemSlot.cs
    SandboxUiInventoryGrid.cs
    SandboxUiDragController.cs
  Screens/
    SandboxInventoryDemoWindow.cs
  Debug/
    SandboxUiDebugOverlay.cs

Assets/Scripts/Sandbox/Inventory/
  SandboxInventoryCommandService.cs   # domain-owned move/split/merge operations

Assets/Tests/EditMode/UI/
  SandboxUiScaleTests.cs
  SandboxUiLayoutTests.cs
  SandboxInventoryViewAdapterTests.cs
  SandboxUiFocusTests.cs

Assets/Tests/PlayMode/UI/
  SandboxUiInteractionTests.cs

Assets/Prefabs/UI/Framework/
  SandboxUiRoot.prefab
  SandboxInventoryDemoWindow.prefab

Assets/Sprites/UI/Themes/Hearthland/
  public, original sprites plus .meta files
```

The existing UI files remain in place during migration. Do not perform a namespace or folder-wide
cleanup in the first slice.

## Minimal vertical-slice plan

The first claimed implementation slice should demonstrate the architecture without migrating every
screen.

1. Add pure scale-step, safe-area clamp, grid-index, and adapter-diff tests.
2. Add `SandboxUiTheme` with the current fallback theme and an original placeholder Hearthland
   theme. Do not reference licensed binary assets in new public theme source.
3. Add `SandboxUiRoot` and named layers around the existing HUD without changing its behavior.
4. Add one reusable framed panel, label, button, scroll view, item slot, tooltip, and modal dialog.
5. Add the cached `SandboxInventoryViewAdapter` and a configurable 5x8 read-only inventory grid.
6. Add one demonstration modal opened from the inventory window. It traps focus and blocks gameplay.
7. Add global theme and validated scale controls to that window.
8. Add an `EventSystem` and deterministic keyboard/mouse/controller focus demonstration.
9. Keep production drag/drop disabled until the inventory command service exists; exercise cancel
   and invalid-drop restoration with a test command handler in the slice.
10. Migrate the hotbar to the item-slot primitive only after the new root passes parity tests.

Observable success: the existing HUD remains functional, the demonstration window uses shared
tokens and primitives, one model-slot change refreshes one view, a modal blocks gameplay and traps
focus, theme/scale can change at runtime, and every required automated check passes.

## Testing plan

### EditMode

- scale quantization for one- and two-pixel themes, user preference clamping, and resolution change;
- safe-area and panel viewport clamping;
- minimum/maximum resize bounds and reset behavior;
- index/row/column mapping for multiple grid dimensions and incomplete final rows;
- deterministic focus navigation and disabled-cell skipping;
- cached adapter emits only affected slot indices after a coarse inventory event;
- drag cancel and invalid-drop restoration;
- modal focus trapping and gameplay-input blocking;
- tooltip delay, replacement, viewport clamping, and dismissal;
- missing theme token/asset fallback;
- existing `SandboxHudTests` parity.

### PlayMode/manual

- mouse, keyboard, and gamepad focus handoff;
- modal cancel/confirm and gameplay leakage checks;
- drag preview layer above windows and below tooltips/modals;
- 1280x720, 1920x1080, 2560x1440, 2560x1080, 3840x2160;
- 16:10, 4:3, 21:9, and a small scroll-required window;
- windowed/fullscreen transitions and restored panel position;
- point-filtered frame corners and integer/module-aligned sprite pixels at every declared scale.

### Repository commands

Run the repository-documented Unity batch validation and targeted EditMode tests from
[`AGENTS.md`](../../AGENTS.md). For documentation changes run:

```text
python3 scripts/check_markdown_links.py
python3 scripts/okf_lint.py --profile project --exclude-prefix archive/ docs
python3 scripts/wiki_lint.py --exclude-prefix archive/
```

Before commit run `python3 scripts/check_paid_assets.py --staged`. Run `tools/tile-viz` only if a
shared sprite/rendering utility is changed; UI-only code must not modify autotile behavior.

## Migration strategy

1. Land root/layers/theme/scale/focus and the demonstration window behind P4-UX-001.
2. Add the inventory command service in the domain layer, then enable production drag/drop.
3. Migrate hotbar slots and tooltips to shared controls while preserving selection and quantity
   behavior.
4. Extract vitals into a presenter plus reusable status controls.
5. Implement settings first as the broad primitive consumer (labels, buttons, toggles, dropdowns,
   scroll, scale/theme changes).
6. Add crafting, pause, death, and multiplayer screens only when their domain dependencies are
   ready.
7. Remove controller-local HUD construction helpers only after all existing callers and tests have
   migrated.

Each step must be shippable and retain the old HUD until the replacement path has parity evidence.

## Risks and unresolved questions

### Confirmed risks

- The current inventory has no move/swap/split/merge API; enabling drag/drop before a domain
  command service would put gameplay rules in UI code.
- Directly created gameplay input actions need a minimal blocking seam before any modal can be safe.
- Existing theme wiring includes a licensed font path; new public themes need a public fallback and
  local override convention.
- Controlled half-step scaling is only safe for assets proven to use a consistent two-pixel module.
- Runtime-built hierarchy is easy to test in code but harder to author visually; primitives should
  use prefabs where that improves iteration without recreating the whole HUD manually.

### Assumptions requiring validation

- P4-UX-001 will approve the Input System action asset and exact UI action names.
- Hearthland art can maintain a two-pixel module across borders, icons, and focus cues.
- A 40-cell non-virtualized pooled grid meets frame and allocation budgets.
- UI layout preference belongs in local settings and does not need multiplayer synchronization.
- Legacy `Text` remains acceptable for the first slice; adopting TextMeshPro requires a separate
  font/licensing/import decision and should not be bundled silently.

## Suggested follow-up tickets

These are proposals to add or split when P4-UX-001 is claimed; they are not new canonical tickets yet.

1. **P4-UX UI foundation vertical slice** - root, layers, theme, scale, focus, primitives, demo grid,
   tooltip, and modal; references issue #48.
2. **P4 inventory command transactions** - move, swap, split, merge, quick transfer, and atomic
   failure results in the domain layer.
3. **P4 production inventory and hotbar migration** - enable drag/drop and migrate the current
   hotbar after command-service readiness.
4. **P4 settings and rebinding screen** - controls, audio/video/gameplay settings, action hints, and
   global UI scale/theme.
5. **P4 localization and accessible semantics** - text catalog, semantic labels, contrast review,
   and non-color state cues.
6. **P5 UI resolution certification** - full platform/aspect/DPI matrix under P5-PLAT-001.

## Acceptance traceability

| Acceptance concern | Design response |
|--------------------|-----------------|
| Shared design tokens | One `SandboxUiTheme` and semantic control roles |
| Crisp scale | Theme-declared pixel module and validated scale quantum |
| Panels remain visible | Safe-area root, clamp on resolution/resize, persisted normalized position |
| Resizable bounds | Opt-in resizer with token/content minimum and safe-area maximum |
| Configurable grids | Columns/rows/slot size/spacing inputs with pooled views |
| Targeted slot refresh | Cache-and-diff adapter emits `SlotChanged(index)` |
| No gameplay rules in UI | Query adapter + domain command handler boundary |
| Unified navigation | One EventSystem and focus controller |
| Modal safety | Modal stack, focus trap, and one gameplay-input blocking signal |
| Correct popup layers | Named root layers and reparented popup/drag/tooltip views |
| Long text | Explicit wrap/ellipsis/dynamic/scroll behavior |
| Licensed asset boundary | Public-safe theme assets and local override; no new paid blobs |
| Existing HUD parity | Incremental migration with current tests retained |

## Related references

- [Gameplay systems](gameplay-systems.md)
- [HUD asset manifest](hud-assets-manifest.md)
- [HUD redesign for PixelLab](hud-redesign-pixellab.md)
- [P4-UX-001](tickets/p4-ux-001-specify-production-ui-flows-for-inventory-crafting-settings-.md)
- [Quality gates](quality-gates.md)
- [Paid and licensed assets](../PAID_ASSETS.md)
