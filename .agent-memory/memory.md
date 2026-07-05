# Project Memory


## Stable Project Facts

- Runtime MCP tools require Play Mode; endpoint is loopback-only on port 8765
- Cloud sessions can run Python lint scripts and tile-viz/world-viz npm tests but not Unity batch or licensed submodule work
- P2-DATA-002 pinned-seed check: compare tile names and surface Y at x=-40/0/+40 seed 1337; raw runtime ids differ from legacy 0-7
- RuntimeMcpServer under DontDestroyOnLoad on Play is normal; it does not substitute for Scene.unity
- Ground autotile uses PixelFantasy 32-tile sheets with slope, material-boundary, and flipX resolution; canonical spec in docs/wiki/ground-autotile-32-rules.md.
- The save file (sandbox-world.json) stores tile.id as runtime palette indices, NOT canonical tile-viz TileId; remap through tilePalette string ids (dirt=2/grass=4/stone=7 -> 1/2/3) when importing a save into tile-viz.

## User Preferences

- (none yet)

## Working Rules

- Autotile changes require Unity EditMode tests and `cd tools/tile-viz && npm test` for C#/JS parity
- Terrain generation changes require Unity EditMode tests and `cd tools/world-viz && npm test`
- Licensed art lives in Assets/_Licensed submodule; never commit paid blobs to the public repo
- Autotile and sprite anchoring must use sprite bounds, not pivots (VISUAL_BEHAVIOR_SPEC)
- Edit .claude/ sources then run python scripts/sync_assistant_trees.py; do not hand-edit .cursor/
- Ground autotile changes require Unity EditMode autotile tests plus cd tools/tile-viz && npm test; keep C# maskBuilder/resolver and JS maskBuilder.js/resolver.js in sync
- CLI debug outputs under tools/tile-viz/out*.png and tools/world-viz/out.* are local-only; do not commit node_modules
- On Windows Unity EditMode batch runs omit -quit with -runTests; see .claude/skills/unity-tests/SKILL.md
- Claude Code cloud env for project-twelve: paste scripts/cloud-environment-setup.sh into claude.ai/code setup script; Trusted network; no Unity or secrets in env vars
- SessionStart hook scripts/cloud-session-check.sh re-runs sync check and npm tests only when CLAUDE_CODE_REMOTE=true
- PixelFantasy ground autotile: flip passes return same sprite ID with flipX=true; never use sheet-geometry partner table as resolver truth
- AutotileGroundSpritePartners and groundSpritePartners.js must stay empty until fixture-validated; vendor baseline is same spriteId + flipX
- Autotile changes require Unity EditMode autotile tests and cd tools/tile-viz && npm test
- One-sided lip column-mirror test should use mask 000/110/000 for sprite 24 + flipX, not 010/010/000 (rule 29)
- tools/tile-viz and tools/world-viz local CLI outputs belong in .gitignore; golden PNGs live under test/fixtures/ and must remain tracked.
- Unity AutotileFixtureExportTests must write AutotileResolver.ResolveSpriteId rule-table ids, not mock Sprite.name (empty), or tile-viz expected JSON parity fails
- On Windows Unity 6 batchmode EditMode: omit -quit with -runTests; allow 60-120s for full suite
- Editor Play Mode runs the currently open scene, not build-settings scene; empty Untitled shows only camera clear color
- If Game view is blank, check Hierarchy scene name first (Untitled vs Scene) before debugging rendering
- Unity flipX must mirror inside tile cell (bounds-relative offset), not around sprite origin — tile-viz blit parity
- Snippet fixture expects must be computed from snippet-local autotile context, not full-world capture
- Autotile changes require Unity EditMode tests and cd tools/tile-viz && npm test; rule JSON under tools/tile-viz/data/ is exported from C#, not hand-edited.
- Ground autotile deliberately has no underside or vertical-face mask remapping: undersides use authored 14-17/31 family, faces use 8/22+flipX; remapping them produced upside-down top sprites and 21 film-strip artifacts.

## Recurring Issues

- Unsafe partner pairs without mask proof: 24->25 (lip vs bridge), 21->22 (pillar vs side-body), 0/7, 8/15, 16/23, 30/31
- VerticalMiddleStripMask must target rule 21 (010/010/010), not rule 25 (000/111/000) or rule 28 (000/010/010)
- Unity MCP RunCommand blocked when Cursor connection revoked in Project Settings > AI > Unity MCP
- Window/hole dirt frames (rules 17/18) need inner-cavity normalization separate from external cliff rules; flipX mesh fix does not address unflipped right-roof caps
- Assets/_Licensed dirty working tree warns on commit but does not stage unless submodule pointer changes; commit asset changes in project-twelve-assets separately.
- Unity EditMode batch runs fail when the Editor is open (project lock); Unity 6000.5.1f1 is not on PATH by default; MCP bridge approval is often revoked.

## Successful Patterns

- F3 SpriteIdLabel + MCP tile_autotile are the acceptance gate before changing masks vs mesh compositing

## Open Questions

- Slope/stair repetition (12, 7, 11, 23 on diagonals) needs separate run classifier; partner substitution does not fix it

## Deprecated / Superseded

- (none yet)
