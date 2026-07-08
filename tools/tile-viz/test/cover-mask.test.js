// Cover mask neighbor values and integrated snippet parity.

import { test } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { buildCoverMask } from '../src/visual/maskBuilder.js';
import { resolveSpriteId } from '../src/visual/resolver.js';
import { loadRuleTables } from '../src/visual/ruleTables.js';
import { loadTileSpaceFromFile } from '../src/io/tileSpace.js';
import { buildAutotileReport } from '../src/report/autotileJson.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const DATA_DIR = path.join(__dirname, '..', 'data');

test('buildCoverMask neighbor value 2 resolves cover sprite 2', () => {
  const tables = loadRuleTables(DATA_DIR);
  const solid = new Set([
    '0,3', '0,2', '0,1', '0,0',
    '1,3', '1,2', '1,1', '1,0',
    '2,2', '2,1', '2,0',
    '3,3', '3,2', '3,1', '3,0',
    '4,3', '4,2', '4,1', '4,0',
  ]);
  const isSolid = (x, y) => solid.has(`${x},${y}`);
  const mask = buildCoverMask(isSolid, 2, 2);
  assert.deepEqual(mask[0], [0, 2, 0]);
  assert.deepEqual(mask[2], [0, 2, 0]);
  const tileset = { spriteCount: 6, rules: tables.cover.rules };
  const result = resolveSpriteId(tileset, mask);
  assert.equal(result.spriteId, '2');
});

test('cover-cliff-step-nook snippet matches mask value 2 integration', () => {
  const space = loadTileSpaceFromFile(
    path.join(__dirname, 'fixtures', 'snippets', 'cover-cliff-step-nook.json'),
  );
  const report = buildAutotileReport(space);
  const cell = report.tiles.find((t) => t.x === 2 && t.y === 2);
  assert.equal(cell.autotile.cover.spriteId, '2');
  assert.deepEqual(cell.autotile.cover.mask[0], [0, 2, 0]);
  assert.deepEqual(cell.autotile.cover.mask[2], [0, 2, 0]);
});

test('cover-riser-suppressed snippet has rendered false on interior riser', () => {
  const space = loadTileSpaceFromFile(
    path.join(__dirname, 'fixtures', 'snippets', 'cover-riser-suppressed.json'),
  );
  const report = buildAutotileReport(space);
  const riser = report.tiles.find((t) => t.x === 1 && t.y === 1);
  assert.equal(riser.autotile.cover.rendered, false);
});
