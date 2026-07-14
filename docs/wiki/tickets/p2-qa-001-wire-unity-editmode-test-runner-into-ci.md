---
type: Task
id: P2-QA-001
title: "[P2-QA-001] Wire a real Unity EditMode test runner into CI."
description: Run Unity EditMode tests against a real editor in CI so test/source drift and C#/JS parity regressions fail the build instead of hiding until a local run.
status: in_progress
phase: "Phase P2 — Core systems alpha"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/117"
github_issue_status: created
resource: wiki/tickets/p2-qa-001-wire-unity-editmode-test-runner-into-ci.md
tags: [docs, wiki, ticket, ci, testing, p2]
timestamp: 2026-07-13T00:00:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/quality-gates.md"
  - "docs/project/00-backlog-workflow.md"
---

# [P2-QA-001] Wire a real Unity EditMode test runner into CI.

## Open knowledge summary

The CI **EditMode tests** job does not execute a real Unity — it completes in ~7 seconds (a
stub/no-op). EditMode tests are therefore only ever run when a developer runs them locally, so
test↔source drift accumulates undetected. This ticket wires a real Unity EditMode run into CI so
failures block merge.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** [synthet/project-twelve#117](https://github.com/synthet/project-twelve/issues/117)
- **Backlink requirement:** The GitHub issue body links back to this markdown ticket.

## Motivation

Two EditMode tests were silently red on `master` and only surfaced during a manual local run
(fixed in PR #116):

- `GroundAutotileDebugModes.Normalize` — broke when the debug enum was renumbered (`8e3874f`)
  without updating the legacy switch/test.
- `AutotileVisualOverrideRenderTests.CoverOverride_UsesGrassASpriteDecision` — a transposed mask
  literal that never matched its rule.

The same gap meant P2-GEN-001's new EditMode tests (`SandboxHashTests`, `TerrainFixtureExportTests`)
could only be verified by a human running Unity locally. Without CI enforcement, the golden-fixture
export that guards C#↔JS terrain parity is never checked automatically.

## User story

As a maintainer, I want CI to run Unity EditMode tests against a real editor so that test/source
drift and C#/JS parity regressions fail the build automatically instead of hiding until someone
happens to run the suite locally.

## Requirements

### Functional requirements

1. CI runs EditMode tests on a real Unity (Unity 6.0.5.1f1) — e.g. GameCI
   `game-ci/unity-test-runner`, or a self-hosted runner with the editor installed.
2. Any EditMode test failure fails the CI check (non-zero exit, red status).
3. NUnit results are uploaded as an artifact and/or surfaced in the checks UI.
4. The golden-fixture export test (`TerrainFixtureExportTests`) runs so C#↔JS terrain parity
   (P2-GEN-001) is enforced in CI, and the offline `tools/world-viz` parity test can consume a
   CI-authored fixture.
5. Unity licensing is provided via a repository secret; no license file is committed.

### Non-functional requirements

1. Added CI wall-clock stays within a documented, reasonable budget (record the added minutes).
2. The runner is documented in `docs/wiki/quality-gates.md` (setup, secret name, how to reproduce
   locally).

## Acceptance criteria

- A deliberately-failing EditMode test in a scratch PR turns the CI check red (proves enforcement).
- EditMode NUnit results are visible from the CI run (artifact or checks annotation).
- `TerrainFixtureExportTests` executes in CI; the `world-viz` parity test passes against the
  CI-authored fixture.
- Licensing handled via secret, documented in `docs/wiki/quality-gates.md`.
- The GitHub issue and this markdown ticket link to each other.
- Exit evidence records the commit, verification commands, and reviewer findings.

## Scope

- **In scope:** EditMode test execution in CI, result reporting, licensing, docs.
- **Out of scope:** PlayMode / Runtime-MCP smoke tests (separate follow-up), batchmode validation
  builds beyond running tests, and any change to test contents.

## Exit evidence checklist

- [x] GitHub issue URL is recorded in this ticket.
- [x] GitHub issue links back to this markdown ticket.
- [ ] EditMode tests run on a real Unity in CI and fail the build on failure (scratch-PR proof).
      *2026-07-13: workflow wired (`.github/workflows/unit-tests.yml`) — missing license now
      **fails** same-repo runs instead of passing as a no-op; `push` trigger fixed from `main`
      to `master`. Blocked on the maintainer adding the `UNITY_LICENSE` (+`UNITY_EMAIL`/
      `UNITY_PASSWORD`) repository secret, then a scratch-PR red proof.*
- [ ] NUnit results surfaced from the CI run. *(Artifact upload + `githubToken` check
      annotation are wired; needs the license secret for a live run.)*
- [x] `docs/wiki/quality-gates.md` documents the runner and its license secret
      (§ "Unity EditMode tests in CI (GameCI)", incl. fixture-parity step and CI budget).
