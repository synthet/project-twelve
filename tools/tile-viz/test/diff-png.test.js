import { test } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { PNG } from 'pngjs';

import { diffPngImages } from '../scripts/diff-png.mjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const goldenPath = path.join(__dirname, 'fixtures', 'render', 'grass-cover-middle.png');

function makePng(width, height, fillFn) {
  const png = new PNG({ width, height });
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      const idx = (width * y + x) << 2;
      const [r, g, b, a] = fillFn(x, y);
      png.data[idx] = r;
      png.data[idx + 1] = g;
      png.data[idx + 2] = b;
      png.data[idx + 3] = a;
    }
  }
  return png;
}

test('diffPngImages reports zero mismatches for identical buffers', () => {
  const a = makePng(2, 2, () => [10, 20, 30, 255]);
  const b = makePng(2, 2, () => [10, 20, 30, 255]);
  const result = diffPngImages(a, b);
  assert.equal(result.mismatches, 0);
  assert.equal(result.totalPixels, 4);
});

test('diffPngImages reports mismatches for single-pixel delta', () => {
  const a = makePng(2, 2, () => [10, 20, 30, 255]);
  const b = makePng(2, 2, (x, y) => (x === 1 && y === 0 ? [11, 20, 30, 255] : [10, 20, 30, 255]));
  const result = diffPngImages(a, b);
  assert.equal(result.mismatches, 1);
  assert.ok(result.mismatchRatio > 0);
});

test('diffPngImages golden PNG matches itself', {
  skip: fs.existsSync(goldenPath) ? false : 'golden missing',
}, () => {
  const golden = PNG.sync.read(fs.readFileSync(goldenPath));
  const result = diffPngImages(golden, golden);
  assert.equal(result.mismatches, 0);
});
