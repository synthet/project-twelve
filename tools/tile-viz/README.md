# tile-viz — offline autotile resolver and sprite compositor

Resolve and render ProjectTwelve autotiles **without the Unity engine**: load tile
snippets, spaces, or procedural worlds from JSON, run the same mask/resolver logic as
`Assets/Scripts/Visual/Tiles/`, and optionally composite licensed sprite sheets to PNG.

Sibling to [`../world-viz`](../world-viz) (terrain generation / flat-color debug). tile-viz
reuses world-viz core modules for world sampling and save loading.

## Setup

```bash
cd tools/tile-viz
npm install
npm test
```

Licensed art is **not** committed. For PNG rendering, point `--assets-root` at your local
submodule tile folder, e.g. `Assets/_Licensed/PixelFantasy/PixelTileEngine/Tiles`.

## Usage

```bash
# Resolve autotile masks + sprite ids (stdout JSON)
node src/cli.js resolve --space test/fixtures/snippets/grass-cover-middle.json

# Assert inline expect[] blocks in a fixture
node src/cli.js test-fixture --space test/fixtures/snippets/dug-west-gap.json

# Render sprite PNG (requires licensed PNGs)
node src/cli.js render --space test/fixtures/snippets/grass-cover-middle.json \
  --assets-root ../../Assets/_Licensed/PixelFantasy/PixelTileEngine/Tiles \
  --scale 16 --png out.png

# Render sprite PNG with visual override sidecar data
node src/cli.js render --space test/fixtures/snippets/grass-cover-middle.json \
  --assets-root ../../Assets/_Licensed/PixelFantasy/PixelTileEngine/Tiles \
  --visual-overrides visual-overrides.json --scale 16 --png out.png

# List visual override sidecar entries
node src/cli.js list-visual-overrides --visual-overrides visual-overrides.json

# Convert runtime MCP dump to tile-space JSON
node src/cli.js import-mcp --file ../world-viz/mcp-dirt-stone.json --out test/fixtures/spaces/dirt-stone.json
```

## Visual override sidecars

`render` accepts `--visual-overrides <file>` to replace resolved ground and/or cover visuals after the normal autotile report has been built. The sidecar can be an `overrides[]` list or coordinate-keyed object:

```json
{
  "overrides": [
    {
      "x": 0,
      "y": 1,
      "ground": { "tileset": "Humus", "spriteId": 4, "flipX": false },
      "cover": { "tileset": "GrassA", "spriteId": 2, "flipX": true, "rendered": true }
    }
  ]
}
```

Use this exact command to inspect a sidecar before rendering:

```bash
node src/cli.js list-visual-overrides --visual-overrides visual-overrides.json
```

## JSON formats (`project-twelve/tile-space/v1`)

| Kind | Purpose |
|------|---------|
| `snippet` | Small hand-authored grid with optional `expect[]` assertions |
| `space` | Bounded region (`xMin`…`yMax`) with sparse `tiles[]` |
| `world` | Procedural `generate{}` + `region{}` and/or `save` path + `edits[]` |

See [`test/fixtures/snippets/`](test/fixtures/snippets/) for examples.

## Parity with Unity

| Artifact | Unity exporter |
|----------|----------------|
| `data/autotile-rules.*.json` | `AutotileRulesFixtureExportTests.cs` |
| `data/tileset-manifest.json` | `AutotileFixtureExportTests.cs` |
| `test/fixtures/expected/*.json` | `AutotileFixtureExportTests.cs` |

Rule tables in `data/` are loaded by the Node resolver — do not edit by hand; regenerate
from Unity or `node scripts/generate-rules-json.mjs` when C# tables change.

## Visual override debug mode

Visual overrides are optional debug annotations applied after normal autotile resolution and before PNG compositing. They are useful for marking or temporarily substituting individual cells while diagnosing drift, but they do not represent world state and should not be treated as fixture truth.

Use `--visual-overrides <file.visual-overrides.json>` with render commands to apply an override file:

```bash
node src/cli.js render --space test/fixtures/captures/sandbox-scene-mountain.json \
  --assets-root ../../Assets/_Licensed/PixelFantasy/PixelTileEngine/Tiles \
  --visual-overrides out/sandbox-scene-mountain.visual-overrides.json \
  --scale 32 --flat-light --png out/sandbox-scene-mountain.override.png

node scripts/render-capture.mjs test/fixtures/captures/sandbox-scene-mountain.json \
  --visual-overrides out/sandbox-scene-mountain.visual-overrides.json \
  --png out/sandbox-scene-mountain.override.png --scale 32 --flat-light
```

List and inspect override files before attaching them to an RCA:

```bash
node src/cli.js visual-overrides list --file out/sandbox-scene-mountain.visual-overrides.json
node src/cli.js visual-overrides inspect --file out/sandbox-scene-mountain.visual-overrides.json --coords -114,29
```

Attach the `*.visual-overrides.json` file with the matching tile-space capture, rendered PNG, and per-cell autotile report so another agent can reproduce the exact visual hypothesis without changing the capture or resolver baseline.

## Tests

```bash
npm test
```

- `resolver.test.js` — rule table + mask resolution invariants
- `snippet.test.js` — all snippet fixtures match `expect[]`
- `render.test.js` — PNG golden (skipped without licensed assets)

Set `TILE_VIZ_ASSETS_ROOT` to override the default licensed tiles path for render tests.

## Drift RCA scripts

See [`docs/wiki/autotile-drift-rca.md`](../../docs/wiki/autotile-drift-rca.md) for the full playbook.

| Script | Purpose |
|--------|---------|
| `scripts/diff-tile-space.mjs` | Compare two space fixtures (world tile id drift) |
| `scripts/export-autotile-baseline.mjs` | Build sparse autotile baseline from a capture |
| `scripts/compare-autotile-baseline.mjs` | Field-level diff vs baseline (ground/cover) |
| `scripts/log-autotile-debug-cells.mjs` | MCP-style per-cell report (`--compact`, optional coords) |
| `scripts/render-capture.mjs` | Render space capture to PNG (`--scale 32 --flat-light`) |
| `scripts/diff-png.mjs` | Pixel diff two PNGs (Unity screenshot vs golden) |

Play Mode MCP companions: `world_export_tile_space`, `autotile_diff_baseline` (see [`AGENTS.md`](../../AGENTS.md)).
