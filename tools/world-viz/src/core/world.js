// Region assembly: combine procedural generation with save-game edits.
//
// A World wraps a TerrainGenerator plus an optional edit overlay keyed by world
// tile coordinate. Edits win over generation (a saved chunk fully overrides its
// generated tiles, matching SandboxWorld.LoadFromPath which regenerates the
// chunk and then applies every stored tile). Pure ESM.

import { CHUNK_SIZE, TerrainGenerator } from './generator.js';
import { floorDiv } from './mathf.js';

/** Stable string key for a world tile coordinate. */
export function worldKey(x, y) {
  return `${x},${y}`;
}

export class World {
  /**
   * @param {TerrainGenerator} generator
   * @param {Map<string, object>} edits  world-coord key -> tile record
   */
  constructor(generator, edits = new Map()) {
    this.generator = generator;
    this.edits = edits;
    this._heightCache = new Map();
  }

  surfaceHeight(worldX) {
    let h = this._heightCache.get(worldX);
    if (h === undefined) {
      h = this.generator.getSurfaceHeight(worldX);
      this._heightCache.set(worldX, h);
    }
    return h;
  }

  /** Tile at a world coordinate: edit overlay first, else procedural. */
  tileAt(worldX, worldY) {
    const edit = this.edits.get(worldKey(worldX, worldY));
    if (edit !== undefined) return edit;
    return this.generator.generateTile(worldY, this.surfaceHeight(worldX));
  }
}

/**
 * Convert parsed save edits (per-chunk local tiles) into a flat world-coord
 * edit map. Mirrors SandboxWorld coordinate math (world = chunk*Size + local).
 * @param {Array<{x:number,y:number,edits:Array}>} chunks
 * @returns {Map<string, object>}
 */
export function editsFromChunks(chunks) {
  const map = new Map();
  for (const chunk of chunks) {
    for (const e of chunk.edits) {
      const worldX = chunk.x * CHUNK_SIZE + e.localX;
      const worldY = chunk.y * CHUNK_SIZE + e.localY;
      map.set(worldKey(worldX, worldY), e.tile);
    }
  }
  return map;
}

/**
 * Sample a rectangular world region (inclusive bounds) into a grid.
 * Returns { minX, maxX, minY, maxY, width, height, tiles } where tiles is a
 * row-major array of rows ordered TOP-DOWN (highest worldY first) so it maps
 * directly onto image rows / printed lines.
 */
export function sampleRegion(world, minX, maxX, minY, maxY) {
  const width = maxX - minX + 1;
  const height = maxY - minY + 1;
  const rows = new Array(height);
  for (let row = 0; row < height; row++) {
    const worldY = maxY - row; // top row = highest Y
    const line = new Array(width);
    for (let col = 0; col < width; col++) {
      line[col] = world.tileAt(minX + col, worldY);
    }
    rows[row] = line;
  }
  return { minX, maxX, minY, maxY, width, height, tiles: rows };
}

/** Bounding box (inclusive) of a set of edited chunk coords, in world tiles. */
export function editedChunksBounds(chunks) {
  if (!chunks || chunks.length === 0) return null;
  let minCx = Infinity;
  let maxCx = -Infinity;
  let minCy = Infinity;
  let maxCy = -Infinity;
  for (const c of chunks) {
    if (c.x < minCx) minCx = c.x;
    if (c.x > maxCx) maxCx = c.x;
    if (c.y < minCy) minCy = c.y;
    if (c.y > maxCy) maxCy = c.y;
  }
  return {
    minX: minCx * CHUNK_SIZE,
    maxX: (maxCx + 1) * CHUNK_SIZE - 1,
    minY: minCy * CHUNK_SIZE,
    maxY: (maxCy + 1) * CHUNK_SIZE - 1,
  };
}

export { floorDiv };
