---
type: Task
id: P1-VISUAL-002
title: "[P1-VISUAL-002] Add visual subsystem EditMode tests."
description: Add EditMode tests for character sheet layout and autotile resolver invariants without licensed art fixtures.
status: open
phase: "Phase P1 — Prototype vertical slice"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/75"
github_issue_status: created
resource: wiki/tickets/p1-visual-002-add-visual-subsystem-editmode-tests.md
tags: [docs, wiki, ticket, visual, testing, p1]
timestamp: 2026-06-30T12:00:00Z
okf_version: 0.1
spec_references:
  - "docs/VISUAL_BEHAVIOR_SPEC.md"
  - "docs/wiki/quality-gates.md"
  - "docs/wiki/visual-integration.md"
---

# [P1-VISUAL-002] Add visual subsystem EditMode tests.

## Open knowledge summary

This ticket adds automated EditMode coverage for deterministic visual invariants: character sprite sheet layout and clip key naming, autotile resolver behavior, and optionally pure layer merge-order logic — all without loading licensed art assets in tests.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** Pending creation via `scripts/sync_wiki_tickets_to_github.py`.
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a developer working on the P1 milestone, I want EditMode tests for visual subsystem invariants so that autotile and character sheet contracts regress safely without licensed PNG fixtures.

## Requirements

### Functional requirements

1. EditMode tests for `CharacterSheetLayout`: clip row order, frame count per row, and library key format (`Idle_0` … `Idle_8`).
2. EditMode tests for `AutotileResolver`: weighted hash selection and horizontal flip matching (extend existing autotile tests if present).
3. Optional: pure merge-order test for `CharacterComposer.BuildLayers()` layer sequence without loading textures.

### Non-functional requirements

1. Tests must not require `Assets/_Licensed` or vendor art in the public repo.
2. Tests run in batch mode: `Unity -batchmode -quit -projectPath . -runTests -testPlatform EditMode`.

## Acceptance criteria

- `CharacterSheetLayout` clip ordering and key naming are covered by at least one EditMode test.
- `AutotileResolver` deterministic pick and flip behavior have EditMode coverage.
- All new tests pass in EditMode batch run.
- The GitHub issue and this markdown ticket link to each other.

## Detailed technical specifications

### Scope

- Test code under `Assets/Tests/EditMode/` only.
- No changes to licensed assets or catalog ScriptableObjects required for tests.

### Inputs and dependencies

- `Assets/Scripts/Visual/Characters/CharacterSheetLayout.cs`
- `Assets/Scripts/Visual/Tiles/AutotileResolver.cs`
- Existing tests in `Assets/Tests/EditMode/` (autotile/sandbox core).

### Verification plan

```bash
Unity -batchmode -quit -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile Logs/unity-editmode-tests.log
```

## Documentation impact

- Update `docs/wiki/quality-gates.md` if new test files are added to the required EditMode set.

## Exit evidence checklist

- [ ] GitHub issue URL is recorded in this ticket.
- [ ] GitHub issue links back to this markdown ticket.
- [ ] EditMode test output attached or linked.
- [ ] No licensed art referenced in test fixtures.
