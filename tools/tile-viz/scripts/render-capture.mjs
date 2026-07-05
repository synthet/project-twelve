#!/usr/bin/env node
// Render a tile-space capture to PNG using licensed sprite sheets.
//
// Usage:
//   node scripts/render-capture.mjs <space.json> --png out.png [--scale 32] [--flat-light] [--no-cover]

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
    scale: 32,
    flatLight: false,
    noCover: false,
    assetsRoot: process.env.TILE_VIZ_ASSETS_ROOT ?? DEFAULT_ASSETS,
  };

  for (let i = 0; i < argv.length; i++) {
    const arg = argv[i];
    if (arg === '--png' && argv[i + 1]) {
      opts.png = argv[++i];
    } else if (arg === '--scale' && argv[i + 1]) {
      opts.scale = Number(argv[++i]);
    } else if (arg === '--assets-root' && argv[i + 1]) {
      opts.assetsRoot = argv[++i];
    } else if (arg === '--flat-light') {
      opts.flatLight = true;
    } else if (arg === '--no-cover') {
      opts.noCover = true;
    } else if (!arg.startsWith('--')) {
      positional.push(arg);
    }
  }

  return { positional, opts };
}

function main() {
  const { positional, opts } = parseArgs(process.argv.slice(2));
  if (positional.length < 1 || !opts.png) {
    console.error(
      'usage: render-capture.mjs <space.json> --png out.png [--scale 32] [--flat-light] [--no-cover]',
    );
    process.exit(2);
  }

  const space = loadTileSpaceFromFile(path.resolve(positional[0]));
  const png = renderAutotilePng(space, {
    assetsRoot: opts.assetsRoot,
    scale: opts.scale,
    flatLight: opts.flatLight,
    noCover: opts.noCover,
  });

  fs.mkdirSync(path.dirname(path.resolve(opts.png)), { recursive: true });
  fs.writeFileSync(opts.png, png);
  console.error(`wrote ${opts.png} (${png.length} bytes)`);
}

main();
