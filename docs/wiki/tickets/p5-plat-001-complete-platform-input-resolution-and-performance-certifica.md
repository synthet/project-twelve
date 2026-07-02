---
type: Task
id: P5-PLAT-001
title: "[P5-PLAT-001] Complete platform, input, resolution, and performance certification tasks."
description: Certification matrix — target platforms, input schemes, resolutions/aspect ratios, and performance budgets verified per cell with recorded evidence.
status: open
phase: "Phase P5 — Release candidate and launch"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/52"
github_issue_status: created
resource: wiki/tickets/p5-plat-001-complete-platform-input-resolution-and-performance-certifica.md
tags: [docs, wiki, ticket, platform, certification, p5]
timestamp: 2026-07-01T00:00:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/spec-driven-development-tasks.md"
  - "docs/wiki/13-tooling-testing.md"
  - "docs/wiki/quality-gates.md"
  - "docs/wiki/tickets/p4-perf-001-create-optimization-tasks-for-chunk-streaming-rendering-coll.md"
---

# [P5-PLAT-001] Complete platform, input, resolution, and performance certification tasks.

## Open knowledge summary

This ticket defines and executes the certification matrix for launch: which platforms ship
(Windows first; macOS/Linux decision recorded here), which input schemes are supported (keyboard/
mouse required; controller per the P4-UX-001 readiness decision), which resolutions/aspect ratios/
display modes must work, and re-running the P4-PERF-001 performance scenario suite on **min-spec
hardware** for the RC build. Every matrix cell gets a smoke-test run with recorded evidence; the
completed matrix is a P5-REL-001 release criterion.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** [synthet/project-twelve#52](https://github.com/synthet/project-twelve/issues/52)
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As the maintainer preparing the RC, I want a certification matrix with per-cell evidence so that
"works on my machine" is replaced by "verified on every shipped configuration" before the go/no-go
review.

## Requirements

### Functional requirements

1. **Platform matrix:** shipped platforms decided and recorded (default: Windows 10/11 x64 at
   launch; macOS/Linux either certified or explicitly deferred with rationale). Min and
   recommended hardware specs finalized (from P4-PERF-001's target-hardware definition).
2. **Input certification:** keyboard/mouse full pass (all P4-UX-001 flows + gameplay); key
   rebinding persistence verified; controller — if the P4-UX-001 navigation targets are wired to
   a scheme, certify it; otherwise record deferral and verify the game clearly communicates
   supported input.
3. **Display certification:** resolutions (1080p, 1440p, 4K), aspect ratios (16:9, 16:10,
   ultrawide), display modes (fullscreen, borderless, windowed + resize), DPI scaling, and
   multi-monitor primary selection; per cell: UI legible/anchored (P4-UX-001 layouts), world
   rendering correct, no cursor-capture issues.
4. **Performance certification:** the P4-PERF-001 scenario suite re-run on min-spec hardware with
   the RC build; budgets met or waived through the P5-REL-001 process; a 2+ hour soak on min-spec
   (memory growth bounded, no degradation).
5. **Build integrity per platform:** clean-machine install/run (no dev dependencies), save
   location correct (`persistentDataPath`), paid-asset guard confirms no licensed source leaks in
   the distributed build, uninstall leaves saves per documented policy.
6. Every cell records: build id, hardware, tester, result, evidence link; failures file issues
   with the P5-REL-001 severity.

### Non-functional requirements

1. The matrix is small enough to execute fully per RC (target ≤ 2 days single-maintainer);
   anything larger must be cut from shipped configurations rather than left uncertified.
2. The procedure is a repeatable runbook (re-executable for patches, reusing the P4-PERF-001
   scenario definitions).

## Acceptance criteria

- Target platforms meet minimum functionality and performance criteria — every matrix cell has a
  recorded pass or a filed issue with severity.
- Platform/input/display matrices are documented with decisions (shipped vs deferred) and
  rationale.
- Min-spec performance run: P4-PERF-001 scenarios green or formally waived; soak-test evidence
  attached.
- Clean-machine install verification recorded for each shipped platform.
- Certification runbook merged and re-executable.
- The GitHub issue and this markdown ticket link to each other.
- Exit evidence records the completed matrix, captures, and reviewer findings.

## Detailed technical specifications

### Scope

- Matrix definition, runbook, and one full certification execution on the RC candidate.
- Out of scope: console/mobile ports, storefront-specific requirements (Steam Deck verification
  etc. — recorded follow-ups), localization QA, accessibility audits.

### Inputs and dependencies

- P4-PERF-001 — scenario suite + budgets (blocking); min-spec hardware availability.
- P4-UX-001 — layout/aspect-ratio and input-scheme groundwork.
- P5-REL-001 — severity taxonomy for failures; consumes the completed matrix.
- `scripts/check_paid_assets.py` — build-content verification support.

### Verification plan

- Full matrix execution with per-cell evidence (screenshots/captures/profiles).
- Min-spec performance + soak captures.
- Runbook dry-run by a second person where available.

## Documentation impact

- New certification page (e.g. `docs/wiki/platform-certification.md`, OKF frontmatter) holding
  matrix + runbook; linked from `docs/wiki/quality-gates.md`.
- Min/recommended specs feed P5-DOC-001 (player docs) and storefront material (P5-LAUNCH-001).
- Update `docs/wiki/spec-driven-development-tasks.md` if task scope or sequencing changes.

## Exit evidence checklist

- [ ] GitHub issue URL is recorded in this ticket.
- [ ] GitHub issue links back to this markdown ticket.
- [ ] Matrices and shipped-platform decisions merged.
- [ ] Full certification executed with per-cell evidence.
- [ ] Min-spec performance and soak results attached.
- [ ] Follow-up tasks filed for deferred platforms/inputs and any failures.
