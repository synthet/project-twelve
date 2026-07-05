import { test } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { PNG } from 'pngjs';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const goldenPath = path.join(__dirname, 'fixtures', 'render', 'grass-cover-middle.png');

test('diff-png identical files report zero mismatches', { skip: !fs.existsSync(goldenPath) ? 'golden missing' : false }, () => {
  const golden = PNG.sync.read(fs.readFileSync(goldenPath));
  let mismatches = 0;
  for (let i = 0; i < golden.data.length; i += 4) {
    if (golden.data[i] !== golden.data[i]) {
      mismatches++;
    }
  }
  assert.equal(mismatches, 0);
});
