// Known-answer vectors for the hash port. These exact values are shared verbatim
// with the C# suite Assets/Tests/EditMode/SandboxHashTests.cs (and the surface
// samples with TerrainFixtureExportTests.SurfaceHeightMatchesKnownAnswer). The two
// suites together are the offline guarantee that the C# and JS implementations agree
// bit-for-bit WITHOUT needing Unity to author a fixture. If the hash or noise math
// changes, regenerate the vectors and update BOTH suites.

import { test } from 'node:test';
import assert from 'node:assert/strict';

import { hash, unitFloat } from '../src/core/hash.js';
import { TerrainGenerator } from '../src/core/generator.js';

test('hash matches known-answer vectors (shared with C# SandboxHashTests)', () => {
  const vectors = [
    [0, 0, 0, 3463101980],
    [1, 2, 3, 1104645647],
    [1337, 1, 0, 2329107406],
    [1337, 1, 1, 1599356354],
    [1337, 1, 0xffffffff, 4015006146],
    [0xffffffff, 0xffffffff, 0xffffffff, 1646814764],
  ];
  for (const [a, b, c, expected] of vectors) {
    assert.equal(hash(a, b, c), expected, `hash(${a >>> 0}, ${b >>> 0}, ${c >>> 0})`);
  }
});

test('hash is deterministic and returns an unsigned 32-bit integer', () => {
  const h = hash(42, 7, 99);
  assert.equal(h, hash(42, 7, 99));
  assert.ok(Number.isInteger(h) && h >= 0 && h <= 0xffffffff);
});

test('unitFloat maps to [0, 1] (shared with C# UnitFloat)', () => {
  assert.equal(unitFloat(0), 0);
  assert.equal(unitFloat(2147483648), 0.5);
  assert.equal(unitFloat(0xffffffff), 1); // narrows to exactly 1
});

test('surface height matches known answers (shared with C# TerrainFixtureExportTests)', () => {
  const gen = new TerrainGenerator({
    seed: 1337, surfaceHeight: 28, terrainAmplitude: 8, terrainFrequency: 0.06, dirtDepth: 8,
  });
  const answers = [
    [-100, 27], [-64, 32], [-1, 29], [0, 29], [1, 29], [32, 33], [64, 31], [100, 34],
  ];
  for (const [x, h] of answers) {
    assert.equal(gen.getSurfaceHeight(x), h, `surface height at x=${x}`);
  }
});
