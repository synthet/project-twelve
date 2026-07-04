// MCP-style autotile debug report for a tile space.

import { buildCoverMask, buildGroundMask, maskToJson } from '../visual/maskBuilder.js';
import {
  DEFAULT_CATALOG,
  getCoverTilesetName,
  getGroundTilesetName,
  sharesCoverAutotileGroup,
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
        tiles.push(buildTileAutotile(space, worldX, worldY, tile, catalog, tilesets, tables));
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

function buildTileAutotile(space, x, y, tile, catalog, tilesets, tables) {
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
    const mask = buildGroundMask(
      (nx, ny) => sharesGroundAutotileGroup(tile.id, getTile(space, nx, ny).id, catalog),
      x,
      y,
    );
    const rules = ts.rules ?? getRulesForSpriteCount(ts.spriteCount ?? GROUND_SPRITE_COUNT, tables);
    const resolved = resolveSpriteId(ts, mask);
    entry.autotile.ground = {
      tileset: groundName,
      mask: maskToJson(mask),
      matchingSpriteIds: findMatchingSpriteIds(rules, mask),
      spriteId: resolved.spriteId,
      flipX: resolved.flipX,
      resolved: resolved.resolved,
    };
  }

  const above = getTile(space, x, y + 1);
  if (shouldRenderGrassCover(tile.id, above)) {
    const coverName = getCoverTilesetName(tile.id, catalog);
    const ts = tilesets.get(coverName);
    if (ts) {
      const mask = buildCoverMask(
        (nx, ny) => sharesCoverAutotileGroup(TileId.Grass, getTile(space, nx, ny).id),
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
    }
  } else if (tile.id === TileId.Grass) {
    entry.autotile.cover = {
      rendered: false,
      reason:
        above.id !== TileId.Air
          ? 'Cover requires air directly above the tile.'
          : 'Cover applies to grass surface tiles only.',
    };
  } else {
    entry.autotile.cover = { rendered: false, reason: 'Cover applies to grass surface tiles only.' };
  }

  return entry;
}

export function assertExpectations(space, report) {
  const expect = space.expect ?? [];
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
