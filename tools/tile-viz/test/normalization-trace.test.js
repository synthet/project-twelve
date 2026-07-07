// Phase 0 debug-output contract: normalizationTrace, baseline/target expectations.
// Guards the debug payload shape the Autotile Next Actions Plan depends on.

import { test } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { loadTileSpace, loadTileSpaceFromFile } from '../src/io/tileSpace.js';
import { buildAutotileReport } from '../src/report/autotileJson.js';
import { buildNormalizationTrace, NORMALIZATION_ORDER } from '../src/visual/maskBuilder.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const SNIPPETS = path.join(__dirname, 'fixtures', 'snippets');

function groundPayloads(snippetFile) {
  const space = loadTileSpaceFromFile(path.join(SNIPPETS, snippetFile));
  const report = buildAutotileReport(space);
  return report.tiles.map((t) => t.autotile?.ground).filter(Boolean);
}

const TRACE_ENTRY = /^(stairInterior|innerCavity|cavityUnderside|materialBoundary): (applied|skipped)/;

test('every ground payload emits normalizationTrace, visualMask, solidMask, partnerSubstitution=false', () => {
  const grounds = groundPayloads('dirt-window-1x1.json');
  assert.ok(grounds.length > 0, 'expected at least one ground payload');
  for (const g of grounds) {
    assert.ok(Array.isArray(g.normalizationTrace), 'normalizationTrace is an array');
    assert.ok(g.normalizationTrace.every((e) => TRACE_ENTRY.test(e)), `trace entries well-formed: ${g.normalizationTrace}`);
    assert.ok(Array.isArray(g.visualMask), 'visualMask emitted');
    assert.ok(Array.isArray(g.solidMask), 'solidMask emitted');
    assert.equal(g.partnerSubstitution, false, 'partner substitution stays disabled');
  }
});

test('normalizationTrace is consistent with the normalization flags', () => {
  for (const g of groundPayloads('dirt-window-1x1.json')) {
    const appliedKeys = NORMALIZATION_ORDER.map((n) => n.key).filter((k) => g.normalization[k]);
    assert.ok(appliedKeys.length <= 1, 'normalizers short-circuit: at most one applied');
    const last = g.normalizationTrace[g.normalizationTrace.length - 1];
    if (appliedKeys.length === 1) {
      assert.match(last, new RegExp(`^${appliedKeys[0]}: applied`), 'trace ends with the applied normalizer');
    } else {
      assert.equal(g.normalizationTrace.length, NORMALIZATION_ORDER.length, 'no-op cell traces every normalizer');
      assert.ok(g.normalizationTrace.every((e) => e.endsWith(': skipped')), 'no-op cell is all skipped');
    }
  }
});

test('buildNormalizationTrace derives an ordered applied/skipped trace', () => {
  assert.deepEqual(buildNormalizationTrace({}), [
    'stairInterior: skipped',
    'innerCavity: skipped',
    'cavityUnderside: skipped',
    'materialBoundary: skipped',
  ]);
  assert.deepEqual(buildNormalizationTrace({ cavityUnderside: true }), [
    'stairInterior: skipped',
    'innerCavity: skipped',
    'cavityUnderside: applied: bridge -> underside',
  ]);
  assert.deepEqual(buildNormalizationTrace({ innerCavity: true }), [
    'stairInterior: skipped',
    'innerCavity: applied: flat lintel span -> underside',
  ]);
  assert.deepEqual(buildNormalizationTrace({ stairInterior: true }), [
    'stairInterior: applied: diagonal step -> interior fill',
  ]);
});

test('baselineExpect / targetExpect vocabulary loads with expect as the baseline alias', () => {
  const legacy = loadTileSpace({ kind: 'snippet', name: 'legacy', expect: [{ x: 0, y: 0 }] });
  assert.deepEqual(legacy.baselineExpect, [{ x: 0, y: 0 }]);
  assert.deepEqual(legacy.targetExpect, [{ x: 0, y: 0 }], 'target defaults to baseline');
  assert.deepEqual(legacy.expect, legacy.baselineExpect, 'expect stays bound to baseline');

  const split = loadTileSpace({
    kind: 'snippet',
    name: 'split',
    baselineExpect: [{ x: 1, y: 1, ground: { spriteId: '25' } }],
    targetExpect: [{ x: 1, y: 1, ground: { spriteId: '17' } }],
  });
  assert.equal(split.baselineExpect[0].ground.spriteId, '25');
  assert.equal(split.targetExpect[0].ground.spriteId, '17');
  assert.equal(split.expect[0].ground.spriteId, '25', 'expect mirrors baseline, not target');
});
