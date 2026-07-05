#!/usr/bin/env node
// Compare two project-twelve/tile-space/v1 space fixtures over overlapping bounds.
//
// Usage:
//   node scripts/diff-tile-space.mjs <spaceA.json> <spaceB.json> [--max-examples N]

import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { loadTileSpaceFromFile } from '../src/io/tileSpace.js';
import { diffTileSpaces } from '../src/io/diffTileSpace.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

function parseArgs(argv) {
  const positional = [];
  const opts = { maxExamples: 50 };
  for (let i = 0; i < argv.length; i++) {
    const arg = argv[i];
    if (arg === '--max-examples' && argv[i + 1]) {
      opts.maxExamples = Number(argv[++i]);
    } else if (!arg.startsWith('--')) {
      positional.push(arg);
    }
  }
  return { positional, opts };
}

function main() {
  const { positional, opts } = parseArgs(process.argv.slice(2));
  if (positional.length < 2) {
    console.error('usage: diff-tile-space.mjs <spaceA.json> <spaceB.json> [--max-examples N]');
    process.exit(2);
  }

  const spaceA = loadTileSpaceFromFile(path.resolve(positional[0]));
  const spaceB = loadTileSpaceFromFile(path.resolve(positional[1]));
  const result = diffTileSpaces(spaceA, spaceB, opts);

  const summary = {
    spaceA: spaceA.name,
    spaceB: spaceB.name,
    diffCount: result.count,
    diffBounds:
      result.count > 0
        ? { xMin: result.xMin, yMin: result.yMin, xMax: result.xMax, yMax: result.yMax }
        : null,
    overlap: result.overlap,
    examples: result.examples,
  };

  console.log(JSON.stringify(summary, null, 2));
  process.exit(result.count > 0 ? 1 : 0);
}

main();
