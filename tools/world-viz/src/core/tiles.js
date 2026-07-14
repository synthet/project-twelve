// Tile identity, naming, and a DEBUG colour palette.
//
// Mirrors Assets/Scripts/Sandbox/SandboxTile.cs (SandboxTileIds) and the light
// model from SandboxChunkRenderer.GetTileLightColor.
//
// NOTE: this is intentionally NOT a literal copy of the engine's
// GetLegacyTileColor, which returns near-grayscale because the real per-tile
// colour comes from the atlas *texture*. For an offline debug view we assign a
// distinct base colour per tile id, then apply the engine's light model on top
// so the surface(light 15)/underground(light 4) seam stays visible.

import { clamp01, lerp } from './mathf.js';

// Runtime tile ids MUST match the engine registry's frozen indices
// (ContentRegistry.Freeze): the empty tile `core:air` is pinned to 0, and every
// other id is assigned by ordinal (lexicographic) string sort. For the current
// core tile set that is: air 0, bricks_a 1, bricks_b 2, bricks_c 3, bricks_d 4,
// dirt 5, frozen 6, grass 7, magma 8, sand 9, stone 10. (The former "ore" tiles were renamed to match the
// vendor BricksA–D art they always rendered with.) The golden fixture stores
// these indices, so the offline tool must use the same numbering to stay
// parity-correct — regenerate the fixture after any registry ID change.
export const TileId = Object.freeze({
  Air: 0,
  BricksA: 1,
  BricksB: 2,
  BricksC: 3,
  BricksD: 4,
  Dirt: 5,
  Frozen: 6,
  Grass: 7,
  Magma: 8,
  Sand: 9,
  Stone: 10,
});

export const TILE_NAMES = Object.freeze({
  0: 'Air',
  1: 'BricksA',
  2: 'BricksB',
  3: 'BricksC',
  4: 'BricksD',
  5: 'Dirt',
  6: 'Frozen',
  7: 'Grass',
  8: 'Magma',
  9: 'Sand',
  10: 'Stone',
});

// Single ASCII glyph per tile id for the text dump.
export const TILE_GLYPHS = Object.freeze({
  0: '.',
  1: 'A',
  2: 'B',
  3: 'C',
  4: 'D',
  5: '#',
  6: 'F',
  7: '"',
  8: 'M',
  9: '~',
  10: '%',
});

// Base [r, g, b] (0-255) per tile id before light shading.
const BASE_COLORS = Object.freeze({
  0: [135, 206, 235], // Air  -> sky blue (rendered unshaded as background)
  1: [132, 132, 144], // BricksA -> grey masonry
  2: [72, 92, 132], // BricksB -> blue masonry
  3: [62, 68, 98], // BricksC -> slate masonry
  4: [182, 84, 52], // BricksD -> red masonry
  5: [134, 96, 67], // Dirt -> brown
  6: [152, 194, 214], // Frozen -> pale blue ice
  7: [83, 160, 60], // Grass -> green
  8: [224, 80, 32], // Magma -> hot orange
  9: [214, 188, 120], // Sand -> warm tan
  10: [128, 128, 128], // Stone -> gray
});

/**
 * Engine light model (SandboxChunkRenderer.GetTileLightColor):
 *   brightness = lerp(0.35, 1, clamp01(light / 15)).
 */
export function lightBrightness(light) {
  return lerp(0.35, 1, clamp01(light / 15));
}

/**
 * Debug RGB for a tile. Air is returned at full sky colour (unshaded) so the
 * background reads cleanly; solid tiles are shaded by their stored light value.
 * Returns [r, g, b] integers in 0-255.
 */
export function tileColor(id, light) {
  const base = BASE_COLORS[id] || [255, 0, 255]; // magenta = unknown id
  if (id === TileId.Air) return base.slice();
  const b = lightBrightness(light);
  return [
    Math.round(base[0] * b),
    Math.round(base[1] * b),
    Math.round(base[2] * b),
  ];
}

/** Construct a tile record matching SandboxTile's fields. */
export function makeTile(id, light = 0, fluid = 0, metadata = 0) {
  return { id, light, fluid, metadata };
}
