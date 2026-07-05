#!/usr/bin/env node
// Compare an autotile baseline against a report, MCP dump, or second baseline.
//
// Usage:
//   node scripts/compare-autotile-baseline.mjs <baseline.json> <actual.json|->
//     [--only ground|cover|all] [--max-diffs N]
//     [--region xmin xmax ymin ymax]
//     [--coords x,y x,y ...] [--compact]

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { buildAutotileReport } from '../src/report/autotileJson.js';
import { compareAutotileBaseline } from '../src/report/autotileCompare.js';
import { loadTileSpaceFromFile } from '../src/io/tileSpace.js';

function parseArgs(argv) {
  const positional = [];
  const opts = {
    only: 'all',
    maxDiffs: 100,
    region: null,
    coords: null,
    compact: false,
  };

  for (let i = 0; i < argv.length; i++) {
    const arg = argv[i];
    if (arg === '--only' && argv[i + 1]) {
      opts.only = argv[++i];
    } else if (arg === '--max-diffs' && argv[i + 1]) {
      opts.maxDiffs = Number(argv[++i]);
    } else if (arg === '--region' && argv[i + 4]) {
      opts.region = {
        xMin: Number(argv[++i]),
        xMax: Number(argv[++i]),
        yMin: Number(argv[++i]),
        yMax: Number(argv[++i]),
      };
    } else if (arg === '--coords') {
      opts.coords = new Set();
      while (argv[i + 1] && !argv[i + 1].startsWith('--')) {
        const [x, y] = argv[++i].split(',').map(Number);
        opts.coords.add(`${x},${y}`);
      }
    } else if (arg === '--compact') {
      opts.compact = true;
    } else if (!arg.startsWith('--')) {
      positional.push(arg);
    }
  }

  return { positional, opts };
}

function loadActualDoc(actualPath) {
  const text =
    actualPath === '-'
      ? fs.readFileSync(0, 'utf8')
      : fs.readFileSync(path.resolve(actualPath), 'utf8');
  const doc = JSON.parse(text);

  if (doc.format === 'project-twelve/autotile-report/v1') {
    return doc;
  }
  if (doc.format === 'project-twelve/autotile-baseline/v1') {
    return doc;
  }
  if (doc.tiles?.[0]?.autotile) {
    return doc;
  }
  if (doc.kind || doc.xMin !== undefined) {
    const space = loadTileSpaceFromFile(path.resolve(actualPath));
    return buildAutotileReport(space);
  }

  throw new Error('actual document must be autotile baseline, report, MCP dump, or tile-space');
}

function main() {
  const { positional, opts } = parseArgs(process.argv.slice(2));
  if (positional.length < 2) {
    console.error(
      'usage: compare-autotile-baseline.mjs <baseline.json> <actual.json|-> [--only ground|cover|all] ...',
    );
    process.exit(2);
  }

  const baseline = JSON.parse(fs.readFileSync(path.resolve(positional[0]), 'utf8'));
  const actual = loadActualDoc(positional[1]);
  const result = compareAutotileBaseline(baseline, actual, opts);

  if (opts.compact) {
    for (const diff of result.diffs) {
      console.log(JSON.stringify({ x: diff.x, y: diff.y, errors: diff.errors }));
    }
    console.log(JSON.stringify(result.summary));
  } else {
    console.log(JSON.stringify(result, null, 2));
  }

  const mismatchCount = result.summary.mismatched + result.summary.missingInActual;
  process.exit(mismatchCount > 0 ? 1 : 0);
}

main();
