---
type: Task
id: P2-LIGHT-001
title: "[P2-LIGHT-001] Specify lighting data layout and propagation rules."
description: Tile lightmap (0–15) with BFS flood-fill propagation, dirty-window relight, cross-chunk seeding, and sky-column sunlight.
status: open
phase: "Phase P2 — Core systems alpha"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/37"
github_issue_status: created
resource: wiki/tickets/p2-light-001-specify-lighting-data-layout-and-propagation-rules.md
tags: [docs, wiki, ticket, lighting, simulation, p2]
timestamp: 2026-07-01T00:00:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/spec-driven-development-tasks.md"
  - "docs/wiki/06-lighting.md"
  - "docs/wiki/simulation-systems.md"
  - "docs/wiki/02-data-models.md"
---

# [P2-LIGHT-001] Specify lighting data layout and propagation rules.

## Open knowledge summary

This ticket specifies the tile-based lighting system from `docs/wiki/06-lighting.md`: a per-tile
light value stored in the existing `SandboxTile.light` byte, BFS flood-fill propagation with
per-material attenuation, sky-fed sunlight columns, and — the load-bearing part — **dirty-window
relighting** so a tile edit never triggers a global recompute. Unity URP 2D lights are explicitly
not used; light is derived state recomputed locally, applied to chunk meshes at render time.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** [synthet/project-twelve#37](https://github.com/synthet/project-twelve/issues/37)
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a simulation developer on the P2 milestone, I want lighting data layout and propagation rules
specified with explicit attenuation, seeding, and dirty-region semantics so that caves darken,
torches glow, and tile edits relight only a bounded window — with tests proving the local update
equals a full relight.

## Requirements

### Functional requirements

1. Data layout: light lives in `SandboxTile.light` (byte), value range **0–15**; 15 = full
   sunlight/source strength, 0 = dark. Single channel for this ticket; colored (RGB) light is an
   explicit non-goal recorded for later.
2. Sources: sky feeds open-air surface tiles at sunlight strength (time-of-day scaling is a
   documented follow-up, initial spec uses constant full sun); emissive tiles contribute their
   `lightEmission` (registry property per P2-DATA-001, e.g. a future `core:torch`).
3. Propagation: BFS flood-fill over the 4-neighborhood; a cell updates only if the incoming value
   beats its current value; overlapping sources take the max.
4. Attenuation: −1 per step through air; opaque solid cost is a **tunable per-material constant
   from the tile registry** (default −3 per the architecture blueprint; not a magic number).
5. Light removal (tile placed, source removed) uses **clear-then-refill**: zero the affected
   window, seed the BFS from the window border values and any sources inside, re-propagate. The
   "only if brighter" rule alone must not be used for removal (stale-brightness pitfall in
   `06-lighting.md`).
6. Dirty window: an edit relights at most a `(2·maxRange+1)²` window (maxRange = 15). Cross-chunk
   windows read neighbor tiles via `SandboxWorld.GetTile`; chunks never read neighbor arrays
   directly (invariant from `02-data-models.md`).
7. A light source near a chunk border must light the neighboring chunk; relighting marks affected
   chunks render-dirty so their meshes re-tint (dirty-flag flow per `SandboxChunk`).
8. Rendering application: chunk mesh vertex colors sampled from tile light values (per
   `06-lighting.md` § Applying light to the screen); exact shader work may be split into a
   follow-up if mesh tinting suffices for alpha.

### Non-functional requirements

1. Light is derived state: never authored, never required in saves (recompute on load is the
   default; see P2-SAVE-001).
2. Relight cost for a single edit stays under the `docs/wiki/quality-gates.md` target (< 5 ms per
   dirty chunk) on representative content.
3. Propagation code is pure C# over tile data (no UnityEngine scene dependencies) so EditMode
   tests run fast and deterministically.
4. No per-frame recompute: light updates run only on edits, source changes, and chunk load.

## Acceptance criteria

- Light updates are chunk-bounded where possible and neighbor propagation is explicit in the spec
  and the implementation.
- EditMode falloff test: a strength-15 source in open air produces value `15−d` at Manhattan
  distance d; behind an opaque tile the value drops by the registry's solid cost.
- EditMode occlusion test: sealing a 1-tile skylight darkens the room below via clear-then-refill;
  reopening restores the previous values exactly.
- EditMode border test: a source 2 tiles from a chunk edge lights tiles in the neighbor chunk and
  marks both chunks render-dirty.
- EditMode equivalence test: dirty-window relight after a random edit sequence equals a
  from-scratch full relight of the same world (the canonical correctness oracle from
  `docs/wiki/quality-gates.md`).
- Play-mode: digging into a cave shows darkness; placing/removing an emissive tile updates a
  bounded region with no visible hitch.
- The GitHub issue and this markdown ticket link to each other.
- Exit evidence records the commit, verification commands, and reviewer findings.

## Detailed technical specifications

### Scope

- Lighting data contract, propagation/removal algorithms, dirty-window scheduling, sky seeding,
  and mesh vertex-color application for the prototype tile set.
- Out of scope: colored light, smooth per-corner gradient sampling (polish follow-up),
  time-of-day sun scaling (spec the hook, defer the implementation), light-influenced gameplay
  (spawn rules belong to P2-AI-001).

### Inputs and dependencies

- `Assets/Scripts/Sandbox/SandboxTile.cs` — `light` byte (already present, currently unused).
- `Assets/Scripts/Sandbox/SandboxWorld.cs` — `GetTile`/`SetTile` choke point; enqueue light
  updates on edit (per `docs/wiki/01-architecture.md` edit flow).
- `Assets/Scripts/Sandbox/SandboxChunkRenderer.cs` — mesh rebuild path that will consume light
  into vertex colors.
- P2-DATA-001 — `opaque` and `lightEmission` registry properties (if not yet landed, use a local
  opacity table keyed by tile ID and record the follow-up).

### Algorithm summary (normative sketch in 06-lighting.md)

| Operation | Procedure |
|-----------|-----------|
| Add source / brighten | BFS from source; update cell if new value > current; enqueue neighbors |
| Remove source / occlude | Zero window (radius ≤ 15) → seed BFS queue with window-border cells at current values + interior sources → propagate |
| Chunk load | Seed from sky columns + sources + loaded-neighbor border values; propagate |
| Sky seeding | For each column, cells above the first opaque tile get sky strength |

### Verification plan

- EditMode unit tests for edits, borders, and occlusion (falloff, clear-then-refill, cross-chunk,
  dirty-window ≡ full relight) — pure data tests, no scenes.
- Play-mode manual check per the acceptance criteria with the light-heatmap overlay from
  P2-TOOL-001 when available.
- Profiler capture of relight cost on a dense edit sequence against the < 5 ms/chunk target.

## Documentation impact

- `docs/wiki/simulation-systems.md` — lighting section updated from planning notes to the
  specified contract (range, attenuation, removal rule, window size).
- `docs/wiki/06-lighting.md` — mark decisions adopted; record the solid-cost default chosen.
- Update `docs/wiki/spec-driven-development-tasks.md` if task scope or sequencing changes.

## Exit evidence checklist

- [ ] GitHub issue URL is recorded in this ticket.
- [ ] GitHub issue links back to this markdown ticket.
- [ ] Lighting contract documented in `simulation-systems.md` before implementation.
- [ ] Falloff, occlusion, border, and dirty-window-equivalence EditMode tests pass.
- [ ] Play-mode cave/torch check recorded with capture.
- [ ] Follow-up tasks created for colored light, time-of-day, and shader polish.
