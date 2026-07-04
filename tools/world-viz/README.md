# world-viz — offline world visualization / debug tool

Render a ProjectTwelve world **without the Unity engine**, from either a random seed
or a JSON save game, to a **PNG**, a **self-contained interactive HTML** page, and/or
an **ASCII** dump. The goal is a fast offline loop for reasoning about generation logic
— terrain shape, dirt/stone banding, the light seed, save-edit overlays — and for
checking hypotheses before changing the engine.

Pure Node, **zero external dependencies** (PNG via the built-in `zlib`, tests via
`node --test`). It uses flat debug colours only and never touches licensed art.

## Usage

```bash
cd tools/world-viz

# From a seed (defaults mirror SandboxWorld: seed 1337, surface 28, amp 8, freq 0.06, dirt 8)
node src/cli.js generate --seed 1337 --png out.png --html out.html --ascii out.txt

# Tweak generation params and the rendered region (world tile coords, inclusive)
node src/cli.js generate --seed 42 --frequency 0.12 --amplitude 14 \
  --min-x -128 --max-x 128 --min-y 0 --max-y 64 --scale 6 --png hills.png

# From a save game (regenerates from the saved seed, overlays saved tile edits)
node src/cli.js load --save /path/to/sandbox-world.json --html save.html --ascii -
```

`--ascii -` prints to stdout. Run `node src/cli.js --help` for the full option list.

### Outputs

- **PNG** — flat-colour raster at `--scale` pixels per tile.
- **HTML** — drag to pan, scroll to zoom, hover any tile to inspect its world/chunk
  coords, id, light and fluid, toggle a light heatmap, and move **live sliders** for
  seed / surfaceHeight / amplitude / frequency / dirtDepth to regenerate in-browser.
  The page inlines the *same* generator code the CLI runs, so the two cannot diverge.
- **ASCII** — a text grid (`.`=Air `#`=Dirt `"`=Grass `%`=Stone `c/i/s/g`=ores) plus a
  per-column surface-height table.

Rows are ordered top-down with higher world-Y at the top, matching in-game orientation.

## What it mirrors

The `src/core/` modules are a faithful port of `Assets/Scripts/Sandbox/`:

| Tool module | Engine source |
|-------------|---------------|
| `core/generator.js` | `SandboxTerrainGenerator.cs` |
| `core/tiles.js` | `SandboxTile.cs` (ids) + `SandboxChunkRenderer` light model |
| `core/mathf.js` | `Mathf.RoundToInt` (banker's rounding), `FloorDiv`/`Mod` |
| `core/world.js` | `SandboxWorld` coord math + chunk/edit assembly |
| `io/saveLoad.js` | `SandboxSaveData.cs` JSON schema |

Only edited chunks are stored in a save; everything else regenerates from the seed.

## Autotile rendering (`tools/tile-viz`)

For **sprite-sheet autotile** resolution and PNG compositing (same mask/resolver logic as
`Assets/Scripts/Visual/Tiles/`), use the sibling package [`../tile-viz`](../tile-viz).
world-viz stays flat-colour for fast terrain inspection; tile-viz loads JSON snippets/spaces/worlds
and optional licensed PNGs under `--assets-root`.

## Fidelity & the golden fixture

The one piece that cannot be ported by inspection is Unity's `Mathf.PerlinNoise`
(native code). `core/perlin.js` is a documented, isolated approximation (Ken Perlin's
improved noise, float32-emulated). Exact parity is enforced by a **Unity-authored
golden fixture**:

- `Assets/Tests/EditMode/TerrainFixtureExportTests.cs` exports
  `test/fixtures/surface.seed1337.json` from the real engine.
- `test/parity.test.js` asserts the JS port reproduces it exactly.

If parity ever fails, `core/perlin.js` is the single file to adjust. Until the fixture
is generated (Unity is required), the parity test is skipped with a message.

## Tests

```bash
node --test            # logic + coord + overlay tests; parity test gates on the fixture
```
