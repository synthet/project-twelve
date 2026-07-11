// Post-resolve material-boundary selection (does not alter blob masks).

import { TileId } from '../../../world-viz/src/core/tiles.js';
import { getGroundTilesetName, sharesGroundAutotileGroup } from './catalog.js';
import { getTile, spaceToSampledGrid } from '../io/tileSpace.js';

/** Ground sprites with open-bottom art — wrong over foreign solid support. */
export const OPEN_BOTTOM_GROUND_SPRITES = new Set(['7', '23', '24', '25', '28']);

/** Opaque flat-top replacements (masks 000/111/111). */
export const FLAT_TOP_GROUND_SPRITES = ['1', '2'];

/** Cover tilesets probed for boundary fringe (overlay path). */
export const BOUNDARY_COVER_TILESETS = {
  dirtAboveStone: 'Moss',
  stoneUnderDirt: 'Moss',
  defaultFringe: 'SandA',
};

const CARDINAL = {
  N: [0, 1],
  S: [0, -1],
  E: [1, 0],
  W: [-1, 0],
};

function isSolid(space, x, y) {
  return getTile(space, x, y).id !== TileId.Air;
}

function sharesGroup(space, x, y, cx, cy, catalog) {
  const center = getTile(space, cx, cy);
  const neighbor = getTile(space, x, y);
  return sharesGroundAutotileGroup(center.id, neighbor.id, catalog);
}

/**
 * @param {object} space
 * @param {number} x
 * @param {number} y
 * @param {object} catalog
 */
export function getMaterialBoundaryContext(space, x, y, catalog) {
  const tile = getTile(space, x, y);
  if (tile.id === TileId.Air) {
    return null;
  }

  const foreignBelow = isSolid(space, x, y - 1) && !sharesGroup(space, x, y - 1, x, y, catalog);
  const foreignAbove = isSolid(space, x, y + 1) && !sharesGroup(space, x, y + 1, x, y, catalog);
  const foreignWest = isSolid(space, x - 1, y) && !sharesGroup(space, x - 1, y, x, y, catalog);
  const foreignEast = isSolid(space, x + 1, y) && !sharesGroup(space, x + 1, y, x, y, catalog);

  const isBoundary = foreignBelow || foreignAbove || foreignWest || foreignEast;
  if (!isBoundary) {
    return null;
  }

  return {
    tileId: tile.id,
    groundTileset: getGroundTilesetName(tile.id, catalog),
    foreignBelow,
    foreignAbove,
    foreignWest,
    foreignEast,
    isHorizontalSeam: foreignBelow || foreignAbove,
    isVerticalSeam: foreignWest || foreignEast,
  };
}

function pickFlatTopSprite(worldX, worldY) {
  const pick = (worldX * 73856093) ^ (worldY * 19349663);
  return FLAT_TOP_GROUND_SPRITES[Math.abs(pick) % FLAT_TOP_GROUND_SPRITES.length];
}

/**
 * Post-resolve ground sprite selection at heterogenic boundaries.
 * @param {object|null} context from getMaterialBoundaryContext
 * @param {{ spriteId: string, flipX: boolean }} resolved
 * @param {number} worldX
 * @param {number} worldY
 * @returns {{ spriteId: string, flipX: boolean, materialBoundarySelection: string }|null}
 */
export function selectBoundaryGroundSprite(context, resolved, worldX, worldY) {
  if (!context || !resolved?.spriteId) {
    return null;
  }

  const id = String(resolved.spriteId);
  if (!OPEN_BOTTOM_GROUND_SPRITES.has(id)) {
    return null;
  }

  // Lip/bridge over foreign solid below → opaque flat top (preserve vendor art when foreign above).
  if (context.foreignBelow) {
    return {
      spriteId: pickFlatTopSprite(worldX, worldY),
      flipX: false,
      materialBoundarySelection: 'flat-top-foreign-below',
    };
  }

  return null;
}

/**
 * Boundary cover on stone (and similar) under foreign dirt — vendor cover expects air above,
 * but Moss fringe at the interface seals residual color seams.
 * @param {object|null} context
 * @param {object} tileAbove
 * @returns {{ rendered: boolean, tileset: string, spriteId: string, flipX: boolean, materialBoundarySelection: string }|null}
 */
export function selectBoundaryCover(context, tileAbove) {
  if (!context) {
    return null;
  }

  // Stone under foreign dirt: draw moss fringe even though dirt is above (not air).
  if (context.foreignAbove && context.groundTileset === 'Rocks' && tileAbove.id !== TileId.Air) {
    const spriteId = context.foreignWest && !context.foreignEast ? '1'
      : !context.foreignWest && context.foreignEast ? '1'
        : '3';
    const flipX = !context.foreignWest && context.foreignEast;
    return {
      rendered: true,
      tileset: BOUNDARY_COVER_TILESETS.stoneUnderDirt,
      spriteId,
      flipX,
      materialBoundarySelection: 'moss-fringe-under-foreign',
    };
  }

  return null;
}

export function listBoundaryCells(space, catalog) {
  const cells = [];
  const grid = spaceToSampledGrid(space);
  for (let row = 0; row < grid.height; row++) {
    const worldY = grid.maxY - row;
    for (let col = 0; col < grid.width; col++) {
      const worldX = grid.minX + col;
      const tile = getTile(space, worldX, worldY);
      if (tile.id === TileId.Air) {
        continue;
      }
      const ctx = getMaterialBoundaryContext(space, worldX, worldY, catalog);
      if (ctx) {
        cells.push({ x: worldX, y: worldY, ...ctx });
      }
    }
  }
  return cells;
}
