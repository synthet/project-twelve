#!/usr/bin/env node
// One-shot helper: regenerate autotile-rules.*.json from the same constants as
// AutotileRuleTables.cs. Unity EditMode export is authoritative; run this only
// when Unity is unavailable and C# tables changed.

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const dataDir = path.join(__dirname, '..', 'data');

function M(...values) {
  const mask = Array.from({ length: 3 }, () => [0, 0, 0]);
  for (let i = 0; i < values.length; i++) {
    mask[i % 3][Math.floor(i / 3)] = values[i];
  }
  return flattenMask(mask);
}

function flattenMask(mask) {
  const flat = [];
  for (let y = 0; y < 3; y++) {
    for (let x = 0; x < 3; x++) {
      flat.push(mask[x][y]);
    }
  }
  return flat;
}

function rule(spriteId, pattern, weight = 1) {
  return { spriteId, weight, pattern };
}

const ground = [
  rule('0', M(0, 0, 0, 0, 1, 1, 0, 1, 1)),
  rule('1', M(0, 0, 0, 1, 1, 1, 1, 1, 1)),
  rule('2', M(0, 0, 0, 1, 1, 1, 1, 1, 1)),
  rule('3', M(1, 1, 0, 1, 1, 1, 0, 1, 1)),
  rule('4', M(0, 1, 0, 1, 1, 1, 1, 1, 0)),
  rule('5', M(0, 1, 0, 1, 1, 1, 1, 1, 1)),
  rule('6', M(0, 0, 0, 1, 1, 1, 0, 1, 0)),
  rule('7', M(0, 0, 0, 1, 1, 0, 0, 1, 0)),
  rule('8', M(0, 1, 1, 0, 1, 1, 0, 1, 1)),
  rule('9', M(1, 1, 1, 1, 1, 1, 1, 1, 1), 4),
  rule('10', M(1, 1, 1, 1, 1, 1, 1, 1, 1), 1),
  rule('11', M(0, 1, 1, 1, 1, 1, 1, 1, 1)),
  rule('12', M(1, 1, 0, 1, 1, 1, 0, 1, 0)),
  rule('13', M(1, 1, 1, 1, 1, 1, 0, 1, 0)),
  rule('14', M(0, 1, 0, 1, 1, 1, 0, 0, 0)),
  rule('15', M(0, 1, 0, 1, 1, 0, 0, 0, 0)),
  rule('16', M(0, 1, 1, 0, 1, 1, 0, 0, 0)),
  rule('17', M(1, 1, 1, 1, 1, 1, 0, 0, 0)),
  rule('18', M(1, 1, 1, 1, 1, 1, 0, 1, 1)),
  rule('19', M(1, 1, 0, 1, 1, 1, 1, 1, 0)),
  rule('20', M(0, 0, 0, 0, 1, 0, 0, 0, 0)),
  rule('21', M(0, 1, 0, 0, 1, 0, 0, 1, 0)),
  rule('22', M(0, 1, 0, 0, 1, 1, 0, 1, 1)),
  rule('23', M(0, 0, 0, 1, 1, 1, 1, 1, 0)),
  rule('24', M(0, 0, 0, 0, 1, 1, 0, 0, 0)),
  rule('25', M(0, 0, 0, 1, 1, 1, 0, 0, 0)),
  rule('26', M(0, 1, 0, 1, 1, 1, 0, 1, 0)),
  rule('27', M(0, 1, 0, 1, 1, 0, 0, 1, 0)),
  rule('28', M(0, 0, 0, 0, 1, 0, 0, 1, 0)),
  rule('29', M(0, 1, 0, 0, 1, 0, 0, 0, 0)),
  rule('30', M(0, 1, 1, 0, 1, 1, 0, 1, 0)),
  rule('31', M(1, 1, 0, 1, 1, 1, 0, 0, 0)),
];

const cover = [
  rule('2', M(0, 0, 0, 2, 1, 2, 0, 0, 0)),
  rule('1', M(0, 0, 0, 0, 1, 2, 0, 0, 0)),
  rule('5', M(0, 0, 0, 1, 1, 2, 0, 0, 0)),
  rule('3', M(0, 0, 0, 0, 1, 1, 0, 0, 0)),
  rule('4', M(0, 0, 0, 1, 1, 1, 0, 0, 0)),
  rule('0', M(0, 0, 0, 0, 1, 0, 0, 0, 0)),
];

function write(name, rules, groundSpriteCount) {
  const payload = {
    format: 'project-twelve/autotile-rules/v1',
    fallbackSpriteId: '20',
    groundSpriteCount,
    rules,
  };
  fs.mkdirSync(dataDir, { recursive: true });
  fs.writeFileSync(path.join(dataDir, name), `${JSON.stringify(payload, null, 2)}\n`);
}

write('autotile-rules.ground.json', ground, 32);
write('autotile-rules.cover.json', cover, 32);
console.log('Wrote autotile-rules.ground.json and autotile-rules.cover.json');
