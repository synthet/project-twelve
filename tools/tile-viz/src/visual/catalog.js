// Port of SandboxTileVisualCatalog defaults (Humus / Rocks / GrassA).

import { TileId } from '../../../world-viz/src/core/tiles.js';

export const DEFAULT_CATALOG = Object.freeze({
  dirtGroundTileset: 'Humus',
  grassGroundTileset: 'Humus',
  stoneGroundTileset: 'Rocks',
  copperOreGroundTileset: 'BricksA',
  ironOreGroundTileset: 'BricksB',
  silverOreGroundTileset: 'BricksC',
  goldOreGroundTileset: 'BricksD',
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
    case TileId.CopperOre:
      return catalog.copperOreGroundTileset;
    case TileId.IronOre:
      return catalog.ironOreGroundTileset;
    case TileId.SilverOre:
      return catalog.silverOreGroundTileset;
    case TileId.GoldOre:
      return catalog.goldOreGroundTileset;
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

// Vendor cover model: cover renders on any exposed-top ground cell (solid here, air above),
// independent of ground material. Mirrors SandboxTileVisualCatalog.ShouldRenderGrassCover.
export function shouldRenderGrassCover(tileId, tileAbove) {
  return tileId !== TileId.Air && tileAbove.id === TileId.Air;
}

export function sharesCoverAutotileGroup(tileIdA, tileIdB) {
  return tileIdA !== TileId.Air && tileIdB !== TileId.Air;
}

// Any solid ground tile maps to the single configured cover tileset; air has no cover.
export function getCoverTilesetName(tileId, catalog = DEFAULT_CATALOG) {
  if (tileId === TileId.Air) {
    return null;
  }
  return catalog.grassCoverTileset;
}
