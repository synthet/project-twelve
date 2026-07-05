#!/usr/bin/env node
// Emit full dual-mask debug rows for target cells (MCP-style report fields).
//
// Usage:
//   node scripts/log-autotile-debug-cells.mjs <snippet.json|space.json> [--compact] [x y ...]
//
// When coordinates are omitted, iterates baselineExpect, then targetExpect, then expect.

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { loadTileSpaceFromFile } from '../src/io/tileSpace.js';
import { buildAutotileReport } from '../src/report/autotileJson.js';

function parseArgs(argv) {
  const positional = [];
  let compact = false;
  const coords = [];

  for (let i = 0; i < argv.length; i++) {
    const arg = argv[i];
    if (arg === '--compact') {
      compact = true;
    } else if (!arg.startsWith('--')) {
      positional.push(arg);
    }
  }

  const snippetPath = positional[0];
  for (let i = 1; i + 1 < positional.length; i += 2) {
    if (Number.isFinite(Number(positional[i])) && Number.isFinite(Number(positional[i + 1]))) {
      coords.push({ x: Number(positional[i]), y: Number(positional[i + 1]) });
    }
  }

  return { snippetPath, compact, coords };
}

function collectExpectCoords(space) {
  const seen = new Set();
  const out = [];
  const lists = [space.baselineExpect, space.targetExpect, space.expect].filter(Boolean);
  for (const list of lists) {
    for (const exp of list) {
      const key = `${exp.x},${exp.y}`;
      if (seen.has(key)) {
        continue;
      }
      seen.add(key);
      out.push({ x: exp.x, y: exp.y });
    }
  }
  return out;
}

function formatRow(tile) {
  const g = tile.autotile.ground;
  return {
    x: tile.x,
    y: tile.y,
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
  };
}

function main() {
  const { snippetPath, compact, coords } = parseArgs(process.argv.slice(2));
  if (!snippetPath) {
    console.error('usage: log-autotile-debug-cells.mjs <snippet.json|space.json> [--compact] [x y ...]');
    process.exit(1);
  }

  const space = loadTileSpaceFromFile(path.resolve(snippetPath));
  const report = buildAutotileReport(space);
  const byKey = new Map(report.tiles.map((t) => [`${t.x},${t.y}`, t]));
  const targets = coords.length > 0 ? coords : collectExpectCoords(space);

  if (targets.length === 0) {
    console.error('no coordinates: pass x y pairs or add baselineExpect/targetExpect/expect to fixture');
    process.exit(1);
  }

  for (const target of targets) {
    const tile = byKey.get(`${target.x},${target.y}`);
    if (!tile?.autotile?.ground) {
      console.log(`(${target.x},${target.y}) missing ground report`);
      continue;
    }

    const row = formatRow(tile);
    console.log(compact ? JSON.stringify(row) : JSON.stringify(row, null, 2));
  }
}

main();
