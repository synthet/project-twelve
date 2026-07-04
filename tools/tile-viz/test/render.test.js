// PNG golden compare — requires licensed assets on disk.

import { test } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { loadTileSpaceFromFile } from '../src/io/tileSpace.js';
import { renderAutotilePng } from '../src/render/autotilePng.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, '..', '..', '..');
const DEFAULT_ASSETS = path.join(REPO_ROOT, 'Assets', '_Licensed', 'PixelFantasy', 'PixelTileEngine', 'Tiles');
const RENDER_DIR = path.join(__dirname, 'fixtures', 'render');
const SNIPPET = path.join(__dirname, 'fixtures', 'snippets', 'grass-cover-middle.json');

const assetsRoot = process.env.TILE_VIZ_ASSETS_ROOT ?? DEFAULT_ASSETS;
const humusPath = path.join(assetsRoot, 'Ground', 'Humus.png');
const assetsAvailable = fs.existsSync(humusPath);

test('render grass-cover-middle PNG matches golden hash', {
  skip: assetsAvailable ? false : `licensed assets not found at ${humusPath} (set TILE_VIZ_ASSETS_ROOT)`,
}, () => {
  const goldenPath = path.join(RENDER_DIR, 'grass-cover-middle.png');
  const goldenExists = fs.existsSync(goldenPath);
  if (!goldenExists) {
    const space = loadTileSpaceFromFile(SNIPPET);
    const png = renderAutotilePng(space, { assetsRoot, scale: 16, flatLight: true });
    fs.mkdirSync(RENDER_DIR, { recursive: true });
    fs.writeFileSync(goldenPath, png);
  }

  const space = loadTileSpaceFromFile(SNIPPET);
  const png = renderAutotilePng(space, { assetsRoot, scale: 16, flatLight: true });
  const golden = fs.readFileSync(goldenPath);
  assert.equal(png.length, golden.length, 'PNG byte length differs from golden');
  assert.deepEqual(png, golden);
});
