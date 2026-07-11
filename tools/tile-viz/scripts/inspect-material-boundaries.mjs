#!/usr/bin/env node
/**
 * Experiment with sprite substitutions at heterogenic material boundaries.
 * Renders baseline + single-cell permutations for each geometric fixture.
 *
 * Usage:
 *   node scripts/inspect-material-boundaries.mjs [--out out/material-boundary-exp]
 */

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { PNG } from 'pngjs';

import { buildAutotileReport } from '../src/report/autotileJson.js';
import { loadTileSpaceFromFile } from '../src/io/tileSpace.js';
import { renderAutotilePng } from '../src/render/autotilePng.js';
import { sharesGroundAutotileGroup } from '../src/visual/catalog.js';
import { getTile } from '../src/io/tileSpace.js';
import { TileId } from '../../world-viz/src/core/tiles.js';
import { getRulesForSpriteCount, GROUND_SPRITE_COUNT, loadRuleTables } from '../src/visual/ruleTables.js';
import { findMatchingSpriteIds } from '../src/visual/resolver.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(__dirname, '..');
const assetsRoot = path.resolve(root, '..', '..', 'Assets', '_Licensed', 'PixelFantasy', 'PixelTileEngine', 'Tiles');
const snippetsDir = path.join(root, 'test', 'fixtures', 'snippets');

const FIXTURES = [
  'material-boundary-horizontal',
  'material-boundary-vertical',
  'dirt-stone-reentrant-west',
  'dirt-over-stone-slab',
  'ore-dirt-boundary',
  'ore-mixed-boundary',
];

/** Sprites commonly relevant at material edges (from ground-autotile-32-rules.md). */
const EDGE_CANDIDATES = [0, 7, 8, 15, 16, 17, 21, 22, 23, 24, 25, 27, 28, 29, 30, 31];

/** Known provisional partner pairs to try as paired substitutions. */
const PARTNER_PAIRS = [
  [24, 25], [25, 24],
  [24, 7], [7, 24],
  [24, 8], [8, 24],
  [24, 22], [22, 24],
  [28, 29], [29, 28],
  [28, 21], [21, 28],
  [29, 21], [21, 29],
  [16, 23], [23, 16],
  [16, 0], [0, 16],
  [30, 31], [31, 30],
  [21, 22], [22, 21],
  [8, 15], [15, 8],
];

const CARDINAL = [
  [0, 1], [0, -1], [1, 0], [-1, 0],
];

function parseArgs(argv) {
  const opts = { outDir: path.join(root, 'out', 'material-boundary-exp'), scale: 32 };
  for (let i = 0; i < argv.length; i++) {
    if (argv[i] === '--out' && argv[i + 1]) {
      opts.outDir = path.resolve(argv[++i]);
    } else if (argv[i] === '--scale' && argv[i + 1]) {
      opts.scale = Number(argv[++i]);
    }
  }
  return opts;
}

function isBoundaryCell(space, x, y) {
  const center = getTile(space, x, y);
  if (center.id === TileId.Air) {
    return false;
  }
  for (const [dx, dy] of CARDINAL) {
    const neighbor = getTile(space, x + dx, y + dy);
    if (neighbor.id !== TileId.Air && !sharesGroundAutotileGroup(center.id, neighbor.id)) {
      return true;
    }
  }
  return false;
}

function maskKey(mask) {
  if (!mask || mask.length !== 3) return '';
  // Display as row-major NW..SE (not column-major x,y storage).
  return [
    [mask[0][0], mask[1][0], mask[2][0]],
    [mask[0][1], mask[1][1], mask[2][1]],
    [mask[0][2], mask[1][2], mask[2][2]],
  ].map((row) => row.join('')).join('/');
}

function maskMatchingSpriteIds(mask, tables) {
  const rules = getRulesForSpriteCount(GROUND_SPRITE_COUNT, tables);
  return findMatchingSpriteIds(rules, mask).map(String);
}

function buildCandidates(cell) {
  const g = cell.autotile.ground;
  if (!g?.resolved) {
    return [];
  }
  const currentId = Number(g.spriteId);
  const currentFlip = Boolean(g.flipX);
  const mask = g.mask ?? g.normalizedMask ?? g.connectivityMask;
  const maskMatches = mask ? maskMatchingSpriteIds(mask, cell._tables) : [];

  const ids = new Set([
    currentId,
    ...maskMatches.map(Number),
    ...EDGE_CANDIDATES,
    currentId - 1,
    currentId + 1,
  ].filter((n) => Number.isFinite(n) && n >= 0 && n < GROUND_SPRITE_COUNT));

  const candidates = [];
  const seen = new Set();

  function add(spriteId, flipX, tag) {
    const key = `${spriteId}:${flipX ? 1 : 0}`;
    if (seen.has(key)) {
      return;
    }
    seen.add(key);
    candidates.push({
      spriteId,
      flipX,
      tag,
      isBaseline: spriteId === currentId && flipX === currentFlip,
    });
  }

  add(currentId, currentFlip, 'baseline');
  add(currentId, !currentFlip, 'flip-toggle');

  for (const id of ids) {
    add(id, false, id === currentId ? 'same-id' : 'candidate');
    if ([0, 7, 8, 15, 16, 22, 23, 24, 30].includes(id)) {
      add(id, true, 'candidate-flip');
    }
  }

  for (const [a, b] of PARTNER_PAIRS) {
    if (currentId === a) {
      add(b, false, `partner-${a}->${b}`);
      add(b, true, `partner-${a}->${b}-flip`);
    }
  }

  return candidates;
}

function makeOverride(x, y, tileset, spriteId, flipX) {
  return {
    x,
    y,
    ground: {
      tileset,
      spriteId,
      flipX,
    },
  };
}

function renderVariant(space, overrides, opts) {
  return renderAutotilePng(space, {
    assetsRoot,
    scale: opts.scale,
    flatLight: true,
    visualOverrides: { overrides },
  });
}

function blitIntoSheet(sheet, srcPng, col, row, cellW, cellH, label) {
  const src = PNG.sync.read(srcPng);
  const ox = col * cellW;
  const oy = row * cellH;
  for (let y = 0; y < src.height && y < cellH; y++) {
    for (let x = 0; x < src.width && x < cellW; x++) {
      const si = (y * src.width + x) * 4;
      const di = ((oy + y) * sheet.width + (ox + x)) * 4;
      sheet.data[di] = src.data[si];
      sheet.data[di + 1] = src.data[si + 1];
      sheet.data[di + 2] = src.data[si + 2];
      sheet.data[di + 3] = src.data[si + 3];
    }
  }
  // dim label bar at bottom of cell (reserved space handled by padding)
  void label;
}

function analyzeFixture(space, tables) {
  const report = buildAutotileReport(space, { includeAir: true });
  const boundaryCells = [];

  for (const tile of report.tiles) {
    if (tile.tileId === TileId.Air) {
      continue;
    }
    if (!isBoundaryCell(space, tile.x, tile.y)) {
      continue;
    }
    const enriched = { ...tile, _tables: tables };
    const g = tile.autotile.ground;
    boundaryCells.push({
      x: tile.x,
      y: tile.y,
      tileName: tile.tileName,
      tileset: g?.tileset,
      mask: maskKey(g?.mask ?? []),
      spriteId: g?.spriteId,
      flipX: g?.flipX,
      candidates: buildCandidates(enriched),
    });
  }

  return { report, boundaryCells };
}

function runFixture(fixtureName, opts) {
  const spacePath = path.join(snippetsDir, `${fixtureName}.json`);
  const space = loadTileSpaceFromFile(spacePath);
  const tables = loadRuleTables();
  const { boundaryCells } = analyzeFixture(space, tables);

  const fixtureDir = path.join(opts.outDir, fixtureName);
  fs.mkdirSync(fixtureDir, { recursive: true });

  const variants = [];
  const baselinePng = renderVariant(space, [], opts);
  const baselinePath = path.join(fixtureDir, '00-baseline.png');
  fs.writeFileSync(baselinePath, baselinePng);
  variants.push({
    file: '00-baseline.png',
    label: 'baseline',
    overrides: [],
  });

  let variantIndex = 1;
  for (const cell of boundaryCells) {
    for (const cand of cell.candidates) {
      if (cand.isBaseline) {
        continue;
      }
      const overrides = [
        makeOverride(cell.x, cell.y, cell.tileset, cand.spriteId, cand.flipX),
      ];
      const label = `${cell.x},${cell.y} ${cell.tileName} ${cand.spriteId}${cand.flipX ? 'f' : ''} (${cand.tag})`;
      const fileName = `${String(variantIndex).padStart(3, '0')}-x${cell.x}-y${cell.y}-s${cand.spriteId}${cand.flipX ? 'f' : ''}.png`;
      const png = renderVariant(space, overrides, opts);
      fs.writeFileSync(path.join(fixtureDir, fileName), png);
      variants.push({ file: fileName, label, overrides, cell, candidate: cand });
      variantIndex++;
    }
  }

  // Multi-cell partner experiments on fixtures with 2+ boundary cells on same material row
  if (boundaryCells.length >= 2 && boundaryCells.length <= 4) {
    const humusCells = boundaryCells.filter((c) => c.tileset === 'Humus');
    if (humusCells.length >= 2) {
      for (const [a, b] of [[24, 25], [25, 24], [24, 24], [7, 7], [8, 8]]) {
        const overrides = humusCells.slice(0, 3).map((cell, i) => {
          const spriteId = i === 1 ? b : a;
          const flipX = cell.spriteId === '24' && cell.flipX && spriteId === 24;
          return makeOverride(cell.x, cell.y, cell.tileset, spriteId, flipX);
        });
        const fileName = `${String(variantIndex).padStart(3, '0')}-multi-humus-${a}-${b}-${b}.png`;
        const png = renderVariant(space, overrides, opts);
        fs.writeFileSync(path.join(fixtureDir, fileName), png);
        variants.push({
          file: fileName,
          label: `multi Humus row [${a},${b},${a}]`,
          overrides,
        });
        variantIndex++;
      }
    }
  }

  const summary = {
    fixture: fixtureName,
    ascii: buildAutotileReport(space, { includeAir: true }).ascii,
    boundaryCells: boundaryCells.map(({ _tables, candidates, ...rest }) => ({
      ...rest,
      candidateCount: candidates.length,
    })),
    variantCount: variants.length,
    variants: variants.map(({ file, label }) => ({ file, label })),
  };

  fs.writeFileSync(path.join(fixtureDir, 'summary.json'), JSON.stringify(summary, null, 2));

  console.log(`\n=== ${fixtureName} ===`);
  console.log(summary.ascii.join('\n'));
  for (const cell of boundaryCells) {
    console.log(
      `  boundary (${cell.x},${cell.y}) ${cell.tileName}/${cell.tileset} ` +
      `mask=${cell.mask} auto=${cell.spriteId}${cell.flipX ? 'f' : ''} ` +
      `candidates=${cell.candidates.length}`,
    );
  }
  console.log(`  rendered ${variants.length} variants -> ${fixtureDir}`);

  return { fixtureName, fixtureDir, variants, boundaryCells, baselinePng, space };
}

async function buildContactSheet(results, opts) {
  const thumbScale = opts.scale;
  const pad = 4;
  const labelH = 0;

  let cols = 0;
  for (const r of results) {
    cols = Math.max(cols, Math.min(r.variants.length, 12));
  }
  const rows = results.length;
  const samplePng = results[0].baselinePng;
  const sample = PNG.sync.read(samplePng);
  const cellW = sample.width + pad;
  const cellH = sample.height + pad + labelH;

  const sheet = new PNG({
    width: cols * cellW,
    height: rows * cellH,
  });
  sheet.data.fill(0x40);

  for (let row = 0; row < results.length; row++) {
    const result = results[row];
    const picks = [
      result.variants[0],
      ...result.variants.filter((v) => v.label.includes('partner') || v.label.includes('multi')).slice(0, 5),
      ...result.variants.slice(1, 4),
    ].slice(0, cols);

    for (let col = 0; col < picks.length; col++) {
      const v = picks[col];
      const png = fs.readFileSync(path.join(result.fixtureDir, v.file));
      blitIntoSheet(sheet, png, col, row, cellW - pad, cellH - pad, v.label);
    }
  }

  const sheetPath = path.join(opts.outDir, 'contact-sheet.png');
  fs.writeFileSync(sheetPath, PNG.sync.write(sheet));
  console.log(`\nContact sheet: ${sheetPath}`);
}

async function main() {
  const opts = parseArgs(process.argv.slice(2));
  fs.mkdirSync(opts.outDir, { recursive: true });

  const results = [];
  for (const fixture of FIXTURES) {
    results.push(await runFixture(fixture, opts));
  }

  await buildContactSheet(results, opts);

  const report = {
    generatedAt: new Date().toISOString(),
    fixtures: results.map((r) => ({
      name: r.fixtureName,
      boundaryCellCount: r.boundaryCells.length,
      variantCount: r.variants.length,
      dir: r.fixtureDir,
    })),
    findings: [
      {
        fixture: 'material-boundary-horizontal',
        issue: 'Center dirt cell resolves bridge sprite 25 (000/111/000) between two lip cells 24/24f above foreign stone row — creates a floating shaft look at the material horizon.',
        bestManualFix: 'Replace center 25 with 24f (all-lip row: 24, 24f, 24f) — see comparisons/horizontal-all-24-lips.png',
        ruleProposal: 'Post-resolve: when mask is 000/111/000 and the cell below is foreign solid, prefer lip 24 (+flipX from chirality) instead of bridge 25.',
      },
      {
        fixture: 'material-boundary-vertical',
        issue: 'Caps 29 (dirt) / 28 (stone) are topologically correct; seam is tileset art mismatch (Humus vs Rocks), not wrong sprite id.',
        bestManualFix: 'No sprite substitution helps — needs art alignment or a dedicated transition tile in the assets submodule.',
        ruleProposal: 'Keep vendor caps; do not remap 28/29 at vertical boundaries.',
      },
      {
        fixture: 'dirt-stone-reentrant-west',
        issue: 'Re-entrant corner uses 0/16/21/24f/28/29/30f mix; dirt bottom cap 29 at (2,0) reads as pillar foot abutting stone lip 24f.',
        bestManualFix: 'Try 24 lip at (2,0) instead of 29 — comparisons/reentrant-dirt-24-lip.png',
        ruleProposal: 'At re-entrant west corners where foreign stone is east-below, prefer horizontal lip 24 over vertical cap 29.',
      },
      {
        fixture: 'dirt-over-stone-slab',
        issue: 'Underside family 16/17 at dirt-on-stone horizon is already correct; 17 is the right cap for flat slab.',
        bestManualFix: 'Keep baseline — underside 17 substitution breaks the slab lip.',
        ruleProposal: 'No change; anti-regression: do not collapse 17 to fill 9/10.',
      },
      {
        fixture: 'ore-dirt-boundary',
        issue: 'Same cap topology as vertical boundary but across BricksA/Humus tilesets; 28/29/21 caps are correct per mask.',
        bestManualFix: 'None via sprite id — ore tileset art defines the seam.',
        ruleProposal: 'Keep foreign-material disconnect in blob mask (already correct).',
      },
      {
        fixture: 'ore-mixed-boundary',
        issue: 'BricksA/BricksB side-by-side uses mirrored 28 caps; seam is ore-to-ore art boundary.',
        bestManualFix: 'None — topology is correct.',
        ruleProposal: 'No resolver change.',
      },
    ],
    rejectedSubstitutions: [
      '24 -> 25 partner swap (bridge breaks one-sided lips; breaks one-sided-house-lip fixture)',
      '21 -> 22 pillar to side-body (unsafe without mask proof)',
      '28 -> 21 cap to pillar at vertical boundaries (loses top-cap shading)',
      'Enabling side-only TryRemapMaterialBoundaryCornerMask (does not fire for layered horizontal boundaries)',
    ],
    comparisonsDir: path.join(opts.outDir, 'comparisons'),
  };
  fs.writeFileSync(path.join(opts.outDir, 'experiment-report.json'), JSON.stringify(report, null, 2));
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
