// Reserved for fixture-validated authored partners — vendor baseline uses flipX on same sprite id.
// See docs/wiki/ground-autotile-32-rules.md § Mirroring policy.

/** @type {Record<string, string>} */
const COLUMN_FLIP_PARTNERS = {};

/** @type {Record<string, string>} */
const ROW_FLIP_PARTNERS = {};

export function usesAuthoredGroundPartners(spriteCount) {
  return spriteCount === 32;
}

export function getColumnFlipPartner(spriteId) {
  return COLUMN_FLIP_PARTNERS[spriteId] ?? null;
}

export function getRowFlipPartner(spriteId) {
  return ROW_FLIP_PARTNERS[spriteId] ?? null;
}
