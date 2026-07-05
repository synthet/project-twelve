// Regression fixtures for inner-cavity, roof slope chirality, and one-sided lips.
// Captures current tile-viz resolver output in snippet-local context (baseline for Unity parity).

import { test } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { loadTileSpaceFromFile } from '../src/io/tileSpace.js';
import { buildAutotileReport } from '../src/report/autotileJson.js';

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

test('one-sided-house-lip keeps sprite 24 (not 25) for single-side cells', () => {
  const { byKey } = reportAt('one-sided-house-lip.json');
  for (const exp of loadTileSpaceFromFile(path.join(SNIPPETS, 'one-sided-house-lip.json')).expect) {
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

test('dirt-window-inner-edges resolves without partner substitution', () => {
  const { byKey } = reportAt('dirt-window-inner-edges.json');
  for (const exp of loadTileSpaceFromFile(path.join(SNIPPETS, 'dirt-window-inner-edges.json')).expect) {
    const g = groundAt(byKey, exp.x, exp.y);
    assert.equal(g.partnerSubstitution, false, `(${exp.x},${exp.y})`);
  }
});
