// Port of AutotileMaskBuilder.cs

/**
 * @param {(dx: number, dy: number) => boolean} has
 * @param {(dx: number) => boolean|null} hasSouth
 * @returns {number[][]}
 */
function buildGroundMaskFromPredicate(has, hasSouth = null) {
  const hasSouthAt = (dx) => (hasSouth !== null ? hasSouth(dx) : has(dx, -1));
  return [
    [
      has(-1, 1) && has(-1, 0) && has(0, 1) ? 1 : 0,
      has(-1, 0) ? 1 : 0,
      hasSouthAt(-1) && has(-1, 0) && hasSouthAt(0) ? 1 : 0,
    ],
    [has(0, 1) ? 1 : 0, 1, hasSouthAt(0) ? 1 : 0],
    [
      has(1, 1) && has(1, 0) && has(0, 1) ? 1 : 0,
      has(1, 0) ? 1 : 0,
      hasSouthAt(1) && has(1, 0) && hasSouthAt(0) ? 1 : 0,
    ],
  ];
}

/**
 * Same-material / same-ground visual connectivity only.
 * @param {(x: number, y: number) => boolean} sharesGroundGroup
 * @param {number} worldX
 * @param {number} worldY
 * @returns {number[][]}
 */
export function buildVisualGroundMask(sharesGroundGroup, worldX, worldY) {
  return buildGroundMaskFromPredicate((dx, dy) => sharesGroundGroup(worldX + dx, worldY + dy));
}

/**
 * Any-solid connectivity for physical support and exposure context.
 * @param {(x: number, y: number) => boolean} isSolid
 * @param {number} worldX
 * @param {number} worldY
 * @returns {number[][]}
 */
export function buildSolidGroundMask(isSolid, worldX, worldY) {
  return buildGroundMaskFromPredicate((dx, dy) => isSolid(worldX + dx, worldY + dy));
}

/**
 * @param {(x: number, y: number) => boolean} sharesGroundGroup
 * @param {number} worldX
 * @param {number} worldY
 * @returns {number[][]}
 */
export function buildGroundMask(sharesGroundGroup, worldX, worldY, isSolid = sharesGroundGroup, isSurfaceTile = null) {
  return buildGroundMaskDetailed(sharesGroundGroup, worldX, worldY, isSolid, isSurfaceTile).finalMask;
}

/**
 * @param {(x: number, y: number) => boolean} sharesGroundGroup
 * @param {number} worldX
 * @param {number} worldY
 * @param {(x: number, y: number) => boolean|null} isSolid
 * @param {(x: number, y: number) => boolean|null} isSurfaceTile
 * @returns {{
 *   visualMask: number[][],
 *   solidMask: number[][]|null,
 *   connectivityMask: number[][],
 *   finalMask: number[][],
 *   normalization: { stairInterior: boolean, cavityUnderside: boolean, materialBoundary: boolean },
 * }}
 */
export function buildGroundMaskDetailed(
  sharesGroundGroup,
  worldX,
  worldY,
  isSolid = sharesGroundGroup,
  isSurfaceTile = null,
) {
  const visualMask = buildVisualGroundMask(sharesGroundGroup, worldX, worldY);
  const solidMask = isSolid !== null ? buildSolidGroundMask(isSolid, worldX, worldY) : null;
  const connectivityMask = buildConnectivityGroundMask(sharesGroundGroup, worldX, worldY, isSolid);
  const { mask: finalMask, normalization } = normalizeGroundMaskDetailed(
    connectivityMask,
    sharesGroundGroup,
    isSolid,
    worldX,
    worldY,
    isSurfaceTile,
  );
  return {
    visualMask,
    solidMask,
    connectivityMask,
    finalMask,
    normalization,
    normalizationTrace: buildNormalizationTrace(normalization),
  };
}

/**
 * Ordered normalizers, evaluated first-match-wins. The trace and the C# side
 * (AutotileMaskBuilder.NormalizationOrder) must stay in lockstep.
 * @type {ReadonlyArray<{ key: string, appliedReason: string }>}
 */
export const NORMALIZATION_ORDER = [
  { key: 'stairInterior', appliedReason: 'diagonal step -> interior fill' },
  { key: 'cavityUnderside', appliedReason: 'bridge -> underside' },
  { key: 'materialBoundary', appliedReason: 'south row cleared' },
];

/**
 * Ordered decision trace derived from the normalization flags. Because the
 * normalizers short-circuit on first match, at most one flag is set: every
 * normalizer before it reads "skipped", the matching one reads "applied", and
 * normalizers after it are never evaluated (and so are omitted).
 * @param {{ [key: string]: boolean }} normalization
 * @returns {string[]}
 */
export function buildNormalizationTrace(normalization) {
  const trace = [];
  for (const { key, appliedReason } of NORMALIZATION_ORDER) {
    if (normalization[key]) {
      trace.push(`${key}: applied: ${appliedReason}`);
      return trace;
    }
    trace.push(`${key}: skipped`);
  }
  return trace;
}

/**
 * @param {(x: number, y: number) => boolean} sharesGroundGroup
 * @param {number} worldX
 * @param {number} worldY
 * @returns {number[][]}
 */
export function buildConnectivityGroundMask(sharesGroundGroup, worldX, worldY, isSolid = null) {
  if (isSolid === null) {
    return buildVisualGroundMask(sharesGroundGroup, worldX, worldY);
  }

  const has = (dx, dy) => sharesGroundGroup(worldX + dx, worldY + dy);
  const checkSupport = (dx, dy) => has(dx, dy) || (dy <= 0 && isSolid(worldX + dx, worldY + dy));

  return [
    [
      checkSupport(-1, 1) && checkSupport(-1, 0) && checkSupport(0, 1) ? 1 : 0,
      checkSupport(-1, 0) ? 1 : 0,
      checkSupport(-1, -1) && checkSupport(-1, 0) && checkSupport(0, -1) ? 1 : 0,
    ],
    [checkSupport(0, 1) ? 1 : 0, 1, checkSupport(0, -1) ? 1 : 0],
    [
      checkSupport(1, 1) && checkSupport(1, 0) && checkSupport(0, 1) ? 1 : 0,
      checkSupport(1, 0) ? 1 : 0,
      checkSupport(1, -1) && checkSupport(1, 0) && checkSupport(0, -1) ? 1 : 0,
    ],
  ];
}

/**
 * @param {number[][]} mask
 * @returns {number[][]}
 */
export function normalizeGroundMask(
  mask,
  sharesGroundGroup = null,
  isSolid = null,
  worldX = 0,
  worldY = 0,
  isSurfaceTile = null,
) {
  return normalizeGroundMaskDetailed(
    mask,
    sharesGroundGroup,
    isSolid,
    worldX,
    worldY,
    isSurfaceTile,
  ).mask;
}

/**
 * @param {number[][]} mask
 * @returns {{ mask: number[][], normalization: { stairInterior: boolean, cavityUnderside: boolean, materialBoundary: boolean } }}
 */
export function normalizeGroundMaskDetailed(
  mask,
  sharesGroundGroup = null,
  isSolid = null,
  worldX = 0,
  worldY = 0,
  isSurfaceTile = null,
) {
  const normalization = {
    stairInterior: false,
    cavityUnderside: false,
    materialBoundary: false,
  };

  const stairInterior = tryRemapStairInteriorDiagonalMask(mask, isSurfaceTile, worldX, worldY);
  if (stairInterior) {
    normalization.stairInterior = true;
    return { mask: stairInterior, normalization };
  }

  const cavityUnderside = tryRemapCavityBridgeToUnderside(mask, sharesGroundGroup, isSolid, worldX, worldY);
  if (cavityUnderside) {
    normalization.cavityUnderside = true;
    return { mask: cavityUnderside, normalization };
  }

  const boundary = tryRemapMaterialBoundaryCornerMask(mask, sharesGroundGroup, isSolid, worldX, worldY);
  if (boundary) {
    normalization.materialBoundary = true;
    return { mask: boundary, normalization };
  }

  return { mask, normalization };
}

/**
 * Stair-step terrain commonly creates repeated NE/NW diagonal-open masks just below grass steps.
 * Those cells have full cardinal support, so visually they read better as interior fill than as a
 * repeated diagonal corner sprite on every step.
 *
 * @param {number[][]} mask
 * @returns {number[][]|null}
 */
export function tryRemapStairInteriorDiagonalMask(mask, isSurfaceTile = null, worldX = 0, worldY = 0) {
  if (!isSurfaceTile) {
    return null;
  }

  if (
    mask[1][0] !== 1
    || mask[0][1] !== 1
    || mask[2][1] !== 1
    || mask[1][2] !== 1
    || mask[0][2] !== 1
    || mask[2][2] !== 1
  ) {
    return null;
  }

  const missingNorthWest = mask[0][0] === 0 && mask[2][0] === 1 && isSurfaceTile(worldX - 1, worldY);
  const missingNorthEast = mask[2][0] === 0 && mask[0][0] === 1 && isSurfaceTile(worldX + 1, worldY);
  if (!missingNorthWest && !missingNorthEast) {
    return null;
  }

  return [
    [1, 1, 1],
    [1, 1, 1],
    [1, 1, 1],
  ];
}

function isBridgeMask(mask) {
  for (let x = 0; x < 3; x++) {
    if (mask[x][0] !== 0 || mask[x][2] !== 0 || mask[x][1] !== 1) {
      return false;
    }
  }
  return true;
}

function fullUndersideMask() {
  return [
    [1, 1, 0],
    [1, 1, 0],
    [1, 1, 0],
  ];
}

function hasFilledSouthRow(mask) {
  return mask[0][2] === 1 || mask[1][2] === 1 || mask[2][2] === 1;
}

/**
 * One-tile-wide cavity lintels/floors often match rule 25 (bridge) but should read as rule 17
 * when corner cells continue on both sides.
 *
 * @param {number[][]} mask
 * @returns {number[][]|null}
 */
export function tryRemapCavityBridgeToUnderside(mask, sharesGroundGroup, isSolid, worldX, worldY) {
  if (!sharesGroundGroup || !isSolid || !isBridgeMask(mask)) {
    return null;
  }

  if (isSolid(worldX, worldY - 1)) {
    return null;
  }

  if (!sharesGroundGroup(worldX - 1, worldY) || !sharesGroundGroup(worldX + 1, worldY)) {
    return null;
  }

  if (!isSolid(worldX, worldY + 1)) {
    return fullUndersideMask();
  }

  if (sharesGroundGroup(worldX - 1, worldY - 1) && sharesGroundGroup(worldX + 1, worldY - 1)) {
    return fullUndersideMask();
  }

  return null;
}

function isWestColumnOpen(mask) {
  return mask[0][0] === 0 && mask[0][1] === 0 && mask[0][2] === 0;
}

function isEastColumnOpen(mask) {
  return mask[2][0] === 0 && mask[2][1] === 0 && mask[2][2] === 0;
}

function clearSouthRow(mask) {
  const cleared = cloneMask(mask);
  for (let x = 0; x < 3; x++) {
    cleared[x][2] = 0;
  }
  return cleared;
}

/**
 * @param {number[][]} mask
 * @param {(x: number, y: number) => boolean|null} sharesGroundGroup
 * @param {(x: number, y: number) => boolean|null} isSolid
 * @param {number} worldX
 * @param {number} worldY
 * @returns {number[][]|null}
 */
export function tryRemapMaterialBoundaryCornerMask(mask, sharesGroundGroup, isSolid, worldX, worldY) {
  if (!sharesGroundGroup || !isSolid) {
    return null;
  }

  const foreignWest = isSolid(worldX - 1, worldY) && !sharesGroundGroup(worldX - 1, worldY);
  const foreignEast = isSolid(worldX + 1, worldY) && !sharesGroundGroup(worldX + 1, worldY);
  if (!foreignWest && !foreignEast) {
    return null;
  }

  if (!hasFilledSouthRow(mask)) {
    return null;
  }

  if (foreignWest && isWestColumnOpen(mask)) {
    return clearSouthRow(mask);
  }

  if (foreignEast && isEastColumnOpen(mask)) {
    return clearSouthRow(mask);
  }

  return null;
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
  if (hasGroundBody(neighborX, neighborY)) {
    return 2;
  }
  if (hasGroundBody(neighborX, neighborY + 1)) {
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
