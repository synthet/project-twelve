import { test } from 'node:test';
import assert from 'node:assert/strict';

import { buildAutotileReport } from '../src/report/autotileJson.js';
import { loadTileSpace } from '../src/io/tileSpace.js';
import { applyVisualOverrides, listVisualOverrides } from '../src/render/autotilePng.js';

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
