// Port of AutotileMaskBuilder.cs

/**
 * @param {(x: number, y: number) => boolean} sharesGroundGroup
 * @param {number} worldX
 * @param {number} worldY
 * @returns {number[][]}
 */
export function buildGroundMask(sharesGroundGroup, worldX, worldY) {
  const has = (dx, dy) => sharesGroundGroup(worldX + dx, worldY + dy);
  return [
    [
      has(-1, 1) && has(-1, 0) && has(0, 1) ? 1 : 0,
      has(-1, 0) ? 1 : 0,
      has(-1, -1) && has(-1, 0) && has(0, -1) ? 1 : 0,
    ],
    [has(0, 1) ? 1 : 0, 1, has(0, -1) ? 1 : 0],
    [
      has(1, 1) && has(1, 0) && has(0, 1) ? 1 : 0,
      has(1, 0) ? 1 : 0,
      has(1, -1) && has(1, 0) && has(0, -1) ? 1 : 0,
    ],
  ];
}

/**
 * @param {(x: number, y: number) => boolean} sharesCoverGroup
 * @param {(x: number, y: number) => boolean} hasGroundBody
 * @param {number} worldX
 * @param {number} worldY
 * @returns {number[][]}
 */
export function buildCoverMask(sharesCoverGroup, hasGroundBody, worldX, worldY) {
  const mask = [
    [0, 0, 0],
    [0, 1, 0],
    [0, 0, 0],
  ];
  mask[0][1] = resolveCoverNeighbor(sharesCoverGroup, hasGroundBody, worldX - 1, worldY);
  mask[2][1] = resolveCoverNeighbor(sharesCoverGroup, hasGroundBody, worldX + 1, worldY);
  return mask;
}

function resolveCoverNeighbor(sharesCoverGroup, hasGroundBody, neighborX, neighborY) {
  if (sharesCoverGroup(neighborX, neighborY)) {
    return 1;
  }
  if (hasGroundBody(neighborX, neighborY) && hasGroundBody(neighborX, neighborY + 1)) {
    return 2;
  }
  return 0;
}

/** Deep copy mask for JSON output. */
export function cloneMask(mask) {
  return mask.map((col) => col.slice());
}

/** JSON-serializable mask as columns [x][y]. */
export function maskToJson(mask) {
  return cloneMask(mask);
}
