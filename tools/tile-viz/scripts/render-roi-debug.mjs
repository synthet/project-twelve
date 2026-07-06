#!/usr/bin/env node
// Render a rectangular ROI from a tile-space capture with optional sprite-id labels.
//
// Usage:
//   node scripts/render-roi-debug.mjs <space.json> --png out.png
//     --xMin -118 --yMin 24 --xMax -110 --yMax 31
//     [--scale 48] [--flat-light] [--no-cover] [--annotate]

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { loadTileSpaceFromFile } from '../src/io/tileSpace.js';
import { renderAutotilePng } from '../src/render/autotilePng.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, '..', '..', '..');
const DEFAULT_ASSETS = path.join(REPO_ROOT, 'Assets', '_Licensed', 'PixelFantasy', 'PixelTileEngine', 'Tiles');

function parseArgs(argv) {
  const positional = [];
  const opts = {
    png: null,
    xMin: null,
    yMin: null,
    xMax: null,
    yMax: null,
    scale: 48,
    flatLight: true,
    noCover: false,
    annotate: false,
    assetsRoot: process.env.TILE_VIZ_ASSETS_ROOT ?? DEFAULT_ASSETS,
  };

  for (let i = 0; i < argv.length; i++) {
    const arg = argv[i];
    if (arg === '--png' && argv[i + 1]) {
      opts.png = argv[++i];
    } else if (arg === '--xMin' && argv[i + 1]) {
      opts.xMin = Number(argv[++i]);
    } else if (arg === '--yMin' && argv[i + 1]) {
      opts.yMin = Number(argv[++i]);
    } else if (arg === '--xMax' && argv[i + 1]) {
      opts.xMax = Number(argv[++i]);
    } else if (arg === '--yMax' && argv[i + 1]) {
      opts.yMax = Number(argv[++i]);
    } else if (arg === '--scale' && argv[i + 1]) {
      opts.scale = Number(argv[++i]);
    } else if (arg === '--assets-root' && argv[i + 1]) {
      opts.assetsRoot = argv[++i];
    } else if (arg === '--flat-light') {
      opts.flatLight = true;
    } else if (arg === '--no-cover') {
      opts.noCover = true;
    } else if (arg === '--annotate') {
      opts.annotate = true;
    } else if (!arg.startsWith('--')) {
      positional.push(arg);
    }
  }

  return { positional, opts };
}

function cropSpace(space, xMin, yMin, xMax, yMax) {
  const tiles = new Map();
  for (const [key, tile] of space.tiles.entries()) {
    const [x, y] = key.split(',').map(Number);
    if (x >= xMin && x <= xMax && y >= yMin && y <= yMax) {
      tiles.set(key, tile);
    }
  }
  return {
    ...space,
    xMin,
    yMin,
    xMax,
    yMax,
    tiles,
  };
}

function main() {
  const { positional, opts } = parseArgs(process.argv.slice(2));
  if (
    positional.length < 1
    || !opts.png
    || opts.xMin == null
    || opts.yMin == null
    || opts.xMax == null
    || opts.yMax == null
  ) {
    console.error(
      'usage: render-roi-debug.mjs <space.json> --png out.png --xMin N --yMin N --xMax N --yMax N [--scale 48] [--annotate] [--no-cover]',
    );
    process.exit(2);
  }

  const full = loadTileSpaceFromFile(path.resolve(positional[0]));
  const cropped = cropSpace(full, opts.xMin, opts.yMin, opts.xMax, opts.yMax);
  const png = renderAutotilePng(cropped, {
    assetsRoot: opts.assetsRoot,
    scale: opts.scale,
    flatLight: opts.flatLight,
    noCover: opts.noCover,
    annotateGround: opts.annotate,
  });

  fs.mkdirSync(path.dirname(path.resolve(opts.png)), { recursive: true });
  fs.writeFileSync(opts.png, png);
  console.error(
    `wrote ${opts.png} (${png.length} bytes) roi [${opts.xMin},${opts.yMin}]..[${opts.xMax},${opts.yMax}] annotate=${opts.annotate}`,
  );
}

main();
