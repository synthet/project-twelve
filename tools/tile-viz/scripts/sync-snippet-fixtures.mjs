#!/usr/bin/env node
// Export expected/*.json from snippets via Node resolver (parity with Unity export shape).

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { loadTileSpaceFromFile } from '../src/io/tileSpace.js';
import { buildAutotileReport } from '../src/report/autotileJson.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const SNIPPETS_DIR = path.join(__dirname, '..', 'test', 'fixtures', 'snippets');
const EXPECTED_DIR = path.join(__dirname, '..', 'test', 'fixtures', 'expected');

function toExpected(report) {
  return {
    name: report.name,
    tiles: report.tiles.map((t) => {
      const entry = {
        x: t.x,
        y: t.y,
        tileId: t.tileId,
        autotile: {},
      };
      if (t.autotile.ground) {
        entry.autotile.ground = {
          tileset: t.autotile.ground.tileset,
          materialGroup: t.autotile.ground.materialGroup,
          rawMask: t.autotile.ground.rawMask,
          normalizedMask: t.autotile.ground.normalizedMask,
          mask: t.autotile.ground.mask,
          matchingSpriteIds: t.autotile.ground.matchingSpriteIds,
          matchedRuleId: t.autotile.ground.matchedRuleId,
          spriteId: t.autotile.ground.spriteId,
          flipX: t.autotile.ground.flipX,
          finalSpriteId: t.autotile.ground.finalSpriteId,
          partnerSubstitution: t.autotile.ground.partnerSubstitution,
          neighborTileIds: t.autotile.ground.neighborTileIds,
          resolved: t.autotile.ground.resolved,
        };
      }
      if (t.autotile.cover) {
        entry.autotile.cover = { ...t.autotile.cover };
      }
      return entry;
    }),
  };
}

fs.mkdirSync(EXPECTED_DIR, { recursive: true });
for (const file of fs.readdirSync(SNIPPETS_DIR).filter((f) => f.endsWith('.json'))) {
  const space = loadTileSpaceFromFile(path.join(SNIPPETS_DIR, file));
  const report = buildAutotileReport(space);
  const out = path.join(EXPECTED_DIR, file);
  fs.writeFileSync(out, `${JSON.stringify(toExpected(report), null, 2)}\n`);
  process.stderr.write(`wrote ${out}\n`);
}
