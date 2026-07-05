#!/usr/bin/env node
// Extract regression snippet fixtures from a tile-space capture with tile-viz autotile expects.
//
// Usage:
//   node scripts/extract-regression-fixtures.mjs

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { loadTileSpaceFromFile, loadTileSpace, getTile } from '../src/io/tileSpace.js';
import { buildAutotileReport } from '../src/report/autotileJson.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(__dirname, '..');
const capturePath = path.join(root, 'test/fixtures/captures/sandbox-world-live.json');
const snippetsDir = path.join(root, 'test/fixtures/snippets');

const regions = [
  {
    name: 'roof-slope-left-vs-right',
    xmin: -105,
    xmax: -86,
    ymin: 27,
    ymax: 31,
    halo: 2,
    expectCoords: [
      [-105, 31], [-102, 30], [-104, 30], [-101, 29],
      [-91, 27], [-89, 28], [-87, 29], [-86, 29],
    ],
  },
  {
    name: 'one-sided-house-lip',
    xmin: -99,
    xmax: -95,
    ymin: 24,
    ymax: 28,
    halo: 1,
    expectCoords: [[-97, 27], [-97, 25]],
  },
  {
    name: 'dirt-window-inner-edges',
    xmin: -118,
    xmax: -106,
    ymin: 26,
    ymax: 29,
    halo: 1,
    expectCoords: [
      [-114, 29], [-113, 29], [-114, 28], [-111, 28],
      [-113, 27], [-111, 27], [-114, 26],
    ],
    targetExpect: [
      { x: -114, y: 29, ground: { spriteId: '17' } },
      { x: -113, y: 29, ground: { spriteId: '17' } },
      { x: -114, y: 28, ground: { spriteId: '8', flipX: true } },
      { x: -111, y: 28, ground: { spriteId: '8' } },
      { x: -113, y: 27, ground: { spriteId: '2' } },
      { x: -111, y: 27, ground: { spriteId: '11' } },
    ],
  },
  {
    name: 'dirt-gap-left-vertical-wall',
    xmin: -108,
    xmax: -102,
    ymin: 16,
    ymax: 20,
    halo: 1,
    expectCoords: [[-105, 19], [-105, 18], [-105, 17], [-104, 18]],
  },
];

function buildSnippet(space, region) {
  const halo = region.halo ?? 1;
  const haloXMin = region.xmin - halo;
  const haloXMax = region.xmax + halo;
  const haloYMin = region.ymin - halo;
  const haloYMax = region.ymax + halo;

  const tileKeys = new Set();
  const tiles = [];

  for (let y = haloYMin; y <= haloYMax; y++) {
    for (let x = haloXMin; x <= haloXMax; x++) {
      const tile = getTile(space, x, y);
      if (tile.id === 0) continue;
      const key = `${x},${y}`;
      if (tileKeys.has(key)) continue;
      tileKeys.add(key);
      tiles.push({ x, y, id: tile.id, light: tile.light ?? 4 });
    }
  }

  tiles.sort((a, b) => b.y - a.y || a.x - b.x);

  const snippet = {
    format: 'project-twelve/tile-space/v1',
    kind: 'snippet',
    name: region.name,
    origin: { x: region.xmin, y: region.ymin },
    width: region.xmax - region.xmin + 1,
    height: region.ymax - region.ymin + 1,
    tiles,
    baselineExpect: [],
  };

  if (region.targetExpect) {
    snippet.targetExpect = region.targetExpect;
  }

  return snippet;
}

function groundExpect(reportTile) {
  const g = reportTile.autotile.ground;
  if (!g?.spriteId) return null;
  const entry = { x: reportTile.x, y: reportTile.y, ground: { spriteId: g.spriteId } };
  if (g.flipX) entry.ground.flipX = true;
  return entry;
}

function fillBaselineExpectsFromSnippetReport(snippet, expectCoords) {
  const snippetSpace = loadTileSpace(snippet, snippetsDir);
  const snippetReport = buildAutotileReport(snippetSpace);
  const byKey = new Map(snippetReport.tiles.map((t) => [`${t.x},${t.y}`, t]));
  snippet.baselineExpect = [];
  for (const [x, y] of expectCoords) {
    const tile = byKey.get(`${x},${y}`);
    if (!tile) {
      throw new Error(`${snippet.name}: missing report tile (${x},${y}) in snippet-local context`);
    }
    const exp = groundExpect(tile);
    if (exp) snippet.baselineExpect.push(exp);
  }
}

function main() {
  const space = loadTileSpaceFromFile(capturePath);

  fs.mkdirSync(snippetsDir, { recursive: true });

  for (const region of regions) {
    const snippet = buildSnippet(space, region);
    fillBaselineExpectsFromSnippetReport(snippet, region.expectCoords);

    const outPath = path.join(snippetsDir, `${region.name}.json`);
    fs.writeFileSync(outPath, `${JSON.stringify(snippet, null, 2)}\n`);
    process.stderr.write(
      `wrote ${outPath} (${snippet.tiles.length} tiles, ${snippet.baselineExpect.length} baseline expects`
        + `${snippet.targetExpect ? `, ${snippet.targetExpect.length} target expects` : ''})\n`,
    );
  }

  // Alias window fixtures to existing hole snippets (same topology, clearer names for cavity work).
  const aliases = [
    ['dirt-hole-1x1.json', 'dirt-window-1x1.json'],
    ['dirt-hole-2x1.json', 'dirt-window-2x1.json'],
    ['dirt-hole-1x2.json', 'dirt-window-1x2.json'],
    ['dirt-hole-door.json', 'dirt-door-opening.json'],
  ];
  for (const [src, dest] of aliases) {
    const srcPath = path.join(snippetsDir, src);
    const destPath = path.join(snippetsDir, dest);
    const doc = JSON.parse(fs.readFileSync(srcPath, 'utf8'));
    doc.name = dest.replace('.json', '');
    fs.writeFileSync(destPath, `${JSON.stringify(doc, null, 2)}\n`);
    process.stderr.write(`wrote alias ${destPath}\n`);
  }
}

main();
