---
id: P1-EDIT-001
type: Feature Spec
title: "[P1-EDIT-001] Specify tile edit flow through a single `SetTile` choke point."
description: Specify and implement the single SetTile choke point that routes all sandbox tile edits through one place.
resource: wiki/tickets/p1-edit-001-specify-tile-edit-flow-through-a-single-settile-choke-point.md
tags: [wiki, ticket, P1, edit]
timestamp: 2026-06-28T00:00:00Z
okf_version: 0.1
status: open
phase: "Phase P1 — Prototype vertical slice"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/26"
github_issue_status: created
spec_references:
  - "docs/wiki/spec-driven-development-tasks.md"
  - "docs/wiki/world-and-chunk-data.md"
---

# [P1-EDIT-001] Specify tile edit flow through a single `SetTile` choke point.

## Open knowledge summary

This ticket captures the shared product, engineering, QA, and documentation knowledge needed to deliver `P1-EDIT-001`. It is intentionally self-contained so the GitHub issue, implementation branch, review notes, and wiki updates can all trace back to the same requirements.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** Pending creation. After the issue is created, replace `github_issue: null` in the front matter with the issue URL.
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a developer or reviewer working on the P1 milestone, I want to specify tile edit flow through a single `SetTile` choke point so that the project can advance through the spec-driven workflow with clear scope, objective validation, and durable documentation.

## Requirements

### Functional requirements

1. The implementation or documentation change must satisfy the backlog task: **Specify tile edit flow through a single `SetTile` choke point.**
2. The work must be traceable to the spec references listed in this ticket.
3. Any behavior, data contract, tool output, or workflow introduced by this task must be documented before the task is considered complete.
4. The task must preserve chunk-first and deterministic-system assumptions where the referenced subsystem depends on them.

### Non-functional requirements

1. The work must be small enough to review as a focused change set.
2. The change must avoid hidden coupling between unrelated systems.
3. Verification evidence must be reproducible by another contributor using documented steps.
4. Any unresolved risk must be recorded as a follow-up ticket or an explicit non-goal.

## Acceptance criteria

- Place/break updates tile data, dirty flags, render, collision, and neighboring border chunks.
- The related specification page is updated or explicitly confirmed to require no change.
- The GitHub issue and this markdown ticket link to each other.
- Exit evidence records the commit, verification commands, manual QA notes when applicable, and reviewer findings.

## Detailed technical specifications

### Scope

- Deliver the behavior, documentation, or planning artifact described by `P1-EDIT-001`.
- Keep implementation details aligned with `docs/wiki/world-and-chunk-data.md` and the cross-phase task template in `docs/wiki/spec-driven-development-tasks.md`.
- Prefer explicit data contracts, invariants, lifecycle rules, and edge cases over implicit conventions.

### Inputs and dependencies

- Primary backlog item: `P1-EDIT-001` from `docs/wiki/spec-driven-development-tasks.md`.
- Primary subsystem reference: `docs/wiki/world-and-chunk-data.md`.
- Project tracking target: `https://github.com/users/synthet/projects/2`.

### Implementation notes

- Start by reviewing the referenced wiki pages and updating the spec if the current behavior is ambiguous.
- Create or adjust automated tests before or alongside implementation when code behavior changes.
- Keep runtime code, editor tooling, and documentation changes separated when practical so reviewers can validate each layer.
- Record migration, save compatibility, networking, and performance impacts when the task touches those systems.

### Verification plan

- Unit tests for central edits and border edits.
- Run repository-level formatting or diff checks before closing the task.
- Attach screenshots, profiler captures, deterministic fixture output, or playtest notes when the verification method calls for them.

## Documentation impact

- Update `docs/wiki/spec-driven-development-tasks.md` if task scope, acceptance criteria, or sequencing changes.
- Update `docs/wiki/world-and-chunk-data.md` when the subsystem contract changes.
- Keep this ticket synchronized with the final GitHub issue URL and outcome.

## Exit evidence checklist

- [x] GitHub issue URL is recorded in this ticket.
- [ ] GitHub issue links back to this markdown ticket.
- [x] Spec references have been reviewed and updated if needed.
- [x] Acceptance criteria have been validated.
- [x] Verification evidence is attached or linked.
- [ ] Follow-up tasks are created for deferred scope, defects, or open risks.

## Exit evidence

- **Implementation:** Routed `SandboxWorld.SetTile` through a new pure static choke point
  `SandboxWorld.ApplyTileEdit` (`Assets/Scripts/Sandbox/SandboxWorld.cs`). The choke point writes
  tile data, raises the owning chunk's render/collider dirty and edit-tracking flags, and dirties
  loaded face-adjacent border neighbors, while `SetTile` keeps the Unity-facing chunk resolution and
  renderer creation. Place and break both flow through `SetTile` (break writes
  `SandboxTileIds.Air`).
- **Spec:** Added the "Tile Edit Choke Point (P1-EDIT-001)" section to
  `docs/wiki/world-and-chunk-data.md` documenting the edit flow, side effects, the deliberate
  `LoadFromPath` bypass, and out-of-bounds handling.
- **Verification:** EditMode tests in `Assets/Tests/EditMode/SandboxCoreTests.cs`
  (`SandboxWorld_Apply*Edit*`) cover central edits, border edits that dirty a loaded neighbor,
  breaking a tile to air, central edits leaving neighbors clean, and out-of-bounds edits changing
  nothing. Run with:
  `Unity -batchmode -quit -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile Logs/unity-editmode-tests.log`.
- **GitHub issue:** [#26](https://github.com/synthet/project-twelve/issues/26), recorded in the
  front matter. The backlink from the issue body to this ticket is not yet confirmed, so that box
  remains open.
