---
type: Task
id: P2-GEN-001
title: "[P2-GEN-001] Specify biome, cave, structure, and ore generation passes."
description: Fixed-order deterministic generation passes with per-pass sub-seeds, chunk-addressable output, and golden-seed verification.
status: open
phase: "Phase P2 — Core systems alpha"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/35"
github_issue_status: created
resource: wiki/tickets/p2-gen-001-specify-biome-cave-structure-and-ore-generation-passes.md
tags: [docs, wiki, ticket, generation, biomes, p2]
timestamp: 2026-07-01T00:00:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/spec-driven-development-tasks.md"
  - "docs/wiki/07-procedural-generation.md"
  - "docs/wiki/generation-and-saving.md"
  - "docs/wiki/tickets/p2-visual-004-specify-biome-tileset-expansion.md"
---

# [P2-GEN-001] Specify biome, cave, structure, and ore generation passes.

## Open knowledge summary

This ticket extends the P1 single-pass terrain generator
(`Assets/Scripts/Sandbox/SandboxTerrainGenerator.cs`) into the fixed multi-pass pipeline from
`docs/wiki/07-procedural-generation.md`: surface heightmap → layer fill → caves → biomes → ores →
structures → validation. It specifies pass ordering, per-pass sub-seed derivation, the
chunk-addressability rule (on-demand chunk generation must equal whole-world generation), and the
conflict-resolution rules that keep later passes deterministic. Biome→tileset visual mapping is
owned by P2-VISUAL-004 and consumes the biome assignment specified here.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** [synthet/project-twelve#35](https://github.com/synthet/project-twelve/issues/35)
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a world-systems developer on the P2 milestone, I want the generation passes specified with
explicit ordering, seeding, and conflict rules so that richer worlds (biomes, caves, ores,
structures) remain bit-for-bit reproducible from a seed — which diff-only saves (P2-SAVE-001) and
server/client agreement (P3-NET-002) both depend on.

## Requirements

### Functional requirements

1. Passes run in the fixed order from `docs/wiki/07-procedural-generation.md`: 1 surface
   heightmap, 2 layer fill, 3 caves, 4 biomes, 5 ores, 6 structures/features, 7 validation. Later
   passes read earlier results; no pass reads a later pass's output.
2. Seeding: a single world seed; each pass/feature derives an independent sub-seed as
   `hash(worldSeed, passId, chunkCoord)` (or column/region coordinate where the pass is not
   chunk-shaped). No pass shares PRNG state with another.
3. Chunk-addressability: generating any chunk on demand yields tiles identical to generating the
   whole world. Noise/PRNG are seeded from coordinates, never from iteration order.
4. Cross-chunk features (cave worms, structures) have a single deterministic **owning anchor**
   (e.g. the chunk containing the feature origin); neighbors reproduce the overlapping portion by
   re-deriving from the owner's seed, never by double-placement.
5. Biome assignment is a pure function `biome(worldSeed, region)` exposed to consumers; it selects
   surface/fill tile IDs per biome (tile-ID/tileset mapping per P2-VISUAL-004).
6. Caves combine at least two methods (thresholded noise for caverns plus worm tunnels per the
   spec page); parameters (thresholds, densities, depth bands) are named constants in the spec,
   not inline literals.
7. Ores are placed per type with depth band, vein count/size parameters, only replacing stone-class
   tiles; conflict rule: ores never overwrite carved air, structures overwrite terrain but not
   each other (first-placed wins within the validation pass's overlap check).
8. Validation pass guarantees: spawn point on solid ground and not inside liquid/rock; structure
   overlap check; documented reachability sanity probe (may be deferred with an explicit non-goal
   note if pathfinding lands later in P2-AI-001).

### Non-functional requirements

1. Generation stays pure with respect to the seed: no `Time`, frame order, unordered-container
   iteration, or platform-dependent float behavior in tile-deciding code paths.
2. Per-chunk generation cost stays within the interactive streaming budget (no visible hitch when
   crossing chunk borders in play mode; profile against the `docs/wiki/quality-gates.md` targets).
3. Pass parameters are grouped into a serializable settings object so future worlds can version
   their generation settings alongside the seed (save header, P2-SAVE-001).

## Acceptance criteria

- Pass order, seed usage, and conflict resolution are documented in
  `docs/wiki/generation-and-saving.md` (or `07-procedural-generation.md`) before implementation.
- Golden-seed EditMode tests: for at least two representative seeds, generated fixture chunks
  (surface, underground with caves, ore-bearing depth, biome transition) match stored golden data
  exactly.
- EditMode test: on-demand chunk generation equals whole-world generation for the same seed
  (chunk-addressability).
- EditMode test: adjacent chunks generated in either order produce identical border columns
  (no order dependence).
- Validation-pass test: generated spawn point is solid-grounded and air-bodied for the golden seeds.
- The GitHub issue and this markdown ticket link to each other.
- Exit evidence records the commit, verification commands, and reviewer findings.

## Detailed technical specifications

### Scope

- Specification and implementation of passes 3–7 on top of the existing surface/layer generation;
  refactor of pass 1–2 into the pass pipeline shape is included.
- Out of scope: biome visual/tileset mapping (P2-VISUAL-004), liquid simulation of generated lakes
  (P2-FLUID-001 simulates; this ticket only places initial fluid), registry-driven tile defs
  (P2-DATA-001; use current tile IDs if the registry has not landed, with a noted follow-up).

### Inputs and dependencies

- `Assets/Scripts/Sandbox/SandboxTerrainGenerator.cs` — current generator to be decomposed into passes.
- `Assets/Scripts/Sandbox/SandboxWorld.cs` — on-demand chunk generation entry point.
- Existing deterministic fixtures: `Assets/Tests/EditMode/TerrainFixtureExportTests.cs` export path
  for golden data.
- Cross-tickets: P2-VISUAL-004 (biome→tileset), P2-DATA-001 (tile registry), P2-SAVE-001
  (settings in save header).

### Pass/sub-seed table (to be finalized in the spec page)

| Pass | Unit | Sub-seed input | Output |
|------|------|----------------|--------|
| 1 Surface | column x | hash(seed, 1, x) | surface height per column |
| 2 Layer fill | chunk | none (pure function of height) | dirt/stone bands |
| 3 Caves | chunk + worm anchors | hash(seed, 3, chunkCoord) | carved air |
| 4 Biomes | region | hash(seed, 4, regionIndex) | biome id per region |
| 5 Ores | chunk × ore type | hash(seed, 5, chunkCoord, oreId) | ore veins in stone |
| 6 Structures | owning chunk | hash(seed, 6, anchorChunk) | placed templates |
| 7 Validation | world/spawn area | derived | spawn fixes, overlap rejects |

### Verification plan

- Golden-seed comparison EditMode tests for representative seeds (store fixtures via the existing
  terrain fixture export path).
- Chunk-addressability and border-consistency EditMode tests.
- Play-mode: pinned-seed traversal across a biome boundary and into a cave system; profiler check
  that chunk generation stays hitch-free per `docs/wiki/quality-gates.md`.

## Documentation impact

- `docs/wiki/generation-and-saving.md` — pass pipeline, sub-seed table, conflict rules.
- `docs/wiki/07-procedural-generation.md` — mark decisions as adopted where the spec firms up.
- P2-VISUAL-004 ticket — confirm the biome-assignment contract cross-link stays accurate.
- Update `docs/wiki/spec-driven-development-tasks.md` if task scope or sequencing changes.

## Exit evidence checklist

- [ ] GitHub issue URL is recorded in this ticket.
- [ ] GitHub issue links back to this markdown ticket.
- [ ] Pass pipeline spec merged before implementation.
- [ ] Golden-seed, chunk-addressability, and border-consistency tests pass.
- [ ] Spawn-safety validation test passes for golden seeds.
- [ ] Follow-up tasks created for deferred passes (e.g. reachability probe) and registry adoption.
