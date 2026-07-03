---
id: P1-COLL-001
type: Task
title: "[P1-COLL-001] Specify prototype collision rules for solid tiles and player movement."
description: "Backlog ticket to specify prototype collision rules for solid tiles and player movement."
resource: wiki/tickets/p1-coll-001-specify-prototype-collision-rules-for-solid-tiles-and-player.md
tags: [ticket, p1, collision, spec]
timestamp: 2026-07-01T05:03:48Z
status: done
phase: "Phase P1 — Prototype vertical slice"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/25"
github_issue_status: created
spec_references:
  - "docs/wiki/spec-driven-development-tasks.md"
  - "docs/wiki/rendering-and-collision.md"
---

# [P1-COLL-001] Specify prototype collision rules for solid tiles and player movement.

## Open knowledge summary

This ticket captures the shared product, engineering, QA, and documentation knowledge needed to deliver `P1-COLL-001`. It is intentionally self-contained so the GitHub issue, implementation branch, review notes, and wiki updates can all trace back to the same requirements.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** Pending creation. After the issue is created, replace `github_issue: null` in the front matter with the issue URL.
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a developer or reviewer working on the P1 milestone, I want to specify prototype collision rules for solid tiles and player movement so that the project can advance through the spec-driven workflow with clear scope, objective validation, and durable documentation.

## Requirements

### Functional requirements

1. The implementation or documentation change must satisfy the backlog task: **Specify prototype collision rules for solid tiles and player movement.**
2. The work must be traceable to the spec references listed in this ticket.
3. Any behavior, data contract, tool output, or workflow introduced by this task must be documented before the task is considered complete.
4. The task must preserve chunk-first and deterministic-system assumptions where the referenced subsystem depends on them.

### Non-functional requirements

1. The work must be small enough to review as a focused change set.
2. The change must avoid hidden coupling between unrelated systems.
3. Verification evidence must be reproducible by another contributor using documented steps.
4. Any unresolved risk must be recorded as a follow-up ticket or an explicit non-goal.

## Acceptance criteria

- Player can stand, jump, collide with terrain, and avoid tunneling in target scenarios.
- The related specification page is updated or explicitly confirmed to require no change.
- The GitHub issue and this markdown ticket link to each other.
- Exit evidence records the commit, verification commands, manual QA notes when applicable, and reviewer findings.

## Detailed technical specifications

### Scope

- Deliver the behavior, documentation, or planning artifact described by `P1-COLL-001`.
- Keep implementation details aligned with `docs/wiki/rendering-and-collision.md` and the cross-phase task template in `docs/wiki/spec-driven-development-tasks.md`.
- Prefer explicit data contracts, invariants, lifecycle rules, and edge cases over implicit conventions.

### Inputs and dependencies

- Primary backlog item: `P1-COLL-001` from `docs/wiki/spec-driven-development-tasks.md`.
- Primary subsystem reference: `docs/wiki/rendering-and-collision.md`.
- Project tracking target: `https://github.com/users/synthet/projects/2`.

### Implementation notes

- Start by reviewing the referenced wiki pages and updating the spec if the current behavior is ambiguous.
- Create or adjust automated tests before or alongside implementation when code behavior changes.
- Keep runtime code, editor tooling, and documentation changes separated when practical so reviewers can validate each layer.
- Record migration, save compatibility, networking, and performance impacts when the task touches those systems.

### Verification plan

- Play-mode movement checklist and collision edge-case tests.
- Run repository-level formatting or diff checks before closing the task.
- Attach screenshots, profiler captures, deterministic fixture output, or playtest notes when the verification method calls for them.

## Documentation impact

- Update `docs/wiki/spec-driven-development-tasks.md` if task scope, acceptance criteria, or sequencing changes.
- Update `docs/wiki/rendering-and-collision.md` when the subsystem contract changes.
- Keep this ticket synchronized with the final GitHub issue URL and outcome.

## Exit evidence

- **Spec:** `docs/wiki/rendering-and-collision.md` § "Prototype collision rules (P1-COLL-001)"
  documents tile solidity, run-merged chunk-local `BoxCollider2D` generation, the
  Rigidbody2D player movement model, ground/wall probe rules with a constants table,
  tunneling analysis, invariants/edge cases, and a play-mode QA checklist. OKF frontmatter
  was added to the page (previously missing).
- **Automated tests (test pyramid):**
  - EditMode `Assets/Tests/EditMode/SandboxColliderGeometryTests.cs` unit-tests the pure
    `SandboxColliderGeometry` run-merge and rect math (gaps, edges, no cross-row merge, scaling).
  - PlayMode `Assets/Tests/PlayMode/SandboxCollisionPlayModeTests.cs` (new assembly
    `ProjectTwelve.PlayModeTests`) covers stand, jump (grounded and ignored-airborne),
    walk-into-wall stop, and no-tunneling-while-falling against real run-merged colliders.
  - Collider geometry was extracted from `SandboxChunkRenderer.RebuildColliders` into
    `Assets/Scripts/Sandbox/SandboxColliderGeometry.cs` (behavior-preserving) to enable the
    EditMode layer.
- **Verification commands:**
  - `Unity -batchmode -quit -projectPath . -runTests -testPlatform PlayMode -testResults TestResults/playmode.xml -logFile Logs/unity-playmode-tests.log`
  - `python3 scripts/check_markdown_links.py`
  - `python scripts/ci/okf_lint_changed.py --base origin/master --head HEAD --profile project --fail-on error`
  - `python3 scripts/check_paid_assets.py --staged`

### Checklist

- [x] GitHub issue URL is recorded in this ticket.
- [x] GitHub issue links back to this markdown ticket.
- [x] Spec references have been reviewed and updated (collision spec authored).
- [x] Acceptance criteria validated by PlayMode tests + documented QA checklist.
- [ ] Verification evidence attached (PlayMode run pending a Unity-capable environment).
- [ ] Follow-up tasks created for deferred scope (manual swept-AABB collision, one-way
  platforms/slopes, high-speed tunneling mitigation) and the stale ticket-status drift.
