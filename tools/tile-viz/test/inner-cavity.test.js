// Phase 1 (inner cavities): lock the already-correct resolution and guard the cavity
// normalizer against over-broad firing.
//
// Finding: the isolated 1x1 / 2x1 / 1x2 / door cavities already resolve correctly — cavity
// lintels normalize to the underside sprite 17 via cavityUnderside, embedded window inner
// walls read as the edge sprite 8, thin (1-wide) walls read as the vertical shaft 21. No new
// TryRemapCavityInnerEdgeMask predicate is warranted for these shapes; these tests pin that
// behavior and prove the existing cavityUnderside normalizer stays narrow.

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

// Positive: cavity lintels normalize to the underside sprite 17 via cavityUnderside.
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

for (const [file, lintels] of cavityFixtures) {
  test(`${file}: cavity lintels normalize to underside 17`, () => {
    const { rep, byKey } = report(file);
    const fired = cavityCells(rep);
    assert.ok(fired.length > 0, 'cavityUnderside should fire on at least one lintel');
    for (const t of fired) {
      assert.equal(t.autotile.ground.spriteId, '17', `cavity cell (${t.x},${t.y}) resolves to 17`);
      assert.match(
        t.autotile.ground.normalizationTrace.at(-1),
        /^cavityUnderside: applied/,
        `trace records the applied normalizer at (${t.x},${t.y})`,
      );
    }
    for (const [x, y] of lintels) {
      const g = byKey.get(`${x},${y}`);
      assert.ok(g, `lintel (${x},${y}) present`);
      assert.equal(g.spriteId, '17', `lintel (${x},${y}) is 17`);
      assert.equal(g.normalization.cavityUnderside, true, `lintel (${x},${y}) went through cavityUnderside`);
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

// Negative: the cavity normalizer must stay narrow — it may not fire on cliffs, slopes,
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
