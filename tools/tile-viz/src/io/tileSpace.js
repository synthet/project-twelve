// Tile-space JSON load/normalize (project-twelve/tile-space/v1).

import fs from 'node:fs';
import path from 'node:path';

import { TerrainGenerator, DEFAULT_PARAMS } from '../../../world-viz/src/core/generator.js';
import { World, sampleRegion, worldKey, editsFromChunks } from '../../../world-viz/src/core/world.js';
import { makeTile, TileId } from '../../../world-viz/src/core/tiles.js';
import { parseSaveText } from '../../../world-viz/src/io/saveLoad.js';

export const FORMAT = 'project-twelve/tile-space/v1';

/**
 * @param {string} filePath
 * @returns {object}
 */
export function loadTileSpaceFromFile(filePath) {
  const text = fs.readFileSync(filePath, 'utf8');
  return loadTileSpace(JSON.parse(text), path.dirname(path.resolve(filePath)));
}

/**
 * @param {object} doc
 * @param {string} [baseDir]
 * @returns {object}
 */
export function loadTileSpace(doc, baseDir = process.cwd()) {
  if (doc.tiles && Array.isArray(doc.tiles) && doc.tiles[0]?.autotile) {
    return normalizeMcpDump(doc);
  }

  const kind = doc.kind ?? inferKind(doc);
  switch (kind) {
    case 'snippet':
      return loadSnippet(doc);
    case 'space':
      return loadSpace(doc);
    case 'world':
      return loadWorld(doc, baseDir);
    default:
      throw new Error(`Unknown tile-space kind "${kind}"`);
  }
}

function inferKind(doc) {
  if (doc.generate || doc.save) return 'world';
  if (doc.xMin !== undefined && doc.yMin !== undefined) return 'space';
  return 'snippet';
}

/**
 * Resolves the baseline/target expectation vocabulary (see the Autotile Next Actions
 * Plan, Phase 0A). `baselineExpect` captures current resolver output for parity/regression
 * tracking; `targetExpect` captures desired visual acceptance after a fix. The legacy
 * `expect` field is treated as `baselineExpect`, and `expect` on the returned space stays
 * bound to the baseline set so existing callers keep working.
 * @param {object} doc
 * @returns {{ expect: object[], baselineExpect: object[], targetExpect: object[] }}
 */
function resolveExpectations(doc) {
  const baselineExpect = doc.baselineExpect ?? doc.expect ?? [];
  const targetExpect = doc.targetExpect ?? baselineExpect;
  return { expect: baselineExpect, baselineExpect, targetExpect };
}

function loadSnippet(doc) {
  const originX = doc.origin?.x ?? 0;
  const originY = doc.origin?.y ?? 0;
  const width = doc.width ?? 1;
  const height = doc.height ?? 1;
  const tiles = new Map();

  if (Array.isArray(doc.tiles)) {
    for (const t of doc.tiles) {
      const x = t.x ?? originX + (t.dx ?? 0);
      const y = t.y ?? originY + (t.dy ?? 0);
      tiles.set(worldKey(x, y), normalizeTile(t));
    }
  } else if (Array.isArray(doc.ids)) {
    let i = 0;
    for (let dy = 0; dy < height; dy++) {
      for (let dx = 0; dx < width; dx++, i++) {
        const id = doc.ids[i] ?? TileId.Air;
        if (id !== TileId.Air) {
          tiles.set(worldKey(originX + dx, originY + dy), makeTile(id, 15));
        }
      }
    }
  }

  return {
    kind: 'snippet',
    name: doc.name ?? 'snippet',
    xMin: originX,
    yMin: originY,
    xMax: originX + width - 1,
    yMax: originY + height - 1,
    tiles,
    ...resolveExpectations(doc),
    raw: doc,
  };
}

function loadSpace(doc) {
  const tiles = new Map();
  if (Array.isArray(doc.tiles)) {
    for (const t of doc.tiles) {
      tiles.set(worldKey(t.x, t.y), normalizeTile(t));
    }
  }
  return {
    kind: 'space',
    name: doc.name ?? 'space',
    xMin: doc.xMin,
    yMin: doc.yMin,
    xMax: doc.xMax,
    yMax: doc.yMax,
    tiles,
    ...resolveExpectations(doc),
    raw: doc,
  };
}

function loadWorld(doc, baseDir) {
  const params = { ...DEFAULT_PARAMS, ...(doc.generate ?? {}) };
  let edits = new Map();

  if (doc.save) {
    const savePath = path.isAbsolute(doc.save) ? doc.save : path.join(baseDir, doc.save);
    const save = parseSaveText(fs.readFileSync(savePath, 'utf8'));
    params.seed = save.seed;
    edits = editsFromChunks(save.chunks);
  }

  if (Array.isArray(doc.edits)) {
    for (const e of doc.edits) {
      edits.set(worldKey(e.x, e.y), normalizeTile(e));
    }
  }

  const world = new World(new TerrainGenerator(params), edits);
  const region = doc.region ?? { xMin: -64, xMax: 64, yMin: 0, yMax: 48 };
  const sampled = sampleRegion(world, region.xMin, region.xMax, region.yMin, region.yMax);
  const tiles = new Map();
  for (let row = 0; row < sampled.height; row++) {
    const worldY = sampled.maxY - row;
    for (let col = 0; col < sampled.width; col++) {
      const worldX = sampled.minX + col;
      const tile = sampled.tiles[row][col];
      if (tile.id !== TileId.Air) {
        tiles.set(worldKey(worldX, worldY), tile);
      }
    }
  }

  return {
    kind: 'world',
    name: doc.name ?? 'world',
    xMin: region.xMin,
    yMin: region.yMin,
    xMax: region.xMax,
    yMax: region.yMax,
    tiles,
    ...resolveExpectations(doc),
    params,
    raw: doc,
  };
}

function normalizeMcpDump(doc) {
  const tiles = new Map();
  for (const t of doc.tiles) {
    tiles.set(worldKey(t.x, t.y), {
      id: t.tileId ?? t.id ?? TileId.Air,
      light: t.light ?? 15,
      fluid: t.fluid ?? 0,
      metadata: t.metadata ?? 0,
    });
  }
  return {
    kind: 'space',
    name: doc.name ?? 'mcp-import',
    xMin: doc.xMin,
    yMin: doc.yMin,
    xMax: doc.xMax,
    yMax: doc.yMax,
    tiles,
    expect: [],
    raw: doc,
  };
}

function normalizeTile(t) {
  const id = t.id ?? t.tileId ?? TileId.Air;
  return {
    id,
    light: t.light ?? (id === TileId.Air ? 0 : id === TileId.Dirt || id === TileId.Stone ? 4 : 15),
    fluid: t.fluid ?? 0,
    metadata: t.metadata ?? 0,
  };
}

export function getTile(space, x, y) {
  return space.tiles.get(worldKey(x, y)) ?? makeTile(TileId.Air, 15);
}

export function spaceToSampledGrid(space) {
  const width = space.xMax - space.xMin + 1;
  const height = space.yMax - space.yMin + 1;
  const rows = [];
  for (let row = 0; row < height; row++) {
    const worldY = space.yMax - row;
    const line = [];
    for (let col = 0; col < width; col++) {
      line.push(getTile(space, space.xMin + col, worldY));
    }
    rows.push(line);
  }
  return {
    minX: space.xMin,
    maxX: space.xMax,
    minY: space.yMin,
    maxY: space.yMax,
    width,
    height,
    tiles: rows,
  };
}

export function exportMcpToSpace(inputPath, outputPath) {
  const doc = JSON.parse(fs.readFileSync(inputPath, 'utf8'));
  const space = normalizeMcpDump(doc);
  const out = {
    format: FORMAT,
    kind: 'space',
    name: space.name,
    xMin: space.xMin,
    yMin: space.yMin,
    xMax: space.xMax,
    yMax: space.yMax,
    tiles: [...space.tiles.entries()].map(([key, tile]) => {
      const [x, y] = key.split(',').map(Number);
      return { x, y, ...tile };
    }),
  };
  fs.mkdirSync(path.dirname(path.resolve(outputPath)), { recursive: true });
  fs.writeFileSync(outputPath, `${JSON.stringify(out, null, 2)}\n`);
}
