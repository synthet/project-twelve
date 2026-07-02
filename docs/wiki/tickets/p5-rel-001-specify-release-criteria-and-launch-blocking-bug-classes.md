---
type: Task
id: P5-REL-001
title: "[P5-REL-001] Specify release criteria and launch-blocking bug classes."
description: Objective ship/no-ship criteria tied to tested scenarios, plus a severity taxonomy defining which bug classes block launch.
status: open
phase: "Phase P5 — Release candidate and launch"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/53"
github_issue_status: created
resource: wiki/tickets/p5-rel-001-specify-release-criteria-and-launch-blocking-bug-classes.md
tags: [docs, wiki, ticket, release, quality, p5]
timestamp: 2026-07-01T00:00:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/spec-driven-development-tasks.md"
  - "docs/wiki/14-roadmap.md"
  - "docs/wiki/quality-gates.md"
---

# [P5-REL-001] Specify release criteria and launch-blocking bug classes.

## Open knowledge summary

This ticket makes the ship decision objective: a release-criteria checklist where every item is a
tested scenario or measurable gate (not a feeling), and a bug severity taxonomy that pre-decides
which classes block launch. For a persistent sandbox, the non-negotiable blockers are the trust
killers: **save data loss/corruption, crashes, progression softlocks, determinism breaks (world
differs across runs/machines for the same seed), and multiplayer desync**. The output is the
checklist the P5-LAUNCH-001 go/no-go review executes.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** [synthet/project-twelve#53](https://github.com/synthet/project-twelve/issues/53)
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As the maintainer approaching launch, I want ship/no-ship criteria and blocking bug classes
agreed before release pressure exists so that the final decision is a checklist execution, not a
judgment call made under deadline stress.

## Requirements

### Functional requirements

1. **Severity taxonomy:** S0 launch-blocking — save loss/corruption, crash-to-desktop or hang,
   progression softlock in the core loop, determinism break, multiplayer desync/dupe,
   paid-asset/licensing violation in the shipped build; S1 launch-blocking-if-frequent
   (documented frequency thresholds) — non-core softlocks, major perf regressions vs P4-PERF-001
   budgets, broken UI flows with workarounds; S2/S3 ship-with-known-issues — polish, minor
   glitches, balance gripes (documented in known issues per P5-DOC-001).
2. **Release criteria checklist**, each item bound to evidence: all quality gates green on the RC
   commit (EditMode/PlayMode suites, link/OKF/paid-asset checks per `quality-gates.md`); P4-PERF
   budgets met on target hardware (P5-PLAT-001 captures); zero open S0 and S1-over-threshold
   bugs; save migration suite green (P5-MIG-001); the P1-QA-001-derived full-game runbook passes
   on the RC build; two-player LAN checklist passes (P3-NET-003/004); docs complete
   (P5-DOC-001); launch assets ready (P5-LAUNCH-001 inputs).
3. **Scenario binding:** every S0 class has at least one named, repeatable test scenario that
   proves its absence (e.g. save-interrupt atomicity test for corruption; 4-hour soak for
   crash/leak; golden-seed cross-machine comparison for determinism).
4. **Process:** triage rules assigning severity at bug-filing time; an exception process
   requiring a written waiver with rationale for shipping any S1-over-threshold (S0 is
   unwaivable); the go/no-go review agenda template.

### Non-functional requirements

1. Criteria are executable by one maintainer in a bounded RC validation pass (target ≤ 2 days).
2. Every criterion states its evidence artifact — nothing is "confirmed verbally".
3. The taxonomy is stable post-agreement; changes require the same review as a spec change.

## Acceptance criteria

- Ship/no-ship decisions are objective and tied to tested scenarios — a dry-run review on the
  current build produces an unambiguous (expectedly "no-go") result with a concrete gap list.
- The severity taxonomy with S0–S3 definitions, frequency thresholds, and examples is merged into
  the release spec page.
- Every S0 class maps to at least one named verification scenario with a documented procedure.
- The go/no-go template and waiver process are documented.
- P4-QA-001 triage adopts the severity taxonomy (cross-link both directions).
- The GitHub issue and this markdown ticket link to each other.
- Exit evidence records the dry-run review output and reviewer findings.

## Detailed technical specifications

### Scope

- Taxonomy, criteria checklist, scenario bindings, review/waiver process, and one dry-run
  execution against a pre-RC build.
- Out of scope: executing the real RC validation (P5-LAUNCH-001), fixing the bugs found,
  storefront/marketing readiness items (P5-LAUNCH-001 owns the launch checklist).

### Inputs and dependencies

- `docs/wiki/quality-gates.md` — the automated-gate baseline the criteria reference.
- P4-PERF-001 budgets, P5-MIG-001 migration suite, P5-PLAT-001 certification matrix,
  P3-NET-003/004 checklists, P1-QA-001 runbook — evidence sources.

### Verification plan

- Dry-run go/no-go review on the current build; gap list filed as issues.
- Review that every criterion names its evidence artifact and every S0 names its scenario.

## Documentation impact

- New/updated release spec page (e.g. `docs/wiki/release-criteria.md`, OKF frontmatter) holding
  taxonomy + checklist; linked from `docs/wiki/14-roadmap.md`.
- P4-QA-001 protocol — adopt severity levels.
- Update `docs/wiki/spec-driven-development-tasks.md` if task scope or sequencing changes.

## Exit evidence checklist

- [ ] GitHub issue URL is recorded in this ticket.
- [ ] GitHub issue links back to this markdown ticket.
- [ ] Taxonomy and criteria checklist merged.
- [ ] S0 scenario bindings documented.
- [ ] Dry-run review executed with gap list filed.
- [ ] Follow-up tasks created for every dry-run gap.
