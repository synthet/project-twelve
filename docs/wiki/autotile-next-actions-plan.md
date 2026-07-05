---
type: Plan
title: Autotile Next Actions Plan
description: Fixture-driven plan to finish ground autotile visual polish via mask normalization, negative-tested and gated, incorporating review feedback.
resource: wiki/autotile-next-actions-plan.md
tags: [docs, wiki, autotile, visual, plan]
timestamp: 2026-07-05T00:00:00Z
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
| Normalization entry point | `AutotileMaskBuilder.NormalizeGroundMask(...)` |
| Existing normalizers | `TryRemapStairInteriorDiagonalMask`, `TryRemapCavityBridgeToUnderside`, `TryRemapMaterialBoundaryCornerMask` |
| Detailed build result | `GroundMaskBuildResult` (visual/solid/connectivity/final masks + 3 flags) |
| Play Mode debug payload | `Assets/Scripts/RuntimeMcp/McpTileDebug.cs` → `AppendGroundAutotile` |
| Offline resolver report | `tools/tile-viz/src/report/autotileJson.js` → `buildTileAutotile` |
| Offline mask builder | `tools/tile-viz/src/visual/maskBuilder.js` |
| Snippet fixtures | `tools/tile-viz/test/fixtures/snippets/*.json` |
| Parity/regression tests | `tools/tile-viz/test/integration-regression-fixtures.test.js` |
| Unity fixture export | `AutotileFixtureExportTests`, `AutotileRulesFixtureExportTests`, `AutotileVisualTests` (EditMode) |
| Rule spec | [`docs/wiki/ground-autotile-32-rules.md`](ground-autotile-32-rules.md) |
| Visual contract | [`docs/VISUAL_BEHAVIOR_SPEC.md`](../VISUAL_BEHAVIOR_SPEC.md) |

Current normalization flags surfaced on both debug surfaces: `stairInterior`,
`cavityUnderside`, `materialBoundary`. There is **no** `TryRemapCavityInnerEdgeMask` yet —
that is the new Phase 1 predicate.

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

**Primary risk: over-broad inner-cavity detection.** `TryRemapCavityInnerEdgeMask` (new) is
the right idea but easy to make too broad — fixing windows while breaking caves, cliffs, and
roof edges. Mitigate with fixture-by-fixture implementation, a negative test for every
predicate, and the decision trace from Phase 0C.

Intended remaps (existing rules only, no new sprite IDs): inner vertical strips move toward
rule `8` / `8f`; lintels avoid noisy `18` and move toward `17`. This complements the existing
`TryRemapCavityBridgeToUnderside` (bridge → underside `17`) rather than replacing it.

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

Anchor slopes to `roof-slope-left-vs-right.json`. Required invariant for paired cells:

```text
left slope normalized mask == horizontalMirror(right slope normalized mask)
left spriteId == right spriteId
left flipX != right flipX
```

If the invariant cannot hold for a given pair, require a **written exception** in the fixture
explaining why. Cover-coordination tests:

```text
cover mask and ground mask agree on slope chirality
cover does not visually contradict ground step direction
```

Keep existing slope fixtures (`slope-ascending-long`, `slope-descending-long`, the stair-run
snippets) locked as anti-regressions; do not change partner substitution.

---

## Phase 3 — Unity blit/mesh parity (GATED)

Default: do this **after** cavity and slope work, and only when the gate opens.

```text
If SpriteIdLabel + MCP + tile-viz all agree, but pixels differ visibly:
    do Phase 3 mesh/blit parity.
If IDs or masks still differ:
    do NOT touch mesh — finish normalization first.
```

tile-viz blits a full 16×16 cell; Unity uses sprite mesh vertices. The earlier `flipX` mesh
bug proves rendering parity matters, but a full-cell blit rewrite is broader than mask
normalization, so it stays gated.

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
