#!/usr/bin/env node
// Export a rectangular region of a Unity SandboxSaveData JSON save into a
// project-twelve/tile-space/v1 `space` fixture (only non-air tiles emitted).
//
// The save stores tile ids as runtime *palette* indices (see `tilePalette`),
// which differ from tile-viz TileId. This script remaps via the palette's
// canonical string ids so the fixture uses tile-viz TileId values.
//
// Usage:
//   node scripts/export-sandbox-scene.mjs <save.json> <out.json> \
//     [--name NAME] [--xmin N --xmax N --ymin N --ymax N]
// Without an explicit crop it exports the largest gap-free horizontal band of
// loaded chunks (contiguous chunk x range) over the full chunk height.

import fs from 'node:fs';
import path from 'node:path';

const CHUNK_SIZE = 32;

// Canonical string id -> tile-viz TileId (world-viz/src/core/tiles.js).
const STRING_ID_TO_TILE_ID = {
  'core:air': 0,
  'core:dirt': 1,
  'core:grass': 2,
  'core:stone': 3,
  'core:copper_ore': 4,
  'core:iron_ore': 5,
  'core:silver_ore': 6,
  'core:gold_ore': 7,
};

function parseArgs(argv) {
  const [savePath, outPath, ...rest] = argv;
  if (!savePath || !outPath) {
    throw new Error('usage: export-sandbox-scene.mjs <save.json> <out.json> [--name N] [--xmin/--xmax/--ymin/--ymax N]');
  }
  const opts = { savePath, outPath, name: null, xmin: null, xmax: null, ymin: null, ymax: null };
  for (let i = 0; i < rest.length; i += 2) {
    const key = rest[i].replace(/^--/, '');
    const val = rest[i + 1];
    opts[key] = key === 'name' ? val : Number(val);
  }
  return opts;
}

function buildPalette(save) {
  const entries = save.tilePalette?.entries;
  if (!Array.isArray(entries)) {
    // Legacy saves already store canonical tile-viz ids.
    return null;
  }
  const map = new Map();
  for (const e of entries) {
    const tileId = STRING_ID_TO_TILE_ID[e.id];
    if (tileId === undefined) {
      throw new Error(`Unknown palette string id "${e.id}"`);
    }
    map.set(e.runtimeIndex, tileId);
  }
  return map;
}

function main() {
  const opts = parseArgs(process.argv.slice(2));
  const save = JSON.parse(fs.readFileSync(opts.savePath, 'utf8'));
  const palette = buildPalette(save);

  // Materialize world tiles from chunk edits.
  const cells = new Map(); // "x,y" -> tileId (tile-viz), plus a light lookup
  const light = new Map();
  const chunkXs = new Set();
  for (const chunk of save.chunks ?? []) {
    chunkXs.add(chunk.x);
    for (const edit of chunk.edits ?? []) {
      const wx = chunk.x * CHUNK_SIZE + edit.localX;
      const wy = chunk.y * CHUNK_SIZE + edit.localY;
      const rawId = edit.tile?.id ?? 0;
      const tileId = palette ? palette.get(rawId) ?? 0 : rawId;
      const key = `${wx},${wy}`;
      cells.set(key, tileId);
      light.set(key, edit.tile?.light ?? 0);
    }
  }

  // Default crop: largest contiguous chunk-x band, full loaded height.
  let { xmin, xmax, ymin, ymax } = opts;
  if (xmin === null || xmax === null) {
    const sorted = [...chunkXs].sort((a, b) => a - b);
    let bestStart = sorted[0];
    let bestLen = 1;
    let curStart = sorted[0];
    let curLen = 1;
    for (let i = 1; i < sorted.length; i++) {
      if (sorted[i] === sorted[i - 1] + 1) {
        curLen++;
      } else {
        curStart = sorted[i];
        curLen = 1;
      }
      if (curLen > bestLen) {
        bestLen = curLen;
        bestStart = curStart;
      }
    }
    xmin = bestStart * CHUNK_SIZE;
    xmax = (bestStart + bestLen) * CHUNK_SIZE - 1;
  }

  // Vertical bounds: default to the non-air extent within the x crop.
  const tiles = [];
  let minY = Infinity;
  let maxY = -Infinity;
  for (const [key, tileId] of cells) {
    if (tileId === 0) continue;
    const [x, y] = key.split(',').map(Number);
    if (x < xmin || x > xmax) continue;
    if (ymin !== null && y < ymin) continue;
    if (ymax !== null && y > ymax) continue;
    tiles.push({ x, y, id: tileId, light: light.get(key) ?? 0 });
    if (y < minY) minY = y;
    if (y > maxY) maxY = y;
  }
  const yLo = ymin !== null ? ymin : minY;
  const yHi = ymax !== null ? ymax : maxY;

  tiles.sort((a, b) => (b.y - a.y) || (a.x - b.x));

  const name = opts.name ?? path.basename(opts.outPath, '.json');
  const out = {
    format: 'project-twelve/tile-space/v1',
    kind: 'space',
    name,
    xMin: xmin,
    yMin: yLo,
    xMax: xmax,
    yMax: yHi,
    tiles,
  };
  fs.mkdirSync(path.dirname(path.resolve(opts.outPath)), { recursive: true });
  fs.writeFileSync(opts.outPath, `${JSON.stringify(out, null, 2)}\n`);
  process.stderr.write(
    `wrote ${opts.outPath}: ${tiles.length} solid tiles, x[${xmin}..${xmax}] y[${yLo}..${yHi}]\n`,
  );
}

main();
