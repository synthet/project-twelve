#!/usr/bin/env node
// Export a sparse autotile baseline from a tile-space fixture for drift RCA.
//
// Usage:
//   node scripts/export-autotile-baseline.mjs <space.json> [--out path.json]

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { loadTileSpaceFromFile } from '../src/io/tileSpace.js';
import { buildAutotileReport } from '../src/report/autotileJson.js';
import { buildBaselineDocument } from '../src/report/autotileCompare.js';

function parseArgs(argv) {
  const positional = [];
  const opts = { out: null };
  for (let i = 0; i < argv.length; i++) {
    const arg = argv[i];
    if (arg === '--out' && argv[i + 1]) {
      opts.out = argv[++i];
    } else if (!arg.startsWith('--')) {
      positional.push(arg);
    }
  }
  return { positional, opts };
}

function main() {
  const { positional, opts } = parseArgs(process.argv.slice(2));
  if (positional.length < 1) {
    console.error('usage: export-autotile-baseline.mjs <space.json> [--out path.json]');
    process.exit(2);
  }

  const spacePath = path.resolve(positional[0]);
  const space = loadTileSpaceFromFile(spacePath);
  const report = buildAutotileReport(space);
  const baseline = buildBaselineDocument(space, report);

  const outPath =
    opts.out ??
    path.join(
      path.dirname(spacePath),
      '..',
      'baselines',
      `${space.name ?? path.basename(spacePath, '.json')}-autotile.json`,
    );

  fs.mkdirSync(path.dirname(path.resolve(outPath)), { recursive: true });
  fs.writeFileSync(outPath, `${JSON.stringify(baseline, null, 2)}\n`);
  console.error(`wrote ${baseline.cellCount} cells to ${path.resolve(outPath)}`);
}

main();
