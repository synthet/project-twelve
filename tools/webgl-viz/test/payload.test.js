import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import test from 'node:test';
import { buildViewerPayload } from '../src/spacePayload.js';
import { buildViewer } from '../src/build.js';

test('buildViewerPayload normalizes a tile-space fixture for browser rendering', () => {
  const payload = buildViewerPayload('../tile-viz/test/fixtures/snippets/grass-cover-middle.json');
  assert.equal(payload.format, 'project-twelve/webgl-viz/v1');
  assert.equal(payload.width, 5);
  assert.equal(payload.height, 3);
  const grass = payload.tiles.find((tile) => tile.x === 1 && tile.y === 2);
  assert.equal(grass.name, 'Grass');
  assert.deepEqual(grass.color, [83, 160, 60]);
  assert.equal(grass.chunkX, 0);
  assert.equal(grass.localX, 1);
});

test('build command emits a self-contained WebGL HTML viewer and payload', () => {
  const outDir = fs.mkdtempSync(path.join(os.tmpdir(), 'webgl-viz-'));
  buildViewer({ outDir });
  const html = fs.readFileSync(path.join(outDir, 'index.html'), 'utf8');
  const payload = JSON.parse(fs.readFileSync(path.join(outDir, 'payload.json'), 'utf8'));
  assert.match(html, /getContext\('webgl'/);
  assert.match(html, /ProjectTwelve WebGL Viz/);
  assert.equal(payload.name, 'grass-cover-middle');
});
