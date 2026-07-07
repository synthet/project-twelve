// Mask builder parity tests mirroring AutotileVisualTests material-boundary cases.

import { test } from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import {
  buildConnectivityGroundMask,
  buildGroundMaskDetailed,
  buildSolidGroundMask,
  buildVisualGroundMask,
  tryRemapCavityInnerEdgeMask,
  tryRemapMaterialBoundaryCornerMask,
} from '../src/visual/maskBuilder.js';
import { loadRuleTables } from '../src/visual/ruleTables.js';
import { resolveSpriteId } from '../src/visual/resolver.js';
import { patternToMask } from '../src/visual/rule.js';
import { loadTileSpaceFromFile } from '../src/io/tileSpace.js';
import { buildAutotileReport } from '../src/report/autotileJson.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const DATA_DIR = path.join(__dirname, '..', 'data');

function tileset() {
  const tables = loadRuleTables(DATA_DIR);
  return { spriteCount: 32, rules: tables.ground.rules };
}

test('visual mask excludes foreign stone on cardinal sides', () => {
  const worldX = 10;
  const worldY = 5;
  const sharesGround = (x, y) => x === worldX && (y === worldY || y === worldY + 1);
  const visual = buildVisualGroundMask(sharesGround, worldX, worldY);
  assert.equal(visual[0][1], 0, 'stone west must not appear in visual mask');
  assert.equal(visual[2][1], 0);
});

test('solid mask includes foreign stone on all sides', () => {
  const worldX = 10;
  const worldY = 5;
  const isSolid = (x, y) => x === worldX && (y === worldY - 1 || y === worldY || y === worldY + 1);
  const solid = buildSolidGroundMask(isSolid, worldX, worldY);
  assert.equal(solid[1][2], 1, 'foreign solid below counts in solid mask');
  assert.equal(solid[0][1], 0);
});

test('connectivity mask matches visual mask (vendor GetMask — no foreign-solid blend)', () => {
  const worldX = 10;
  const worldY = 4;
  const sharesGround = (x, y) => x === worldX && (y === worldY || y === worldY - 1 || y === worldY - 2);
  const isSolid = (x, y) =>
    (x === worldX && (y === worldY || y === worldY - 1 || y === worldY - 2))
    || (x === worldX + 1 && y === worldY - 2);
  const visual = buildVisualGroundMask(sharesGround, worldX, worldY);
  const connectivity = buildConnectivityGroundMask(sharesGround, worldX, worldY, isSolid);
  assert.deepEqual(connectivity, visual, 'foreign solid beside/below must not change connectivity');
  assert.equal(connectivity[1][0], 0, 'stone must not connect to foreign dirt above');
});

test('material boundary resolves vendor rules without normalization', () => {
  const sharesGround = (x, y) => {
    if (x === 2 && y === 20) {
      return x === 2 && (y === 20 || y === 19);
    }
    return x >= 0 && x <= 2 && y === 20;
  };
  const isSolid = (x, y) => {
    if (y === 19 && x >= 0 && x <= 3) {
      return true;
    }
    if (y === 20 && x >= 0 && x <= 3) {
      return true;
    }
    return false;
  };
  const build = buildGroundMaskDetailed(sharesGround, 2, 20, isSolid);
  assert.equal(build.normalization.materialBoundary, false);
  const result = resolveSpriteId(tileset(), build.finalMask);
  assert.equal(result.spriteId, '24');
  assert.equal(result.flipX, true);
});

test('re-entrant dirt beside stone resolves vendor corner 16 not interior fill 9/10', () => {
  const space = loadTileSpaceFromFile(path.join(__dirname, 'fixtures', 'snippets', 'dirt-stone-reentrant-west.json'));
  const report = buildAutotileReport(space);
  const tile = report.tiles.find((t) => t.x === 1 && t.y === 1);
  assert.equal(tile.autotile.ground.spriteId, '16');
  assert.notEqual(tile.autotile.ground.spriteId, '9');
  assert.notEqual(tile.autotile.ground.spriteId, '10');
});

test('pillar mask stays 21 not 22 or 25', () => {
  const tables = loadRuleTables(DATA_DIR);
  const rule21 = tables.ground.rules.find((r) => r.spriteId === '21');
  const mask = patternToMask(rule21.pattern);
  const result = resolveSpriteId(tileset(), mask);
  assert.equal(result.spriteId, '21');
  assert.equal(result.flipX, false);
  assert.notEqual(result.spriteId, '22');
  assert.notEqual(result.spriteId, '25');
});

test('cavity inner-corner south notch is not flattened to underside 17', () => {
  const mask = [
    [1, 1, 1],
    [1, 1, 1],
    [1, 1, 0],
  ];
  const sharesGround = (x, y) => y === 29;
  const isSolid = (x, y) => {
    if (y === 30) return true;
    if (y === 29) return true;
    if (y === 28 && x === -113) return false;
    if (y === 28) return true;
    return false;
  };
  const remapped = tryRemapCavityInnerEdgeMask(mask, sharesGround, isSolid, -114, 29);
  assert.equal(remapped, null);
  const result = resolveSpriteId(tileset(), mask);
  assert.equal(result.spriteId, '18');
  assert.equal(result.flipX, true);
});

test('dirt-window-inner-edges flat lintel middle still resolves 17', () => {
  const space = loadTileSpaceFromFile(path.join(__dirname, 'fixtures', 'snippets', 'dirt-window-inner-edges.json'));
  const report = buildAutotileReport(space);
  const tile = report.tiles.find((t) => t.x === -113 && t.y === 29);
  assert.equal(tile.autotile.ground.spriteId, '17');
  assert.equal(tile.autotile.ground.normalization.innerCavity, false);
});

test('foreign solid below does not trigger cavity underside remap', () => {
  const mask = [
    [0, 1, 0],
    [0, 1, 0],
    [0, 1, 0],
  ];
  const sharesGround = (x, y) => y === 5;
  const isSolid = (x, y) => y === 4 || y === 5;
  const remapped = tryRemapMaterialBoundaryCornerMask(mask, sharesGround, isSolid, 5, 5);
  assert.equal(remapped, null);
});
