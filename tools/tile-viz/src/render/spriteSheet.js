// PNG tileset decode and sprite cell slicing (PPU 16, 8-column ground matrix).



import fs from 'node:fs';

import path from 'node:path';

import { PNG } from 'pngjs';



/** @type {Map<string, PNG>} */

const cache = new Map();



/**

 * @param {string} filePath

 * @returns {PNG}

 */

export function loadPng(filePath) {

  const resolved = path.resolve(filePath);

  if (cache.has(resolved)) {

    return cache.get(resolved);

  }

  const buf = fs.readFileSync(resolved);

  const png = PNG.sync.read(buf);

  cache.set(resolved, png);

  return png;

}



export function clearPngCache() {

  cache.clear();

}



/**

 * @param {string|number} spriteIndex

 * @param {{ cellSize?: number, columns?: number }} layout

 * @param {PNG} png

 */

export function spriteRect(spriteIndex, layout, png) {

  const cellSize = layout.cellSize ?? 16;

  const columns = layout.columns ?? 8;

  const index = parseInt(String(spriteIndex), 10);

  if (Number.isNaN(index)) {

    return { x: 0, y: 0, w: cellSize, h: cellSize };

  }

  const col = index % columns;

  const row = Math.floor(index / columns);

  return {

    x: col * cellSize,

    y: row * cellSize,

    w: cellSize,

    h: cellSize,

  };

}



function clamp(value, min, max) {

  return Math.min(max, Math.max(min, value));

}



function samplePixel(src, x, y) {
  const sx = clamp(x, 0, src.width - 1);
  const sy = clamp(y, 0, src.height - 1);
  const si = (sy * src.width + sx) * 4;
  return [src.data[si], src.data[si + 1], src.data[si + 2], src.data[si + 3]];
}



/**

 * @param {PNG} src

 * @param {{ x: number, y: number, w: number, h: number }} rect

 * @param {Uint8Array} dest

 * @param {number} destW

 * @param {number} destX

 * @param {number} destY

 * @param {boolean} flipX

 * @param {[number,number,number]} tint

 * @param {boolean} extrude include 1px bleed from adjacent sheet cells

 * @param {number} [destSize] output cell size in pixels (defaults to the sprite's native size)

 */

export function blitSprite(src, rect, dest, destW, destX, destY, flipX, tint = [255, 255, 255], extrude = true, destSize = rect.w) {
  const pad = extrude ? 1 : 0;
  const spanW = rect.w + pad * 2;
  const spanH = rect.h + pad * 2;
  const xMin = rect.x;
  const xMax = rect.x + rect.w - 1;
  const yMin = rect.y;
  const yMax = rect.y + rect.h - 1;
  const outW = destSize;
  const outH = destSize;

  for (let py = 0; py < outH; py++) {
    for (let px = 0; px < outW; px++) {
      const fx = outW <= 1 ? 0.5 : px / (outW - 1);
      const fy = outH <= 1 ? 0.5 : py / (outH - 1);
      const sampleFx = flipX ? 1 - fx : fx;
      // Clamp extrude to this cell's bounds — do not bleed from adjacent sheet cells.
      let srcX = rect.x - pad + sampleFx * (spanW - 1);
      let srcY = rect.y - pad + fy * (spanH - 1);
      srcX = clamp(srcX, xMin, xMax);
      srcY = clamp(srcY, yMin, yMax);
      const [r, g, b, a] = samplePixel(src, Math.round(srcX), Math.round(srcY));

      const alpha = a / 255;

      if (alpha <= 0) {
        continue;
      }

      const destPx = destX + px;
      const destPy = destY + py;
      if (destPx < 0 || destPy < 0) {
        continue;
      }

      const di = (destPy * destW + destPx) * 4;
      dest[di] = Math.round(dest[di] * (1 - alpha) + r * (tint[0] / 255) * alpha);
      dest[di + 1] = Math.round(dest[di + 1] * (1 - alpha) + g * (tint[1] / 255) * alpha);
      dest[di + 2] = Math.round(dest[di + 2] * (1 - alpha) + b * (tint[2] / 255) * alpha);
      dest[di + 3] = Math.round(Math.min(255, dest[di + 3] + alpha * 255));
    }
  }
}



/**

 * @param {object} manifest

 * @param {string} assetsRoot

 */

export function loadTilesetsFromManifest(manifest, assetsRoot) {

  const map = new Map();

  for (const ts of manifest.tilesets) {

    const file = path.join(assetsRoot, ts.path);

    const png = loadPng(file);

    map.set(ts.name, {

      png,

      layout: {

        cellSize: ts.cellSize ?? 16,

        columns: ts.columns ?? 8,

        spriteCount: ts.spriteCount ?? 32,

      },

    });

  }

  return map;

}

