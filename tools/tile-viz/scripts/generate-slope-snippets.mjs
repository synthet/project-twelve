#!/usr/bin/env node
// Generate compact long slope snippets for autotile regression tests.

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const SNIPPETS_DIR = path.join(__dirname, '..', 'test', 'fixtures', 'snippets');

const TileId = {
  Dirt: 1,
  Grass: 2,
};

function buildLongSlope(name, direction) {
  const width = 12;
  const height = 14;
  const minTop = 2;
  const maxTop = minTop + width - 1;
  const tiles = [];

  for (let x = 0; x < width; x++) {
    const topY = direction === 'ascending' ? minTop + x : maxTop - x;
    for (let y = 0; y <= topY; y++) {
      tiles.push({
        x,
        y,
        id: y === topY ? TileId.Grass : TileId.Dirt,
        light: y === topY ? 15 : 4,
      });
    }
  }

  return {
    format: 'project-twelve/tile-space/v1',
    kind: 'snippet',
    name,
    origin: { x: 0, y: 0 },
    width,
    height,
    tiles,
    expect: buildLongSlopeExpectations(direction, width, minTop, maxTop),
  };
}

function buildLongSlopeExpectations(direction, width, minTop, maxTop) {
  const expect = [];
  const topYAt = (x) => (direction === 'ascending' ? minTop + x : maxTop - x);

  for (let x = 1; x < width - 1; x++) {
    expect.push({
      x,
      y: topYAt(x) - 1,
      ground: {
        spriteId: '9',
        flipX: false,
      },
    });
  }

  const lowX = direction === 'ascending' ? 0 : width - 1;
  const highX = direction === 'ascending' ? width - 1 : 0;
  expect.push({
    x: lowX,
    y: topYAt(lowX),
    ground: {
      spriteId: '0',
      flipX: direction !== 'ascending',
    },
    cover: {
      rendered: true,
      spriteId: '1',
      flipX: direction !== 'ascending',
    },
  });
  expect.push({
    x: highX,
    y: topYAt(highX),
    ground: {
      spriteId: '28',
      flipX: false,
    },
    cover: {
      rendered: true,
      spriteId: '0',
      flipX: false,
    },
  });

  return expect;
}

fs.mkdirSync(SNIPPETS_DIR, { recursive: true });
for (const fixture of [
  buildLongSlope('slope-ascending-long', 'ascending'),
  buildLongSlope('slope-descending-long', 'descending'),
]) {
  const out = path.join(SNIPPETS_DIR, `${fixture.name}.json`);
  fs.writeFileSync(out, `${JSON.stringify(fixture, null, 2)}\n`);
  process.stderr.write(`wrote ${out}\n`);
}
