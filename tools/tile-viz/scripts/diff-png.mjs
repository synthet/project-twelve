#!/usr/bin/env node
// Per-pixel PNG diff for visual drift RCA (uses pngjs).
//
// Usage:
//   node scripts/diff-png.mjs <golden.png> <actual.png> [--out diff.png] [--threshold 0]

import fs from 'node:fs';
import path from 'node:path';
import { PNG } from 'pngjs';

function parseArgs(argv) {
  const positional = [];
  const opts = { out: null, threshold: 0 };
  for (let i = 0; i < argv.length; i++) {
    const arg = argv[i];
    if (arg === '--out' && argv[i + 1]) {
      opts.out = argv[++i];
    } else if (arg === '--threshold' && argv[i + 1]) {
      opts.threshold = Number(argv[++i]);
    } else if (!arg.startsWith('--')) {
      positional.push(arg);
    }
  }
  return { positional, opts };
}

function readPng(filePath) {
  return PNG.sync.read(fs.readFileSync(path.resolve(filePath)));
}

function main() {
  const { positional, opts } = parseArgs(process.argv.slice(2));
  if (positional.length < 2) {
    console.error('usage: diff-png.mjs <golden.png> <actual.png> [--out diff.png] [--threshold 0]');
    process.exit(2);
  }

  const golden = readPng(positional[0]);
  const actual = readPng(positional[1]);
  if (golden.width !== actual.width || golden.height !== actual.height) {
    console.error(
      JSON.stringify({
        error: 'dimension mismatch',
        golden: { width: golden.width, height: golden.height },
        actual: { width: actual.width, height: actual.height },
      }),
    );
    process.exit(2);
  }

  const diff = new PNG({ width: golden.width, height: golden.height });
  let mismatches = 0;

  for (let y = 0; y < golden.height; y++) {
    for (let x = 0; x < golden.width; x++) {
      const idx = (golden.width * y + x) << 2;
      const dr = Math.abs(golden.data[idx] - actual.data[idx]);
      const dg = Math.abs(golden.data[idx + 1] - actual.data[idx + 1]);
      const db = Math.abs(golden.data[idx + 2] - actual.data[idx + 2]);
      const delta = Math.max(dr, dg, db);

      if (delta > opts.threshold) {
        mismatches++;
        diff.data[idx] = 255;
        diff.data[idx + 1] = 0;
        diff.data[idx + 2] = 0;
        diff.data[idx + 3] = 255;
      } else {
        const gray = Math.round((golden.data[idx] + golden.data[idx + 1] + golden.data[idx + 2]) / 3);
        diff.data[idx] = gray;
        diff.data[idx + 1] = gray;
        diff.data[idx + 2] = gray;
        diff.data[idx + 3] = 255;
      }
    }
  }

  const summary = {
    width: golden.width,
    height: golden.height,
    totalPixels: golden.width * golden.height,
    mismatches,
    mismatchRatio: mismatches / (golden.width * golden.height),
    threshold: opts.threshold,
  };

  if (opts.out) {
    fs.mkdirSync(path.dirname(path.resolve(opts.out)), { recursive: true });
    fs.writeFileSync(opts.out, PNG.sync.write(diff));
    summary.diffPath = path.resolve(opts.out);
  }

  console.log(JSON.stringify(summary, null, 2));
  process.exit(mismatches > 0 ? 1 : 0);
}

main();
