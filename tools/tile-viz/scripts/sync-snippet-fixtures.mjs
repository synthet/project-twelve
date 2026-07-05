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

const only = process.argv.slice(2);

function toUnityExpected(report) {
  return {
    name: report.name,
    tiles: report.tiles.map((t) => {
      const entry = { x: t.x, y: t.y };
      if (t.autotile.ground) {
        const g = t.autotile.ground;
        entry.ground = {
          tileset: g.tileset,
          materialGroup: g.materialGroup,
          visualMask: g.visualMask,
          solidMask: g.solidMask,
          connectivityMask: g.connectivityMask,
          rawMask: g.rawMask ?? g.connectivityMask,
          normalizedMask: g.normalizedMask ?? g.mask,
          mask: g.mask,
          normalization: g.normalization,
          matchingSpriteIds: g.matchingSpriteIds,
          matchedRuleId: g.matchedRuleId,
          spriteId: g.spriteId,
          flipX: g.flipX,
          finalSpriteId: g.finalSpriteId,
          partnerSubstitution: g.partnerSubstitution,
          neighborTileIds: g.neighborTileIds,
          resolved: g.resolved,
        };
      }
      if (t.autotile.cover) {
        entry.cover = { ...t.autotile.cover };
      }
      return entry;
    }),
  };
}

fs.mkdirSync(EXPECTED_DIR, { recursive: true });
const files = fs.readdirSync(SNIPPETS_DIR).filter((f) => f.endsWith('.json'));
for (const file of files) {
  const base = file.replace(/\.json$/, '');
  if (only.length > 0 && !only.includes(base) && !only.includes(file)) {
    continue;
  }
  const space = loadTileSpaceFromFile(path.join(SNIPPETS_DIR, file));
  const report = buildAutotileReport(space);
  const out = path.join(EXPECTED_DIR, file);
  fs.writeFileSync(out, `${JSON.stringify(toUnityExpected(report), null, 2)}\n`);
  process.stderr.write(`wrote ${out}\n`);
}
