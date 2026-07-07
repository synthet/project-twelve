// Regression fixtures for inner-cavity, roof slope chirality, and one-sided lips.
// Captures current tile-viz resolver output in halo-correct snippet context (baseline for Unity parity).

import { test } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { loadTileSpaceFromFile } from '../src/io/tileSpace.js';
import { buildAutotileReport, assertExpectations } from '../src/report/autotileJson.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const SNIPPETS = path.join(__dirname, 'fixtures', 'snippets');

function reportAt(snippetFile) {
  const space = loadTileSpaceFromFile(path.join(SNIPPETS, snippetFile));
  const report = buildAutotileReport(space);
  const byKey = new Map(report.tiles.map((t) => [`${t.x},${t.y}`, t]));
  return { space, byKey };
}

function groundAt(byKey, x, y) {
  const tile = byKey.get(`${x},${y}`);
  assert.ok(tile?.autotile?.ground, `missing ground at (${x},${y})`);
  return tile.autotile.ground;
}

function baselineExpects(space) {
  return space.baselineExpect?.length ? space.baselineExpect : space.expect ?? [];
}

test('one-sided-house-lip keeps sprite 24 (not 25) for single-side cells', () => {
  const { space, byKey } = reportAt('one-sided-house-lip.json');
  for (const exp of baselineExpects(space)) {
    const g = groundAt(byKey, exp.x, exp.y);
    assert.equal(g.spriteId, '24', `(${exp.x},${exp.y})`);
    assert.notEqual(g.spriteId, '25');
    assert.equal(g.partnerSubstitution, false);
  }
});

test('roof-slope-left-vs-right captures paired left/right cap cells', () => {
  const { byKey } = reportAt('roof-slope-left-vs-right.json');
  const leftCap = groundAt(byKey, -102, 30);
  const rightCap = groundAt(byKey, -87, 29);
  assert.equal(leftCap.spriteId, '0');
  assert.equal(leftCap.flipX, true);
  assert.equal(rightCap.spriteId, '0');
  assert.equal(rightCap.flipX, false);
});

test('roof-slope-left-vs-right cover caps mirror chirality', () => {
  const { byKey } = reportAt('roof-slope-left-vs-right.json');
  const leftCap = byKey.get('-102,30');
  const rightCap = byKey.get('-87,29');
  assert.equal(leftCap.autotile.cover?.spriteId, '3');
  assert.equal(leftCap.autotile.cover?.flipX, true);
  assert.equal(rightCap.autotile.cover?.spriteId, '3');
  assert.equal(rightCap.autotile.cover?.flipX, false);
});

test('roof-slope-left-vs-right tread steps mirror cover chirality', () => {
  const { byKey } = reportAt('roof-slope-left-vs-right.json');
  const leftTread = groundAt(byKey, -104, 30);
  const rightTread = groundAt(byKey, -88, 28);
  assert.equal(leftTread.spriteId, '2');
  assert.equal(rightTread.spriteId, '2');
  const leftCover = byKey.get('-104,30').autotile.cover;
  const rightCover = byKey.get('-88,28').autotile.cover;
  assert.equal(leftCover.spriteId, '5');
  assert.equal(leftCover.flipX, true);
  assert.equal(rightCover.spriteId, '5');
  assert.equal(rightCover.flipX, false);
});

test('dirt-window-inner-edges resolves without partner substitution', () => {
  const { space, byKey } = reportAt('dirt-window-inner-edges.json');
  for (const exp of baselineExpects(space)) {
    const g = groundAt(byKey, exp.x, exp.y);
    assert.equal(g.partnerSubstitution, false, `(${exp.x},${exp.y})`);
  }
});

test('dirt-window-inner-edges baseline matches halo-correct capture at inner verticals', () => {
  const { byKey } = reportAt('dirt-window-inner-edges.json');
  const left = groundAt(byKey, -114, 28);
  const right = groundAt(byKey, -111, 28);
  assert.equal(left.spriteId, '8');
  assert.equal(left.flipX, true);
  assert.equal(right.spriteId, '8');
  assert.equal(right.flipX, false);
});

test('dirt-window-inner-edges meets targetExpect under vendor-aligned resolution', () => {
  const space = loadTileSpaceFromFile(path.join(SNIPPETS, 'dirt-window-inner-edges.json'));
  const report = buildAutotileReport(space);
  const errors = assertExpectations(space, report, space.targetExpect ?? []);
  assert.equal(errors.length, 0, errors.join('\n'));
});

test('mountain-window-corner meets targetExpect for window-top inner corners', () => {
  const space = loadTileSpaceFromFile(path.join(SNIPPETS, 'mountain-window-corner.json'));
  const report = buildAutotileReport(space);
  const errors = assertExpectations(space, report, space.targetExpect ?? []);
  assert.equal(errors.length, 0, errors.join('\n'));
});

test('open-sky-bridge-lintel keeps vendor bridge sprite 25 without normalization', () => {
  const space = loadTileSpaceFromFile(path.join(SNIPPETS, 'open-sky-bridge-lintel.json'));
  const report = buildAutotileReport(space);
  const errors = assertExpectations(space, report, space.targetExpect ?? []);
  assert.equal(errors.length, 0, errors.join('\n'));
});
