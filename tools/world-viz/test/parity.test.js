// Parity test: the JS port must reproduce the Unity-exported golden fixture
// EXACTLY (surface heights + sampled chunk tile ids/lights).
//
// The fixture (test/fixtures/surface.seed1337.json) is authored by the Unity
// EditMode test Assets/Tests/EditMode/TerrainFixtureExportTests.cs. That test
// guarantees the fixture matches the engine; this test guarantees the port
// matches the fixture -> end-to-end parity, and any drift fails CI.
//
// If the fixture is absent (e.g. Unity has not been run in this checkout), the
// test is skipped with an explanatory message rather than failing.

import { test } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { TerrainGenerator, CHUNK_SIZE } from '../src/core/generator.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const FIXTURE = path.join(__dirname, 'fixtures', 'surface.seed1337.json');

const exists = fs.existsSync(FIXTURE);

test('JS generator reproduces the Unity golden fixture', { skip: exists ? false : `fixture not found at ${FIXTURE} (run Unity EditMode tests to generate it)` }, () => {
  const fx = JSON.parse(fs.readFileSync(FIXTURE, 'utf8'));
  const gen = new TerrainGenerator({
    seed: fx.seed,
    surfaceHeight: fx.surfaceHeight,
    terrainAmplitude: fx.terrainAmplitude,
    terrainFrequency: fx.terrainFrequency,
    dirtDepth: fx.dirtDepth,
  });

  // 1) Surface heights across the exported world-X range.
  let idx = 0;
  for (let x = fx.minX; x <= fx.maxX; x++, idx++) {
    assert.equal(
      gen.getSurfaceHeight(x),
      fx.surfaceHeights[idx],
      `surface height mismatch at worldX=${x}`,
    );
  }

  // 2) Full sampled chunks (row-major: localX outer, localY inner).
  for (const chunk of fx.chunks || []) {
    const tiles = gen.generateChunk(chunk.x, chunk.y);
    let i = 0;
    for (let lx = 0; lx < CHUNK_SIZE; lx++) {
      for (let ly = 0; ly < CHUNK_SIZE; ly++, i++) {
        assert.equal(
          tiles[lx][ly].id,
          chunk.ids[i],
          `id mismatch in chunk (${chunk.x},${chunk.y}) at local (${lx},${ly})`,
        );
        assert.equal(
          tiles[lx][ly].light,
          chunk.lights[i],
          `light mismatch in chunk (${chunk.x},${chunk.y}) at local (${lx},${ly})`,
        );
      }
    }
  }
});
