// Logic tests for the engine-free port: coordinate math, layering, determinism,
// and save-edit overlay. These do NOT depend on the Unity fixture.

import { test } from 'node:test';
import assert from 'node:assert/strict';

import { floorDiv, mod, roundToInt } from '../src/core/mathf.js';
import { TerrainGenerator, CHUNK_SIZE } from '../src/core/generator.js';
import { TileId } from '../src/core/tiles.js';
import {
  World,
  editsFromChunks,
  sampleRegion,
  editedChunksBounds,
  worldKey,
} from '../src/core/world.js';

test('roundToInt uses banker\'s rounding (matches .NET Math.Round)', () => {
  assert.equal(roundToInt(0.5), 0);
  assert.equal(roundToInt(1.5), 2);
  assert.equal(roundToInt(2.5), 2);
  assert.equal(roundToInt(-2.5), -2);
  assert.equal(roundToInt(-3.5), -4);
  assert.equal(roundToInt(2.4), 2);
  assert.equal(roundToInt(2.6), 3);
});

test('floorDiv / mod resolve negative world coords like SandboxWorld', () => {
  assert.equal(floorDiv(-1, 32), -1);
  assert.equal(floorDiv(-32, 32), -1);
  assert.equal(floorDiv(-33, 32), -2);
  assert.equal(mod(-1, 32), 31);
  assert.equal(mod(-32, 32), 0);
  assert.equal(mod(-33, 32), 31);
});

test('generated columns layer grass -> dirt -> stone downward', () => {
  const gen = new TerrainGenerator({ seed: 1337 });
  const worldX = 0;
  const h = gen.getSurfaceHeight(worldX);
  assert.equal(gen.getGeneratedTileId(h + 1, h), TileId.Air);
  assert.equal(gen.getGeneratedTileId(h, h), TileId.Grass);
  assert.equal(gen.getGeneratedTileId(h - 1, h), TileId.Dirt);
  assert.equal(gen.getGeneratedTileId(h - gen.dirtDepth + 1, h), TileId.Dirt);
  assert.equal(gen.getGeneratedTileId(h - gen.dirtDepth, h), TileId.Stone);
  assert.equal(gen.getGeneratedTileId(h - 100, h), TileId.Stone);
});

test('light seed: surface and above are bright (15), underground dim (4)', () => {
  const gen = new TerrainGenerator({ seed: 1337 });
  const h = gen.getSurfaceHeight(0);
  assert.equal(gen.generateTile(h, h).light, 15);
  assert.equal(gen.generateTile(h - 1, h).light, 4);
});

test('generation is deterministic for a fixed seed', () => {
  const a = new TerrainGenerator({ seed: 1337 }).generateChunk(2, 3);
  const b = new TerrainGenerator({ seed: 1337 }).generateChunk(2, 3);
  for (let x = 0; x < CHUNK_SIZE; x++) {
    for (let y = 0; y < CHUNK_SIZE; y++) {
      assert.equal(a[x][y].id, b[x][y].id);
      assert.equal(a[x][y].light, b[x][y].light);
    }
  }
});

test('different seeds produce different worlds', () => {
  const a = new TerrainGenerator({ seed: 1337 }).generateChunk(0, 0);
  const b = new TerrainGenerator({ seed: 9001 }).generateChunk(0, 0);
  let diff = false;
  for (let x = 0; x < CHUNK_SIZE && !diff; x++) {
    for (let y = 0; y < CHUNK_SIZE && !diff; y++) {
      if (a[x][y].id !== b[x][y].id) diff = true;
    }
  }
  assert.ok(diff, 'distinct seeds should not generate identical chunks');
});

test('save edits overlay at the correct world coordinate and win over generation', () => {
  const chunks = [
    { x: 1, y: -1, edits: [{ localX: 3, localY: 4, tile: { id: TileId.BricksD, light: 9, fluid: 0, metadata: 0 } }] },
  ];
  const edits = editsFromChunks(chunks);
  const expectedX = 1 * CHUNK_SIZE + 3; // 35
  const expectedY = -1 * CHUNK_SIZE + 4; // -28
  assert.ok(edits.has(worldKey(expectedX, expectedY)));

  const world = new World(new TerrainGenerator({ seed: 1337 }), edits);
  const tile = world.tileAt(expectedX, expectedY);
  assert.equal(tile.id, TileId.BricksD);
  assert.equal(tile.light, 9);
});

test('editedChunksBounds expands chunk coords to inclusive world-tile bounds', () => {
  const b = editedChunksBounds([{ x: 0, y: 0 }, { x: 2, y: 1 }]);
  assert.deepEqual(b, { minX: 0, maxX: 3 * CHUNK_SIZE - 1, minY: 0, maxY: 2 * CHUNK_SIZE - 1 });
});

test('sampleRegion is row-major top-down (first row is the highest world Y)', () => {
  const world = new World(new TerrainGenerator({ seed: 1337 }));
  const r = sampleRegion(world, 0, 4, 10, 14);
  assert.equal(r.width, 5);
  assert.equal(r.height, 5);
  // Top row corresponds to maxY; confirm by comparing against direct tile lookups.
  for (let col = 0; col < r.width; col++) {
    assert.equal(r.tiles[0][col].id, world.tileAt(0 + col, 14).id);
    assert.equal(r.tiles[4][col].id, world.tileAt(0 + col, 10).id);
  }
});
