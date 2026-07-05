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

const assetsRoot = process.env.TILE_VIZ_ASSETS_ROOT ?? DEFAULT_ASSETS;
const humusPath = path.join(assetsRoot, 'Ground', 'Humus.png');
const assetsAvailable = fs.existsSync(humusPath);
const renderFixtures = [
  'grass-cover-middle',
  'slope-ascending-stair-run',
  'slope-descending-stair-run',
  'slope-ascending-long',
  'slope-descending-long',
  'material-boundary-horizontal',
  'dirt-stone-reentrant-west',
];

for (const fixture of renderFixtures) {
  test(`render ${fixture} PNG matches golden hash`, {
    skip: assetsAvailable ? false : `licensed assets not found at ${humusPath} (set TILE_VIZ_ASSETS_ROOT)`,
  }, () => {
    const snippetPath = path.join(__dirname, 'fixtures', 'snippets', `${fixture}.json`);
    const goldenPath = path.join(RENDER_DIR, `${fixture}.png`);
    const goldenExists = fs.existsSync(goldenPath);
    if (!goldenExists) {
      const space = loadTileSpaceFromFile(snippetPath);
      const png = renderAutotilePng(space, { assetsRoot, scale: 16, flatLight: true });
      fs.mkdirSync(RENDER_DIR, { recursive: true });
      fs.writeFileSync(goldenPath, png);
    }

    const space = loadTileSpaceFromFile(snippetPath);
    const png = renderAutotilePng(space, { assetsRoot, scale: 16, flatLight: true });
    const golden = fs.readFileSync(goldenPath);
    assert.equal(png.length, golden.length, 'PNG byte length differs from golden');
    assert.deepEqual(png, golden);
  });
}
