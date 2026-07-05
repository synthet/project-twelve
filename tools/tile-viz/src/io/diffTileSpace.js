// Compare two loaded tile-space documents over overlapping bounds.

import { getTile } from './tileSpace.js';
import { TileId } from '../../../world-viz/src/core/tiles.js';

/**
 * @param {object} spaceA
 * @param {object} spaceB
 * @param {object} [options]
 * @param {number} [options.maxExamples]
 * @returns {{ count: number, xMin: number|null, yMin: number|null, xMax: number|null, yMax: number|null, examples: object[] }}
 */
export function diffTileSpaces(spaceA, spaceB, options = {}) {
  const maxExamples = options.maxExamples ?? 50;
  const xMin = Math.max(spaceA.xMin, spaceB.xMin);
  const yMin = Math.max(spaceA.yMin, spaceB.yMin);
  const xMax = Math.min(spaceA.xMax, spaceB.xMax);
  const yMax = Math.min(spaceA.yMax, spaceB.yMax);

  if (xMax < xMin || yMax < yMin) {
    return { count: 0, xMin: null, yMin: null, xMax: null, yMax: null, examples: [] };
  }

  const examples = [];
  let count = 0;
  let diffXMin = Infinity;
  let diffYMin = Infinity;
  let diffXMax = -Infinity;
  let diffYMax = -Infinity;

  for (let y = yMin; y <= yMax; y++) {
    for (let x = xMin; x <= xMax; x++) {
      const idA = getTile(spaceA, x, y).id;
      const idB = getTile(spaceB, x, y).id;
      if (idA === idB) {
        continue;
      }

      count++;
      diffXMin = Math.min(diffXMin, x);
      diffYMin = Math.min(diffYMin, y);
      diffXMax = Math.max(diffXMax, x);
      diffYMax = Math.max(diffYMax, y);

      if (examples.length < maxExamples) {
        examples.push({
          x,
          y,
          a: idA,
          b: idB,
          aIsAir: idA === TileId.Air,
          bIsAir: idB === TileId.Air,
        });
      }
    }
  }

  return {
    count,
    xMin: count > 0 ? diffXMin : null,
    yMin: count > 0 ? diffYMin : null,
    xMax: count > 0 ? diffXMax : null,
    yMax: count > 0 ? diffYMax : null,
    overlap: { xMin, yMin, xMax, yMax },
    examples,
  };
}
