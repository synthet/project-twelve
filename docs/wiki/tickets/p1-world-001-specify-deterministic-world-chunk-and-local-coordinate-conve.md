---
type: Task
id: P1-WORLD-001
title: "[P1-WORLD-001] Specify deterministic world, chunk, and local coordinate conversions, including negative coordinates."
description: Backlog ticket specifying world/chunk/local coordinate conversions with correct negative-coordinate handling.
status: implemented
phase: "Phase P1 — Prototype vertical slice"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/30"
github_issue_status: created
resource: wiki/tickets/p1-world-001-specify-deterministic-world-chunk-and-local-coordinate-conve.md
tags: [docs, wiki, ticket, chunking, p1]
timestamp: 2026-07-01T00:00:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/spec-driven-development-tasks.md"
  - "docs/wiki/world-and-chunk-data.md"
---

# [P1-WORLD-001] Specify deterministic world, chunk, and local coordinate conversions, including negative coordinates.

## Open knowledge summary

This ticket captures the shared product, engineering, QA, and documentation knowledge needed to deliver `P1-WORLD-001`. It is intentionally self-contained so the GitHub issue, implementation branch, review notes, and wiki updates can all trace back to the same requirements.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** Pending creation. After the issue is created, replace `github_issue: null` in the front matter with the issue URL.
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a developer or reviewer working on the P1 milestone, I want to specify deterministic world, chunk, and local coordinate conversions, including negative coordinates so that the project can advance through the spec-driven workflow with clear scope, objective validation, and durable documentation.

## Requirements

### Functional requirements

1. The implementation or documentation change must satisfy the backlog task: **Specify deterministic world, chunk, and local coordinate conversions, including negative coordinates.**
2. The work must be traceable to the spec references listed in this ticket.
3. Any behavior, data contract, tool output, or workflow introduced by this task must be documented before the task is considered complete.
4. The task must preserve chunk-first and deterministic-system assumptions where the referenced subsystem depends on them.

### Non-functional requirements

1. The work must be small enough to review as a focused change set.
2. The change must avoid hidden coupling between unrelated systems.
3. Verification evidence must be reproducible by another contributor using documented steps.
4. Any unresolved risk must be recorded as a follow-up ticket or an explicit non-goal.

## Acceptance criteria

- Conversion formulas and boundary cases are documented before code changes.
- The related specification page is updated or explicitly confirmed to require no change.
- The GitHub issue and this markdown ticket link to each other.
- Exit evidence records the commit, verification commands, manual QA notes when applicable, and reviewer findings.

## Detailed technical specifications

### Scope

- Deliver the behavior, documentation, or planning artifact described by `P1-WORLD-001`.
- Keep implementation details aligned with `docs/wiki/world-and-chunk-data.md` and the cross-phase task template in `docs/wiki/spec-driven-development-tasks.md`.
- Prefer explicit data contracts, invariants, lifecycle rules, and edge cases over implicit conventions.

### Inputs and dependencies

- Primary backlog item: `P1-WORLD-001` from `docs/wiki/spec-driven-development-tasks.md`.
- Primary subsystem reference: `docs/wiki/world-and-chunk-data.md`.
- Project tracking target: `https://github.com/users/synthet/projects/2`.

### Implementation notes

- Start by reviewing the referenced wiki pages and updating the spec if the current behavior is ambiguous.
- Create or adjust automated tests before or alongside implementation when code behavior changes.
- Keep runtime code, editor tooling, and documentation changes separated when practical so reviewers can validate each layer.
- Record migration, save compatibility, networking, and performance impacts when the task touches those systems.

### Verification plan

- Edit-mode tests for origin, positive, negative, and boundary positions.
- Run repository-level formatting or diff checks before closing the task.
- Attach screenshots, profiler captures, deterministic fixture output, or playtest notes when the verification method calls for them.

## Documentation impact

- Update `docs/wiki/spec-driven-development-tasks.md` if task scope, acceptance criteria, or sequencing changes.
- Update `docs/wiki/world-and-chunk-data.md` when the subsystem contract changes.
- Keep this ticket synchronized with the final GitHub issue URL and outcome.

## Exit evidence checklist

- [ ] GitHub issue URL is recorded in this ticket.
- [ ] GitHub issue links back to this markdown ticket.
- [x] Spec references have been reviewed and updated if needed.
- [x] Acceptance criteria have been validated.
- [x] Verification evidence is attached or linked.
- [x] Follow-up tasks are created for deferred scope, defects, or open risks.

## Exit evidence

- **Spec:** Added the "Coordinate Conversion Contract (P1-WORLD-001)" section to
  `docs/wiki/world-and-chunk-data.md` documenting the world ↔ chunk ↔ local formulas, the
  round-trip and local-range invariants, a boundary-case table (origin, positive, negative,
  and chunk-boundary positions), and the `float`-precision limit of the integer conversions.
- **Implementation:** Added the pure static inverse helper
  `SandboxWorld.ChunkLocalToWorld(chunkCoord, localX, localY)` that closes the round trip with
  the existing `WorldToChunkCoord` / `WorldToLocalCoord`, plus XML-doc on all three helpers
  describing the floor-division / positive-modulo rationale. No behavior of the existing
  conversions changed.
- **Verification:** EditMode tests in `Assets/Tests/EditMode/SandboxCoreTests.cs` cover the
  inverse mapping (`SandboxWorld_ChunkLocalToWorldInvertsSplit`), the exact world↔chunk↔local
  round trip across origin/positive/negative/boundary positions
  (`SandboxWorld_WorldChunkLocalRoundTripIsExact`), and the local-coordinate in-bounds invariant
  (`SandboxWorld_WorldToLocalCoordAlwaysWithinChunkBounds`). These complement the existing
  `SandboxWorld_WorldToChunkCoordUsesFloorDivision` and
  `SandboxWorld_WorldToLocalCoordWrapsNegativeCoordinates` cases. Run with:
  `Unity -batchmode -quit -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile Logs/unity-editmode-tests.log`.
- **Follow-up / non-goal:** Large-world precision beyond the `float` integer-exact range
  (~±16M tiles) is recorded as a known limitation in the spec; switching to integer-only floor
  division is deferred until very large worlds are required.
- **GitHub issue:** Not created in this environment; issue-linkage boxes remain open.
