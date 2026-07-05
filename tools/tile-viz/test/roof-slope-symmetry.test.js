// Phase 2 (roof-slope symmetry): structural mirror invariant for ground and cover.
//
// Finding: on a geometrically symmetric hill the resolver already produces mirror-symmetric
// output for every cell, ground and cover. This pins that behavior so a future normalization
// change can't silently break left/right symmetry.
//
// Precise invariant (the plan's "left flipX != right flipX" holds only for asymmetric masks):
//   left.spriteId === right.spriteId
//   left.normalizedMask === horizontalMirror(right.normalizedMask)
//   flipX: opposite when the mask is asymmetric; both false when the mask is self-mirror.

import { test } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { loadTileSpaceFromFile } from '../src/io/tileSpace.js';
import { buildAutotileReport } from '../src/report/autotileJson.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const SNIPPETS = path.join(__dirname, 'fixtures', 'snippets');

// maskToJson emits columns [x][y]; horizontal mirror swaps west/east columns.
const mirror = (mask) => mask.slice().reverse();
const eq = (a, b) => JSON.stringify(a) === JSON.stringify(b);
const selfMirror = (mask) => eq(mask, mirror(mask));

function groundIndex(snippetFile) {
  const space = loadTileSpaceFromFile(path.join(SNIPPETS, snippetFile));
  const rep = buildAutotileReport(space);
  return new Map(rep.tiles.map((t) => [`${t.x},${t.y}`, t.autotile]));
}

function assertGroundMirror(a, b, label) {
  assert.equal(a.spriteId, b.spriteId, `${label}: ground spriteId ${a.spriteId} vs ${b.spriteId}`);
  assert.ok(eq(a.normalizedMask, mirror(b.normalizedMask)), `${label}: normalized masks are not horizontal mirrors`);
  if (selfMirror(a.normalizedMask)) {
    assert.equal(a.flipX, false, `${label}: self-mirror mask must not flip (left)`);
    assert.equal(b.flipX, false, `${label}: self-mirror mask must not flip (right)`);
  } else {
    assert.notEqual(a.flipX, b.flipX, `${label}: asymmetric mask must flip on exactly one side`);
  }
}

test('symmetric-pyramid: ground + cover mirror invariant holds for every pair', () => {
  const idx = groundIndex('symmetric-pyramid.json');
  let pairs = 0;
  for (const [key, at] of idx) {
    const [x, y] = key.split(',').map(Number);
    if (x < 0 || !at.ground) continue;

    if (x === 0) {
      // Center column: must be self-mirror and never flipped.
      assert.ok(selfMirror(at.ground.normalizedMask), `center (0,${y}) mask must be self-mirror`);
      assert.equal(at.ground.flipX, false, `center (0,${y}) must not flip`);
      continue;
    }

    const mate = idx.get(`${-x},${y}`);
    assert.ok(mate?.ground, `mirror cell for (${x},${y}) exists`);
    assertGroundMirror(at.ground, mate.ground, `(${x},${y})<->(${-x},${y})`);

    const c = at.cover?.rendered ? at.cover : null;
    const mc = mate.cover?.rendered ? mate.cover : null;
    assert.equal(Boolean(c), Boolean(mc), `(${x},${y}): cover rendered state must match its mirror`);
    if (c && mc) {
      assert.equal(c.spriteId, mc.spriteId, `(${x},${y}): cover spriteId ${c.spriteId} vs ${mc.spriteId}`);
      if (selfMirror(c.mask)) {
        assert.equal(c.flipX, false);
        assert.equal(mc.flipX, false);
      } else {
        assert.notEqual(c.flipX, mc.flipX, `(${x},${y}): asymmetric cover must flip on exactly one side`);
      }
    }
    pairs++;
  }
  assert.ok(pairs >= 5, `expected several mirror pairs, saw ${pairs}`);
});

test('roof-slope-left-vs-right: designated left/right cap pairs mirror', () => {
  const idx = groundIndex('roof-slope-left-vs-right.json');
  // Author-intended equivalent caps on the two hills (left descending vs right ascending).
  const pairs = [
    ['-102,30', '-87,29'],
    ['-101,29', '-89,28'],
    ['-97,27', '-91,27'],
  ];
  for (const [l, r] of pairs) {
    const left = idx.get(l)?.ground;
    const right = idx.get(r)?.ground;
    assert.ok(left && right, `both cells present: ${l} / ${r}`);
    assertGroundMirror(left, right, `${l}<->${r}`);
  }
});
