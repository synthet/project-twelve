// Resolver parity: rule tables and vendor-style masks match expected sprite ids.

import { test } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { loadRuleTables, GROUND_SPRITE_COUNT, FALLBACK_SPRITE_ID } from '../src/visual/ruleTables.js';
import { buildGroundMask, buildCoverMask } from '../src/visual/maskBuilder.js';
import { resolveSpriteId, findMatchingSpriteIds } from '../src/visual/resolver.js';
import { patternToMask, ruleMatches } from '../src/visual/rule.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const DATA_DIR = path.join(__dirname, '..', 'data');

test('ground rules JSON has 32 entries and cover has 6', () => {
  const tables = loadRuleTables(DATA_DIR);
  assert.equal(tables.ground.rules.length, 32);
  assert.equal(tables.cover.rules.length, 6);
  assert.equal(tables.ground.fallbackSpriteId, FALLBACK_SPRITE_ID);
  assert.equal(tables.ground.groundSpriteCount, GROUND_SPRITE_COUNT);
});

test('isolated center mask resolves to fallback sprite 20', () => {
  const tables = loadRuleTables(DATA_DIR);
  const mask = [
    [0, 0, 0],
    [0, 1, 0],
    [0, 0, 0],
  ];
  const tileset = { spriteCount: 32, rules: tables.ground.rules };
  const result = resolveSpriteId(tileset, mask);
  assert.equal(result.spriteId, '20');
  assert.equal(result.flipX, false);
});

test('surrounded interior resolves to weighted sprite 9 or 10', () => {
  const tables = loadRuleTables(DATA_DIR);
  const mask = [
    [1, 1, 1],
    [1, 1, 1],
    [1, 1, 1],
  ];
  const tileset = { spriteCount: 32, rules: tables.ground.rules };
  const result = resolveSpriteId(tileset, mask);
  assert.ok(result.spriteId === '9' || result.spriteId === '10');
});

test('vertical run mask resolves to sprite 21', () => {
  const tables = loadRuleTables(DATA_DIR);
  const rule21 = tables.ground.rules.find((r) => r.spriteId === '21');
  const mask = patternToMask(rule21.pattern);
  const tileset = { spriteCount: 32, rules: tables.ground.rules };
  const result = resolveSpriteId(tileset, mask);
  assert.equal(result.spriteId, '21');
});

test('horizontal grass cover middle resolves to sprite 4', () => {
  const tables = loadRuleTables(DATA_DIR);
  const mask = buildCoverMask(
    (x, y) => y === 5 && (x === 4 || x === 6),
    () => false,
    5,
    5,
  );
  const tileset = { spriteCount: 6, rules: tables.cover.rules };
  const result = resolveSpriteId(tileset, mask);
  assert.equal(result.spriteId, '4');
});

test('grass cover east end resolves to sprite 3 with flipX', () => {
  const tables = loadRuleTables(DATA_DIR);
  const mask = buildCoverMask(
    (x, y) => y === 5 && x === 4,
    () => false,
    5,
    5,
  );
  const tileset = { spriteCount: 6, rules: tables.cover.rules };
  const result = resolveSpriteId(tileset, mask);
  assert.equal(result.spriteId, '3');
  assert.equal(result.flipX, true);
});

test('ground east run end resolves to rule 0 with flipX', () => {
  const tables = loadRuleTables(DATA_DIR);
  const mask = [
    [0, 1, 1],
    [0, 1, 1],
    [0, 0, 0],
  ];
  const tileset = { spriteCount: 32, rules: tables.ground.rules };
  const result = resolveSpriteId(tileset, mask);
  assert.equal(result.spriteId, '0');
  assert.equal(result.flipX, true);
});

test('one-sided lip column mirror resolves to rule 24 with flipX', () => {
  const tables = loadRuleTables(DATA_DIR);
  const mask = [
    [0, 1, 0],
    [0, 1, 0],
    [0, 0, 0],
  ];
  const tileset = { spriteCount: 32, rules: tables.ground.rules };
  const result = resolveSpriteId(tileset, mask);
  assert.equal(result.spriteId, '24');
  assert.equal(result.flipX, true);
  assert.notEqual(result.spriteId, '25');
});

test('vertical pillar mask stays sprite 21 without flipX', () => {
  const tables = loadRuleTables(DATA_DIR);
  const rule21 = tables.ground.rules.find((r) => r.spriteId === '21');
  const mask = patternToMask(rule21.pattern);
  const tileset = { spriteCount: 32, rules: tables.ground.rules };
  const result = resolveSpriteId(tileset, mask);
  assert.equal(result.spriteId, '21');
  assert.equal(result.flipX, false);
  assert.notEqual(result.spriteId, '22');
  assert.notEqual(result.spriteId, '25');
});

test('bridge-shaped mask remains sprite 25 and is not used for vertical pillar remap', () => {
  const tables = loadRuleTables(DATA_DIR);
  const mask = [
    [0, 1, 0],
    [0, 1, 0],
    [0, 1, 0],
  ];
  const tileset = { spriteCount: 32, rules: tables.ground.rules };
  const result = resolveSpriteId(tileset, mask);
  assert.equal(result.spriteId, '25');
  assert.equal(result.flipX, false);
});

test('side-mass vertical mask resolves to sprite 22 only for its own topology', () => {
  const tables = loadRuleTables(DATA_DIR);
  const mask = [
    [0, 0, 0],
    [1, 1, 1],
    [0, 1, 1],
  ];
  const tileset = { spriteCount: 32, rules: tables.ground.rules };
  const result = resolveSpriteId(tileset, mask);
  assert.equal(result.spriteId, '22');
  assert.equal(result.flipX, false);
});

test('cover unmatched mask falls back to sprite 0', () => {
  const tables = loadRuleTables(DATA_DIR);
  const mask = [
    [0, 0, 0],
    [0, 1, 0],
    [0, 0, 0],
  ];
  const tileset = { spriteCount: 6, rules: tables.cover.rules };
  const result = resolveSpriteId(tileset, mask);
  assert.equal(result.spriteId, '0');
});

test('each ground rule pattern matches itself', () => {
  const tables = loadRuleTables(DATA_DIR);
  for (const rule of tables.ground.rules) {
    const mask = patternToMask(rule.pattern);
    assert.ok(ruleMatches(rule.pattern, mask, false), `rule ${rule.spriteId} should match its own pattern`);
  }
});

const EXPECTED_DIR = path.join(__dirname, 'fixtures', 'expected');
const expectedExists = fs.existsSync(EXPECTED_DIR) && fs.readdirSync(EXPECTED_DIR).length > 0;

test('JS resolver matches Unity-exported expected fixtures', {
  skip: expectedExists ? false : 'no expected/*.json yet (run Unity EditMode AutotileFixtureExportTests)',
}, () => {
  for (const file of fs.readdirSync(EXPECTED_DIR)) {
    if (!file.endsWith('.json')) continue;
    const expected = JSON.parse(fs.readFileSync(path.join(EXPECTED_DIR, file), 'utf8'));
    if (!Array.isArray(expected.tiles)) continue;
    for (const tile of expected.tiles) {
      const ground = tile.autotile?.ground ?? tile.ground;
      if (ground?.mask) {
        const tileset = { spriteCount: 32, rules: loadRuleTables(DATA_DIR).ground.rules };
        const mask = ground.mask.map((col) => col.slice());
        const result = resolveSpriteId(tileset, mask);
        assert.equal(
          result.spriteId,
          ground.spriteId,
          `${file} (${tile.x},${tile.y}) ground sprite`,
        );
        if (ground.flipX !== undefined) {
          assert.equal(result.flipX, ground.flipX);
        }
      }
    }
  }
});
