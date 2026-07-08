# tile-viz golden fixtures

## `snippets/*.json`

Hand-authored `project-twelve/tile-space/v1` grids. Each snippet should include
`baselineExpect` (current resolver output). Use `targetExpect` when acceptance
criteria differ from baseline (normalization roadmap).

`expect` remains accepted as an alias for `baselineExpect` in `assertExpectations`.

## `expected/*.json`

Unity EditMode test `Assets/Tests/EditMode/AutotileFixtureExportTests.cs` is the **authority**
for resolver expectations when Unity is available. The Node script
`scripts/sync-snippet-fixtures.mjs` can regenerate these from the offline port.

Until Unity runs in this checkout, committed `expected/*.json` are seeded from the Node
port; re-run EditMode tests after resolver changes to refresh from the engine.

Snippets with a `.visual-overrides` sidecar are excluded from mask parity in
`resolver.test.js` (debug-only overrides).

## `coverage-matrix.json`

Machine-readable map from autotile logic branch to fixture names. Validated by
`test/coverage-matrix.test.js` — every branch must reference at least one snippet.

## `render/*.png`

Optional PNG goldens for `test/render.test.js`. Requires licensed tile PNGs under
`Assets/_Licensed/PixelFantasy/PixelTileEngine/Tiles` (or `TILE_VIZ_ASSETS_ROOT`).

## `captures/` and `baselines/`

Per-fixture MCP tile/autotile dumps under `captures/*-{tiles,autotile}.json` are on-demand RCA artifacts.

Play Mode baseline drift still uses `Assets/StreamingAssets/AutotileBaselines/` (see [`docs/wiki/autotile-drift-rca.md`](../../../../docs/wiki/autotile-drift-rca.md)).

## Regenerate from Unity

```bash
Unity -batchmode -quit -projectPath . -runTests -testPlatform EditMode \
  -testResults TestResults/editmode.xml -logFile Logs/unity-editmode-tests.log
```

## Regenerate expected from Node

```bash
node scripts/sync-snippet-fixtures.mjs
```
