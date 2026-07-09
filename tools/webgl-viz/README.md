# webgl-viz — browser proof-of-concept viewer

`webgl-viz` is the first runnable implementation of the WebGL prototype viewer. It packages a
`project-twelve/tile-space/v1` fixture into a static browser page that renders flat debug tiles with
WebGL, supports pan/zoom, tile inspection, overlay toggles, drag/drop of compatible payload JSON, and
an evidence JSON export.

It deliberately reuses the existing `tools/world-viz` tile colours/coordinate math and
`tools/tile-viz` tile-space loader instead of creating another terrain or autotile implementation.
Unity remains the source of truth; this tool is a review/debug surface that runs without Unity and
without licensed art.

## Usage

```bash
cd tools/webgl-viz
npm test
npm run build
python3 -m http.server 8080 --directory dist
```

Open <http://127.0.0.1:8080/>. The default build embeds
`tools/tile-viz/test/fixtures/snippets/grass-cover-middle.json`.

Build with a different fixture:

```bash
npm run build -- --space ../tile-viz/test/fixtures/spaces/platform-grass-corners.json
```

The build writes:

- `dist/index.html` — self-contained WebGL viewer.
- `dist/payload.json` — normalized tile payload suitable for drag/drop or review attachments.
