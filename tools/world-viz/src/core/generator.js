// 1:1 port of Assets/Scripts/Sandbox/SandboxTerrainGenerator.cs.
//
// Pure and deterministic: given identical settings (seed + shaping params) and a
// chunk coordinate, generation always produces identical tile data. Untouched
// chunks are regenerated from the seed rather than persisted (see
// docs/wiki/generation-and-saving.md). Pure ESM — shared verbatim by the CLI and
// the inlined in-browser generator.

import { perlinNoise } from './perlin.js';
import { roundToInt, fr } from './mathf.js';
import { TileId, makeTile } from './tiles.js';

export const CHUNK_SIZE = 32; // SandboxChunk.Size

// float32 value of the 0.001f literal used for the noise Y input.
const FR_0001 = fr(0.001);

export const DEFAULT_PARAMS = Object.freeze({
  seed: 1337,
  surfaceHeight: 28,
  terrainAmplitude: 8,
  terrainFrequency: 0.06,
  dirtDepth: 8,
});

export class TerrainGenerator {
  constructor(params = {}) {
    const p = { ...DEFAULT_PARAMS, ...params };
    this.seed = p.seed | 0;
    this.surfaceHeight = p.surfaceHeight | 0;
    this.terrainAmplitude = p.terrainAmplitude | 0;
    // Stored as float32: the engine's TerrainFrequency is a `float` field.
    this.terrainFrequency = fr(p.terrainFrequency);
    this.dirtDepth = p.dirtDepth | 0;
  }

  /**
   * Surface height in world tiles for a world column. Mirrors
   * GetSurfaceHeight: float32-emulated noise inputs, then banker's RoundToInt.
   */
  getSurfaceHeight(worldX) {
    // Mirror the engine's float data flow: (int -> float) * float, all
    // float32-rounded at each step. perlinNoise itself returns a float.
    const xInput = fr(fr(worldX + this.seed) * this.terrainFrequency);
    const yInput = fr(this.seed * FR_0001);
    const noise = perlinNoise(xInput, yInput);
    // (noise - 0.5f) * TerrainAmplitude * 2f, then banker's RoundToInt.
    const offset = roundToInt(fr(fr(fr(noise - 0.5) * this.terrainAmplitude) * 2));
    return this.surfaceHeight + offset;
  }

  /** Resolves the tile id for a world row relative to its column surface height. */
  getGeneratedTileId(worldY, height) {
    if (worldY > height) return TileId.Air;
    if (worldY === height) return TileId.Grass;
    return worldY > height - this.dirtDepth ? TileId.Dirt : TileId.Stone;
  }

  /** Generates a single tile, including the prototype light seed used by rendering. */
  generateTile(worldY, height) {
    const id = this.getGeneratedTileId(worldY, height);
    return makeTile(id, worldY >= height ? 15 : 4);
  }

  /**
   * Generates the full tile grid for a chunk. Returns a column-major 2D array
   * tiles[localX][localY], matching SandboxChunk's [localX, localY] indexing.
   */
  generateChunk(chunkX, chunkY) {
    const tiles = new Array(CHUNK_SIZE);
    for (let localX = 0; localX < CHUNK_SIZE; localX++) {
      const worldX = chunkX * CHUNK_SIZE + localX;
      const height = this.getSurfaceHeight(worldX);
      const column = new Array(CHUNK_SIZE);
      for (let localY = 0; localY < CHUNK_SIZE; localY++) {
        const worldY = chunkY * CHUNK_SIZE + localY;
        column[localY] = this.generateTile(worldY, height);
      }
      tiles[localX] = column;
    }
    return tiles;
  }
}
