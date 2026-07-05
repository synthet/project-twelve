import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { buildAutotileReport } from '../src/report/autotileJson.js';
import { loadTileSpace } from '../src/io/tileSpace.js';
import { renderAutotilePng } from '../src/render/autotilePng.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(__dirname, '..');
const assetsRoot = path.resolve(root, '..', '..', 'Assets', '_Licensed', 'PixelFantasy', 'PixelTileEngine', 'Tiles');
const outDir = path.join(root, 'out-slope');

function buildDescendingScenario() {
  const tiles = [];
  const width = 14;
  const height = 10;
  const surfaceY = (x) => 8 - Math.floor(x / 2);

  for (let x = 0; x < width; x++) {
    const top = surfaceY(x);
    tiles.push({ x, y: top, id: 2, light: 15 });
    for (let y = 0; y < top; y++) {
      tiles.push({ x, y, id: 1, light: 4 });
    }
  }

  return {
    format: 'project-twelve/tile-space/v1',
    kind: 'snippet',
    name: 'scratch-descending-slope',
    origin: { x: 0, y: 0 },
    width,
    height,
    tiles,
    expect: [],
  };
}

function buildAscendingScenario() {
  const tiles = [];
  const width = 14;
  const height = 10;
  const surfaceY = (x) => 2 + Math.floor(x / 2);

  for (let x = 0; x < width; x++) {
    const top = surfaceY(x);
    tiles.push({ x, y: top, id: 2, light: 15 });
    for (let y = 0; y < top; y++) {
      tiles.push({ x, y, id: 1, light: 4 });
    }
  }

  return {
    format: 'project-twelve/tile-space/v1',
    kind: 'snippet',
    name: 'scratch-ascending-slope',
    origin: { x: 0, y: 0 },
    width,
    height,
    tiles,
    expect: [],
  };
}

function printKeyRows(space) {
  const report = buildAutotileReport(space, { includeAir: true });
  console.log(`\n${space.name}`);
  console.log(report.ascii.join('\n'));

  for (const tile of report.tiles) {
    if (tile.tileId === 0) {
      continue;
    }

    const above = report.tiles.find((candidate) => candidate.x === tile.x && candidate.y === tile.y + 1);
    const exposed = !above || above.tileId === 0;
    const nearSurface = exposed || report.tiles.some((candidate) => (
      candidate.x === tile.x && candidate.y > tile.y && candidate.y <= tile.y + 2 && candidate.tileId === 0
    ));
    if (!nearSurface) {
      continue;
    }

    const ground = tile.autotile.ground;
    const cover = tile.autotile.cover;
    const coverText = cover?.rendered ? `${cover.spriteId}${cover.flipX ? 'f' : ''}` : '-';
    console.log(
      `${String(tile.x).padStart(2)},${String(tile.y).padStart(2)} ` +
      `${tile.tileName.padEnd(5)} g=${ground.spriteId}${ground.flipX ? 'f' : ' '} ` +
      `c=${coverText.padEnd(3)} mask=${ground.mask.map((col) => col.join('')).join('/')}`,
    );
  }
}

function render(space) {
  fs.mkdirSync(outDir, { recursive: true });
  const png = renderAutotilePng(space, { assetsRoot, scale: 32, flatLight: true });
  fs.writeFileSync(path.join(outDir, `${space.name}.png`), png);
}

const scenarios = [buildDescendingScenario(), buildAscendingScenario()];
for (const scenario of scenarios) {
  const space = loadTileSpace(scenario);
  printKeyRows(space);
  render(space);
}
