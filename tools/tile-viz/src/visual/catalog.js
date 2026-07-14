// Port of SandboxTileVisualCatalog defaults (Humus / Rocks / GrassA).

import { TileId } from '../../../world-viz/src/core/tiles.js';

export const DEFAULT_CATALOG = Object.freeze({
  dirtGroundTileset: 'Humus',
  grassGroundTileset: 'Humus',
  stoneGroundTileset: 'Rocks',
  bricksAGroundTileset: 'BricksA',
  bricksBGroundTileset: 'BricksB',
  bricksCGroundTileset: 'BricksC',
  bricksDGroundTileset: 'BricksD',
  frozenGroundTileset: 'Frozen',
  magmaGroundTileset: 'Magma',
  sandGroundTileset: 'Sand',
  grassCoverTileset: 'GrassA',
});

/**
 * @param {number} tileId
 * @param {object} [catalog]
 * @returns {string|null}
 */
export function getGroundTilesetName(tileId, catalog = DEFAULT_CATALOG) {
  switch (tileId) {
    case TileId.Dirt:
      return catalog.dirtGroundTileset;
    case TileId.Grass:
      return catalog.grassGroundTileset;
    case TileId.Stone:
      return catalog.stoneGroundTileset;
    case TileId.BricksA:
      return catalog.bricksAGroundTileset;
    case TileId.BricksB:
      return catalog.bricksBGroundTileset;
    case TileId.BricksC:
      return catalog.bricksCGroundTileset;
    case TileId.BricksD:
      return catalog.bricksDGroundTileset;
    case TileId.Frozen:
      return catalog.frozenGroundTileset;
    case TileId.Magma:
      return catalog.magmaGroundTileset;
    case TileId.Sand:
      return catalog.sandGroundTileset;
    default:
      return null;
  }
}

export function sharesGroundAutotileGroup(tileIdA, tileIdB, catalog = DEFAULT_CATALOG) {
  if (tileIdA === TileId.Air || tileIdB === TileId.Air) {
    return false;
  }
  const nameA = getGroundTilesetName(tileIdA, catalog);
  const nameB = getGroundTilesetName(tileIdB, catalog);
  if (!nameA || !nameB) {
    return false;
  }
  return nameA === nameB;
}

// Grass-growth cover model: the green cover is gameplay state, so it renders only on a grass tile
// with an exposed (air) top — not on bare dirt/stone. Mirrors
// SandboxTileVisualCatalog.ShouldRenderGrassCover.
export function shouldRenderGrassCover(tileId, tileAbove) {
  return tileId === TileId.Grass && tileAbove.id === TileId.Air;
}

export function sharesCoverAutotileGroup(tileIdA, tileIdB) {
  return tileIdA === TileId.Grass && tileIdB === TileId.Grass;
}

// Only the grass tile carries a cover tileset; every other material is bare.
export function getCoverTilesetName(tileId, catalog = DEFAULT_CATALOG) {
  if (tileId !== TileId.Grass) {
    return null;
  }
  return catalog.grassCoverTileset;
}
