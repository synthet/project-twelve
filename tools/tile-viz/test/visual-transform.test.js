import { test } from 'node:test';
import assert from 'node:assert/strict';
import { PNG } from 'pngjs';

import { blitSprite, normalizeRotationDegrees, transformSpriteUv } from '../src/render/spriteSheet.js';

function uvKey({ u, v }) {
  return `${u},${v}`;
}

function makeAsymmetricPng() {
  const png = new PNG({ width: 2, height: 2 });
  const colors = [
    [255, 0, 0, 255],    // top-left
    [0, 255, 0, 255],    // top-right
    [0, 0, 255, 255],    // bottom-left
    [255, 255, 0, 255],  // bottom-right
  ];
  for (let i = 0; i < colors.length; i++) {
    png.data.set(colors[i], i * 4);
  }
  return png;
}

function blitColorGrid(transform) {
  const src = makeAsymmetricPng();
  const dest = new Uint8Array(2 * 2 * 4);
  blitSprite(src, { x: 0, y: 0, w: 2, h: 2 }, dest, 2, 0, 0, transform.flipX ?? false, [255, 255, 255], false, 2, transform);
  const cells = [];
  for (let i = 0; i < 4; i++) {
    cells.push(Array.from(dest.slice(i * 4, i * 4 + 4)).join(','));
  }
  return cells.join('|');
}

test('visual transform normalizes negative quarter turns and rejects non-quarter turns', () => {
  assert.equal(normalizeRotationDegrees(-90), 270);
  assert.equal(normalizeRotationDegrees(-180), 180);
  assert.equal(normalizeRotationDegrees(450), 90);
  assert.throws(() => normalizeRotationDegrees(45), /0, 90, 180, or 270/);
});

test('visual transform applies flipX, then flipY, then clockwise rotation', () => {
  const transformed = transformSpriteUv(0, 0, { flipX: true, flipY: false, rotationDegrees: 90 });
  assert.equal(uvKey(transformed), '0,0');

  const rotateFirstThenFlipX = { u: 1, v: 1 };
  assert.notDeepEqual(transformed, rotateFirstThenFlipX);
});

test('blitSprite transform combinations stay distinct with asymmetric fixture', () => {
  const outputs = new Map();
  for (const flipX of [false, true]) {
    for (const flipY of [false, true]) {
      for (const rotationDegrees of [0, 90, 180, 270]) {
        const key = JSON.stringify({ flipX, flipY, rotationDegrees });
        outputs.set(key, blitColorGrid({ flipX, flipY, rotationDegrees }));
      }
    }
  }

  assert.equal(outputs.size, 16);
  assert.equal(new Set(outputs.values()).size, 8, 'D4 transforms of a quad should produce all eight distinct corner mappings');
  assert.notEqual(
    outputs.get(JSON.stringify({ flipX: true, flipY: false, rotationDegrees: 90 })),
    outputs.get(JSON.stringify({ flipX: true, flipY: false, rotationDegrees: 0 })),
  );
});
