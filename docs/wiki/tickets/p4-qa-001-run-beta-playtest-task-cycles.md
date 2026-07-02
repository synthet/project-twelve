---
type: Task
id: P4-QA-001
title: "[P4-QA-001] Run beta playtest task cycles."
description: Repeatable playtest cycle protocol — session design, feedback capture, and triage into spec changes, bugs, tuning, or deferred scope.
status: open
phase: "Phase P4 — Feature complete and beta"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/47"
github_issue_status: created
resource: wiki/tickets/p4-qa-001-run-beta-playtest-task-cycles.md
tags: [docs, wiki, ticket, qa, playtest, p4]
timestamp: 2026-07-01T00:00:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/spec-driven-development-tasks.md"
  - "docs/wiki/13-tooling-testing.md"
  - "docs/wiki/quality-gates.md"
  - "docs/project/00-backlog-workflow.md"
---

# [P4-QA-001] Run beta playtest task cycles.

## Open knowledge summary

This ticket defines and runs the beta playtest **cycle protocol**: a repeatable loop of build →
scripted-and-free-play sessions → structured feedback capture → triage into exactly one of four
buckets (spec change, bug, tuning, deferred scope) → backlog items → next build. The deliverable
is both the protocol document and evidence of at least two completed cycles feeding the backlog
per `docs/project/00-backlog-workflow.md`. The protocol exists so feedback becomes tracked work
instead of anecdotes — the "promote repeated defects into new spec requirements" governance rule
from `docs/wiki/spec-driven-development-tasks.md`.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** [synthet/project-twelve#47](https://github.com/synthet/project-twelve/issues/47)
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As the maintainer running the beta, I want a playtest protocol that reliably converts play
sessions into triaged backlog items so that every cycle measurably improves the game and no
recurring complaint gets lost or re-reported untracked.

## Requirements

### Functional requirements

1. **Cycle definition:** each cycle = pinned build (tagged commit + build artifact), session
   plan, 2+ playtesters (at least one non-developer per cycle where possible), capture, triage,
   backlog update, and a cycle report — cadence documented (e.g. bi-weekly during beta).
2. **Session design:** every cycle includes (a) a scripted scenario pass reusing the P1-QA-001
   runbook extended with P2–P4 features (craft chain, combat, save/reload, two-player LAN), and
   (b) unscripted free play with a stated focus theme (e.g. "early progression pacing").
3. **Capture format:** structured per-session record — build id, tester, platform, session
   length, task-completion outcomes, friction moments (timestamped), bugs with repro attempts,
   and subjective ratings on a small fixed scale for the focus theme; debug tooling
   (P2-TOOL-001 console, seed pinning) documented as capture aids.
4. **Triage taxonomy (every item lands in exactly one):** **spec change** (design intent wrong →
   ticket editing the wiki spec), **bug** (behavior violates existing spec → bug ticket with
   repro), **tuning** (numbers, not rules → data change per P4-CONTENT-001 workflow), or
   **deferred** (recorded with reason and revisit trigger). Recurring items (≥2 cycles) escalate:
   defect → spec requirement per the governance rule.
5. **Traceability:** every triaged item becomes a wiki ticket / GitHub issue linked to the cycle
   report; cycle reports live under `docs/wiki/` (or `docs/project/`) with OKF frontmatter and
   link to the sessions they summarize.
6. **Exit measurement:** each cycle report states whether the previous cycle's top-3 issues were
   resolved, verifying the loop actually closes.

### Non-functional requirements

1. The protocol is executable by one maintainer + volunteer testers (no QA-team assumptions).
2. A full cycle's overhead (setup, capture, triage, reporting) stays within a documented budget
   (target: ≤ 1 day of maintainer time per cycle).
3. Personal data in reports is limited to what testers consent to (name/handle optional).

## Acceptance criteria

- Feedback is triaged into spec changes, bugs, tuning, or deferred scope — every captured item
  from every session lands in exactly one bucket with a tracked artifact or recorded deferral.
- The protocol document exists (session plan template, capture template, triage rules, report
  template) before the first cycle runs.
- At least two full cycles are completed: two builds, session records, triage tables, and cycle
  reports linked to filed backlog items.
- Cycle 2's report evaluates cycle 1's top-3 issues (loop closure).
- Recurring-issue escalation demonstrated or explicitly noted as not-yet-triggered.
- The GitHub issue and this markdown ticket link to each other.
- Exit evidence records the cycle reports, filed items, and reviewer findings.

## Detailed technical specifications

### Scope

- Protocol authoring, templates, and the first two executed cycles with reports.
- Out of scope: public/open beta distribution, telemetry/analytics instrumentation (recorded
  follow-up if manual capture proves insufficient), balance decisions themselves (P4-CONTENT-001
  data owns tuning; cycles produce the inputs).

### Inputs and dependencies

- Feature-complete build including P4-CONTENT-001 loops and P3-NET-003 LAN (scripted scenario
  coverage).
- P1-QA-001 runbook — base for the scripted pass.
- P2-TOOL-001 — seed pinning, console, and state dumps as capture aids.
- `docs/project/00-backlog-workflow.md` — where triaged items land.

### Verification plan

- Review: protocol document completeness (all four templates present).
- Evidence: two cycle reports with session records and linked backlog items.
- Spot-check: random sampled feedback items trace to a bucket artifact.

## Documentation impact

- New protocol page (e.g. `docs/wiki/beta-playtest-protocol.md`, type `Runbook`, OKF frontmatter).
- Cycle reports added per cycle with links into the backlog.
- `docs/wiki/13-tooling-testing.md` — reference the protocol from the testing section.
- Update `docs/wiki/spec-driven-development-tasks.md` if task scope or sequencing changes.

## Exit evidence checklist

- [ ] GitHub issue URL is recorded in this ticket.
- [ ] GitHub issue links back to this markdown ticket.
- [ ] Protocol document with all templates merged before cycle 1.
- [ ] Two cycle reports with triage tables and linked items attached.
- [ ] Loop-closure evaluation present in cycle 2's report.
- [ ] Follow-up tasks created for telemetry and open-beta distribution if warranted.
