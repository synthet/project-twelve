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
 *   normalization: { stairInterior: boolean, cavityUnderside: boolean, materialBoundary: boolean, innerCavity: boolean },
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
  { key: 'innerCavity', appliedReason: 'flat lintel span -> underside' },
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
 * Vendor-aligned connectivity mask: same-material blob only (PixelTileEngine GetMask).
 * isSolid is ignored; use buildSolidGroundMask for physical-support debug context.
 * @param {(x: number, y: number) => boolean} sharesGroundGroup
 * @param {number} worldX
 * @param {number} worldY
 * @param {(x: number, y: number) => boolean|null} [_isSolid]
 * @returns {number[][]}
 */
export function buildConnectivityGroundMask(sharesGroundGroup, worldX, worldY, _isSolid = null) {
  return buildVisualGroundMask(sharesGroundGroup, worldX, worldY);
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
 * @returns {{ mask: number[][], normalization: { stairInterior: boolean, cavityUnderside: boolean, materialBoundary: boolean, innerCavity: boolean } }}
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
    innerCavity: false,
  };

  // Vendor alignment: the base PixelTileEngine autotiler has no normalization layer — it resolves
  // the raw blob mask directly (exact match -> mirror -> fallback). The project normalization remaps
  // (stairInterior / innerCavity / cavityUnderside / materialBoundary) are intentionally disabled so
  // ground resolution matches vendor behavior exactly. The tryRemap* helpers are retained for
  // reference/tests but are no longer invoked here. Keep this in lockstep with the C#
  // AutotileMaskBuilder.NormalizeGroundMask pass-through.
  void sharesGroundGroup;
  void isSolid;
  void worldX;
  void worldY;
  void isSurfaceTile;
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

/**
 * Window/hole lintels and inner vertical strips inside cavities often match outside-body
 * rules (18, 0) even though they should read as underside (17) or inner face (8).
 *
 * @param {number[][]} mask
 * @returns {number[][]|null}
 */
export function tryRemapCavityInnerEdgeMask(mask, sharesGroundGroup, isSolid, worldX, worldY) {
  if (!sharesGroundGroup || !isSolid) {
    return null;
  }

  if (!sharesGroundGroup(worldX - 1, worldY) || !sharesGroundGroup(worldX + 1, worldY)) {
    return null;
  }

  if (!isSolid(worldX, worldY + 1)) {
    return null;
  }

  if (mask[1][0] !== 1 || mask[0][1] !== 1 || mask[2][1] !== 1) {
    return null;
  }

  const lintel = tryRemapCavityLintelToUnderside(mask, isSolid, worldX, worldY);
  if (lintel) {
    return lintel;
  }

  if (!isSolid(worldX, worldY - 1)) {
    return tryRemapCavityInnerVerticalMask(mask, sharesGroundGroup, worldX, worldY);
  }

  return null;
}

function tryRemapCavityLintelToUnderside(mask, isSolid, worldX, worldY) {
  if (!hasFilledSouthRow(mask)) {
    return null;
  }

  if (isCavityInnerCornerMask(mask)) {
    return null;
  }

  if (mask[0][2] !== 0 && mask[2][2] !== 0) {
    return null;
  }

  const cavityBelow = !isSolid(worldX, worldY - 1)
    || !isSolid(worldX - 1, worldY - 1)
    || !isSolid(worldX + 1, worldY - 1);
  if (!cavityBelow) {
    return null;
  }

  return fullUndersideMask();
}

function tryRemapCavityInnerVerticalMask(mask, sharesGroundGroup, worldX, worldY) {
  const westOpenOutsideCorner = mask[0][0] === 0 && mask[0][1] === 1 && mask[1][1] === 1
    && mask[1][2] === 1 && mask[2][2] === 1;
  const eastOpenOutsideCorner = mask[2][0] === 0 && mask[2][1] === 1 && mask[1][1] === 1
    && mask[1][2] === 1 && mask[0][2] === 1;
  if (!westOpenOutsideCorner && !eastOpenOutsideCorner) {
    return null;
  }

  if (westOpenOutsideCorner && !sharesGroundGroup(worldX + 1, worldY)) {
    return null;
  }

  if (eastOpenOutsideCorner && !sharesGroundGroup(worldX - 1, worldY)) {
    return null;
  }

  return westOpenOutsideCorner ? westOpenVerticalMask() : eastOpenVerticalMask();
}

function westOpenVerticalMask() {
  return [
    [0, 1, 1],
    [0, 1, 1],
    [0, 1, 1],
  ];
}

function eastOpenVerticalMask() {
  return [
    [1, 1, 0],
    [1, 1, 0],
    [1, 1, 0],
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

function isCavityInnerCornerMask(mask) {
  const southWestNotch = mask[0][2] === 1 && mask[1][2] === 1 && mask[2][2] === 0;
  const southEastNotch = mask[0][2] === 0 && mask[1][2] === 1 && mask[2][2] === 1;
  if (southWestNotch || southEastNotch) {
    return true;
  }

  const eastReentrant = mask[2][0] === 0 && mask[1][0] === 1 && mask[0][0] === 1
    && mask[0][2] === 1 && mask[1][2] === 1 && mask[2][2] === 1;
  const westReentrant = mask[0][0] === 0 && mask[1][0] === 1 && mask[2][0] === 1
    && mask[0][2] === 1 && mask[1][2] === 1 && mask[2][2] === 1;
  return eastReentrant || westReentrant;
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
 * Vendor cover model (PixelTileEngine LevelBuilder.SetCover/GetMask): the surface overlay applies
 * to any exposed-top ground cell, independent of material, so a side neighbor reads by solidity
 * alone — air is an end cap (0), an exposed-top ground continues the run (1), and ground with more
 * ground stacked above it is a rising cliff step (2). Keep in lockstep with C# BuildCoverMask.
 * @param {(x: number, y: number) => boolean} isSolid
 * @param {number} worldX
 * @param {number} worldY
 * @returns {number[][]}
 */
export function buildCoverMask(isSolid, worldX, worldY) {
  const mask = [
    [0, 0, 0],
    [0, 1, 0],
    [0, 0, 0],
  ];
  mask[0][1] = resolveCoverNeighbor(isSolid, worldX - 1, worldY);
  mask[2][1] = resolveCoverNeighbor(isSolid, worldX + 1, worldY);
  return mask;
}

function resolveCoverNeighbor(isSolid, neighborX, neighborY) {
  if (!isSolid(neighborX, neighborY)) {
    return 0;
  }
  if (isSolid(neighborX, neighborY + 1)) {
    return 2;
  }
  return 1;
}

/** Deep copy mask for JSON output. */
export function cloneMask(mask) {
  return mask.map((col) => col.slice());
}

/** JSON-serializable mask as columns [x][y]. */
export function maskToJson(mask) {
  return cloneMask(mask);
}
