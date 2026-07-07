---
type: Plan
title: Autotile Next Actions Plan
description: Fixture-driven plan to finish ground autotile visual polish via mask normalization, negative-tested and gated, incorporating review feedback.
resource: wiki/autotile-next-actions-plan.md
tags: [docs, wiki, autotile, visual, plan]
timestamp: 2026-07-06T05:45:00Z
okf_version: 0.1
---

# Autotile Next Actions Plan

Plan to close the remaining ground-autotile visual issues (inner cavities, roof-slope
symmetry, one-sided lips) through **small, fixture-backed mask normalization** — not broad
sprite-ID partner tables and not more `flipX` debugging.

The important gates have already passed: tile-viz and Unity labels agree on checked cells,
partner substitution is disabled (`partnerSubstitution: false` on both surfaces), and the
`flipX` mesh-anchoring bug is fixed (sprites anchor by bounds, not pivots — see
[`history-regression-guard`](../../.claude/rules/history-regression-guard.md)). The remaining
ugliness is mask normalization, so the work below stays inside the existing 32-rule
PixelFantasy topology.

**Document map** — behavior authority lives elsewhere; this plan tracks *what to do next*:

| Doc | Role |
|-----|------|
| [`VISUAL_BEHAVIOR_SPEC.md`](../VISUAL_BEHAVIOR_SPEC.md) | Behavioral contract |
| [`autotile-algorithm.md`](autotile-algorithm.md) | Algorithm + normalization (§8) |
| [`ground-autotile-32-rules.md`](ground-autotile-32-rules.md) | 32 masks + acceptance checks |
| This page | Roadmap, phases, fixture workflow |

This document folds in the plan review captured in
`autotile_next_actions_comments.md`. The seven review additions are treated as
requirements, not options:

1. Phase 0 is mandatory and blocks all normalization work.
2. Add a `normalizationTrace` array to both debug surfaces.
3. Every new inner-cavity predicate ships with positive **and** negative tests.
4. Roof-slope cells log ground **and** cover layers separately.
5. Phase 3 (mesh/blit parity) stays behind the "labels match but pixels differ" gate.
6. Docs gain an explicit "non-goals" section to stop future partner-table regressions.
7. Visual artifacts are saved after validation as regression references.

## Where the code stands today

Grounding for every phase below. These are the real APIs the work extends.

| Concern | Canonical location |
|---------|--------------------|
| Mask build + normalization | `Assets/Scripts/Visual/Tiles/AutotileMaskBuilder.cs` |
| Normalization entry point | `AutotileMaskBuilder.NormalizeGroundMask(...)` — **pass-through** (vendor-aligned) |
| Retained helpers (not invoked) | `TryRemapStairInteriorDiagonalMask`, `TryRemapCavityInnerEdgeMask`, `TryRemapCavityBridgeToUnderside`, `TryRemapMaterialBoundaryCornerMask` |
| Detailed build result | `GroundMaskBuildResult` (visual/solid/connectivity/final masks + four normalization flags, all false) |
| Play Mode debug payload | `Assets/Scripts/RuntimeMcp/McpTileDebug.cs` → `AppendGroundAutotile` |
| Offline resolver report | `tools/tile-viz/src/report/autotileJson.js` → `buildTileAutotile` |
| Offline mask builder | `tools/tile-viz/src/visual/maskBuilder.js` |
| Snippet fixtures | `tools/tile-viz/test/fixtures/snippets/*.json` |
| Parity/regression tests | `tools/tile-viz/test/integration-regression-fixtures.test.js` |
| Unity fixture export | `AutotileFixtureExportTests`, `AutotileRulesFixtureExportTests`, `AutotileVisualTests` (EditMode) |
| Rule spec (32 masks) | [`docs/wiki/ground-autotile-32-rules.md`](ground-autotile-32-rules.md) |
| Algorithm reference | [`docs/wiki/autotile-algorithm.md`](autotile-algorithm.md) |
| Visual contract | [`docs/VISUAL_BEHAVIOR_SPEC.md`](../VISUAL_BEHAVIOR_SPEC.md) |

Current normalization flags on debug surfaces: `stairInterior`, `innerCavity`,
`cavityUnderside`, `materialBoundary` (same order in `normalizationTrace`). Under **vendor
alignment** each entry reads `skipped` — `finalMask` equals `connectivityMask`.

> **Shipped (2026-07-06) — vendor-aligned ground resolution.** The normalization layer is
> disabled at runtime. Window-top corners resolve vendor **18** (+`flipX`), flat lintel spans
> **17**, open-sky bridge lintels **25**, all from raw blob masks. Fixtures: `mountain-window-corner`,
> `open-sky-bridge-lintel`, `dirt-window-inner-edges`. Canonical behavior:
> [`autotile-algorithm.md`](autotile-algorithm.md) §8.

> **Next phases** target **selective re-enable** only where vendor-raw regressions appear (grass
> stair runs, dirt/stone lips) — each with `baselineExpect`/`targetExpect` and negative guards.

> **Phase 0 status.** 0A (baseline/target vocabulary) and 0C (`normalizationTrace` + C#/JS
> field parity) are **implemented**. 0B (Unity expected-JSON export) is **blocked** in the
> headless container: Unity 6000.5.1f1 is not installed, so EditMode export tests were added
> but not run here. `normalizationTrace` is derived from the normalization flags by a shared
> helper — `buildNormalizationTrace` (maskBuilder.js) and `AutotileMaskBuilder.BuildNormalizationTrace`
> (C#) — sharing one order and label set so the trace is parity-correct by construction. The
> scalar/mask fields the JS report carried that the C# MCP lacked (`materialGroup`, `rawMask`,
> `matchedRuleId`, `finalSpriteId`) are now emitted on both. Neighbor context remains an
> **intentional shape difference**, not drift: C# exposes it via the payload-level `neighbors`
> object, JS via the ground-block `neighborTileIds` / `neighborMaterials`.

## Sequencing

```text
Phase 0  fixtures + trace (mandatory gate)
  → Phase 1  inner-cavity normalization, one shape at a time
  → Phase 2  roof slope symmetry (ground first, cover only if needed)
  → Phase 3  Unity blit/mesh parity (GATED)
  → Phase 4  edge-case recheck
  → Phase 5  validation + visual artifacts
```

---

## Phase 0 — Fixture vocabulary and decision trace (mandatory)

**No normalization work starts until Phase 0 is complete.** Right now some snippet `expect`
blocks describe *current resolver output* and others describe *desired visual acceptance*.
That ambiguity makes every downstream test hard to reason about. Fix the vocabulary first.

### 0A. Split `baselineExpect` from `targetExpect`

Today each snippet under `tools/tile-viz/test/fixtures/snippets/` carries a single `expect`
array (see `dirt-window-1x1.json`). Extend the schema so a fixture can distinguish current
behavior from acceptance behavior:

```json
{
  "baselineExpect": [
    { "x": 1, "y": 0, "ground": { "spriteId": "17" } }
  ],
  "targetExpect": [
    { "x": 1, "y": 0, "ground": { "spriteId": "17" } }
  ]
}
```

- `baselineExpect` = current resolver output → parity / regression tracking.
- `targetExpect` = desired visual acceptance → the assertion a fix must satisfy.
- Keep `expect` accepted as an alias for `baselineExpect` during migration so
  `assertExpectations` (`autotileJson.js`) and existing tests keep passing; migrate
  fixture-by-fixture, do not do a big-bang rename.
- Where baseline already equals target (e.g. `dirt-window-1x1` already resolves `17`), both
  arrays are identical and the cell is a locked anti-regression.

Update the loader/asserter so tests can request either set explicitly:

```text
baseline tests = parity / regression tracking
target tests   = acceptance after fix
```

### 0B. Export Unity expected JSON

Confirm the Unity EditMode fixture-export tests (`AutotileFixtureExportTests`,
`AutotileRulesFixtureExportTests`) emit the same cells the tile-viz snippets assert, so
`baselineExpect` is anchored to real C# output rather than hand-authored guesses. Do **not**
hand-edit exported JSON under `tools/tile-viz/data/`; regenerate it from C#
(EditMode export tests or `tools/tile-viz/scripts/generate-rules-json.mjs`) per
[`unity-project`](../../.claude/rules/unity-project.md).

### 0C. Add `normalizationTrace`

Booleans (`stairInterior`, `cavityUnderside`, `materialBoundary`) answer *what happened*. A
trace answers *why it happened, and why other rules did not fire* — essential once multiple
normalizers exist. Add a short ordered `normalizationTrace` array to both debug surfaces:

```json
{
  "normalizationTrace": [
    "stairInterior: skipped",
    "cavityBridgeToUnderside: skipped",
    "materialBoundary: skipped",
    "innerCavity: applied: lintel -> rule17"
  ]
}
```

- Emit one entry per normalizer in evaluation order, each `applied:`/`skipped` with a short
  reason.
- Add to `McpTileDebug.AppendGroundAutotile` (alongside the existing `normalization` object)
  **and** `autotileJson.buildTileAutotile`, identical field names.
- `partnerSubstitution` stays `false`; assert this in the debug-output contract tests.

**Phase 0 exit criteria:** every touched snippet has `baselineExpect`/`targetExpect`;
`normalizationTrace` present on both surfaces; `cd tools/tile-viz && npm test` green; Unity
EditMode export tests green (or Unity unavailability recorded as the explicit blocker).

---

## Phase 1 — Inner-cavity normalization (one shape at a time)

> **Phase 1 status (2026-07-06) — closed via vendor alignment.** Window-top corners, flat lintel
> spans, and open-sky bridge lintels now resolve from **raw vendor masks** with normalization
> disabled (`NormalizeGroundMask` pass-through). Fixtures: `mountain-window-corner`,
> `open-sky-bridge-lintel`, `dirt-window-inner-edges`. Future inner-cavity work is **selective
> re-enable** only when vendor-raw output regresses elsewhere — capture `baselineExpect` /
> `targetExpect` before turning a `TryRemap*` helper back on. See
> [`autotile-algorithm.md`](autotile-algorithm.md) §8.

**Primary risk if re-enabling:** over-broad predicates (the original inner-cavity and
`cavityUnderside` remaps flattened corners and bridge cells). Any re-enable needs negative guards
on stairs, cliffs, material boundaries, and unrelated cavities.

### Implementation order (one shape per change)

```text
1. 1×1 hole          (dirt-hole-1x1.json, dirt-window-1x1.json)
2. 2×1 horizontal    (dirt-hole-2x1.json, dirt-window-2x1.json)
3. 1×2 vertical      (dirt-hole-1x2.json, dirt-window-1x2.json)
4. door opening      (dirt-door-opening.json, dirt-hole-door.json)
5. arch / irregular
```

For each shape:

1. Capture baseline output → `baselineExpect`.
2. Define desired output → `targetExpect`.
3. Add one narrow normalization predicate branch.
4. Add positive **and** negative tests.
5. Re-render tile-viz.
6. Check Play Mode `SpriteIdLabel` / MCP labels.

### Negative tests (required per predicate)

The predicate must **not** fire on any of these:

```text
external-cliff-not-inner-cavity
roof-overhang-not-inner-cavity
stone-boundary-not-inner-cavity   (unless material-boundary context applies)
ordinary-slope-not-inner-cavity
```

Add these to `integration-regression-fixtures.test.js` next to the positive cases.

### Weighted-variant guard

Rules `1`/`2` and `9`/`10` are weighted visual variants. Any expectation touching them must
accept a set or use deterministic selection:

```text
surface: 1 or 2
fill:    9 or 10
```

Keep exact assertions only for structural rules (`17`, `24`+flipX, `8f`, `16`, `31`). Do not
write tests that fail solely because a weighted variant changed.

---

## Phase 2 — Roof slope symmetry

> **Phase 2 status — evidence-driven finding.** On a geometrically symmetric hill
> (`symmetric-pyramid.json`) the resolver already produces **mirror-symmetric output for every
> cell, ground and cover** — see `roof-slope-symmetry.test.js`. The anchor
> `roof-slope-left-vs-right` fixture is *not* a clean geometric mirror (its two slopes have
> different profiles — irregular 1-then-2-wide on the left, regular 2:1 on the right), so it
> only supports the author's designated cap pairs, which also mirror correctly. No ground-mask
> asymmetry bug reproduces in the resolver; Phase 2 locks the symmetry as an anti-regression.
> Any residual "brown chunk under grass" seen in a full scene is therefore not a resolver
> symmetry defect and must be reproduced as a concrete fixture (or chased in the render/mesh
> path, Phase 3) before further ground changes.

The "brown chunk under grass" issue may be a **cover↔ground coordination bug**, not just a
ground sprite bug. Try ground-mask normalization first; inspect cover only if ground IDs are
already structurally correct but pixels still read wrong.

### Log both layers per slope cell

For each roof-slope target cell, dump both layers (the debug payloads already carry `ground`
and `cover` blocks — assert on both):

```text
ground: spriteId / flipX / rawMask (connectivityMask) / normalizedMask
cover:  spriteId / flipX / mask
```

If ground IDs are structurally correct but cover is visually offset or chirally inconsistent,
changing ground masks risks new regressions — fix the coordinating layer instead.

### Structural mirror invariant

Anchor a clean geometric mirror with `symmetric-pyramid.json` (and check the designated cap
pairs on `roof-slope-left-vs-right.json`). Required invariant for paired cells:

```text
left spriteId == right spriteId
left normalized mask == horizontalMirror(right normalized mask)
flipX: opposite when the mask is asymmetric; both false when the mask is self-mirror
```

The last line is the corrected rule — a naive "left flipX != right flipX" is wrong for
self-mirror masks (e.g. fill `9`, bottom edge `17`), where the correct `flipX` is `false` on
both sides. If the invariant cannot hold for a given pair, require a **written exception** in
the fixture explaining why. Cover-coordination tests:

```text
cover mask and ground mask agree on slope chirality
cover does not visually contradict ground step direction
```

Keep existing slope fixtures (`slope-ascending-long`, `slope-descending-long`, the stair-run
snippets) locked as anti-regressions; do not change partner substitution.

---

## Cover layer RCA (post Phases 0–2)

> **Cover RCA status — evidence-driven finding (2026-07-05).** Offline drift RCA on the frozen
> `sandbox-scene-mountain` capture reports **0 mismatches** for all 2642 solid cells on both
> ground and cover fields (`compare-autotile-baseline.mjs --only ground|cover`). Spot cells:
> hill peaks `(-102,30)` / `(-87,29)` render cover sprite **3** with correct `flipX`; window
> lintel middle `(-113,29)` resolves ground **17**; window top corners (e.g. `(-114,29)`) resolve
> **18 flipX** after the inner-corner guard (see `mountain-window-corner` fixture). tile-viz PNG:
> golden matches `--with-cover` render (0 px diff); cover vs `--no-cover` differs on ~1.4% of
> pixels (GrassA layer is visually active). **Conclusion:** cover resolver ids are correct on the
> frozen capture; any Play Mode `ResolveDetail` red while offline compare is 0 means **live
> world data drift** (refresh capture/save) or **baseline failed to load** (device builds need
> `StreamingAssets/AutotileBaselines/` — Editor falls back to `tools/tile-viz/test/fixtures/baselines/`).
> EditMode `MountainCapture_FullBaselineGroundAndCoverParity` locks C# ground+cover vs baseline on
> the capture. Phase 3 mesh/blit parity stays **gated** until Unity Game View PNG diff (same crop/
> scale as tile-viz) shows pixels differ while ids match.

---

## Phase 3 — Unity blit/mesh parity (IMPLEMENTED)

Gate opened: Play Mode ids matched baseline on slopes/island but pixels diverged; bottom-row
cells also resolved fill `9` instead of underside `17` because procedural stone below `y=0`
counted as solid south support (tile-viz treats off-space as air).

**Shipped fixes (2026-07-05):**

| Fix | Location |
|-----|----------|
| Exposure floor — neighbors below `autotileExposureFloorY` (default `0`) are air for `isSolid` | [`AutotileExposure.cs`](../../Assets/Scripts/Visual/Tiles/AutotileExposure.cs), [`SandboxWorld.cs`](../../Assets/Scripts/Sandbox/SandboxWorld.cs) |
| Ground mesh parity — `AppendGroundAutotileSprite` uses full-cell UV quad when sprite mesh does not span the logical cell | [`AutotileSpriteMeshBuilder.cs`](../../Assets/Scripts/Visual/Tiles/AutotileSpriteMeshBuilder.cs), [`SandboxChunkRenderer.cs`](../../Assets/Scripts/Sandbox/SandboxChunkRenderer.cs) |

Cover layer still uses sprite-mesh `AppendSprite`. Re-verify Play Mode ROI vs
`render-roi-debug.mjs --no-cover` after Unity recompile.

### Option A — full-cell quad for autotiles (preferred if Phase 3 is needed)

Render every autotile sprite as a fixed 16×16 quad with UVs into the sprite region.

- Pros: matches tile-viz, simple mental model, stable `flipX`, good for pixel-art terrain.
- Cons: may ignore sprite mesh trimming; needs careful UV padding/extrusion.

### Option B — normalized cell mapping

Keep sprite mesh geometry but normalize vertices into cell-local coordinates.

- Pros: preserves mesh path, less invasive.
- Cons: harder to guarantee tile-viz parity; more edge cases with asymmetric sprites.

**Preference:** fixed full-cell quads for autotile terrain; keep sprite-mesh rendering for
non-autotile sprites. Test with asymmetric sprites and atlas padding/extrude; compare Play
Mode ROI against tile-viz output to catch seams.

---

## Phase 4 — Edge-case recheck (permanent anti-regressions)

After Phases 1–2, re-verify these existing fixtures and keep them locked:

```text
one-sided-house-lip            (24 + flipX must not become 25)
dirt-gap-left-vertical-wall
material-boundary-horizontal
dirt-stone-reentrant-west
```

Invariants to protect:

```text
24 + flipX must not become 25
21 must not become 22 unless the mask truly has side mass
stone counts as support but not dirt connectivity
foreign solid below does not equal air
foreign solid beside dirt does not make dirt resolve as 9/10 fill
inner-cavity logic does not fire on external cliffs
roof-slope logic does not fire on windows
```

---

## Phase 5 — Validation and visual artifacts

Run the full gate set from [`CLAUDE.md`](../../CLAUDE.md) § Commands:

```bash
cd tools/tile-viz && npm test
# Unity EditMode (if available): AutotileVisualTests + fixture export tests
python3 scripts/check_markdown_links.py
python scripts/ci/okf_lint_changed.py --base origin/main --head HEAD --profile project --fail-on error
```

Then save visual outputs as regression references — automated tests catch logic, artifacts
catch "technically correct but ugly":

```text
tools/tile-viz/out-slope/sandbox-scene-mountain-after-inner-cavity.png
tools/tile-viz/out-slope/roof-slope-left-vs-right-after.png
tools/tile-viz/reports/sandbox-scene-mountain-after-inner-cavity.json
```

Also capture the Play Mode **SpriteId label overlay** (per-tile rule ID + flag coloring) for
the same mountain scene, before and after. It is the primary in-engine instrument for
Phases 1–2 — problem cells surface as flagged tiles clustered at slope peaks and shoulders —
and a paired before/after overlay is the fastest visual proof that a normalization change hit
only the intended cells.

---

## Recommended execution order

```text
0A. Split baselineExpect / targetExpect.
0B. Export Unity expected JSON.
0C. Add normalizationTrace (both surfaces; close C#/JS field drift).

1A. Fix only 1×1 hole.
1B. Fix 2×1 window.
1C. Fix 1×2 / door.
1D. Add negative tests for external cliffs and roof overhangs.

2A. Add roof-slope left/right structural symmetry tests.
2B. Log both ground and cover layers.
2C. Fix ground first; cover only if needed.

3.  Mesh/blit parity ONLY if labels match but pixels still differ.

4.  Recheck one-sided lip, dirt-gap-left-vertical-wall,
    material-boundary-horizontal, dirt-stone-reentrant-west.

5.  Full validation; save visual artifacts; update docs.
```

## Debug-output contract tests

Add to the tile-viz test suite (and mirror assertions in Unity MCP dispatcher tests where the
tool contract changes):

```text
normalizationTrace exists on ground payload
innerCavity flag/trace entry appears only when applied
partnerSubstitution remains false
visualMask and solidMask are both emitted
```

## Non-goals (documentation guard)

When updating [`VISUAL_BEHAVIOR_SPEC.md`](../VISUAL_BEHAVIOR_SPEC.md) and
[`ground-autotile-32-rules.md`](ground-autotile-32-rules.md), add a **non-goals** subsection so
future agents do not "simplify" the fix back into a partner table:

```text
Inner-cavity normalization does not introduce new sprite IDs.
It only chooses existing PixelFantasy rule masks.
It does not replace vendor flipX behavior.
It does not add broad authored partner substitution.
It does not change the 32-rule PixelFantasy topology.
It is fixture-backed and only applies to explicit cavity contexts.
```

## Risks and mitigations

| Risk | Mitigation |
|------|------------|
| Cavity remap becomes too broad | Fixture-by-fixture; negative test per predicate; `normalizationTrace` |
| Roof-slope fix breaks ordinary slopes | Keep existing slope fixtures; left/right mirror invariant; don't touch partner substitution |
| Mesh/blit parity introduces seams | Keep Phase 3 gated; test asymmetric sprites + atlas padding/extrude; compare Play Mode ROI |
| Tests brittle from variant sprites | Allow variant sets for `1`/`2` and `9`/`10`; exact assertions only for structural IDs |

## Bottom line

```text
Correct order, correct constraints.
Biggest risk: over-broad innerCavity detection.
Best mitigation: fixture-driven implementation with negative tests and decision traces.
```
