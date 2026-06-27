---
id: P1-WORLD-002
title: "[P1-WORLD-002] Implement chunk lifecycle tasks for load, visibility, dirty flags, and unload."
status: implemented
phase: "Phase P1 — Prototype vertical slice"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: null
github_issue_status: pending-creation
spec_references:
  - "docs/wiki/spec-driven-development-tasks.md"
  - "docs/wiki/world-and-chunk-data.md"
---

# [P1-WORLD-002] Implement chunk lifecycle tasks for load, visibility, dirty flags, and unload.

## Open knowledge summary

This ticket captures the shared product, engineering, QA, and documentation knowledge needed to deliver `P1-WORLD-002`. It is intentionally self-contained so the GitHub issue, implementation branch, review notes, and wiki updates can all trace back to the same requirements.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** Pending creation. After the issue is created, replace `github_issue: null` in the front matter with the issue URL.
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a developer or reviewer working on the P1 milestone, I want to implement chunk lifecycle tasks for load, visibility, dirty flags, and unload so that the project can advance through the spec-driven workflow with clear scope, objective validation, and durable documentation.

## Requirements

### Functional requirements

1. The implementation or documentation change must satisfy the backlog task: **Implement chunk lifecycle tasks for load, visibility, dirty flags, and unload.**
2. The work must be traceable to the spec references listed in this ticket.
3. Any behavior, data contract, tool output, or workflow introduced by this task must be documented before the task is considered complete.
4. The task must preserve chunk-first and deterministic-system assumptions where the referenced subsystem depends on them.

### Non-functional requirements

1. The work must be small enough to review as a focused change set.
2. The change must avoid hidden coupling between unrelated systems.
3. Verification evidence must be reproducible by another contributor using documented steps.
4. Any unresolved risk must be recorded as a follow-up ticket or an explicit non-goal.

## Acceptance criteria

- Chunks load around the player, mark render/collision dirtiness independently, and unload outside range.
- The related specification page is updated or explicitly confirmed to require no change.
- The GitHub issue and this markdown ticket link to each other.
- Exit evidence records the commit, verification commands, manual QA notes when applicable, and reviewer findings.

## Detailed technical specifications

### Scope

- Deliver the behavior, documentation, or planning artifact described by `P1-WORLD-002`.
- Keep implementation details aligned with `docs/wiki/world-and-chunk-data.md` and the cross-phase task template in `docs/wiki/spec-driven-development-tasks.md`.
- Prefer explicit data contracts, invariants, lifecycle rules, and edge cases over implicit conventions.

### Inputs and dependencies

- Primary backlog item: `P1-WORLD-002` from `docs/wiki/spec-driven-development-tasks.md`.
- Primary subsystem reference: `docs/wiki/world-and-chunk-data.md`.
- Project tracking target: `https://github.com/users/synthet/projects/2`.

### Implementation notes

- Start by reviewing the referenced wiki pages and updating the spec if the current behavior is ambiguous.
- Create or adjust automated tests before or alongside implementation when code behavior changes.
- Keep runtime code, editor tooling, and documentation changes separated when practical so reviewers can validate each layer.
- Record migration, save compatibility, networking, and performance impacts when the task touches those systems.

### Verification plan

- Play-mode traversal plus dirty-flag unit tests.
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
- [ ] Follow-up tasks are created for deferred scope, defects, or open risks.

## Exit evidence

- **Implementation:** Extracted the chunk lifecycle selection policy into two pure helpers in
  `Assets/Scripts/Sandbox/SandboxWorld.cs`: `GetChunksInLoadRange` (the square load window centered
  on the player's chunk) and `GetRenderersToUnload` (loaded coordinates beyond the padded unload
  window). `RefreshLoadedChunks` and `UnloadDistantRenderers` now drive load/unload from those
  helpers, keeping streaming bounded to a predictable neighborhood with hysteresis at the edge. Newly
  loaded chunks request both render and collider rebuilds, and the two dirty flags remain independent.
- **Spec:** Added the "Chunk Lifecycle Contract (P1-WORLD-002)" section to
  `docs/wiki/world-and-chunk-data.md` documenting the load window, padded unload window with
  hysteresis, and the independent render/collider dirty-flag rules.
- **Verification:** `SandboxWorld_LoadRange*`, `SandboxWorld_Unload*`, and
  `SandboxChunk_*DirtyFlags*` / `SandboxChunk_NewlyLoadedChunk*` EditMode tests in
  `Assets/Tests/EditMode/SandboxCoreTests.cs` cover the square load window, zero/negative radius,
  padded unload selection, edge hysteresis, the empty-loaded case, newly loaded rebuild requests,
  and independent dirty-flag toggling. Run with:
  `Unity -batchmode -quit -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile Logs/unity-editmode-tests.log`.
- **GitHub issue:** Not created in this environment; issue-linkage boxes remain open.
