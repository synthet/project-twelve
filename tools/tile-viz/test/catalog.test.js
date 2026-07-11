// Catalog tileset grouping and cover gating.

import { test } from 'node:test';
import assert from 'node:assert/strict';

import {
  DEFAULT_CATALOG,
  getGroundTilesetName,
  sharesGroundAutotileGroup,
  shouldRenderGrassCover,
  getCoverTilesetName,
} from '../src/visual/catalog.js';
import { TileId } from '../../world-viz/src/core/tiles.js';

test('getGroundTilesetName maps core materials and ores', () => {
  assert.equal(getGroundTilesetName(TileId.Dirt), 'Humus');
  assert.equal(getGroundTilesetName(TileId.Grass), 'Humus');
  assert.equal(getGroundTilesetName(TileId.Stone), 'Rocks');
  assert.equal(getGroundTilesetName(TileId.CopperOre), 'BricksA');
  assert.equal(getGroundTilesetName(TileId.IronOre), 'BricksB');
  assert.equal(getGroundTilesetName(TileId.SilverOre), 'BricksC');
  assert.equal(getGroundTilesetName(TileId.GoldOre), 'BricksD');
  assert.equal(getGroundTilesetName(TileId.Air), null);
});

test('sharesGroundAutotileGroup connects same tileset only', () => {
  assert.equal(sharesGroundAutotileGroup(TileId.Dirt, TileId.Grass), true);
  assert.equal(sharesGroundAutotileGroup(TileId.Dirt, TileId.Stone), false);
  assert.equal(sharesGroundAutotileGroup(TileId.CopperOre, TileId.IronOre), false);
  assert.equal(sharesGroundAutotileGroup(TileId.CopperOre, TileId.Dirt), false);
  assert.equal(sharesGroundAutotileGroup(TileId.Air, TileId.Dirt), false);
});

test('shouldRenderGrassCover gates on grass material with an exposed top', () => {
  assert.equal(shouldRenderGrassCover(TileId.Grass, { id: TileId.Air }), true);
  assert.equal(shouldRenderGrassCover(TileId.Dirt, { id: TileId.Air }), false);
  assert.equal(shouldRenderGrassCover(TileId.Stone, { id: TileId.Air }), false);
  assert.equal(shouldRenderGrassCover(TileId.CopperOre, { id: TileId.Air }), false);
  assert.equal(shouldRenderGrassCover(TileId.Grass, { id: TileId.Dirt }), false);
  assert.equal(shouldRenderGrassCover(TileId.Air, { id: TileId.Air }), false);
});

test('getCoverTilesetName returns GrassA only for grass', () => {
  assert.equal(getCoverTilesetName(TileId.Grass, DEFAULT_CATALOG), 'GrassA');
  assert.equal(getCoverTilesetName(TileId.Stone, DEFAULT_CATALOG), null);
  assert.equal(getCoverTilesetName(TileId.CopperOre, DEFAULT_CATALOG), null);
  assert.equal(getCoverTilesetName(TileId.Air, DEFAULT_CATALOG), null);
});
