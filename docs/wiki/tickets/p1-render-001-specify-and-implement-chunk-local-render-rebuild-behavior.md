---
id: P1-RENDER-001
type: Feature Spec
title: "[P1-RENDER-001] Specify and implement chunk-local render rebuild behavior."
description: Specify and implement chunk-local render rebuilds bounded to dirty, visible chunks so edits never touch unrelated or offscreen chunks.
resource: wiki/tickets/p1-render-001-specify-and-implement-chunk-local-render-rebuild-behavior.md
tags: [wiki, ticket, P1, render]
timestamp: 2026-06-28T00:00:00Z
okf_version: 0.1
status: done
phase: "Phase P1 — Prototype vertical slice"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/29"
github_issue_status: closed
spec_references:
  - "docs/wiki/spec-driven-development-tasks.md"
  - "docs/wiki/rendering-and-collision.md"
---

# [P1-RENDER-001] Specify and implement chunk-local render rebuild behavior.

## Open knowledge summary

This ticket captures the shared product, engineering, QA, and documentation knowledge needed to deliver `P1-RENDER-001`. It is intentionally self-contained so the GitHub issue, implementation branch, review notes, and wiki updates can all trace back to the same requirements.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** Pending creation. After the issue is created, replace `github_issue: null` in the front matter with the issue URL.
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a developer or reviewer working on the P1 milestone, I want to specify and implement chunk-local render rebuild behavior so that the project can advance through the spec-driven workflow with clear scope, objective validation, and durable documentation.

## Requirements

### Functional requirements

1. The implementation or documentation change must satisfy the backlog task: **Specify and implement chunk-local render rebuild behavior.**
2. The work must be traceable to the spec references listed in this ticket.
3. Any behavior, data contract, tool output, or workflow introduced by this task must be documented before the task is considered complete.
4. The task must preserve chunk-first and deterministic-system assumptions where the referenced subsystem depends on them.

### Non-functional requirements

1. The work must be small enough to review as a focused change set.
2. The change must avoid hidden coupling between unrelated systems.
3. Verification evidence must be reproducible by another contributor using documented steps.
4. Any unresolved risk must be recorded as a follow-up ticket or an explicit non-goal.

## Acceptance criteria

- Rebuilds are bounded to dirty visible chunks and do not touch unrelated chunks.
- The related specification page is updated or explicitly confirmed to require no change.
- The GitHub issue and this markdown ticket link to each other.
- Exit evidence records the commit, verification commands, manual QA notes when applicable, and reviewer findings.

## Detailed technical specifications

### Scope

- Deliver the behavior, documentation, or planning artifact described by `P1-RENDER-001`.
- Keep implementation details aligned with `docs/wiki/rendering-and-collision.md` and the cross-phase task template in `docs/wiki/spec-driven-development-tasks.md`.
- Prefer explicit data contracts, invariants, lifecycle rules, and edge cases over implicit conventions.

### Inputs and dependencies

- Primary backlog item: `P1-RENDER-001` from `docs/wiki/spec-driven-development-tasks.md`.
- Primary subsystem reference: `docs/wiki/rendering-and-collision.md`.
- Project tracking target: `https://github.com/users/synthet/projects/2`.

### Implementation notes

- Start by reviewing the referenced wiki pages and updating the spec if the current behavior is ambiguous.
- Create or adjust automated tests before or alongside implementation when code behavior changes.
- Keep runtime code, editor tooling, and documentation changes separated when practical so reviewers can validate each layer.
- Record migration, save compatibility, networking, and performance impacts when the task touches those systems.

### Verification plan

- Render rebuild tests and profiler sample during tile edits.
- Run repository-level formatting or diff checks before closing the task.
- Attach screenshots, profiler captures, deterministic fixture output, or playtest notes when the verification method calls for them.

## Documentation impact

- Update `docs/wiki/spec-driven-development-tasks.md` if task scope, acceptance criteria, or sequencing changes.
- Update `docs/wiki/rendering-and-collision.md` when the subsystem contract changes.
- Keep this ticket synchronized with the final GitHub issue URL and outcome.

## Exit evidence checklist

- [ ] GitHub issue URL is recorded in this ticket.
- [ ] GitHub issue links back to this markdown ticket.
- [x] Spec references have been reviewed and updated if needed.
- [x] Acceptance criteria have been validated.
- [x] Verification evidence is attached or linked.
- [ ] Follow-up tasks are created for deferred scope, defects, or open risks.

## Exit evidence

- **Implementation:** Extracted the rebuild selection policy into the pure
  `SandboxWorld.GetChunksNeedingRebuild` helper (`Assets/Scripts/Sandbox/SandboxWorld.cs`).
  `RebuildDirtyChunks` now drives rebuilds from that selection, so only visible chunks whose
  own render/collider dirty flags are set are rebuilt; unrelated clean chunks and loaded-but-not-visible
  chunks are never touched.
- **Spec:** Added the "Chunk-Local Render Rebuild Contract (P1-RENDER-001)" section to
  `docs/wiki/rendering-and-collision.md` documenting the selection inputs, rules, and invariants.
- **Verification:** `SandboxWorld_RebuildSelection*` EditMode tests in
  `Assets/Tests/EditMode/SandboxCoreTests.cs` cover visible-dirty, collider-only-dirty,
  visible-clean, dirty-but-offscreen, missing-chunk, and mixed-set selection. Run with:
  `Unity -batchmode -quit -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile Logs/unity-editmode-tests.log`.
- **GitHub issue:** Not created in this environment; issue-linkage boxes remain open.
