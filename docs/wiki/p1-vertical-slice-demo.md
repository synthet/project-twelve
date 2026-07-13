---
type: Runbook
title: P1 Vertical-Slice Demo Runbook
description: Reviewer-executable demo script validating movement, generation, rendering, collision, editing, autotile terrain, and the composed avatar in one scene.
resource: wiki/p1-vertical-slice-demo.md
tags: [docs, wiki, runbook, qa, p1]
timestamp: 2026-07-13T02:18:45Z
okf_version: 0.1
---

# P1 Vertical-Slice Demo Runbook

> **Status:** Active — this is the reviewer-executable checklist for closing the P1 milestone
> ([P1-QA-001](tickets/p1-qa-001-package-the-prototype-vertical-slice-demo-checklist.md),
> [issue #28](https://github.com/synthet/project-twelve/issues/28)).
> **Time budget:** under 30 minutes for the full pass, including evidence capture.
> **Prerequisite knowledge:** none — every step names the exact input and the observable outcome.

This runbook validates the whole P1 vertical slice (movement, chunk streaming, deterministic
generation, chunk-local render rebuilds, collision, tile editing, autotiled terrain, composed
avatar) in one play session of one scene. Subsystem behavior is specified elsewhere; this page only
orders the checks and defines the evidence. See [Quality Gates](quality-gates.md),
[Tooling, Testing & Profiling](13-tooling-testing.md), and
[Visual Integration](visual-integration.md) for the underlying contracts.

## Pinned demo configuration

| Item | Pinned value |
|------|--------------|
| Unity Editor | 6.0.5.1f1, opened at the repository root |
| Scene | `Assets/Scene.unity` |
| World seed | `1337` (serialized on the scene's `SandboxWorld`; do not change it) |
| Generation parameters | `surfaceHeight 28`, `terrainAmplitude 8`, `terrainFrequency 0.06`, `dirtDepth 8` (scene defaults) |
| Chunk constants | 32×32 tiles per chunk, tile size 1 world unit, load radius 3 chunks, unload padding 1 chunk |
| Player spawn | Authored scene position `(0.5, 40)`; the player falls onto the generated surface (y ≈ 28 ± 8) |
| Save shortcuts | **Do not press F5/F6 during the run** — F5 overwrites and F6 loads `sandbox-world.json`, which breaks the determinism steps |

### Required local setup (full pass)

1. `Assets/_Licensed` submodule initialized and catalogs imported — follow
   [Visual setup](../VISUAL_SETUP.md) (one-time machine setup, steps 1–4).
2. Scene references confirmed per that guide: `SandboxTileVisualCatalog` → `AutotileCatalog`,
   player `SandboxPlayerAvatarVisual` → `CharacterLayerCatalog` + character prefab.

### Degraded setup (code-only checkout)

Without `Assets/_Licensed`, the prototype still runs: terrain renders via the vertex-color
fallback and avatar/autotile systems log warnings. Execute the full runbook anyway and mark steps
6, 9, and 10 as **SKIPPED (no submodule)** — skipped, not failed. All simulation steps
(1–5, 7, 8) must still pass.

## Controls

| Input | Action |
|-------|--------|
| `A` / `D` or `←` / `→` | Move left / right |
| `Space` | Jump (grounded only) |
| Left mouse button | Break the tile under the cursor (within 6 units of the player) |
| Right mouse button | Place a Dirt tile under the cursor (within 6 units) |

## Evidence rules

Capture evidence per step as listed in the script table and attach it to the PR or issue that
cites this run (for the milestone pass: issue #28 / the P1-QA-001 ticket's exit evidence).
Prefix each artifact with the step number (e.g. `step-07-replay-x-minus-40.png`). Record once at
the top of your report: commit hash, Unity version, full vs. degraded setup, and OS.

## Demo script

Execute the steps in order in a single play session (steps 7–8 restart Play mode as instructed).
Every step is pass/fail; a skipped visual step in degraded setup is recorded as SKIPPED.

| # | Step | Pass condition | Evidence |
|---|------|----------------|----------|
| 1 | Open `Assets/Scene.unity`, confirm `SandboxWorld` seed is `1337` in the Inspector, press Play | Player falls and lands on solid generated ground — not inside terrain, not falling forever | Screenshot of first screen after landing |
| 2 | Hold `D` ~3 s, then `A` ~3 s; press `Space` while moving | Responsive horizontal movement at constant speed; jump only fires when grounded; no wall-sticking | Note |
| 3 | Walk right until the world X coordinate passes +96 (three chunk widths), then walk left past −96 | New chunks appear ahead of the player with no hitching; chunk renderer objects appear in the Hierarchy as you advance and disappear behind you beyond the unload padding | Short capture or Hierarchy screenshots near +96 and −96 |
| 4 | Stand still; break 3–4 tiles mid-chunk with LMB, place them back with RMB | Edited tiles disappear/appear immediately; only the edited chunk's renderer rebuilds; the player collides with placed tiles and falls through broken ones | Screenshot after edits |
| 5 | Find a chunk border (Hierarchy chunk names change as you cross; borders sit at multiples of 32 on X). Break and place tiles in the two columns straddling the border | Both chunks' meshes and colliders update — no stale sliver of terrain and no invisible collision on either side of the border | Screenshot of the edited border region |
| 6 | *(Full setup)* Inspect the seams around the step-4 and step-5 edits | Autotile edges/corners re-resolve around every edit: no missing sprites, no mismatched edge pieces; grass cover appears only on surface tiles | Close-up screenshot of the edited seams |
| 7 | **Determinism:** while still in the first session, visit columns `x = −40`, `x = 0`, `x = +40`; for each, note the Y of the highest solid tile and the terrain silhouette. Stop Play, press Play again (same seed), revisit all three columns | Surface height and silhouette identical at all three columns, including the negative-coordinate one; first screen after landing matches step 1 | Paired screenshots (run 1 vs. run 2) per column |
| 8 | **Collision edge cases:** with RMB, build a 3-step staircase, a 1-tile gap, and a 2-tile overhang; walk, jump, and fall through them | Player ascends the stairs with jumps, never clips or tunnels through any tile, and slides cleanly along walls and ceilings | Short capture or screenshots of the structures |
| 9 | *(Full setup)* Observe terrain rendering anywhere | Chunks render with autotiled ground/cover sprites (Humus/GrassA/Rocks per the [default tile mapping](visual-integration.md#default-tile-mapping)), not flat vertex-color quads | Screenshot |
| 10 | *(Full setup)* Watch the avatar while idle, running, jumping off a ledge, and landing | A composed layered avatar (not a placeholder sprite) plays Idle, Run, Jump, Fall, and Land matching the movement state, per the [visual setup play-mode checklist](../VISUAL_SETUP.md) | Short capture of one Idle→Run→Jump→Fall→Land cycle |
| 11 | Free play for 5 minutes: move, jump, edit tiles across several chunks | Stable frame rate, no errors or exceptions in the Console, no runaway memory growth (optionally verify 0 steady-state GC allocations via the Profiler targets in [Quality Gates](quality-gates.md#profiler-targets-known-cliffs)) | Console screenshot after the session |

## Optional: MCP-assisted verification

If the in-game MCP bridge is running (`http://127.0.0.1:8765/mcp`, see
[Tooling, Testing & Profiling](13-tooling-testing.md#debug-visualizations-build-these-early)),
agents or reviewers can harden steps 3 and 7 without eyeballing:

- `world_info` — loaded chunk count before/after traversal (step 3).
- `player_teleport` + `tile_at` — read exact tile IDs at the three determinism columns and diff
  them across runs (step 7).
- `perf` — frame time samples during free play (step 11).

## Reporting results

- **All steps pass (or SKIPPED in degraded setup):** attach the evidence to the citing PR/issue
  and check the corresponding boxes in the
  [P1-QA-001 exit evidence checklist](tickets/p1-qa-001-package-the-prototype-vertical-slice-demo-checklist.md#exit-evidence-checklist).
- **Any step fails:** file a follow-up issue per failing step (label `p1`, `type:bug`), link it
  from the run report, and record the step number, expected vs. observed behavior, and evidence.

## See also

- [Quality Gates](quality-gates.md) — merge-blocking checks and profiler targets this runbook draws from.
- [Tooling, Testing & Profiling](13-tooling-testing.md) — automated coverage and the runtime MCP bridge.
- [Visual Integration](visual-integration.md) — autotile and avatar contracts verified in steps 6, 9, 10.
- [Visual setup](../VISUAL_SETUP.md) — machine setup for the full (non-degraded) pass.
