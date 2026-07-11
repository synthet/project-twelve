#!/usr/bin/env node
/**
 * Sweep material-boundary fixtures: baseline vs selection-enabled render, gap metric, honest pairs.
 *
 * Usage: node scripts/sweep-material-boundaries.mjs [--out out/material-boundary-exp/sweep]
 */

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { PNG } from 'pngjs';

import { loadTileSpaceFromFile } from '../src/io/tileSpace.js';
import { renderAutotilePng } from '../src/render/autotilePng.js';
import { seamBandGapPixels } from '../src/visual/seamGapMetric.js';
import { DEFAULT_CATALOG } from '../src/visual/catalog.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(__dirname, '..');
const assetsRoot = path.resolve(root, '..', '..', 'Assets', '_Licensed', 'PixelFantasy', 'PixelTileEngine', 'Tiles');
const snippetsDir = path.join(root, 'test', 'fixtures', 'snippets', 'material-exp');
const auditPath = path.join(root, 'data', 'material-boundary-audit.json');

function parseArgs(argv) {
  const opts = { outDir: path.join(root, 'out', 'material-boundary-exp', 'sweep'), scale: 48 };
  for (let i = 0; i < argv.length; i++) {
    if (argv[i] === '--out' && argv[i + 1]) {
      opts.outDir = path.resolve(argv[++i]);
    } else if (argv[i] === '--scale' && argv[i + 1]) {
      opts.scale = Number(argv[++i]);
    }
  }
  return opts;
}

function listFixtures() {
  if (!fs.existsSync(snippetsDir)) {
    return [];
  }
  return fs.readdirSync(snippetsDir)
    .filter((f) => f.endsWith('.json') && !f.endsWith('.meta.json') && f !== 'manifest.json')
    .map((f) => path.join(snippetsDir, f));
}

function renderSpace(space, opts, materialBoundarySelection = true) {
  return renderAutotilePng(space, {
    assetsRoot,
    scale: opts.scale,
    flatLight: true,
    materialBoundarySelection,
  });
}

function stitchBeforeAfter(beforeBuf, afterBuf) {
  const left = PNG.sync.read(beforeBuf);
  const right = PNG.sync.read(afterBuf);
  const gap = 16;
  const sheet = new PNG({ width: left.width + gap + right.width, height: Math.max(left.height, right.height) });
  sheet.data.fill(0x28);
  const blit = (img, ox) => {
    for (let y = 0; y < img.height; y++) {
      for (let x = 0; x < img.width; x++) {
        const si = (y * img.width + x) * 4;
        const di = (y * sheet.width + (ox + x)) * 4;
        sheet.data[di] = img.data[si];
        sheet.data[di + 1] = img.data[si + 1];
        sheet.data[di + 2] = img.data[si + 2];
        sheet.data[di + 3] = img.data[si + 3];
      }
    }
  };
  blit(left, 0);
  blit(right, left.width + gap);
  return PNG.sync.write(sheet);
}

function runFixture(fixturePath, opts) {
  const space = loadTileSpaceFromFile(fixturePath);
  const name = space.name;
  const fixtureDir = path.join(opts.outDir, name);
  fs.mkdirSync(fixtureDir, { recursive: true });

  const baselinePng = renderSpace(space, opts, false);
  const selectedPng = renderSpace(space, opts, true);
  const baselineGap = seamBandGapPixels(baselinePng, space, DEFAULT_CATALOG, opts.scale);
  const selectedGap = seamBandGapPixels(selectedPng, space, DEFAULT_CATALOG, opts.scale);
  const improved = selectedGap.gapPixels < baselineGap.gapPixels;

  fs.writeFileSync(path.join(fixtureDir, 'baseline.png'), baselinePng);
  fs.writeFileSync(path.join(fixtureDir, 'selected.png'), selectedPng);

  if (improved) {
    fs.writeFileSync(
      path.join(fixtureDir, 'best-before-after.png'),
      stitchBeforeAfter(baselinePng, selectedPng),
    );
  }

  const result = {
    fixture: name,
    baselineGap,
    selectedGap,
    improved,
    delta: baselineGap.gapPixels - selectedGap.gapPixels,
    selection: 'flat-top-ground + boundary-cover',
  };
  fs.writeFileSync(path.join(fixtureDir, 'ranked.json'), `${JSON.stringify(result, null, 2)}\n`);
  console.log(
    `${name}: baseline=${baselineGap.gapPixels} selected=${selectedGap.gapPixels} ` +
    `${improved ? 'IMPROVED' : 'no improvement'} (Δ${result.delta})`,
  );
  return { name, result, baselinePng, selectedPng, improved };
}

function buildSummaryGrid(results, opts) {
  const pairs = results.filter((r) => r.improved);
  if (!pairs.length) {
    return null;
  }
  const thumbs = pairs.map((r) => PNG.sync.read(fs.readFileSync(path.join(opts.outDir, r.name, 'best-before-after.png'))));
  const cellW = Math.max(...thumbs.map((t) => t.width));
  const cellH = Math.max(...thumbs.map((t) => t.height));
  const cols = Math.min(3, thumbs.length);
  const rows = Math.ceil(thumbs.length / cols);
  const grid = new PNG({ width: cols * cellW, height: rows * cellH });
  grid.data.fill(0x20);
  for (let i = 0; i < thumbs.length; i++) {
    const img = thumbs[i];
    const col = i % cols;
    const row = Math.floor(i / cols);
    const ox = col * cellW + Math.floor((cellW - img.width) / 2);
    const oy = row * cellH + Math.floor((cellH - img.height) / 2);
    for (let y = 0; y < img.height; y++) {
      for (let x = 0; x < img.width; x++) {
        const si = (y * img.width + x) * 4;
        const di = ((oy + y) * grid.width + (ox + x)) * 4;
        grid.data[di] = img.data[si];
        grid.data[di + 1] = img.data[si + 1];
        grid.data[di + 2] = img.data[si + 2];
        grid.data[di + 3] = img.data[si + 3];
      }
    }
  }
  return PNG.sync.write(grid);
}

function writeSweetSpots(results, opts) {
  const lines = [
    '# Material boundary sweet spots',
    '',
    `Generated: ${new Date().toISOString()}`,
    '',
    'Art path: overlay-first (Moss/SandA boundary cover) + flat-top ground (sprites 1/2) over foreign solid.',
    '',
    'Audit: `data/material-boundary-audit.json`',
    '',
  ];
  for (const r of results) {
    lines.push(`## ${r.name}`);
    lines.push(`- Baseline gap pixels: ${r.result.baselineGap.gapPixels}`);
    lines.push(`- Selected gap pixels: ${r.result.selectedGap.gapPixels}`);
    lines.push(`- Improved: ${r.improved ? 'yes' : 'no'}`);
    lines.push('');
  }
  const improved = results.filter((r) => r.improved).length;
  lines.push(`## Summary`);
  lines.push(`${improved}/${results.length} fixtures improved with selection pass.`);
  if (improved > 0) {
    lines.push('Authored ground-transition sheets **not required** for these geometries.');
  } else {
    lines.push('Overlay + flat-top insufficient — proceed to authored transition art in submodule.');
  }
  fs.writeFileSync(path.join(opts.outDir, 'sweet-spots.md'), `${lines.join('\n')}\n`);
}

function main() {
  const opts = parseArgs(process.argv.slice(2));
  fs.mkdirSync(opts.outDir, { recursive: true });

  const fixtures = listFixtures();
  if (!fixtures.length) {
    console.error('No fixtures — run: node scripts/generate-material-fixtures.mjs');
    process.exit(1);
  }

  fs.copyFileSync(auditPath, path.join(opts.outDir, 'material-boundary-audit.json'));

  const results = fixtures.map((f) => runFixture(f, opts));
  const grid = buildSummaryGrid(results, opts);
  if (grid) {
    fs.writeFileSync(path.join(opts.outDir, 'summary-grid.png'), grid);
  }
  writeSweetSpots(results, opts);
  fs.writeFileSync(path.join(opts.outDir, 'sweep-summary.json'), `${JSON.stringify(results.map((r) => r.result), null, 2)}\n`);
}

main();
