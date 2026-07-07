// Vendor alignment: the project normalization layer is disabled, so ground cells resolve
// straight from the raw blob mask (exact match -> mirror -> fallback), exactly like the base
// PixelTileEngine autotiler. No normalizer ever fires; 1-wide cavity lintels/floors read as the
// vendor horizontal-shaft sprite 25 (not underside 17), and window-top corners read inner-corner
// 18 (+flipX) — all straight from the raw rules.

import { test } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { loadTileSpaceFromFile } from '../src/io/tileSpace.js';
import { buildAutotileReport } from '../src/report/autotileJson.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const SNIPPETS = path.join(__dirname, 'fixtures', 'snippets');

function report(snippetFile) {
  const space = loadTileSpaceFromFile(path.join(SNIPPETS, snippetFile));
  const rep = buildAutotileReport(space);
  const byKey = new Map(rep.tiles.map((t) => [`${t.x},${t.y}`, t.autotile?.ground]).filter(([, g]) => g));
  return { rep, byKey };
}

function cavityCells(rep) {
  return rep.tiles.filter((t) => t.autotile?.ground?.normalization?.cavityUnderside);
}

// Vendor alignment: no normalizer fires on any cavity fixture, and the final mask equals the
// raw connectivity mask (nothing remapped). The 1-wide lintels/floors read as vendor sprite 25.
const cavityFixtures = [
  ['dirt-hole-1x1.json', [[1, 2], [1, 0]]],
  ['dirt-window-1x1.json', [[1, 2], [1, 0]]],
  ['dirt-hole-2x1.json', [[1, 2], [2, 2], [1, 0], [2, 0]]],
  ['dirt-window-2x1.json', [[1, 2], [2, 2], [1, 0], [2, 0]]],
  ['dirt-hole-1x2.json', [[1, 3], [1, 0]]],
  ['dirt-window-1x2.json', [[1, 3], [1, 0]]],
  ['dirt-door-opening.json', [[1, 3], [2, 3], [1, 0], [2, 0]]],
  ['dirt-hole-door.json', [[1, 3], [2, 3], [1, 0], [2, 0]]],
];

for (const [file] of cavityFixtures) {
  test(`${file}: resolves raw vendor rules with normalization disabled`, () => {
    const { rep } = report(file);
    const solids = rep.tiles.filter((t) => t.autotile?.ground);
    assert.ok(solids.length > 0, 'fixture has solid ground cells');
    assert.equal(cavityCells(rep).length, 0, 'no cavityUnderside remap fires under vendor alignment');
    for (const t of solids) {
      const g = t.autotile.ground;
      assert.equal(g.normalization.stairInterior, false, `(${t.x},${t.y}) stairInterior off`);
      assert.equal(g.normalization.innerCavity, false, `(${t.x},${t.y}) innerCavity off`);
      assert.equal(g.normalization.cavityUnderside, false, `(${t.x},${t.y}) cavityUnderside off`);
      assert.equal(g.normalization.materialBoundary, false, `(${t.x},${t.y}) materialBoundary off`);
      assert.deepEqual(g.mask, g.connectivityMask, `(${t.x},${t.y}) mask not remapped`);
    }
  });
}

test('embedded window inner vertical walls read as the edge sprite 8 without normalization', () => {
  const { byKey } = report('dirt-window-inner-edges.json');
  const left = byKey.get('-116,27'); // wall east of a window: air west, mass east -> left edge
  const right = byKey.get('-108,27'); // wall west of a window: air east, mass west -> right edge (mirror)
  assert.ok(left && right, 'both embedded inner-wall cells present');
  assert.equal(left.spriteId, '8');
  assert.equal(left.flipX, false);
  assert.equal(right.spriteId, '8');
  assert.equal(right.flipX, true);
  // Edge faces come straight from the raw rule, not from a cavity remap.
  assert.equal(left.normalization.cavityUnderside, false);
  assert.equal(right.normalization.cavityUnderside, false);
  assert.equal(left.partnerSubstitution, false);
  assert.equal(right.partnerSubstitution, false);
});

test('mountain-window-corner inner corners resolve 18 without innerCavity flattening', () => {
  const { byKey } = report('mountain-window-corner.json');
  for (const [x, y, flipX] of [[-104, 26, true], [-102, 26, false], [-108, 29, true], [-106, 29, false]]) {
    const g = byKey.get(`${x},${y}`);
    assert.ok(g, `(${x},${y}) present`);
    assert.equal(g.spriteId, '18', `(${x},${y}) sprite`);
    assert.equal(g.flipX, flipX, `(${x},${y}) flipX`);
    assert.equal(g.normalization.innerCavity, false, `(${x},${y}) innerCavity`);
  }
  for (const [x, y] of [[-103, 26], [-107, 29]]) {
    const g = byKey.get(`${x},${y}`);
    assert.equal(g.spriteId, '17', `flat middle (${x},${y})`);
    assert.equal(g.normalization.innerCavity, false);
  }
});
// vertical walls, overhangs, undersides, or plain corners.
const negativeFixtures = [
  'slope-ascending-long.json',
  'slope-descending-long.json',
  'slope-ascending-stair-run.json',
  'slope-descending-stair-run.json',
  'vertical-wall-run.json',
  'overhang-inside-corner.json',
  'ceiling-underside.json',
  'floating-platform-underside.json',
  'corner-sides-left-right.json',
];

for (const file of negativeFixtures) {
  test(`${file}: cavityUnderside does not fire (guard against over-broad detection)`, () => {
    const { rep } = report(file);
    const fired = cavityCells(rep).map((t) => `(${t.x},${t.y})`);
    assert.equal(fired.length, 0, `cavityUnderside must stay clear here; fired at ${fired.join(',')}`);
  });
}
