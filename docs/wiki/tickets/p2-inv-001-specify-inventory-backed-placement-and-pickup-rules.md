---
type: Task
id: P2-INV-001
title: "[P2-INV-001] Specify inventory-backed placement and pickup rules."
description: Item-consuming tile placement and drop-yielding tile breaking through the SetTile choke point, with stack, reach, and hotbar rules.
status: claimed
phase: "Phase P2 — Core systems alpha"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/36"
github_issue_status: created
resource: wiki/tickets/p2-inv-001-specify-inventory-backed-placement-and-pickup-rules.md
tags: [docs, wiki, ticket, inventory, gameplay, p2]
timestamp: 2026-07-01T00:00:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/spec-driven-development-tasks.md"
  - "docs/wiki/gameplay-systems.md"
  - "docs/wiki/02-data-models.md"
  - "docs/wiki/12-modding.md"
---

# [P2-INV-001] Specify inventory-backed placement and pickup rules.

## Open knowledge summary

This ticket replaces the P1 free-editing mode (place/break without cost) with inventory-backed
editing: placing a tile consumes an item stack, breaking a tile yields its drop, and both flow
through the single `SandboxWorld.SetTile` choke point established by P1-EDIT-001. It specifies the
inventory data contract (slot-based, registry item IDs per P2-DATA-001), stack rules, reach
validation at the controller boundary, and the pickup lifecycle — placed so the same validation
can later run server-side (P3-NET-001).

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** [synthet/project-twelve#36](https://github.com/synthet/project-twelve/issues/36)
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a gameplay developer on the P2 milestone, I want placement and pickup rules specified with
explicit consume/restore semantics so that items and tiles stay consistent under any edit sequence
— nothing duplicates, nothing vanishes — and the validation seam is ready to become
server-authoritative in P3.

## Requirements

### Functional requirements

1. Inventory contract: fixed-size ordered slot array (hotbar = first row) as recommended by
   `docs/wiki/02-data-models.md`; each slot holds `(itemId, count)` with `count ≤ maxStack`
   (per-item registry property, default 999 for tile items).
2. Item identity is a registry string ID (P2-DATA-001); tile items reference the tile they place,
   tile defs reference the item they drop (both directions validated at registry load).
3. **Place:** validated request → if the active hotbar slot holds a placeable item with `count ≥ 1`
   and the target cell is air (and not occluded by the player's own body), call
   `SandboxWorld.SetTile`, then decrement the stack. If `SetTile` fails, the stack is not touched.
4. **Break:** validated request → if the target tile is breakable, call `SetTile(air)`; on success
   spawn the tile's drop as a pickup entity at the cell center.
5. **Pickup:** drop entities are magnetized to the player within `pickupRadius` and merge into the
   first matching non-full stack, then the first empty slot; when inventory is full the drop
   remains in the world. Drops despawn after a documented lifetime.
6. Validation placement: reach (`editRange` in tiles), target-cell rules, and rate limits live at
   the controller/request boundary (`SandboxPlayerController`), not inside `SetTile` — matching
   the `docs/wiki/gameplay-systems.md` split (authoritative mutation in world, request validation
   near input) so P3 can move the same checks server-side.
7. Consistency invariant: for any edit sequence, `tiles placed − tiles broken` equals
   `items consumed − items gained` per item type (no dupes, no losses), including failure paths
   (blocked placement, full inventory, out-of-range requests).
8. Named constants specified: `editRange`, `pickupRadius`, `dropLifetime`, default `maxStack`.

### Non-functional requirements

1. Inventory state is serializable and joins the save format (P2-SAVE-001 header already carries
   player position; extend with inventory).
2. Inventory logic is pure C# (no scene dependencies) for EditMode testing; pickup entity behavior
   is the only Play-mode-dependent piece.
3. UI in this ticket is minimal (hotbar selection + counts); production inventory UI belongs to
   P4-UX-001.

## Acceptance criteria

- Items are consumed and restored consistently by place/break actions across all failure paths
  (the consistency invariant above holds in tests).
- EditMode: place decrements exactly one item on success and zero on failure (occupied cell,
  empty slot, out of range).
- EditMode: break yields exactly the registry-defined drop; full-inventory break leaves the drop
  in the world without loss.
- EditMode: stack merge fills existing stacks before opening new slots and respects `maxStack`.
- Play-mode checklist: dig → auto-pickup → counter increments → place elsewhere → counter
  decrements; drops magnetize within radius; out-of-range clicks do nothing.
- Save/load round-trip preserves inventory contents exactly (with P2-SAVE-001).
- The GitHub issue and this markdown ticket link to each other.
- Exit evidence records the commit, verification commands, and reviewer findings.

## Detailed technical specifications

### Scope

- Inventory data model, place/break/pickup rules, hotbar selection, drop entity lifecycle, and
  wiring into the existing mouse-edit path of `SandboxPlayerController`.
- Out of scope: crafting and tool properties (P4-CONTENT-001 owns tool strength/cooldown/damage),
  production inventory UI (P4-UX-001), multiplayer ownership of drops (P3-NET-001).

### Inputs and dependencies

- `Assets/Scripts/Sandbox/SandboxPlayerController.cs` — current mouse tile-edit path to gate.
- `Assets/Scripts/Sandbox/SandboxWorld.cs` — `SetTile` choke point (P1-EDIT-001 contract).
- P2-DATA-001 — item registry and tile↔item cross-references (blocking dependency; if not landed,
  a temporary item table keyed by tile ID is acceptable with a recorded follow-up).
- P2-SAVE-001 — inventory persistence.

### Verification plan

- EditMode inventory tests: consume/restore consistency, stack merge/overflow, failure paths —
  table-driven over edit sequences.
- Play-mode placement checklist per acceptance criteria.
- Save/load round-trip test extension once P2-SAVE-001 lands.

## Documentation impact

- `docs/wiki/gameplay-systems.md` — inventory/items section updated from intent to contract
  (slots, stacks, constants, validation boundary).
- P2-DATA-001 / P2-SAVE-001 tickets — cross-references for item defs and persistence.
- Update `docs/wiki/spec-driven-development-tasks.md` if task scope or sequencing changes.

## Exit evidence checklist

- [ ] GitHub issue URL is recorded in this ticket.
- [ ] GitHub issue links back to this markdown ticket.
- [ ] Inventory contract documented in `gameplay-systems.md` before implementation.
- [ ] Consistency, stack, and failure-path EditMode tests pass.
- [ ] Play-mode placement checklist executed with notes.
- [ ] Follow-up tasks created for crafting/tools, inventory UI, and drop ownership in multiplayer.
