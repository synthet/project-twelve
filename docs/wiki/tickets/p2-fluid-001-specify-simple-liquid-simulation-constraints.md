---
type: Task
id: P2-FLUID-001
title: "[P2-FLUID-001] Specify simple liquid simulation constraints."
description: Cellular-automaton fluid with per-cell amount, mass conservation, active-set sleep/wake, per-tick budget, and save persistence rules.
status: in_progress
phase: "Phase P2 — Core systems alpha"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/34"
github_issue_status: created
resource: wiki/tickets/p2-fluid-001-specify-simple-liquid-simulation-constraints.md
tags: [docs, wiki, ticket, fluids, simulation, p2]
timestamp: 2026-07-04T00:00:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/spec-driven-development-tasks.md"
  - "docs/wiki/08-liquids.md"
  - "docs/wiki/simulation-systems.md"
  - "docs/wiki/02-data-models.md"
---

# [P2-FLUID-001] Specify simple liquid simulation constraints.

## Open knowledge summary

This ticket specifies the grid cellular-automaton liquid simulation from `docs/wiki/08-liquids.md`
over the existing `SandboxTile.fluid` field (0.0–1.0 fill amount): flow rules (down → sideways →
pressure-up), an **active set** so still water costs nothing, mass conservation as the primary
invariant, chunk wake/sleep integration, per-tick update budget, and what fluid state persists in
saves. Water is the only liquid in scope; lava and interactions are recorded follow-ups.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** [synthet/project-twelve#34](https://github.com/synthet/project-twelve/issues/34)
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a simulation developer on the P2 milestone, I want the liquid simulation's flow rules, budget,
and sleep/wake constraints specified up front so that water behaves predictably (falls, spreads,
equalizes), never leaks mass, and never becomes the frame-budget problem that naive all-cells
simulation causes.

## Requirements

### Functional requirements

1. Data: fluid amount is `SandboxTile.fluid ∈ [0.0, 1.0]` (already present). A cell is renderable
   fluid when `fluid > minVisibleFill`; solid tiles hold no fluid.
2. Rule order per tick, applied bottom-up over **active cells only** (per `08-liquids.md`):
   flow down (gravity) → equalize sideways (split when both neighbors open) → pressure-up (excess
   above `maxFill` pushes into the cell above, enabling U-tube equalization).
3. Active set: a cell sleeps when its net transfer for a tick is below `settleEpsilon`; tile
   edits, neighbor changes, and new sources wake cells (wired into the `SetTile` edit flow).
4. Determinism: fixed iteration order within a tick (bottom-up rows; alternate or randomize L/R
   scan per row **from a seeded PRNG** to avoid directional drift while staying reproducible).
5. Chunk lifecycle: fluids pause in unloaded/distant chunks; on load/resume, border cells re-wake
   so flow continues consistently across the seam.
6. Saves: `fluid` amounts persist for dirty chunks (unlike light, fluid is not cheaply
   recomputable); coordinate the field with P2-SAVE-001.
7. Named constants specified (not inline literals): `maxFill = 1.0`, `settleEpsilon`,
   `minVisibleFill`, `maxTransferPerTick` (flow rate), iterations per frame, and the per-tick
   active-cell budget.
8. Rendering contract (minimal): fill height proportional to `fluid` on an overlay above terrain;
   full visual polish is out of scope.

### Non-functional requirements

1. **Mass conservation:** total fluid across the loaded world changes only via explicit
   sources/sinks; per-tick numeric drift stays within a documented epsilon.
2. Still water costs ~zero: a settled lake performs no per-tick work (active set empty).
3. Per-tick simulation cost stays under the `docs/wiki/quality-gates.md` target (< 3 ms for the
   active-set step) on representative content; the budget cap defers excess cells to the next tick
   rather than blowing the frame.
4. Simulation core is pure C# over tile data (EditMode-testable without scenes) and chunk-local in
   its data access (neighbor reads via `SandboxWorld.GetTile`).

## Acceptance criteria

- Flow rate, update budget, chunk wake/sleep, and save format are defined in the spec page before
  implementation.
- EditMode conservation test: random terrain + random fluid drops, N ticks, total mass constant
  within epsilon.
- EditMode settling test: a poured column in a closed basin reaches a flat surface and the active
  set becomes empty (no infinite jitter).
- EditMode U-tube test: connected columns equalize to the same level via the pressure rule.
- EditMode determinism test: same initial state + seed ⇒ identical fluid field after N ticks.
- EditMode wake test: removing a tile under a settled pool wakes exactly the affected cells and
  flow resumes.
- Play-mode: digging a channel drains a generated lake convincingly with no frame hitches.
- The GitHub issue and this markdown ticket link to each other.
- Exit evidence records the commit, verification commands, and reviewer findings.

## Detailed technical specifications

### Scope

- Water-only pressure-model CA, active-set management, budget scheduling, save persistence of
  amounts, and a minimal fill-height render overlay.
- Out of scope: lava and fluid-type interactions (obsidian etc.), fluid-driven gameplay (drowning,
  buoyancy), swimming animation hooks, and polished fluid rendering.

### Inputs and dependencies

- `Assets/Scripts/Sandbox/SandboxTile.cs` — `fluid` field (present, unused).
- `Assets/Scripts/Sandbox/SandboxWorld.cs` — edit flow wake hook; chunk load/unload pause hook.
- Generated lakes from P2-GEN-001 as initial fluid placement.
- P2-SAVE-001 — persistence of fluid amounts for dirty chunks.
- P2-TOOL-001 — fluid amount + active-set overlay for debugging.

### Tick procedure (normative sketch in 08-liquids.md)

```text
for each active cell c, bottom-up (row scan order from seeded PRNG):
    if solid(c): continue
    move min-capacity flow c → below; wake changed cells
    equalize c with left/right toward equal fill
    if c.fluid > maxFill: move excess c → above (pressure)
    keep c awake iff it changed by ≥ settleEpsilon, else sleep
```

### Verification plan

- Deterministic EditMode fixtures: conservation, settling, U-tube, determinism, wake — pure data
  tests over small handcrafted terrains.
- Performance check: profiler capture of the active-set step against the < 3 ms target while
  draining a large lake; confirm settled state costs ~0.
- Play-mode manual check per acceptance criteria with the P2-TOOL-001 overlay when available.

## Documentation impact

- `docs/wiki/simulation-systems.md` — liquids section updated to the specified constants and rules.
- `docs/wiki/08-liquids.md` — mark the pressure-amount model as adopted; record chosen constants.
- P2-SAVE-001 ticket — fluid persistence cross-reference.
- Update `docs/wiki/spec-driven-development-tasks.md` if task scope or sequencing changes.

## Exit evidence checklist

- [x] GitHub issue URL is recorded in this ticket.
- [x] GitHub issue links back to this markdown ticket.
- [x] Fluid constants and rules documented in `simulation-systems.md` and `08-liquids.md`
      (§ "P2-FLUID-001 specification") before implementation (2026-07-04).
- [x] Implementation landed under `Assets/Scripts/Sandbox/Fluid/` (`SandboxFluidSimulator`,
      `SandboxFluidConstants`, `ISandboxFluidGrid`, `SandboxWorldFluidGrid`, `SandboxFluidController`)
      with the `SetTile` wake hook (`SandboxWorld.TileFluidWakeRequested`) and fluid read/write on
      `SandboxWorld`/`SandboxChunk` (2026-07-04).
- [~] Conservation, settling, U-tube, determinism, and wake EditMode tests authored in
      `Assets/Tests/EditMode/SandboxFluidSimulatorTests.cs` (with `FluidTestGrid`). The Unity
      Editor is unavailable in this headless environment, so the official EditMode run is still
      required before merge:
      `Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile Logs/unity-editmode-tests.log`.
      As interim evidence the simulator was ported line-for-line to Python and the same acceptance
      properties executed offline (2026-07-05): **20/20 checks pass** — mass conservation
      (|Δ| ≈ 3.6e-15 over 300 ticks), settling to a flat surface + empty active set (100 ticks,
      spread 0.005), still-water zero-cost, U-tube equalization (both shafts top row 4), bitwise
      determinism, edit-driven wake, unloaded-chunk containment, and the per-tick budget. The
      offline run **found and fixed a bug in the merged `Wake_...` fixture**: its drop space was
      open, so drained water correctly spread across the floor (~0.2/cell) instead of resting at
      `(2,0)=1.0` — the assertion would have failed in Unity. Fixed by walling the drop space into
      a one-wide well (`BuildWalledColumnOnShelf`). Algorithm logic is verified; only the in-engine
      run and single-precision float behaviour remain unconfirmed.
- [ ] Profiler evidence for budget and sleeping-lake cost attached. *(Requires the Unity Editor /
      Play Mode; see acceptance criteria.)*
- [ ] Follow-up tasks created for lava/interactions and fluid rendering polish. *(Recorded as
      out-of-scope follow-ups in `08-liquids.md`; the fluid render overlay and P2-SAVE-001 fluid
      persistence coupling are the tracked next steps — deferred to maintainer triage.)*
