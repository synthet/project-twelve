// Tiny bitmap labels for autotile debug overlays (3×5 glyphs, 1px stroke).

const GLYPHS = {
  0: ['111', '101', '101', '101', '111'],
  1: ['010', '110', '010', '010', '111'],
  2: ['111', '001', '111', '100', '111'],
  3: ['111', '001', '111', '001', '111'],
  4: ['101', '101', '111', '001', '001'],
  5: ['111', '100', '111', '001', '111'],
  6: ['111', '100', '111', '101', '111'],
  7: ['111', '001', '001', '001', '001'],
  8: ['111', '101', '111', '101', '111'],
  9: ['111', '101', '111', '001', '111'],
  f: ['000', '101', '110', '101', '000'],
};

/**
 * @param {Uint8Array} rgba
 * @param {number} destW
 * @param {number} destX
 * @param {number} destY
 * @param {string} text
 * @param {[number, number, number]} color
 * @param {number} [pixelScale]
 */
export function drawCellLabel(rgba, destW, destX, destY, text, color, pixelScale = 2) {
  const chars = String(text).toLowerCase();
  let cursorX = destX;
  for (let ci = 0; ci < chars.length; ci++) {
    const glyph = GLYPHS[chars[ci]];
    if (!glyph) {
      cursorX += 4 * pixelScale;
      continue;
    }
    for (let gy = 0; gy < glyph.length; gy++) {
      for (let gx = 0; gx < glyph[gy].length; gx++) {
        if (glyph[gy][gx] !== '1') {
          continue;
        }
        for (let py = 0; py < pixelScale; py++) {
          for (let px = 0; px < pixelScale; px++) {
            const x = cursorX + gx * pixelScale + px;
            const y = destY + gy * pixelScale + py;
            if (x < 0 || y < 0) {
              continue;
            }
            const i = (y * destW + x) * 4;
            rgba[i] = color[0];
            rgba[i + 1] = color[1];
            rgba[i + 2] = color[2];
            rgba[i + 3] = 255;
          }
        }
      }
    }
    cursorX += (glyph[0].length + 1) * pixelScale;
  }
}

/**
 * @param {string|number|null|undefined} spriteId
 * @param {boolean} flipX
 * @returns {string}
 */
export function formatSpriteLabel(spriteId, flipX) {
  if (spriteId == null) {
    return '';
  }
  const base = String(spriteId);
  return flipX ? `${base}f` : base;
}
