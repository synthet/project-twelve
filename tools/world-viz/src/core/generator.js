// 1:1 port of Assets/Scripts/Sandbox/SandboxTerrainGenerator.cs.
//
// Pure and deterministic: given identical settings (seed + shaping params) and a
// chunk coordinate, generation always produces identical tile data. Untouched
// chunks are regenerated from the seed rather than persisted (see
// docs/wiki/generation-and-saving.md). Pure ESM — shared verbatim by the CLI and
// the inlined in-browser generator.

import { hash, unitFloat } from './hash.js';
import { roundToInt, fr } from './mathf.js';
import { TileId, makeTile } from './tiles.js';

export const CHUNK_SIZE = 32; // SandboxChunk.Size

// SandboxGenPass.SurfaceHeightmap — passId component of the pass-1 sub-seed.
const SURFACE_PASS = 1;

// Quintic smootherstep (6t^5 - 15t^4 + 10t^3), float32 at each step to mirror the
// engine's `float` SmoothStep. Named distinctly so it inlines cleanly into the HTML view.
function smoothStep(t) {
  const t3 = fr(fr(fr(t * t) * t));
  const inner = fr(fr(t * fr(fr(t * 6) - 15)) + 10);
  return fr(t3 * inner);
}

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
   * Surface height in world tiles for a world column. Mirrors GetSurfaceHeight:
   * pass-1 value noise — a smootherstep blend of per-lattice random samples drawn
   * from the deterministic integer hash hash(seed, passId, latticeX). float32 at
   * each step, then banker's RoundToInt. terrainFrequency sets the lattice spacing.
   */
  getSurfaceHeight(worldX) {
    const t = fr(worldX * this.terrainFrequency);
    const latticeX = Math.floor(t);
    const frac = fr(t - latticeX);

    const a = unitFloat(hash(this.seed >>> 0, SURFACE_PASS, latticeX >>> 0));
    const b = unitFloat(hash(this.seed >>> 0, SURFACE_PASS, (latticeX + 1) >>> 0));
    // a + (b - a) * smoothStep(frac), all float32.
    const noise = fr(a + fr(fr(b - a) * smoothStep(frac)));
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
