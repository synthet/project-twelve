# tile-viz golden fixtures

## `expected/*.json`

Unity EditMode test `Assets/Tests/EditMode/AutotileFixtureExportTests.cs` is the **authority**
for resolver expectations. It reads each snippet under `snippets/` and writes matching
`expected/<name>.json` files using the real C# `AutotileMaskBuilder` + `AutotileResolver`.

Until Unity runs in this checkout, committed `expected/*.json` may be seeded from the Node
port; re-run EditMode tests after resolver changes to refresh from the engine.

## Regenerate from Unity

```bash
Unity -batchmode -quit -projectPath . -runTests -testPlatform EditMode \
  -testResults TestResults/editmode.xml -logFile Logs/unity-editmode-tests.log
```

## `render/*.png`

Optional PNG goldens for `test/render.test.js`. Requires licensed tile PNGs under
`Assets/_Licensed/PixelFantasy/PixelTileEngine/Tiles` (or `TILE_VIZ_ASSETS_ROOT`).
