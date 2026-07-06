import { test } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { PNG } from 'pngjs';

import { loadTileSpace, loadTileSpaceFromFile } from '../src/io/tileSpace.js';
import { buildAutotileReport } from '../src/report/autotileJson.js';
import { applyVisualOverrides, listVisualOverrides, renderAutotilePng } from '../src/render/autotilePng.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, '..', '..', '..');
const DEFAULT_ASSETS = path.join(REPO_ROOT, 'Assets', '_Licensed', 'PixelFantasy', 'PixelTileEngine', 'Tiles');
const assetsRoot = process.env.TILE_VIZ_ASSETS_ROOT ?? DEFAULT_ASSETS;
const humusPath = path.join(assetsRoot, 'Ground', 'Humus.png');
const assetsAvailable = fs.existsSync(humusPath);
const fixturePath = path.join(__dirname, 'fixtures', 'snippets', 'visual-override-ground-flipx.json');

const space = {
  format: 'project-twelve/tile-space/v1',
  kind: 'snippet',
  name: 'visual-overrides-test',
  xMin: 0,
  yMin: 0,
  xMax: 0,
  yMax: 0,
  width: 1,
  height: 1,
  rows: ['G'],
};

function tileDiffs(leftBuffer, rightBuffer, scale) {
  const left = PNG.sync.read(leftBuffer);
  const right = PNG.sync.read(rightBuffer);
  assert.equal(left.width, right.width);
  assert.equal(left.height, right.height);
  const changed = new Set();
  for (let y = 0; y < left.height; y++) {
    for (let x = 0; x < left.width; x++) {
      const i = (left.width * y + x) * 4;
      if (
        left.data[i] !== right.data[i]
        || left.data[i + 1] !== right.data[i + 1]
        || left.data[i + 2] !== right.data[i + 2]
        || left.data[i + 3] !== right.data[i + 3]
      ) {
        changed.add(`${Math.floor(x / scale)},${Math.floor(y / scale)}`);
      }
    }
  }
  return [...changed].sort();
}

test('applyVisualOverrides replaces resolved ground and cover visuals by coordinate', () => {
  const report = buildAutotileReport(loadTileSpace(space), { includeAir: true });

  applyVisualOverrides(report, {
    overrides: [
      {
        x: 0,
        y: 0,
        ground: { tileset: 'Rocks', spriteId: 12, flipX: true },
        cover: { tileset: 'GrassA', spriteId: 3, flipX: false, rendered: true },
      },
    ],
  });

  const tile = report.tiles.find((t) => t.x === 0 && t.y === 0);
  assert.equal(tile.autotile.ground.tileset, 'Rocks');
  assert.equal(tile.autotile.ground.spriteId, 12);
  assert.equal(tile.autotile.ground.flipX, true);
  assert.equal(tile.autotile.ground.visualOverride, true);
  assert.equal(tile.autotile.cover.rendered, true);
  assert.equal(tile.autotile.cover.spriteId, 3);
  assert.equal(tile.autotile.cover.visualOverride, true);
});

test('listVisualOverrides accepts coordinate-keyed sidecars', () => {
  const entries = listVisualOverrides({
    '2,-1': {
      ground: { tileset: 'Humus', spriteId: 4, flipX: false },
    },
  });

  assert.deepEqual(entries, [
    { x: 2, y: -1, layers: ['ground tileset=Humus spriteId=4 flipX=false'] },
  ]);
});

test('visual override report preserves auto snapshot and exposes override decision fields', () => {
  const spaceWithOverrides = loadTileSpaceFromFile(fixturePath);
  const report = buildAutotileReport(spaceWithOverrides);
  const tile = report.tiles.find((entry) => entry.x === 1 && entry.y === 1);

  assert.equal(tile.autotile.ground.auto.spriteId, '31');
  assert.equal(tile.autotile.ground.auto.flipX, true);
  assert.equal(tile.autotile.ground.override.spriteId, '17');
  assert.equal(tile.autotile.ground.override.flipX, false);
  assert.equal(tile.autotile.ground.override.flipY, false);
  assert.equal(tile.autotile.ground.override.rotationDegrees, 0);
  assert.equal(tile.autotile.ground.spriteId, '17');
  assert.equal(tile.autotile.ground.flipX, false);
  assert.equal(tile.autotile.ground.overrideApplied, true);
});

test('visual override PNG changes only the overridden cell', {
  skip: assetsAvailable ? false : `licensed assets not found at ${humusPath} (set TILE_VIZ_ASSETS_ROOT)`,
}, () => {
  const scale = 16;
  const baseDoc = JSON.parse(fs.readFileSync(fixturePath, 'utf8'));
  const baseline = loadTileSpace({ ...baseDoc, visualOverrides: [] }, path.dirname(fixturePath));
  const overridden = loadTileSpaceFromFile(fixturePath);

  const baselinePng = renderAutotilePng(baseline, { assetsRoot, scale, flatLight: true });
  const overridePng = renderAutotilePng(overridden, { assetsRoot, scale, flatLight: true });

  assert.deepEqual(tileDiffs(baselinePng, overridePng, scale), ['1,1']);
});
