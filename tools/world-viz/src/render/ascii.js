// ASCII / text dump of a sampled region, plus a per-column surface-height table.
// Useful for quick terminal diffing and as a stable target for golden tests.

import { TILE_GLYPHS, TILE_NAMES, TileId } from '../core/tiles.js';

/**
 * Render a sampled region (from world.sampleRegion) as text.
 * @param {object} region  { minX, maxX, minY, maxY, width, height, tiles }
 * @param {object} world   World instance, used for the surface-height table
 * @returns {string}
 */
export function renderAscii(region, world) {
  const lines = [];
  lines.push(
    `# world-viz region  x:[${region.minX}..${region.maxX}]  y:[${region.minY}..${region.maxY}]  ` +
      `(${region.width}x${region.height} tiles, top row = y ${region.maxY})`,
  );
  lines.push(`# legend: ${legend()}`);
  lines.push('');

  for (let row = 0; row < region.height; row++) {
    const worldY = region.maxY - row;
    let s = '';
    for (let col = 0; col < region.width; col++) {
      s += TILE_GLYPHS[region.tiles[row][col].id] ?? '?';
    }
    lines.push(`${String(worldY).padStart(5)} |${s}`);
  }

  lines.push('');
  lines.push('# surface height per world column (x: height)');
  const parts = [];
  for (let x = region.minX; x <= region.maxX; x++) {
    parts.push(`${x}:${world.surfaceHeight(x)}`);
  }
  // Wrap the height table for readability.
  for (let i = 0; i < parts.length; i += 8) {
    lines.push('  ' + parts.slice(i, i + 8).join('  '));
  }
  lines.push('');
  return lines.join('\n');
}

function legend() {
  const ids = [
    TileId.Air,
    TileId.Dirt,
    TileId.Grass,
    TileId.Stone,
    TileId.CopperOre,
    TileId.IronOre,
    TileId.SilverOre,
    TileId.GoldOre,
  ];
  return ids.map((id) => `${TILE_GLYPHS[id]}=${TILE_NAMES[id]}`).join('  ');
}
