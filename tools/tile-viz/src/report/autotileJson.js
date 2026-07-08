// MCP-style autotile debug report for a tile space.

import {
  buildCoverMask,
  buildGroundMaskDetailed,
  maskToJson,
} from '../visual/maskBuilder.js';
import {
  DEFAULT_CATALOG,
  getCoverTilesetName,
  getGroundTilesetName,
  sharesGroundAutotileGroup,
  shouldRenderGrassCover,
} from '../visual/catalog.js';
import { getTile, spaceToSampledGrid } from '../io/tileSpace.js';
import { loadRuleTables, getRulesForSpriteCount, GROUND_SPRITE_COUNT } from '../visual/ruleTables.js';
import { findMatchingSpriteIds, resolveSpriteId } from '../visual/resolver.js';
import { TILE_GLYPHS, TILE_NAMES, TileId } from '../../../world-viz/src/core/tiles.js';

export function buildAutotileReport(space, options = {}) {
  const catalog = options.catalog ?? DEFAULT_CATALOG;
  const tables = options.tables ?? loadRuleTables(options.dataDir);
  const includeAir = options.includeAir ?? false;
  const tilesets = buildTilesetLookup(options.manifest, tables);
  const visualOverrides = buildVisualOverrideLookup(space.visualOverrides);

  const tiles = [];
  const asciiRows = [];
  const grid = spaceToSampledGrid(space);

  for (let row = 0; row < grid.height; row++) {
    let line = '';
    const worldY = grid.maxY - row;
    for (let col = 0; col < grid.width; col++) {
      const worldX = grid.minX + col;
      const tile = getTile(space, worldX, worldY);
      if (tile.id === TileId.Air && !includeAir) {
        line += '.';
        continue;
      }
      line += TILE_GLYPHS[tile.id] ?? '?';
      if (tile.id !== TileId.Air || includeAir) {
        tiles.push(buildTileAutotile(space, worldX, worldY, tile, catalog, tilesets, tables, visualOverrides));
      }
    }
    asciiRows.push(line);
  }

  return {
    format: 'project-twelve/autotile-report/v1',
    name: space.name,
    xMin: space.xMin,
    yMin: space.yMin,
    xMax: space.xMax,
    yMax: space.yMax,
    width: grid.width,
    height: grid.height,
    ascii: asciiRows,
    tiles,
  };
}


function buildVisualOverrideLookup(overrides = []) {
  const lookup = new Map();
  for (const override of overrides) {
    lookup.set(overrideKey(override.x, override.y, override.layer), override);
  }
  return lookup;
}

function overrideKey(x, y, layer) {
  return `${x},${y},${layer}`;
}

function applyVisualOverride(layerInfo, override) {
  if (!layerInfo || !override) {
    return;
  }
  const auto = {
    spriteId: layerInfo.spriteId,
    flipX: layerInfo.flipX ?? false,
    flipY: layerInfo.flipY ?? false,
    rotationDegrees: layerInfo.rotationDegrees ?? 0,
  };
  layerInfo.auto = auto;
  layerInfo.override = {
    spriteId: override.spriteId,
    flipX: override.flipX ?? false,
    flipY: override.flipY ?? false,
    rotationDegrees: override.rotationDegrees ?? 0,
  };
  layerInfo.spriteId = layerInfo.override.spriteId;
  layerInfo.finalSpriteId = layerInfo.override.spriteId;
  layerInfo.flipX = layerInfo.override.flipX;
  layerInfo.flipY = layerInfo.override.flipY;
  layerInfo.rotationDegrees = layerInfo.override.rotationDegrees;
  layerInfo.overrideApplied = true;
}

function buildTilesetLookup(manifest, tables) {
  const lookup = new Map();
  if (!manifest?.tilesets) {
    lookup.set('Humus', { name: 'Humus', spriteCount: GROUND_SPRITE_COUNT, rules: tables.ground.rules, layer: 'ground' });
    lookup.set('Rocks', { name: 'Rocks', spriteCount: GROUND_SPRITE_COUNT, rules: tables.ground.rules, layer: 'ground' });
    lookup.set('GrassA', { name: 'GrassA', spriteCount: 6, rules: tables.cover.rules, layer: 'cover' });
    for (const name of ['BricksA', 'BricksB', 'BricksC', 'BricksD']) {
      lookup.set(name, { name, spriteCount: GROUND_SPRITE_COUNT, rules: tables.ground.rules, layer: 'ground' });
    }
    return lookup;
  }
  for (const ts of manifest.tilesets) {
    const rules =
      ts.layer === 'cover'
        ? tables.cover.rules
        : ts.spriteCount === GROUND_SPRITE_COUNT
          ? tables.ground.rules
          : tables.cover.rules;
    lookup.set(ts.name, { ...ts, rules });
  }
  return lookup;
}

function buildTileAutotile(space, x, y, tile, catalog, tilesets, tables, visualOverrides) {
  const entry = {
    x,
    y,
    tileId: tile.id,
    tileName: TILE_NAMES[tile.id] ?? 'Unknown',
    solid: tile.id !== TileId.Air,
    light: tile.light,
    autotile: {},
  };

  const groundName = getGroundTilesetName(tile.id, catalog);
  if (groundName && tilesets.has(groundName)) {
    const ts = tilesets.get(groundName);
    const sharesGround = (nx, ny) => sharesGroundAutotileGroup(tile.id, getTile(space, nx, ny).id, catalog);
    const isSolid = (nx, ny) => getTile(space, nx, ny).id !== TileId.Air;
    const isSurfaceTile = (nx, ny) => {
      const surface = getTile(space, nx, ny);
      return surface.id === TileId.Grass && getTile(space, nx, ny + 1).id === TileId.Air;
    };
    const maskBuild = buildGroundMaskDetailed(sharesGround, x, y, isSolid, isSurfaceTile);
    const mask = maskBuild.finalMask;
    const rules = ts.rules ?? getRulesForSpriteCount(ts.spriteCount ?? GROUND_SPRITE_COUNT, tables);
    const resolved = resolveSpriteId(ts, mask);
    entry.autotile.ground = {
      tileset: groundName,
      materialGroup: groundName,
      visualMask: maskToJson(maskBuild.visualMask),
      solidMask: maskBuild.solidMask ? maskToJson(maskBuild.solidMask) : null,
      connectivityMask: maskToJson(maskBuild.connectivityMask),
      rawMask: maskToJson(maskBuild.connectivityMask),
      normalizedMask: maskToJson(mask),
      mask: maskToJson(mask),
      normalization: maskBuild.normalization,
      normalizationTrace: maskBuild.normalizationTrace,
      matchingSpriteIds: findMatchingSpriteIds(rules, mask),
      matchedRuleId: resolved.spriteId,
      spriteId: resolved.spriteId,
      flipX: resolved.flipX,
      finalSpriteId: resolved.spriteId,
      partnerSubstitution: false,
      neighborTileIds: buildNeighborTileIds(space, x, y),
      neighborMaterials: buildNeighborMaterials(space, x, y),
      resolved: resolved.resolved,
    };
    applyVisualOverride(entry.autotile.ground, visualOverrides.get(overrideKey(x, y, 'ground')));
  }

  const above = getTile(space, x, y + 1);
  if (shouldRenderGrassCover(tile.id, above)) {
    const coverName = getCoverTilesetName(tile.id, catalog);
    const ts = tilesets.get(coverName);
    if (ts) {
      const mask = buildCoverMask(
        (nx, ny) => getTile(space, nx, ny).id !== TileId.Air,
        x,
        y,
      );
      const rules = ts.rules ?? tables.cover.rules;
      const resolved = resolveSpriteId(ts, mask);
      entry.autotile.cover = {
        rendered: true,
        tileset: coverName,
        mask: maskToJson(mask),
        matchingSpriteIds: findMatchingSpriteIds(rules, mask),
        spriteId: resolved.spriteId,
        flipX: resolved.flipX,
        resolved: resolved.resolved,
      };
      applyVisualOverride(entry.autotile.cover, visualOverrides.get(overrideKey(x, y, 'cover')));
    }
  } else {
    entry.autotile.cover = {
      rendered: false,
      reason:
        tile.id === TileId.Air
          ? 'Cover applies to solid ground tiles only.'
          : 'Cover requires air directly above the tile.',
    };
  }

  return entry;
}

function buildNeighborTileIds(space, x, y) {
  const ids = [];
  for (let dx = -1; dx <= 1; dx++) {
    const column = [];
    for (let dy = 1; dy >= -1; dy--) {
      column.push(getTile(space, x + dx, y + dy).id);
    }
    ids.push(column);
  }
  return ids;
}

function buildNeighborMaterials(space, x, y) {
  const labels = ['NW', 'N', 'NE', 'W', 'E', 'SW', 'S', 'SE'];
  const offsets = [
    [-1, 1],
    [0, 1],
    [1, 1],
    [-1, 0],
    [1, 0],
    [-1, -1],
    [0, -1],
    [1, -1],
  ];
  const materials = {};
  for (let i = 0; i < labels.length; i++) {
    const [dx, dy] = offsets[i];
    const neighbor = getTile(space, x + dx, y + dy);
    materials[labels[i]] = TILE_NAMES[neighbor.id] ?? 'Unknown';
  }
  return materials;
}

export function assertExpectations(space, report, options = {}) {
  const which = options.which ?? 'baseline';
  const expect =
    which === 'target'
      ? space.targetExpect ?? space.expect ?? []
      : space.baselineExpect ?? space.expect ?? [];
  const errors = [];
  for (const exp of expect) {
    const found = report.tiles.find((t) => t.x === exp.x && t.y === exp.y);
    if (!found) {
      errors.push(`(${exp.x},${exp.y}): tile not in report`);
      continue;
    }
    if (exp.ground?.spriteId !== undefined) {
      const got = found.autotile.ground?.spriteId;
      if (got !== exp.ground.spriteId) {
        errors.push(`(${exp.x},${exp.y}) ground: expected sprite ${exp.ground.spriteId}, got ${got}`);
      }
      if (exp.ground.flipX !== undefined && found.autotile.ground?.flipX !== exp.ground.flipX) {
        errors.push(`(${exp.x},${exp.y}) ground flipX: expected ${exp.ground.flipX}, got ${found.autotile.ground?.flipX}`);
      }
    }
    if (exp.cover) {
      if (exp.cover.rendered === false) {
        if (found.autotile.cover?.rendered !== false) {
          errors.push(`(${exp.x},${exp.y}) cover: expected not rendered`);
        }
      } else if (exp.cover.spriteId !== undefined) {
        const got = found.autotile.cover?.spriteId;
        if (got !== exp.cover.spriteId) {
          errors.push(`(${exp.x},${exp.y}) cover: expected sprite ${exp.cover.spriteId}, got ${got}`);
        }
        if (exp.cover.flipX !== undefined && found.autotile.cover?.flipX !== exp.cover.flipX) {
          errors.push(`(${exp.x},${exp.y}) cover flipX: expected ${exp.cover.flipX}, got ${found.autotile.cover?.flipX}`);
        }
      }
    }
  }
  return errors;
}
