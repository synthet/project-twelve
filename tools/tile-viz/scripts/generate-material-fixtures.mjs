#!/usr/bin/env node
/**
 * Generate material-exp snippet fixtures from row patterns.
 * 1 = Dirt (Humus), 2 = Stone (Rocks), . = Air
 *
 * Usage: node scripts/generate-material-fixtures.mjs
 */

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { DEFAULT_CATALOG } from '../src/visual/catalog.js';
import { listBoundaryCells } from '../src/visual/materialBoundarySelection.js';
import { loadTileSpace } from '../src/io/tileSpace.js';
import { TileId } from '../../world-viz/src/core/tiles.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(__dirname, '..');
const outDir = path.join(root, 'test', 'fixtures', 'snippets', 'material-exp');

const CHAR_TO_TILE = {
  '1': TileId.Dirt,
  '2': TileId.Stone,
  '.': TileId.Air,
};

const PATTERNS = [
  { name: 'horizontal-dirt-on-stone', pattern: ['111/222'] },
  { name: 'dirt-stone-dirt-row', pattern: ['111/121'] },
  { name: 'framed-dirt-stone-core', pattern: ['111/121/121/111'] },
  { name: 'stone-column-through-dirt', pattern: ['1111/1221/1221/1221'] },
  { name: 'stone-band-through-dirt', pattern: ['1111/1221/1111'] },
  { name: 'vertical-single-column', pattern: ['1/2'] },
  { name: 'vertical-two-wide', pattern: ['11/22'] },
  { name: 'alternating-columns', pattern: ['12/12'] },
  { name: 'partial-stone-intrusion', pattern: ['112/112'] },
  { name: 'stone-heavy-top', pattern: ['211/211'] },
];

function parsePattern(patternRows) {
  const rows = Array.isArray(patternRows) ? patternRows.join('/') : String(patternRows);
  return rows.split('/').map((row) => row.split('').map((ch) => {
    if (!(ch in CHAR_TO_TILE)) {
      throw new Error(`Unknown char '${ch}' in pattern row ${row}`);
    }
    return CHAR_TO_TILE[ch];
  }));
}

function buildSnippet(name, grid) {
  const height = grid.length;
  const width = Math.max(...grid.map((r) => r.length));
  const tiles = [];
  for (let row = 0; row < height; row++) {
    const worldY = height - 1 - row;
    for (let col = 0; col < grid[row].length; col++) {
      const id = grid[row][col];
      if (id === TileId.Air) {
        continue;
      }
      tiles.push({ x: col, y: worldY, id, light: id === TileId.Grass ? 15 : 4 });
    }
  }

  const space = {
    format: 'project-twelve/tile-space/v1',
    kind: 'snippet',
    name,
    origin: { x: 0, y: 0 },
    width,
    height,
    tiles,
  };

  const boundaryCells = listBoundaryCells(loadTileSpace(space), DEFAULT_CATALOG);
  return { space, boundaryCells };
}

function main() {
  fs.mkdirSync(outDir, { recursive: true });
  const manifest = [];

  for (const { name, pattern } of PATTERNS) {
    const grid = parsePattern(pattern);
    const { space, boundaryCells } = buildSnippet(name, grid);
    const jsonPath = path.join(outDir, `${name}.json`);
    fs.writeFileSync(jsonPath, `${JSON.stringify(space, null, 2)}\n`);

    const metaPath = path.join(outDir, `${name}.meta.json`);
    fs.writeFileSync(metaPath, `${JSON.stringify({
      name,
      pattern: pattern[0],
      boundaryCellCount: boundaryCells.length,
      boundaryCells,
    }, null, 2)}\n`);

    manifest.push({ name, pattern: pattern[0], file: jsonPath, boundaryCells: boundaryCells.length });
    console.log(`${name}: ${boundaryCells.length} boundary cells`);
  }

  fs.writeFileSync(path.join(outDir, 'manifest.json'), `${JSON.stringify(manifest, null, 2)}\n`);
}

main();
