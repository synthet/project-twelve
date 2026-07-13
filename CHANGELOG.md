# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.5.0] — 2026-07-12

Tile lighting (P2-LIGHT-001), deterministic terrain noise (P2-GEN-001), PixelLab HUD
pipeline, and related specs/tooling.

### Added

- Sandbox lighting runtime (`Assets/Scripts/Sandbox/Lighting/`): BFS `SandboxLightSolver`
  (clear-then-refill, outside-seeded dirty updates), `SandboxWorldLightGrid`, and
  `TileDefinition.LightAttenuation` (opaque core tiles attenuate by 3). Wired through
  chunk light writes and `SandboxWorld` edit/load relight; EditMode
  `SandboxLightSolverTests`. Lighting wiki marked adopted for P2-LIGHT-001.
- Sandbox inventory runtime (`Assets/Scripts/Sandbox/Inventory/`): hotbar-backed place/pick
  via `SandboxInventoryEditService` and world adapter.
- Project-owned deterministic noise for terrain generation (P2-GEN-001): `SandboxHash` /
  gen-pass integration with `world-viz` parity.
- PixelLab MCP skill (`.claude/skills/pixellab-mcp/`) plus import/normalize scripts,
  HUD asset manifest/redesign docs, and production sprites under
  `Assets/Sprites/UI/Generated/` (hearts, panels, slots, portrait, hotbar, tile icons).
- `SandboxHudPixelPerfectScaler` and expanded HUD prefab/controller coverage for the
  PixelLab v3 layout contract.
- Specs and backlog docs: save/load format, enemy navigation/spawn, P2-QA-001 CI runner
  ticket, expanded P2-DATA-002; HUD mockup contact sheets under `docs/images/hud-mockups/`.

### Changed

- Sandbox HUD vitals/hotbar/selected-item layout and theme wiring; health-panel frame
  sizing; MCP example configs scoped to project settings.
- Paid-assets push-base handling and documentation navigation refinements.

### Fixed

- Autotile EditMode drift repairs; P2-FLUID-001 wake fixture correction.
- HUD health panel frame distortion and layout width.

## [0.4.0] — 2026-07-11

Sandbox HUD editor tooling and test coverage for the prototype hotbar/vitals UI.

### Added

- Editor `SandboxHudPrefabBuilder` (`ProjectTwelve/UI/Rebuild Sandbox HUD`): rebuilds
  `Assets/Prefabs/UI/SandboxHUD.prefab`, assigns theme sprites/fonts, and wires the HUD into
  `Assets/Scene.unity` with `SandboxPlayerVitals` on the player.
- EditMode `SandboxHudTests` plus Editor/EditMode asmdef references for sandbox UI coverage.
- HUD documentation in `docs/wiki/gameplay-systems.md`.

## [0.3.0] — 2026-07-11

Grass growth simulation: grass becomes real, simulated tile state instead of a render-time overlay.

### Added

- Grass growth simulation (`Assets/Scripts/Sandbox/Grass/`): `SandboxGrassSimulator` (vertical
  sky-cast sunlight, neighbor spread, spontaneous growth on sunlit bare dirt, buried/unlit death),
  `SandboxGrassController` play-mode driver, and `SandboxWorldGrassAdapter`. Tunable growth interval,
  spread/spontaneous chances, sky-scan cap, and load-time grass-loss chance.
- `SandboxRegistries.DirtIndex`/`StoneIndex` and `SandboxCoreContent.DirtTileId`/`StoneTileId`.
- `SandboxWorld.LoadedChunkCoords`; load-time grass-loss roll in `LoadFromPath`.
- EditMode `SandboxGrassSimulatorTests`.

### Changed

- **Cover is now grass gameplay state, not material-agnostic.** The green surface cover renders only
  on a `core:grass` tile with an exposed top; bare dirt, stone, and ores stay uncovered until grass
  grows onto them. `ShouldRenderGrassCover`/`TryGetCoverTileset` gate on the grass tile id. Unity C#,
  the `tile-viz` JS resolver, snippet/`expected` fixtures, render goldens, and
  `VISUAL_BEHAVIOR_SPEC.md` were updated together to keep parity.

## [0.2.0] — 2026-07-11

First tagged repository release. Covers agent infrastructure, offline tooling, runtime MCP,
registry migration, autotile/visual work, and sandbox gameplay prototypes merged through PR #111.

### Added

- Visual override system: in-game edit mode, sidecar persistence, rendering-boundary lookup,
  F3 debug labels, and runtime MCP tools (`visual_override_*`).
- Runtime MCP tile dumps and autotile debug (`tiles_area`, `tile_autotile`, `tiles_autotile_area`,
  `autotile_diff_baseline`).
- Tile registry runtime holder; ground tilesets resolved from registry atlas sprites; legacy
  atlas UV/tint fallback keyed on registry indices.
- Fluid water simulation (P2-FLUID-001).
- AI walker pathfinding and spawn rules (P2-AI-001).
- Autotile Phase 0–2: baseline/target fixtures, normalization trace, roof-slope symmetry tests,
  drift RCA toolchain (offline scripts, MCP, F3 baseline modes).
- Offline `tile-viz` and `world-viz` packages for engine-free autotile and terrain parity.
- Agent scaffolding: flat `.claude` skills/commands, CI guard scripts, `unity-tests` skill,
  cloud env notes.
- Assets submodule publish workflow (`scripts` + skill).
- Research docs: game mechanics transferability, WebGL viewer proof-of-concept outline.

### Changed

- `SandboxTileIds` repurposed as registry runtime indices; save/load migrates legacy tile ids.
- Autotile cover mask aligned with vendor model; exposure floor and full-cell ground rendering.
- Ground autotile 32-tile rules and slope handling expanded.
- Visual overrides use a sidecar-only save contract and transform contract for rendering.

### Fixed

- CI: allow submodule doc links; correct autotile RCA documentation path.
- Visual override keyboard shortcut routing.
- Deduplicated missing autotile override sprite warnings.
- EditMode suite fixes (navigation, invalid mirror pair in roof-slope symmetry test).

### Removed

- Session-scoped `AgentDebugLog` debug instrumentation.

[Unreleased]: https://github.com/synthet/project-twelve/compare/v0.4.0...HEAD
[0.4.0]: https://github.com/synthet/project-twelve/compare/v0.3.0...v0.4.0
[0.3.0]: https://github.com/synthet/project-twelve/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/synthet/project-twelve/releases/tag/v0.2.0
