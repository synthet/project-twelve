// End-to-end snippet fixtures with inline expect blocks.

import { test } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { loadTileSpaceFromFile } from '../src/io/tileSpace.js';
import { buildAutotileReport, assertExpectations } from '../src/report/autotileJson.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const SNIPPETS_DIR = path.join(__dirname, 'fixtures', 'snippets');

for (const file of fs.readdirSync(SNIPPETS_DIR).filter((f) => f.endsWith('.json'))) {
  test(`snippet fixture ${file}`, () => {
    const space = loadTileSpaceFromFile(path.join(SNIPPETS_DIR, file));
    const report = buildAutotileReport(space);
    const errors = assertExpectations(space, report);
    assert.equal(errors.length, 0, errors.join('\n'));
  });
}

test('tile-space v1 format field is recognized', () => {
  const space = loadTileSpaceFromFile(path.join(SNIPPETS_DIR, 'grass-cover-middle.json'));
  assert.equal(space.name, 'grass-cover-middle');
  assert.ok(space.tiles.size > 0);
});
