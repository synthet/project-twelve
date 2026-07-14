---
type: Task
id: P2-INV-001
title: "[P2-INV-001] Specify inventory-backed placement and pickup rules."
description: Item-consuming tile placement and drop-yielding tile breaking through the SetTile choke point, with stack, reach, and hotbar rules.
status: done
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

- [x] GitHub issue URL is recorded in this ticket.
- [x] GitHub issue links back to this markdown ticket (verified 2026-07-13).
- [ ] Inventory contract documented in `gameplay-systems.md` before implementation.
- [x] Consistency, stack, and failure-path pure EditMode cases pass (13 inventory invocations via
      Unity Mono against the compiled EditMode assembly on 2026-07-13).
- [x] Play-mode placement checklist executed with user-provided evidence on 2026-07-13.
- [x] Follow-up ownership already exists: crafting/tools in P4-CONTENT-001 (#44), production
      inventory UI in P4-UX-001 (#48), and authoritative drop ownership in P3-NET-001 (#40).

## Progress log

### 2026-07-13 — implementation pass

- Added the pure fixed-slot `SandboxInventory`, registry-backed stack validation, deterministic
  merge/overflow behavior, save DTOs, and `SandboxInventoryEditService` transaction rules under
  `Assets/Scripts/Sandbox/Inventory/`.
- Routed gameplay placement/breaking through `SandboxWorld.SetTile`; controller-bound validation
  now enforces a 6-tile reach, player-body exclusion, and 0.10-second cadence. Successful breaks
  create magnetized pickups with a 2.5-tile radius and 120-second lifetime; full inventories retain
  the uncollected remainder.
- Replaced HUD infinity markers with live counts, added the missing `core:grass` placeable item,
  and joined ordered inventory contents to the existing world save payload without breaking legacy
  saves that lack the new field.
- Verification so far: runtime and EditMode assemblies compile with zero errors via `dotnet build`;
  12 engine-independent focused inventory cases pass. Two Unity-native serialization/world cases
  are compiled but remain pending in-engine execution because the active Unity MCP connection is
  revoked and the open Editor locks canonical batch runs.
- During the pass, an external release workflow committed the initial implementation as `82e4530`
  and follow-up HUD/test coverage as `b927b8a` and `beb791a`; this agent did not stage or create
  those commits. The ticket therefore moved to `in_progress`. A canonical focused Unity command
  was attempted without `-quit`, but the locked project produced neither test XML nor a test log.
- Resume verification after concurrent registry renames found and fixed alias ingress so retired
  ore IDs canonicalize during slot writes, pickup merges, counts, and legacy save loading instead
  of splitting stacks. Pickup radius, collect distance, and movement speed now scale from tile
  units through `SandboxWorld.TileSize`. The current runtime and EditMode assemblies compile with
  zero errors; 13 pure inventory invocations plus the live-hotbar state case pass against the
  compiled EditMode assembly. Unity-native save integration and the Play-mode checklist remain
  pending because the Unity MCP connection is revoked, the open Editor locks a second batch run,
  and no runtime MCP endpoint is listening on port 8765.
- User-run Unity Test Runner evidence on 2026-07-13 confirms all 15
  `SandboxInventoryTests` pass in-engine, including exact inventory serialization and registered
  world save/load integration. The same full-suite screenshot exposed six non-inventory failures:
  a stale HUD prefab field, three locale-sensitive save-pose JSON fixtures, and two audio tests
  coupled to scene-global listeners. Those fixtures and isolation seams were corrected; a fresh
  full-suite rerun is still pending. The inventory Play-mode checklist remains outstanding.
- User-provided Play Mode evidence shows the live ten-slot HUD with the migrated bricks icon,
  finite quantities changed from the 100-item prototype loadout (`114` dirt and `95` grass), a
  visible pickup near the player, and a successful save log. This covers the visual
  dig/pickup/place counter flow. Out-of-range no-op behavior and pickup-radius boundaries are not
  independently visible in the screenshot and remain pending explicit confirmation.
- Final user-run Unity EditMode evidence is fully green: `408/408` tests passed with zero failures,
  including all 15 inventory tests, 29 HUD tests, 15 save/load tests, and the audio isolation
  coverage. The repair used `JsonUtility`-generated save fixtures, serialized-field migration for
  `copperOreIcon` → `bricksIcon`, and deterministic listener arrays with guaranteed cleanup.
- The user confirmed the remaining Play Mode checklist complete. Final evidence shows placement
  across multiple registered tile types, finite hotbar counts, save/load restoration in the
  console, and the requested out-of-range no-op check. All acceptance behavior is verified; ticket
  status was moved to `done` on the user's explicit completion instruction. GitHub issue #36
  remains open for the eventual completing PR to close with `Closes #36`.
- The licensed Ground-material follow-up fills the full hotbar with Dirt, Grass, Stone, Bricks A–D,
  Frozen, Magma, and Sand. Registry-backed item definitions, finite starting stacks, HUD names/icons,
  stable save IDs, and offline visualizer mappings cover all ten materials. The original inventory
  acceptance contract remains complete; expanded-material Play Mode evidence is tracked with the
  visual follow-up rather than reopening this ticket.
