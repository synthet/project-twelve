import { test } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { loadTileSpaceFromFile } from '../src/io/tileSpace.js';
import { buildAutotileReport } from '../src/report/autotileJson.js';
import {
  buildBaselineDocument,
  compareAutotileBaseline,
  diffCompareCells,
  extractCompareCell,
} from '../src/report/autotileCompare.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const MOUNTAIN = path.join(__dirname, 'fixtures', 'captures', 'sandbox-scene-mountain.json');

test('extractCompareCell normalizes ground and cover fields', () => {
  const space = loadTileSpaceFromFile(MOUNTAIN);
  const report = buildAutotileReport(space);
  const tile = report.tiles.find((t) => t.x === -113 && t.y === 26);
  assert.ok(tile);
  const cell = extractCompareCell(tile);
  assert.equal(cell.ground.spriteId, '0');
  assert.equal(cell.ground.flipX, false);
});

test('compareAutotileBaseline self-compare has zero mismatches', () => {
  const space = loadTileSpaceFromFile(MOUNTAIN);
  const report = buildAutotileReport(space);
  const baseline = buildBaselineDocument(space, report);
  const result = compareAutotileBaseline(baseline, baseline, { maxDiffs: 10 });
  assert.equal(result.summary.mismatched, 0);
  assert.equal(result.summary.missingInActual, 0);
  assert.equal(result.diffs.length, 0);
});

test('diffCompareCells detects spriteId drift', () => {
  const expected = {
    tileId: 1,
    ground: { spriteId: '17', flipX: false, normalization: { innerCavity: true } },
    cover: { rendered: false },
  };
  const actual = {
    tileId: 1,
    ground: { spriteId: '18', flipX: false, normalization: { innerCavity: true } },
    cover: { rendered: false },
  };
  const errors = diffCompareCells(expected, actual, { only: 'ground' });
  assert.ok(errors.some((e) => e.includes('ground.spriteId')));
});

test('compareAutotileBaseline respects coord filter', () => {
  const space = loadTileSpaceFromFile(MOUNTAIN);
  const report = buildAutotileReport(space);
  const baseline = buildBaselineDocument(space, report);
  const mutated = JSON.parse(JSON.stringify(baseline));
  mutated.cells[0].ground.spriteId = '99';
  const result = compareAutotileBaseline(baseline, mutated, {
    coords: new Set(['99999,99999']),
    maxDiffs: 5,
  });
  assert.equal(result.summary.compared, 0);
});
