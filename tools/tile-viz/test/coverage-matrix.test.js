// Branch coverage manifest: every logic path maps to at least one integrated fixture.

import { test } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { loadTileSpaceFromFile } from '../src/io/tileSpace.js';
import { buildAutotileReport, assertExpectations } from '../src/report/autotileJson.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const FIXTURES = path.join(__dirname, 'fixtures');
const SNIPPETS = path.join(FIXTURES, 'snippets');
const matrix = JSON.parse(fs.readFileSync(path.join(FIXTURES, 'coverage-matrix.json'), 'utf8'));

function snippetPath(name) {
  return path.join(SNIPPETS, `${name}.json`);
}

function reportAt(name) {
  const space = loadTileSpaceFromFile(snippetPath(name));
  const report = buildAutotileReport(space);
  const byKey = new Map(report.tiles.map((t) => [`${t.x},${t.y}`, t]));
  return { space, report, byKey };
}

for (const [branch, fixtures] of Object.entries(matrix.branches)) {
  test(`coverage branch ${branch} references existing fixtures`, () => {
    assert.ok(fixtures.length > 0, `${branch} must list at least one fixture`);
    for (const name of fixtures) {
      assert.ok(fs.existsSync(snippetPath(name)), `missing snippet for ${branch}: ${name}.json`);
    }
  });
}

const snippetFixtures = new Set(Object.values(matrix.branches).flat());

for (const name of snippetFixtures) {
  test(`coverage fixture ${name} passes baselineExpect`, () => {
    const space = loadTileSpaceFromFile(snippetPath(name));
    const report = buildAutotileReport(space);
    const errors = assertExpectations(space, report, { which: 'baseline' });
    assert.equal(errors.length, 0, errors.join('\n'));
  });
}

test('column-mirror-ground resolves sprite 24 not bridge 25', () => {
  const { byKey } = reportAt('column-mirror-ground');
  const g = byKey.get('1,2')?.autotile?.ground;
  assert.equal(g?.spriteId, '24');
  assert.equal(g?.flipX, true);
  assert.notEqual(g?.spriteId, '25');
});

test('cover-cliff-step-nook uses cover sprite 2 (neighbor mask value 2)', () => {
  const { byKey } = reportAt('cover-cliff-step-nook');
  const cover = byKey.get('2,2')?.autotile?.cover;
  assert.equal(cover?.rendered, true);
  assert.equal(cover?.spriteId, '2');
  assert.deepEqual(cover?.mask?.[0], [0, 2, 0]);
  assert.deepEqual(cover?.mask?.[2], [0, 2, 0]);
});

test('bricks-dirt-boundary keeps Bricks A and dirt as separate exterior caps', () => {
  const { byKey } = reportAt('bricks-dirt-boundary');
  const copper = byKey.get('0,2')?.autotile?.ground;
  const dirt = byKey.get('1,2')?.autotile?.ground;
  assert.equal(copper?.spriteId, '28');
  assert.equal(dirt?.spriteId, '28');
  assert.equal(copper?.tileset, 'BricksA');
  assert.equal(dirt?.tileset, 'Humus');
});

test('bricks-mixed-boundary keeps Bricks A and Bricks B in separate tilesets', () => {
  const { byKey } = reportAt('bricks-mixed-boundary');
  assert.equal(byKey.get('0,1')?.autotile?.ground?.tileset, 'BricksA');
  assert.equal(byKey.get('1,1')?.autotile?.ground?.tileset, 'BricksB');
});

test('one-sided-house-lip anti-regression: sprite 24 not 25', () => {
  const { space, byKey } = reportAt('one-sided-house-lip');
  const expects = space.baselineExpect ?? [];
  for (const exp of expects) {
    const g = byKey.get(`${exp.x},${exp.y}`)?.autotile?.ground;
    assert.equal(g?.spriteId, '24');
    assert.notEqual(g?.spriteId, '25');
  }
});
