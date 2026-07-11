// Objective seam-gap pixel count for material-boundary renders.

import { PNG } from 'pngjs';
import { tileColor, TileId } from '../../../world-viz/src/core/tiles.js';
import { getTile } from '../io/tileSpace.js';
import { sharesGroundAutotileGroup } from './catalog.js';
import { spaceToSampledGrid } from '../io/tileSpace.js';

const SKY = tileColor(TileId.Air, 15);
const BAND_PX = 2;

function colorDist(a, b) {
  return Math.abs(a[0] - b[0]) + Math.abs(a[1] - b[1]) + Math.abs(a[2] - b[2]);
}

function isSkyColor(r, g, b, alpha, tolerance = 18) {
  if (alpha < 128) {
    return true;
  }
  return colorDist([r, g, b], SKY) <= tolerance;
}

function isSolid(space, x, y) {
  return getTile(space, x, y).id !== 0;
}

function isForeignHorizon(space, x, y, yBelow, catalog) {
  const upper = getTile(space, x, y);
  const lower = getTile(space, x, yBelow);
  if (upper.id === 0 || lower.id === 0) {
    return false;
  }
  return !sharesGroundAutotileGroup(upper.id, lower.id, catalog);
}

function isForeignVertical(space, x, xRight, y, catalog) {
  const left = getTile(space, x, y);
  const right = getTile(space, xRight, y);
  if (left.id === 0 || right.id === 0) {
    return false;
  }
  return !sharesGroundAutotileGroup(left.id, right.id, catalog);
}

/**
 * Count sky-colored pixels in bands centered on material horizons.
 * @param {Buffer} pngBuffer
 * @param {object} space
 * @param {object} catalog
 * @param {number} scale
 * @returns {{ gapPixels: number, horizons: number, bandPx: number }}
 */
export function seamBandGapPixels(pngBuffer, space, catalog, scale = 16) {
  const img = PNG.sync.read(pngBuffer);
  const grid = spaceToSampledGrid(space);
  let gapPixels = 0;
  let horizons = 0;

  for (let row = 0; row < grid.height - 1; row++) {
    const worldY = grid.maxY - row;
    const worldYBelow = worldY - 1;
    for (let col = 0; col < grid.width; col++) {
      const worldX = grid.minX + col;
      if (!isForeignHorizon(space, worldX, worldY, worldYBelow, catalog)) {
        continue;
      }
      horizons++;
      const seamScreenY = (row + 1) * scale;
      for (let dy = -BAND_PX; dy <= BAND_PX; dy++) {
        const py = seamScreenY + dy;
        if (py < 0 || py >= img.height) {
          continue;
        }
        for (let px = col * scale; px < (col + 1) * scale; px++) {
          const i = (py * img.width + px) * 4;
          if (isSkyColor(img.data[i], img.data[i + 1], img.data[i + 2], img.data[i + 3])) {
            gapPixels++;
          }
        }
      }
    }
  }

  for (let row = 0; row < grid.height; row++) {
    const worldY = grid.maxY - row;
    for (let col = 0; col < grid.width - 1; col++) {
      const worldX = grid.minX + col;
      const worldXRight = worldX + 1;
      if (!isForeignVertical(space, worldX, worldXRight, worldY, catalog)) {
        continue;
      }
      horizons++;
      const seamScreenX = (col + 1) * scale;
      for (let dx = -BAND_PX; dx <= BAND_PX; dx++) {
        const px = seamScreenX + dx;
        if (px < 0 || px >= img.width) {
          continue;
        }
        for (let py = row * scale; py < (row + 1) * scale; py++) {
          const i = (py * img.width + px) * 4;
          if (isSkyColor(img.data[i], img.data[i + 1], img.data[i + 2], img.data[i + 3])) {
            gapPixels++;
          }
        }
      }
    }
  }

  return { gapPixels, horizons, bandPx: BAND_PX };
}
