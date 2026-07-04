// Port of AutotileRule.Matches — pattern flattening is pattern[x + y * 3] = mask[x,y].

/**
 * @param {number[]} pattern
 * @param {number[][]} mask 3x3 as mask[x][y]
 * @param {boolean} flipInput north/south mirror within each column
 * @returns {boolean}
 */
export function ruleMatches(pattern, mask, flipInput) {
  if (!mask || mask.length !== 3 || mask[0].length !== 3) {
    return false;
  }
  for (let x = 0; x < 3; x++) {
    for (let y = 0; y < 3; y++) {
      const my = flipInput ? 2 - y : y;
      if (pattern[x + y * 3] !== mask[x][my]) {
        return false;
      }
    }
  }
  return true;
}

/**
 * @param {number[]} pattern
 * @param {number[][]} mask 3x3 as mask[x][y]
 * @param {boolean} flipColumns west/east column mirror
 * @returns {boolean}
 */
export function ruleMatchesColumns(pattern, mask, flipColumns) {
  if (!mask || mask.length !== 3 || mask[0].length !== 3) {
    return false;
  }
  for (let x = 0; x < 3; x++) {
    for (let y = 0; y < 3; y++) {
      const mx = flipColumns ? 2 - x : x;
      if (pattern[x + y * 3] !== mask[mx][y]) {
        return false;
      }
    }
  }
  return true;
}

/** @param {number[]} pattern */
export function patternToMask(pattern) {
  const mask = Array.from({ length: 3 }, () => [0, 0, 0]);
  for (let x = 0; x < 3; x++) {
    for (let y = 0; y < 3; y++) {
      mask[x][y] = pattern[x + y * 3];
    }
  }
  return mask;
}

export function masksEqual(left, right) {
  if (!left || !right) return false;
  for (let x = 0; x < 3; x++) {
    for (let y = 0; y < 3; y++) {
      if (left[x][y] !== right[x][y]) return false;
    }
  }
  return true;
}
