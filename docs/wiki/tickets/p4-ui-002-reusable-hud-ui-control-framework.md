---
type: Task
id: P4-UI-002
title: "[P4-UI-002] Provide a reusable HUD/UI control framework (design + vertical slice)."
description: Reusable uGUI HUD/UI framework — design tokens, pixel-perfect scaling, layered rendering, focus/input model, and an inventory view-model boundary — as the predecessor the P4-UX-001 screens build on.
status: open
phase: "Phase P4 — Feature complete and beta"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues"
github_issue_status: proposed
resource: wiki/tickets/p4-ui-002-reusable-hud-ui-control-framework.md
tags: [docs, wiki, ticket, ui, hud, p4]
timestamp: 2026-07-20T16:23:11Z
okf_version: 0.1
spec_references:
  - "docs/wiki/flexible-hud-framework.md"
  - "docs/wiki/tickets/p4-ux-001-specify-production-ui-flows-for-inventory-crafting-settings-.md"
  - "docs/wiki/tickets/p2-inv-001-specify-inventory-backed-placement-and-pickup-rules.md"
  - "docs/wiki/hud-assets-manifest.md"
---

# [P4-UI-002] Provide a reusable HUD/UI control framework (design + vertical slice).

> **Status note:** Proposed predecessor to [P4-UX-001 (#48)](p4-ux-001-specify-production-ui-flows-for-inventory-crafting-settings-.md).
> A GitHub issue has not yet been filed; `github_issue_status: proposed`. File the issue and record its
> URL here before claiming, per [`../../project/00-backlog-workflow.md`](../../project/00-backlog-workflow.md).

## Open knowledge summary

P4-UX-001 specifies production UI flows but explicitly defers the UI-stack decision and depends on a
reusable control layer that does not yet exist. This ticket provides that layer: one design-token
system, pixel-perfect integer scaling, explicit UI layers, a single focus/input model, and a clean
inventory view-model / command boundary — plus a minimal, tested vertical slice — without replacing the
existing HUD. Full architecture: [`../flexible-hud-framework.md`](../flexible-hud-framework.md).

## User story

As a developer building the P4 screens, I want reusable, themed, controller-navigable UI primitives and
a clean domain boundary, so each screen composes controls instead of re-inventing layout, scaling,
focus, and interaction — and gameplay rules never leak into UI components.

## Requirements

### Functional requirements

1. **Design tokens:** a `UiTheme` asset centralises spacing, sizes, timings, and semantic
   color/sprite roles, with a built-in fallback for every role.
2. **Foundation:** `HudRoot` with ordered UI layers, integer pixel-perfect `UiScaleController` (0 =
   auto, user preference clamped), `ModalStack`, `UiFocusController`, and a shared `TooltipService`.
3. **Primitives:** framed panel, button, label, scroll view, tooltip target, and modal dialog — each
   themed, with disabled/hover/pressed/focused/selected states and `Selectable`-based navigation.
4. **Inventory boundary:** `IInventoryViewModel` (per-slot `SlotChanged`) and `IInventoryCommandHandler`
   with a configurable, pooled `InventoryGridView`; a single-slot model change refreshes only that slot.
5. **Sandbox bridge (domain side):** an adapter diffing the coarse `SandboxInventory.Changed` into
   per-slot events and a command handler routing move/split/swap/clear through the inventory API.
6. **Vertical slice:** a demo assembling window + grid + tooltip + scale control + modal.

### Non-functional requirements

1. Presentation only: UI reads via view-models and writes via command handlers; no inventory/stacking
   rules in view components (mirrors P4-UX-001 NFR-1).
2. Event-driven; no per-frame inventory polling or per-frame hierarchy rebuilds (NFR-2).
3. Pixel-perfect: integer canvas scale, sprite bounds not pivots, no blurred borders at supported scales.
4. `ProjectTwelve.UI` must not reference the Sandbox domain (adapters live in `ProjectTwelve.Runtime`).
5. Text is theme/data-driven; long strings wrap/ellipsize rather than silently overflow.

## Acceptance criteria

- One shared design-token system; global scaling stays integer (no blurred borders at scale steps).
- Panels remain visible after resolution changes; inventory grids build with different dimensions
  without code duplication; a single-slot update refreshes only the affected slot.
- Mouse, keyboard, and controller navigation go through one focus model; modals block gameplay input
  and trap focus; tooltips/context menus/drag previews appear on the correct layers.
- Licensed assets are not added to the public repo; new assets keep `.meta` files.
- Existing HUD behavior remains functional (unchanged).
- EditMode tests cover scale math, grid mapping/navigation, single-slot refresh, drag rejection/restore,
  modal trap + input block, theme fallback, and the Sandbox adapter/command handler.

## Detailed technical specifications

### Scope

- Framework assembly `ProjectTwelve.UI` (foundation, theme, primitives, inventory interfaces + views),
  the Sandbox bridge, a demo + editor builder, a placeholder theme, and EditMode tests.
- Out of scope: migrating the existing HUD or other screens; resizable/persisted windows; dropdown,
  context menu, tab strip, text input primitives; controller bindings; localization content.

### Inputs and dependencies

- P2-INV-001 inventory API; HUD asset contract (`../hud-assets-manifest.md`); UI-stack decision recorded
  in `../flexible-hud-framework.md` (extend uGUI).

### Verification plan

- Unity batch validation and EditMode tests (see `.claude/skills/unity-tests/SKILL.md`).
- Manual screenshot matrix at 1280×720, 1920×1080, 2560×1440, 2560×1080, 3840×2160, and a small window.
- Docs gates: markdown links, OKF lint, wiki lint; paid-assets guard before commit.

## Documentation impact

- New `docs/wiki/flexible-hud-framework.md` (this framework's contract).
- New canonical-source row in `docs/CANONICAL_SOURCES.md` pointing at the framework doc.

## Exit evidence checklist

- [ ] GitHub issue filed and its URL recorded in this ticket (currently proposed).
- [ ] Framework design doc merged and linked from CANONICAL_SOURCES.
- [ ] EditMode tests pass (or Unity-availability blocker recorded).
- [ ] Screenshot matrix attached.
- [ ] Follow-up tickets created for HUD migration, resizable windows, and remaining primitives.
