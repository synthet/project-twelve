// End-to-end snippet fixtures with inline expect blocks.

import { test } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { getTile, loadTileSpace, loadTileSpaceFromFile } from '../src/io/tileSpace.js';
import { buildAutotileReport, assertExpectations } from '../src/report/autotileJson.js';
import { TileId } from '../../world-viz/src/core/tiles.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const SNIPPETS_DIR = path.join(__dirname, 'fixtures', 'snippets');

for (const file of fs.readdirSync(SNIPPETS_DIR).filter((f) => f.endsWith('.json'))) {
  test(`snippet fixture ${file}`, () => {
    const space = loadTileSpaceFromFile(path.join(SNIPPETS_DIR, file));
    const report = buildAutotileReport(space);
    const baselineErrors = assertExpectations(space, report, { which: 'baseline' });
    assert.equal(baselineErrors.length, 0, baselineErrors.join('\n'));
    if (space.targetExpect?.length) {
      const targetErrors = assertExpectations(space, report, { which: 'target' });
      assert.equal(targetErrors.length, 0, targetErrors.join('\n'));
    }
  });
}

test('tile-space v1 format field is recognized', () => {
  const space = loadTileSpaceFromFile(path.join(SNIPPETS_DIR, 'grass-cover-middle.json'));
  assert.equal(space.name, 'grass-cover-middle');
  assert.ok(space.tiles.size > 0);
});

test('tile-space v1 legacy ids normalize to current runtime ids', () => {
  const space = loadTileSpace({
    format: 'project-twelve/tile-space/v1',
    kind: 'space',
    name: 'all-ground-materials',
    xMin: 0,
    yMin: 0,
    xMax: 10,
    yMax: 0,
    tiles: Array.from({ length: 11 }, (_, id) => ({ x: id, y: 0, id })),
  });

  const expectedRuntimeIds = [
    TileId.Air,
    TileId.Dirt,
    TileId.Grass,
    TileId.Stone,
    TileId.BricksA,
    TileId.BricksB,
    TileId.BricksC,
    TileId.BricksD,
    TileId.Frozen,
    TileId.Magma,
    TileId.Sand,
  ];
  for (let x = 0; x < expectedRuntimeIds.length; x++) {
    assert.equal(getTile(space, x, 0).id, expectedRuntimeIds[x]);
  }
});
