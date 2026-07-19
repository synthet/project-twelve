---
type: Concept
title: Generation And Saving
description: Documentation for Generation And Saving.
resource: docs/wiki/generation-and-saving.md
tags: [docs, wiki]
timestamp: 2026-07-19T01:28:50Z
okf_version: 0.1
---

# Generation and Saving

## Generation Pipeline

The prototype currently generates terrain height from noise, then fills grass, dirt, stone, or air. The full generator should become pass-based:

1. Surface height.
2. Terrain layers.
3. Cave carving.
4. Biome assignment.
5. Ore and resource placement.
6. Structures and points of interest.
7. Validation and spawn safety.

## Deterministic Generation Contract (P1-GEN-001)

Generation is owned by the pure `SandboxTerrainGenerator` struct (`Assets/Scripts/Sandbox/SandboxTerrainGenerator.cs`). `SandboxWorld` builds the generator from its serialized settings and delegates chunk creation to it, so generation has no hidden dependency on Unity lifecycle, scene state, or chunk load order.

**Inputs** (the complete determinism key):

- `seed`
- `surfaceHeight` — base surface row.
- `terrainAmplitude` — vertical noise range.
- `terrainFrequency` — horizontal noise scale.
- `dirtDepth` — dirt band thickness below the surface tile.
- The target chunk coordinate.

**Outputs:** A fully populated `SandboxChunk` whose tiles are a pure function of those inputs.

- Per column, `GetSurfaceHeight(worldX)` derives height from `Mathf.PerlinNoise` seeded by `worldX + seed`.
- Per tile, rows above the surface are `Air`, the surface row is `Grass`, the `dirtDepth` rows below are `Dirt`, and deeper rows are `Stone`.
- At-or-above-surface tiles seed light `15`; underground tiles seed light `4`.

**Invariant:** Identical inputs produce byte-identical tile ids and light across runs. Distinct seeds are expected to diverge. Both properties are covered by the golden-seed tests in `Assets/Tests/EditMode/SandboxCoreTests.cs`. Because untouched chunks are reproducible from the seed, only edited chunks need to be persisted (see Saving Strategy below).

## Saving Strategy

Save chunks independently with a format version, seed, generation settings, dirty chunk data, entity state, inventories, time of day, and global events. Untouched chunks can be regenerated from the seed; edited chunks need persisted diffs or complete chunk payloads.
