// Normalize and compare autotile cells for drift RCA.

const BASELINE_FORMAT = 'project-twelve/autotile-baseline/v1';

/**
 * @param {object} tileEntry from buildAutotileReport or MCP dump
 * @returns {object|null}
 */
export function extractCompareCell(tileEntry) {
  if (!tileEntry || tileEntry.solid === false) {
    return null;
  }

  const ground = tileEntry.autotile?.ground;
  const cover = tileEntry.autotile?.cover;
  const cell = {
    x: tileEntry.x,
    y: tileEntry.y,
    tileId: tileEntry.tileId,
    ground: ground
      ? {
          spriteId: ground.spriteId ?? ground.finalSpriteId ?? null,
          flipX: ground.flipX ?? false,
          finalSpriteId: ground.finalSpriteId ?? ground.spriteId ?? null,
          normalization: {
            innerCavity: ground.normalization?.innerCavity ?? false,
          },
          resolved: ground.resolved !== false,
        }
      : null,
    cover: cover
      ? {
          rendered: cover.rendered === true,
          spriteId: cover.spriteId ?? null,
          flipX: cover.flipX ?? false,
        }
      : { rendered: false, spriteId: null, flipX: false },
  };

  return cell;
}

/**
 * @param {object} doc baseline or report document
 * @returns {Map<string, object>}
 */
export function indexAutotileCells(doc) {
  const map = new Map();
  const rows = doc.cells ?? doc.tiles ?? [];
  for (const entry of rows) {
    const cell = entry.ground || entry.cover ? entry : extractCompareCell(entry);
    if (!cell) {
      continue;
    }
    map.set(`${cell.x},${cell.y}`, cell);
  }
  return map;
}

/**
 * @param {object} expected
 * @param {object} actual
 * @param {object} [options]
 * @param {'ground'|'cover'|'all'} [options.only]
 * @returns {string[]}
 */
export function diffCompareCells(expected, actual, options = {}) {
  const only = options.only ?? 'all';
  const errors = [];

  if (only === 'all' || only === 'ground') {
    if (expected.tileId !== actual.tileId) {
      errors.push(`tileId: expected ${expected.tileId}, got ${actual.tileId}`);
    }
    const eg = expected.ground;
    const ag = actual.ground;
    if (!eg && ag) {
      errors.push('ground: expected none, got entry');
    } else if (eg && !ag) {
      errors.push('ground: missing in actual');
    } else if (eg && ag) {
      if (eg.spriteId !== ag.spriteId) {
        errors.push(`ground.spriteId: expected ${eg.spriteId}, got ${ag.spriteId}`);
      }
      if (Boolean(eg.flipX) !== Boolean(ag.flipX)) {
        errors.push(`ground.flipX: expected ${Boolean(eg.flipX)}, got ${Boolean(ag.flipX)}`);
      }
      const ei = eg.normalization?.innerCavity ?? false;
      const ai = ag.normalization?.innerCavity ?? false;
      if (ei !== ai) {
        errors.push(`ground.normalization.innerCavity: expected ${ei}, got ${ai}`);
      }
    }
  }

  if (only === 'all' || only === 'cover') {
    const ec = expected.cover ?? { rendered: false };
    const ac = actual.cover ?? { rendered: false };
    if (Boolean(ec.rendered) !== Boolean(ac.rendered)) {
      errors.push(`cover.rendered: expected ${Boolean(ec.rendered)}, got ${Boolean(ac.rendered)}`);
    } else if (ec.rendered && ac.rendered) {
      if (ec.spriteId !== ac.spriteId) {
        errors.push(`cover.spriteId: expected ${ec.spriteId}, got ${ac.spriteId}`);
      }
      if (Boolean(ec.flipX) !== Boolean(ac.flipX)) {
        errors.push(`cover.flipX: expected ${Boolean(ec.flipX)}, got ${Boolean(ac.flipX)}`);
      }
    }
  }

  return errors;
}

/**
 * @param {object} baselineDoc
 * @param {object} actualDoc report or baseline-shaped doc
 * @param {object} [options]
 * @returns {{ summary: object, diffs: object[] }}
 */
export function compareAutotileBaseline(baselineDoc, actualDoc, options = {}) {
  const baseline = indexAutotileCells(baselineDoc);
  const actual = indexAutotileCells(actualDoc);
  const only = options.only ?? 'all';
  const maxDiffs = options.maxDiffs ?? 100;
  const region = options.region ?? null;
  const coordFilter = options.coords ?? null;

  const diffs = [];
  let compared = 0;
  let matched = 0;
  let missingInActual = 0;

  for (const [key, expected] of baseline.entries()) {
    const [x, y] = key.split(',').map(Number);
    if (region) {
      if (x < region.xMin || x > region.xMax || y < region.yMin || y > region.yMax) {
        continue;
      }
    }
    if (coordFilter && !coordFilter.has(key)) {
      continue;
    }

    compared++;
    const got = actual.get(key);
    if (!got) {
      missingInActual++;
      if (diffs.length < maxDiffs) {
        diffs.push({ x, y, errors: ['missing in actual'] });
      }
      continue;
    }

    const errors = diffCompareCells(expected, got, { only });
    if (errors.length === 0) {
      matched++;
      continue;
    }

    if (diffs.length < maxDiffs) {
      diffs.push({ x, y, expected, actual: got, errors });
    }
  }

  return {
    summary: {
      baselineName: baselineDoc.name ?? 'baseline',
      actualName: actualDoc.name ?? 'actual',
      compared,
      matched,
      mismatched: compared - matched - missingInActual,
      missingInActual,
      truncated: diffs.length >= maxDiffs,
    },
    diffs,
  };
}

/**
 * @param {object} space
 * @param {object} report from buildAutotileReport
 * @returns {object}
 */
export function buildBaselineDocument(space, report) {
  const cells = [];
  for (const tile of report.tiles) {
    const cell = extractCompareCell(tile);
    if (cell) {
      cells.push(cell);
    }
  }

  return {
    format: BASELINE_FORMAT,
    name: space.name ?? report.name,
    xMin: space.xMin,
    yMin: space.yMin,
    xMax: space.xMax,
    yMax: space.yMax,
    cellCount: cells.length,
    cells,
  };
}

export { BASELINE_FORMAT };
