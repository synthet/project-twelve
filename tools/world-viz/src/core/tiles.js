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

export const TileId = Object.freeze({
  Air: 0,
  Dirt: 1,
  Grass: 2,
  Stone: 3,
  CopperOre: 4,
  IronOre: 5,
  SilverOre: 6,
  GoldOre: 7,
});

export const TILE_NAMES = Object.freeze({
  0: 'Air',
  1: 'Dirt',
  2: 'Grass',
  3: 'Stone',
  4: 'CopperOre',
  5: 'IronOre',
  6: 'SilverOre',
  7: 'GoldOre',
});

// Single ASCII glyph per tile id for the text dump.
export const TILE_GLYPHS = Object.freeze({
  0: '.',
  1: '#',
  2: '"',
  3: '%',
  4: 'c',
  5: 'i',
  6: 's',
  7: 'g',
});

// Base [r, g, b] (0-255) per tile id before light shading.
const BASE_COLORS = Object.freeze({
  0: [135, 206, 235], // Air  -> sky blue (rendered unshaded as background)
  1: [134, 96, 67], // Dirt -> brown
  2: [83, 160, 60], // Grass -> green
  3: [128, 128, 128], // Stone -> gray
  4: [184, 115, 51], // CopperOre -> copper
  5: [120, 124, 134], // IronOre -> cool steel
  6: [205, 210, 220], // SilverOre -> bright silver
  7: [212, 175, 55], // GoldOre -> gold
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
