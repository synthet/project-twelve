---
type: Task
id: P4-UX-001
title: "[P4-UX-001] Specify production UI flows for inventory, crafting, settings, and multiplayer."
description: Screen inventory and interaction flows for inventory, crafting, settings, and multiplayer with keyboard/mouse plus controller-ready navigation.
status: claimed
phase: "Phase P4 — Feature complete and beta"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/48"
github_issue_status: created
resource: wiki/tickets/p4-ux-001-specify-production-ui-flows-for-inventory-crafting-settings-.md
tags: [docs, wiki, ticket, ux, ui, p4]
timestamp: 2026-07-01T00:00:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/spec-driven-development-tasks.md"
  - "docs/wiki/gameplay-systems.md"
  - "docs/wiki/tickets/p2-inv-001-specify-inventory-backed-placement-and-pickup-rules.md"
  - "docs/wiki/tickets/p4-content-001-specify-crafting-progression-combat-and-loot-loops.md"
---

# [P4-UX-001] Specify production UI flows for inventory, crafting, settings, and multiplayer.

## Open knowledge summary

This ticket specifies the production UI layer over systems that already have data contracts:
inventory grid + hotbar (P2-INV-001 slots), crafting list with station context (P4-CONTENT-001
recipes), settings (audio/video/controls/gameplay), and multiplayer host/join (P3-NET-003 flow).
It defines the screen inventory, per-screen interaction flows, an input matrix for keyboard/mouse
with **controller-ready navigation targets** (every interactive element reachable by directional
focus — even though controller support itself ships later), and UI-state rules (pause behavior,
stacking, dismissal). UI reads and writes game state only through the existing system APIs —
presentation logic stays out of simulation code.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** [synthet/project-twelve#48](https://github.com/synthet/project-twelve/issues/48)
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a player in the beta, I want coherent, predictable UI for managing items, crafting, settings,
and joining a friend's world so that the systems built in P2–P4 are usable without developer
knowledge — and as a developer, I want the flows specified so UI work doesn't invent new
gameplay rules.

## Requirements

### Functional requirements

1. **Screen inventory (specified per screen: entry points, states, exits):** HUD (hotbar,
   health), Inventory (grid + drag/drop + hotbar assignment), Crafting (recipe list with
   craftable-now filtering, station-aware per P4-CONTENT-001), Settings (audio, video,
   rebindable controls, gameplay toggles), Multiplayer (host world / join by address, connection
   status, disconnect messaging per P3-NET-004), Pause menu, and death/respawn screen.
2. **Inventory interactions:** drag-and-drop between slots, stack splitting (documented gesture),
   quick-move (shift-click), hotbar binding; all mutations route through P2-INV-001 APIs —
   the UI never edits slot data directly.
3. **Crafting interactions:** recipe rows show inputs/outputs with have/need counts; craft
   button disabled with reason when uncraftable; station-gated recipes appear only in station
   context (or greyed with the station named — decision recorded).
4. **Input matrix:** every action mapped for keyboard/mouse; every interactive element has a
   directional-navigation target and focus state so a controller scheme can bind later without
   re-layout. Rebinding UI covers the gameplay action set.
5. **UI-state rules:** which screens pause single-player (and explicitly don't pause
   multiplayer), screen stacking/dismissal order (Esc semantics), and world-input suppression
   while a screen is open (no digging through the inventory screen).
6. **Feedback rules:** pickup/craft confirmations, denied-action feedback (out of range, full
   inventory), and connection-state changes all have specified, non-blocking presentation.

### Non-functional requirements

1. UI is a presentation layer: reads via system queries/events, writes via system APIs; no
   gameplay rules live in UI code.
2. UI updates are event-driven (no per-frame polling of inventories); opening any screen causes
   no perceptible hitch.
3. Layouts remain usable at 16:9, 16:10, and ultrawide at 1080p-class resolutions (full
   resolution certification is P5-PLAT-001).
4. Text is centralized for future localization (no hardcoded user-facing strings in components).

## Acceptance criteria

- UI supports keyboard/mouse and controller-ready navigation targets across all specified
  screens (focus-traversal review passes with no unreachable elements).
- UX review checklist executed per screen: entry/exit paths work, Esc semantics consistent,
  pause rules honored in single-player and multiplayer.
- Inventory flow: drag/drop, split, quick-move, and hotbar binding work and round-trip through
  P2-INV-001 (EditMode tests on any UI-side view-model logic).
- Crafting flow: craftable filtering matches registry state; denied crafts show the reason.
- Multiplayer flow: host → second client joins by address → disconnect shows the specified
  message (with P3-NET-003/004).
- Screenshot capture of every screen at the three aspect ratios attached as evidence.
- The GitHub issue and this markdown ticket link to each other.
- Exit evidence records the commit, verification commands, and reviewer findings.

## Detailed technical specifications

### Scope

- Screen inventory, flow specs, input matrix, focus/navigation contract, and implementation of
  the listed screens with placeholder-quality art.
- Out of scope: final visual design/art pass, controller input implementation (targets only),
  localization content (structure only), accessibility features beyond focus order (recorded
  follow-up per quality-gates non-goals), map/minimap UI.

### Inputs and dependencies

- P2-INV-001 (inventory API), P4-CONTENT-001 (recipes/stations), P3-NET-003/004 (host/join and
  disconnect states), P2-TOOL-001 console (debug entry points stay separate from player UI).
- Unity UI stack choice (uGUI vs UI Toolkit) — decision recorded in this ticket at
  implementation time with rationale.

### First framework vertical slice (2026-07-19)

The runtime technology decision is **uGUI**. It preserves the existing Canvas/prefab, point-sprite,
pixel-scale, and EditMode-test investment while providing built-in `Selectable` navigation. A
parallel UI Toolkit foundation would introduce a second focus/theme/render stack before it solved
a production flow. The detailed rationale and migration contract are recorded in
[Flexible HUD Framework](../flexible-hud-framework.md).

This claimed slice implements the shared root/layers, safe-area anchoring, theme tokens, reusable
controls, scale policy, one Input System `EventSystem`, a modal screen stack and gameplay-input
gate, targeted inventory view updates, tooltips, and a read-only 5x8 Backpack demonstration. The
keyboard mapping for this slice is `I` to toggle Backpack and `Esc` to dismiss the top screen;
mouse interaction and deterministic directional focus are supported by the same uGUI controls.

This is partial evidence for issue #48, not completion of the ticket. Production inventory
transactions (drag/drop, split, quick-move, hotbar binding), crafting, settings/rebinding,
multiplayer, pause/death flows, centralized localization, controller bindings, UX review, and the
three-aspect screenshot matrix remain open.

### Verification plan

- Per-screen UX review checklist + focus-traversal audit.
- EditMode tests for view-model logic (craftable filtering, slot view mapping).
- Screenshot matrix (screens × aspect ratios) as exit evidence.

## Documentation impact

- `docs/wiki/gameplay-systems.md` — UI/flows section added (screen inventory + state rules).
- P4-CONTENT-001 / P2-INV-001 tickets — cross-references for the APIs UI consumes.
- Update `docs/wiki/spec-driven-development-tasks.md` if task scope or sequencing changes.

## Exit evidence checklist

- [x] GitHub issue URL is recorded in this ticket.
- [x] GitHub issue links back to this markdown ticket.
- [ ] Screen inventory and flow specs documented before implementation.
- [ ] Focus-traversal audit passes (controller-ready).
- [ ] Screenshot matrix attached.
- [ ] Follow-up tasks created for controller bindings, localization, and accessibility.

### Vertical-slice verification evidence

- Runtime assembly source compilation: passed with 0 errors using the generated Unity project plus
  temporary compile includes for the newly added scripts.
- Targeted Unity EditMode execution: `SandboxUiFrameworkTests` passed 17/17. Coverage includes
  scale, clamp/resize math, grid navigation, targeted adapter diffs, modal input/focus state, drag
  rejection/cancel restoration, tooltip delay, and theme fallback. Existing `SandboxHudTests`
  parity coverage passed 29/29 after the prefab hierarchy and scaler changes.
- Repository documentation validation passed: 382 local Markdown files resolved, OKF lint reported
  0 errors/0 warnings, and wiki lint reported no missing or broken links.
