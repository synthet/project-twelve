---
type: Task
id: P4-PERF-001
title: "[P4-PERF-001] Create optimization tasks for chunk streaming, rendering, collision, lighting, and liquids."
description: Define per-system performance budgets on target hardware, measure against them, and spawn targeted optimization tasks with before/after profiler gates.
status: open
phase: "Phase P4 — Feature complete and beta"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/46"
github_issue_status: created
resource: wiki/tickets/p4-perf-001-create-optimization-tasks-for-chunk-streaming-rendering-coll.md
tags: [docs, wiki, ticket, performance, profiling, p4]
timestamp: 2026-07-01T00:00:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/spec-driven-development-tasks.md"
  - "docs/wiki/13-tooling-testing.md"
  - "docs/wiki/quality-gates.md"
---

# [P4-PERF-001] Create optimization tasks for chunk streaming, rendering, collision, lighting, and liquids.

## Open knowledge summary

This ticket is a measurement-and-planning task: define frame-level performance budgets for the
feature-complete game on documented target hardware, profile the five known cliffs from
`docs/wiki/13-tooling-testing.md` (chunk streaming, mesh rebuild, collider rebuild, lighting
relight, fluid iteration) under standardized stress scenarios, and convert every budget breach
into a **separate, targeted optimization ticket** with its own before/after profiler gate. The
per-chunk baseline targets already exist in `docs/wiki/quality-gates.md` (render < 1 ms, collider
< 2 ms, relight < 5 ms, fluid step < 3 ms, 0 steady-state GC alloc); this ticket extends them to
whole-frame and content-scale budgets and validates them empirically.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** [synthet/project-twelve#46](https://github.com/synthet/project-twelve/issues/46)
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a developer preparing for beta, I want budgets and measured baselines for every hot system so
that optimization work is driven by data against agreed targets — not by guessing — and each fix
is provably an improvement.

## Requirements

### Functional requirements

1. **Budget definition:** target hardware spec documented (min + recommended desktop); frame
   budget 16.6 ms at 60 FPS with a per-system allocation table (simulation, rendering, physics,
   lighting, fluids, streaming, headroom); content-scale assumptions stated (view distance in
   chunks, active entity count, active fluid cells).
2. **Stress scenarios (standardized, seeded, repeatable):** (a) high-speed traversal (streaming
   churn), (b) bulk edit / explosion (remesh + relight + collider storm), (c) large waterfall +
   drained lake (fluid worst case), (d) dense enemy population (AI + pathfinding), (e) long-idle
   settled world (steady-state cost must be ~0 for fluids/lighting; 0 GC alloc).
3. **Measurement:** run every scenario on target hardware with the Unity Profiler; record
   per-system timings, GC allocations, draw calls, and memory per chunk; store captures and a
   summary table as repo evidence.
4. **Task generation:** each budget breach produces a new optimization ticket
   (`P4-PERF-00N`) naming the system, the scenario, the measured number, the target, and a
   candidate approach (from the options catalogued in the wiki: mesh-rebuild batching, collider
   run-merge tuning, light-window capping, fluid budget shaping, streaming prefetch).
5. **Regression guard:** the scenario suite + measurement procedure is documented as a repeatable
   runbook so P4-QA-001 cycles and P5-PLAT-001 certification rerun it identically.

### Non-functional requirements

1. Scenarios are deterministic (pinned seeds/scripts) so before/after comparisons are valid.
2. Measurements come from builds (not editor-only) for frame-time numbers; editor profiling is
   acceptable for allocation attribution.
3. No optimization work lands inside this ticket itself — it produces the measured backlog
   (minimal-diff discipline; fixes go to their own tickets with their own gates).

## Acceptance criteria

- Performance budgets are defined for target hardware and content scale and recorded in
  `docs/wiki/quality-gates.md` (extending the existing per-chunk table).
- All five stress scenarios are scripted/documented and produce repeatable captures (two runs of
  the same scenario agree within a documented variance).
- A measurement summary table exists: scenario × system → measured vs budget, green/red.
- Every red cell has a corresponding filed optimization ticket with baseline capture attached.
- The idle-world scenario shows ~0 active-set cost and 0 steady-state GC allocation, or tickets
  exist for the violations.
- The GitHub issue and this markdown ticket link to each other.
- Exit evidence records the commit, captures, the summary table, and reviewer findings.

## Detailed technical specifications

### Scope

- Budget table, scenario scripts/runbook, measurement passes, filed optimization tickets.
- Out of scope: implementing optimizations (child tickets), mobile/console profiling
  (P5-PLAT-001), network bandwidth budgets (owned by P3-NET-003's report; may be folded into a
  child ticket if breached).

### Inputs and dependencies

- Feature-complete systems: P2 core set, P3 networking, P4-CONTENT-001 (enemy density scenario).
- `docs/wiki/quality-gates.md` — existing per-chunk targets to extend.
- P2-TOOL-001 overlays + `perf` MCP tool — instrumentation for scenario setup.

### Verification plan

- Repeatability check: duplicate runs agree within documented variance.
- Review: each filed child ticket contains scenario, numbers, target, and candidate approach.
- Runbook dry-run by someone other than the author.

## Documentation impact

- `docs/wiki/quality-gates.md` — extended budget table and scenario runbook link.
- `docs/wiki/13-tooling-testing.md` — profiling-targets section updated with scenario suite.
- New child tickets under `docs/wiki/tickets/` per breach.
- Update `docs/wiki/spec-driven-development-tasks.md` if task scope or sequencing changes.

## Exit evidence checklist

- [ ] GitHub issue URL is recorded in this ticket.
- [ ] GitHub issue links back to this markdown ticket.
- [ ] Budget table and target hardware documented.
- [ ] Five scenario captures + summary table attached.
- [ ] Optimization tickets filed for every breach.
- [ ] Runbook validated by a second person.
