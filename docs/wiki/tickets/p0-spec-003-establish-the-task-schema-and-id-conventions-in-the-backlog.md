---
id: P0-SPEC-003
type: Feature Spec
title: "[P0-SPEC-003] Establish the task schema and ID conventions in the backlog."
description: Establish the canonical backlog task schema and phase-prefixed ID conventions, with sample first-sprint tasks, so every item traces from spec to task to issue to commit to verification.
resource: wiki/tickets/p0-spec-003-establish-the-task-schema-and-id-conventions-in-the-backlog.md
tags: [wiki, ticket, P0, spec, backlog, schema]
timestamp: 2026-06-28T00:00:00Z
okf_version: 0.1
status: in_progress
phase: "Phase P0 — Discovery and specification baseline"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/22"
github_issue_status: created
spec_references:
  - "docs/wiki/spec-driven-development-tasks.md"
  - "docs/wiki/00-overview.md"
---

# [P0-SPEC-003] Establish the task schema and ID conventions in the backlog.

## Open knowledge summary

This ticket captures the shared product, engineering, QA, and documentation knowledge needed to deliver `P0-SPEC-003`. It is intentionally self-contained so the GitHub issue, implementation branch, review notes, and wiki updates can all trace back to the same requirements.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** Pending creation. After the issue is created, replace `github_issue: null` in the front matter with the issue URL.
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a developer or reviewer working on the P0 milestone, I want to establish the task schema and ID conventions in the backlog so that the project can advance through the spec-driven workflow with clear scope, objective validation, and durable documentation.

## Requirements

### Functional requirements

1. The implementation or documentation change must satisfy the backlog task: **Establish the task schema and ID conventions in the backlog.**
2. The work must be traceable to the spec references listed in this ticket.
3. Any behavior, data contract, tool output, or workflow introduced by this task must be documented before the task is considered complete.
4. The task must preserve chunk-first and deterministic-system assumptions where the referenced subsystem depends on them.

### Non-functional requirements

1. The work must be small enough to review as a focused change set.
2. The change must avoid hidden coupling between unrelated systems.
3. Verification evidence must be reproducible by another contributor using documented steps.
4. Any unresolved risk must be recorded as a follow-up ticket or an explicit non-goal.

## Acceptance criteria

- New work can be traced from spec to task to commit to verification.
- The related specification page is updated or explicitly confirmed to require no change.
- The GitHub issue and this markdown ticket link to each other.
- Exit evidence records the commit, verification commands, manual QA notes when applicable, and reviewer findings.

## Detailed technical specifications

### Scope

- Deliver the behavior, documentation, or planning artifact described by `P0-SPEC-003`.
- Keep implementation details aligned with `docs/wiki/00-overview.md` and the cross-phase task template in `docs/wiki/spec-driven-development-tasks.md`.
- Prefer explicit data contracts, invariants, lifecycle rules, and edge cases over implicit conventions.

### Inputs and dependencies

- Primary backlog item: `P0-SPEC-003` from `docs/wiki/spec-driven-development-tasks.md`.
- Primary subsystem reference: `docs/wiki/00-overview.md`.
- Project tracking target: `https://github.com/users/synthet/projects/2`.

### Implementation notes

- Start by reviewing the referenced wiki pages and updating the spec if the current behavior is ambiguous.
- Create or adjust automated tests before or alongside implementation when code behavior changes.
- Keep runtime code, editor tooling, and documentation changes separated when practical so reviewers can validate each layer.
- Record migration, save compatibility, networking, and performance impacts when the task touches those systems.

### Verification plan

- Create sample tasks for the first prototype sprint.
- Run repository-level formatting or diff checks before closing the task.
- Attach screenshots, profiler captures, deterministic fixture output, or playtest notes when the verification method calls for them.

## Documentation impact

- Update `docs/wiki/spec-driven-development-tasks.md` if task scope, acceptance criteria, or sequencing changes.
- Update `docs/wiki/00-overview.md` when the subsystem contract changes.
- Keep this ticket synchronized with the final GitHub issue URL and outcome.

## Exit evidence checklist

- [x] GitHub issue URL is recorded in this ticket (#22).
- [x] GitHub issue links back to this markdown ticket (issue body links the ticket path).
- [x] Spec references have been reviewed and updated if needed (`spec-driven-development-tasks.md` now points at the canonical schema; `00-overview.md` needs no contract change).
- [x] Acceptance criteria have been validated (see Exit evidence below).
- [x] Verification evidence is attached or linked.
- [x] Follow-up tasks are created for deferred scope, defects, or open risks (backfilling OKF frontmatter on pre-baseline tickets is recorded as a non-goal below).

## Exit evidence

- **Deliverable:** Added [`docs/wiki/task-schema.md`](../task-schema.md), the single P0 reference
  for **how a backlog item is identified and structured**. It fixes the `P<phase>-<AREA>-<NNN>`
  **ID format** and its invariants (immutable, unique, non-recycled), an **area-code registry**
  mapped to owning specs, the required **ticket frontmatter and body schema**, the **status
  lifecycle**, and the end-to-end **traceability chain** (`spec → ticket → issue → branch →
  commit → PR → verification`). The acceptance criterion *"new work can be traced from spec to
  task to commit to verification"* is satisfied by the explicit chain table plus the join-key
  rules that keep it navigable.
- **Sample first-sprint tasks:** The page's *Sample tasks — first prototype sprint (P1)* section
  enumerates the seven P1 vertical-slice tickets (`P1-WORLD-001` … `P1-QA-001`) as worked
  examples, each with its spec reference, issue, acceptance criterion, and verification — fulfilling
  the verification plan's *"create sample tasks for the first prototype sprint."* No new tickets
  were invented; the existing P1 backlog is presented as the schema's reference instances.
- **Cross-links:** [`spec-driven-development-tasks.md`](../spec-driven-development-tasks.md) (ID
  field + template intro), [`spec-ownership.md`](../spec-ownership.md) (new registry row),
  [`README.md`](../README.md) (index row), and
  [`docs/project/00-backlog-workflow.md`](../../project/00-backlog-workflow.md) (Ticket format +
  Related docs) now point at the canonical schema so the convention has a single source of truth.
- **Spec references reviewed:** `docs/wiki/spec-driven-development-tasks.md` gained a pointer to the
  schema; `docs/wiki/00-overview.md` is a subsystem-principles page and needs no change for this
  process artifact.
- **Verification:** Documentation-only change. Validated with
  `python scripts/okf_lint.py --profile project --only wiki/task-schema.md` (new page passes the
  changed-docs OKF gate) and `python3 scripts/check_markdown_links.py` (all local links resolve).
- **Follow-up / non-goal:** Several pre-baseline tickets still emit `missing_*` OKF findings;
  backfilling their frontmatter is out of scope here and is noted as a non-goal rather than fixed
  in this focused change.
- **GitHub issue:** [#22](https://github.com/synthet/project-twelve/issues/22); the issue body
  already links back to this ticket.
