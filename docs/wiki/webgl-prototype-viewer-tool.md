---
type: Guide
title: WebGL Prototype Viewer Tool
description: Proof-of-concept plan for turning the existing JavaScript visualizers into a browser-hosted ProjectTwelve viewer.
resource: wiki/webgl-prototype-viewer-tool.md
tags: [docs, wiki, tooling, webgl, prototype]
timestamp: 2026-07-09T00:00:00Z
okf_version: 0.1
---

# WebGL Prototype Viewer Tool

> **Status:** Proof of concept implemented in [`tools/webgl-viz`](../../tools/webgl-viz/).
> **Decision:** Treat WebGL as a proof-of-concept viewer/tooling surface first, not as a replacement runtime.
> **Invariants:** Unity remains the authoritative game runtime; JavaScript visualizers stay deterministic, fixture-backed, and safe to run without licensed assets.

## Idea

ProjectTwelve already has two engine-free JavaScript visualizers:

- [`tools/world-viz`](../../tools/world-viz/) renders generated or saved worlds as flat-colour PNG,
  ASCII, or self-contained interactive HTML for terrain-shape inspection.
- [`tools/tile-viz`](../../tools/tile-viz/) resolves autotile masks and, when local licensed art is
  available, composites the same sprite-sheet decisions used by Unity.

The WebGL prototype viewer packages those ideas into a browser-first proof of concept: an
interactive canvas that can load a pinned seed, a tile-space capture, or a local save export and let a
reviewer pan, zoom, inspect tiles, toggle overlays, and compare visual hypotheses without opening
Unity. It is a tooling and review surface for the sandbox, not the production Web build of the game.

The practical goal is to make the JS visualizers feel less like one-off CLIs and more like a lightweight
"world microscope" that can be attached to bug reports, design reviews, and agent handoffs.

## Why WebGL instead of only PNG/HTML

The current HTML output is intentionally simple and self-contained, which is useful for snapshots.
WebGL becomes worthwhile when the viewer needs to handle richer, larger, or more dynamic scenes:

- **Large regions:** GPU-backed tile drawing can pan/zoom across chunk-scale captures more smoothly
  than rebuilding DOM or 2D canvas output for every interaction.
- **Layer toggles:** terrain, cover, collision, light, fluid, edit overlays, chunk borders, and resolver
  labels can be drawn as independent render layers.
- **A/B comparisons:** the viewer can show Unity capture vs. JS resolver output, baseline vs. changed
  rules, or before/after save edits with swipe and diff overlays.
- **Inspectable debug metadata:** clicking a cell can reveal world coordinates, chunk/local coordinates,
  tile id/name, solidity, light, fluid, autotile mask, resolved sprite id, flip flags, and normalization
  trace.
- **Shareable evidence:** a static export can bundle JSON fixtures and viewer code so reviewers can
  reproduce the exact visual state that a PR or RCA discusses.

## Proposed viewer modes

| Mode | Input | Purpose |
|------|-------|---------|
| Procedural world | Seed + generator parameters + region bounds | Tune terrain parameters and inspect generation determinism quickly. |
| Save viewer | `sandbox-world.json` or compatible fixture | Rebuild generated terrain, overlay dirty chunk edits, and inspect saved state. |
| Tile-space viewer | `project-twelve/tile-space/v1` snippets/spaces/worlds | Debug isolated autotile cases and attach exact repros to issues. |
| Autotile diff | Baseline report + candidate report | Highlight cells where sprite id, flipX, cover rendering, or masks changed. |
| Runtime capture review | Export from runtime MCP `world_export_tile_space` | Review live Play Mode captures later without requiring Unity or Play Mode. |

## Relationship to existing JS visualizers

The proof of concept should reuse the existing packages instead of creating a third, drifting port.
The desired shape is:

1. Keep deterministic generation, save loading, coordinate math, tile ids, mask building, and resolver
   rules in the existing `world-viz` and `tile-viz` modules.
2. Add a viewer shell that imports those modules and renders their outputs through a WebGL layer.
3. Keep CLI outputs as first-class artifacts; WebGL augments PNG/HTML/JSON evidence rather than
   replacing it.
4. Continue to gate behavior with Node tests and Unity-authored fixtures where Unity is the source of
   truth.

This preserves the current safety property: agents and reviewers can run the visualizers in cloud or
CI environments that do not have Unity installed, while Unity tests remain the authoritative parity
check for engine behavior.

## Minimum proof of concept

The implemented first spike is deliberately small:

1. **Static bundled viewer:** `cd tools/webgl-viz && npm run build` emits a self-contained `dist/index.html` and normalized `payload.json`.
2. **Load one fixture:** the default build embeds `grass-cover-middle.json`; compatible normalized payload JSON can be drag/dropped into the page.
3. **Render flat debug tiles:** WebGL draws the same tile colours used by `world-viz` with pan/zoom.
4. **Inspector panel:** clicking a tile shows world coords, chunk/local coords, tile id/name, light, fluid, solidity, and colour.
5. **Overlay toggles:** chunk grid, light heatmap, and solid-tile tinting are available in the viewer.
6. **Export evidence:** the page saves a small JSON report describing source payload, viewport, enabled overlays, and selected tile.

That scope proves the viewer architecture without requiring licensed sprites, Unity Web builds, or a
full gameplay loop.

## Implemented package

The current proof of concept lives in [`tools/webgl-viz`](../../tools/webgl-viz/) with package-local
usage notes and tests. Build it from the package directory:

```bash
cd tools/webgl-viz
npm test
npm run build
python3 -m http.server 8080 --directory dist
```

The generated `dist/` directory is intentionally ignored; commit source, fixtures, and tests rather
than local static build output.

## Follow-up capabilities

After the flat-colour POC is stable, add features in this order:

1. **Autotile report layer:** call the existing `tile-viz` resolver and show sprite id / flipX / cover
   labels per solid tile.
2. **Licensed-art rendering when available:** load local art from a user-supplied directory or a dev-only
   asset pack path; never embed licensed PNGs in committed viewer fixtures.
3. **Diff mode:** compare two reports or captures and tint only changed cells.
4. **Runtime MCP import shortcut:** paste or load a `world_export_tile_space` capture from Play Mode.
5. **Scenario gallery:** curated fixtures for common terrain, caves, slopes, save edits, and known
   autotile regression cases.
6. **Playback hooks:** later, replay edit sequences or generation parameter changes frame-by-frame.

## Non-goals

- Do not implement the production Unity WebGL build here.
- Do not duplicate gameplay physics, character control, inventory, or networking in JavaScript.
- Do not hand-maintain a second terrain/autotile implementation; reuse the tested modules.
- Do not commit licensed art, generated local debug PNGs, or machine-specific paths.
- Do not make the browser viewer the source of truth for rules that Unity owns.

## Acceptance criteria for the spike

- The viewer loads at least one committed fixture and displays it with pan/zoom and tile inspection.
- It runs with `npm test` in the relevant `tools/` package and does not require Unity.
- It consumes the same JS core modules as `world-viz` / `tile-viz` rather than copying resolver logic.
- It documents the exact commands needed to build, run, and export evidence.
- It respects the paid-asset policy by using flat colours by default and optional local art only.
