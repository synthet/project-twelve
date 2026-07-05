#!/usr/bin/env node
// Emit full dual-mask debug rows for target cells (MCP-style report fields).
//
// Usage:
//   node scripts/log-autotile-debug-cells.mjs test/fixtures/snippets/roof-slope-left-vs-right.json

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { loadTileSpaceFromFile } from '../src/io/tileSpace.js';
import { buildAutotileReport } from '../src/report/autotileJson.js';

const snippetPath = process.argv[2];
if (!snippetPath) {
  console.error('usage: log-autotile-debug-cells.mjs <snippet.json>');
  process.exit(1);
}

const space = loadTileSpaceFromFile(path.resolve(snippetPath));
const report = buildAutotileReport(space);
const byKey = new Map(report.tiles.map((t) => [`${t.x},${t.y}`, t]));

for (const exp of space.expect ?? []) {
  const tile = byKey.get(`${exp.x},${exp.y}`);
  if (!tile?.autotile?.ground) {
    console.log(`(${exp.x},${exp.y}) missing ground report`);
    continue;
  }

  const g = tile.autotile.ground;
  console.log(JSON.stringify({
    x: exp.x,
    y: exp.y,
    tileId: tile.tileId,
    tileName: tile.tileName,
    visualMask: g.visualMask,
    solidMask: g.solidMask,
    connectivityMask: g.connectivityMask,
    normalizedMask: g.normalizedMask,
    spriteId: g.spriteId,
    flipX: g.flipX,
    finalSpriteId: g.finalSpriteId,
    partnerSubstitution: g.partnerSubstitution,
    normalization: g.normalization,
    neighborMaterials: g.neighborMaterials,
    cover: tile.autotile.cover,
  }, null, 2));
}
