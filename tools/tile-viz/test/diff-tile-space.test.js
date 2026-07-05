import { test } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { loadTileSpaceFromFile } from '../src/io/tileSpace.js';
import { diffTileSpaces } from '../src/io/diffTileSpace.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const MOUNTAIN = path.join(__dirname, 'fixtures', 'captures', 'sandbox-scene-mountain.json');
const LIVE = path.join(__dirname, 'fixtures', 'captures', 'sandbox-world-live.json');

test('diffTileSpaces identical fixture has zero diffs', () => {
  const space = loadTileSpaceFromFile(MOUNTAIN);
  const result = diffTileSpaces(space, space);
  assert.equal(result.count, 0);
  assert.equal(result.examples.length, 0);
});

test('diffTileSpaces reports tile id changes', () => {
  const a = loadTileSpaceFromFile(MOUNTAIN);
  const b = loadTileSpaceFromFile(MOUNTAIN);
  b.tiles.set('-114,29', { id: 99, light: 4, fluid: 0, metadata: 0 });
  const result = diffTileSpaces(a, b, { maxExamples: 5 });
  assert.equal(result.count, 1);
  assert.equal(result.examples[0].x, -114);
  assert.equal(result.examples[0].y, 29);
});

test('diffTileSpaces mountain vs live summary is JSON-serializable', () => {
  const mountain = loadTileSpaceFromFile(MOUNTAIN);
  const live = loadTileSpaceFromFile(LIVE);
  const result = diffTileSpaces(mountain, live, { maxExamples: 3 });
  assert.ok(typeof result.count === 'number');
  assert.ok(result.overlap);
});
