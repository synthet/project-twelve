// Loads Unity-exported autotile rule tables from data/*.json.

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const DATA_DIR = path.join(__dirname, '..', '..', 'data');

export const GROUND_SPRITE_COUNT = 32;
export const FALLBACK_SPRITE_ID = '20';

/** @type {{ ground: object, cover: object } | null} */
let cache = null;

function loadJson(name) {
  const file = path.join(DATA_DIR, name);
  return JSON.parse(fs.readFileSync(file, 'utf8'));
}

export function loadRuleTables(dataDir = DATA_DIR) {
  if (dataDir === DATA_DIR && cache) {
    return cache;
  }
  const ground = JSON.parse(fs.readFileSync(path.join(dataDir, 'autotile-rules.ground.json'), 'utf8'));
  const cover = JSON.parse(fs.readFileSync(path.join(dataDir, 'autotile-rules.cover.json'), 'utf8'));
  const tables = { ground, cover };
  if (dataDir === DATA_DIR) {
    cache = tables;
  }
  return tables;
}

/**
 * @param {number} spriteCount
 * @param {{ ground: object, cover: object }} tables
 */
export function getRulesForSpriteCount(spriteCount, tables = loadRuleTables()) {
  return spriteCount === GROUND_SPRITE_COUNT ? tables.ground.rules : tables.cover.rules;
}

export function getFallbackSpriteId(spriteCount = GROUND_SPRITE_COUNT, tables = loadRuleTables()) {
  return spriteCount === GROUND_SPRITE_COUNT
    ? (tables.ground.fallbackSpriteId ?? FALLBACK_SPRITE_ID)
    : '0';
}
