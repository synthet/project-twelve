// Integration spot checks for sandbox-scene-mountain capture.

import { test } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { loadTileSpaceFromFile } from '../src/io/tileSpace.js';
import { buildAutotileReport } from '../src/report/autotileJson.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const CAPTURE = path.join(__dirname, 'fixtures', 'captures', 'sandbox-scene-mountain.json');

const spotChecks = [
  { x: -113, y: 26, ground: { spriteId: '2', flipX: false } },
  { x: -112, y: 26, ground: { spriteId: '2', flipX: false } },
  { x: -117, y: 25, ground: { spriteId: '17', flipX: false } },
  { x: -90, y: 12, ground: { spriteId: '9', flipX: false } },
];

test('sandbox-scene-mountain integration spot checks', () => {
  const space = loadTileSpaceFromFile(CAPTURE);
  const report = buildAutotileReport(space);
  assert.equal(report.name, 'sandbox-scene-mountain');
  assert.ok(report.tiles.length > 0);

  for (const check of spotChecks) {
    const tile = report.tiles.find((t) => t.x === check.x && t.y === check.y);
    assert.ok(tile, `(${check.x},${check.y}) missing from report`);
    if (check.ground?.spriteId) {
      assert.equal(
        tile.autotile.ground?.spriteId,
        check.ground.spriteId,
        `(${check.x},${check.y}) ground sprite`,
      );
    }
    if (check.ground?.flipX !== undefined) {
      assert.equal(tile.autotile.ground?.flipX, check.ground.flipX);
    }
  }
});

test('sandbox-scene-mountain exposes dual-mask debug fields', () => {
  const space = loadTileSpaceFromFile(CAPTURE);
  const report = buildAutotileReport(space);
  const sample = report.tiles.find((t) => t.autotile.ground?.visualMask);
  assert.ok(sample, 'expected at least one ground tile with visualMask');
  assert.ok(sample.autotile.ground.connectivityMask);
  assert.ok(sample.autotile.ground.normalization);
});
