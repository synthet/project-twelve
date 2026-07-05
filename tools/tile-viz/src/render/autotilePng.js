// Composite autotile layers to RGBA then encode PNG via world-viz encoder.

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { encodePng } from '../../../world-viz/src/render/png.js';
import { tileColor, lightBrightness, TileId } from '../../../world-viz/src/core/tiles.js';
import { buildAutotileReport } from '../report/autotileJson.js';
import { getTile, spaceToSampledGrid } from '../io/tileSpace.js';
import { blitSprite, loadTilesetsFromManifest, spriteRect } from './spriteSheet.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const DEFAULT_MANIFEST = path.join(__dirname, '..', '..', 'data', 'tileset-manifest.json');

/**
 * @param {object} space
 * @param {object} options
 * @returns {Buffer}
 */
export function renderAutotilePng(space, options = {}) {
  const scale = options.scale ?? 16;
  const manifestPath = options.manifest ?? DEFAULT_MANIFEST;
  const assetsRoot = options.assetsRoot;
  if (!assetsRoot) {
    throw new Error('render requires --assets-root pointing at licensed tile PNG folder');
  }

  const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));
  const tilesets = loadTilesetsFromManifest(manifest, assetsRoot);
  const report = buildAutotileReport(space, { manifest, includeAir: true });
  const grid = spaceToSampledGrid(space);
  const pixelW = grid.width * scale;
  const pixelH = grid.height * scale;
  const rgba = new Uint8Array(pixelW * pixelH * 4);

  const sky = tileColor(TileId.Air, 15);
  for (let i = 0; i < pixelW * pixelH; i++) {
    const o = i * 4;
    rgba[o] = sky[0];
    rgba[o + 1] = sky[1];
    rgba[o + 2] = sky[2];
    rgba[o + 3] = 255;
  }

  const reportByKey = new Map(report.tiles.map((t) => [`${t.x},${t.y}`, t]));

  for (let row = 0; row < grid.height; row++) {
    const worldY = grid.maxY - row;
    for (let col = 0; col < grid.width; col++) {
      const worldX = grid.minX + col;
      const tile = getTile(space, worldX, worldY);
      if (tile.id === TileId.Air) {
        continue;
      }

      const info = reportByKey.get(`${worldX},${worldY}`);
      const px = col * scale;
      const py = row * scale;
      const brightness = options.flatLight ? 1 : lightBrightness(tile.light);
      const tint = [
        Math.round(255 * brightness),
        Math.round(255 * brightness),
        Math.round(255 * brightness),
      ];

      if (info?.autotile?.ground) {
        const { tileset: name, spriteId, flipX } = info.autotile.ground;
        const ts = tilesets.get(name);
        if (ts && spriteId != null) {
          const rect = spriteRect(spriteId, ts.layout, ts.png);
          blitSprite(ts.png, rect, rgba, pixelW, px, py, flipX, tint, options.extrude !== false, scale);
        }
      }

      if (!options.noCover && info?.autotile?.cover?.rendered) {
        const { tileset: name, spriteId, flipX } = info.autotile.cover;
        const ts = tilesets.get(name);
        if (ts && spriteId != null) {
          const rect = spriteRect(spriteId, ts.layout, ts.png);
          blitSprite(ts.png, rect, rgba, pixelW, px, py, flipX, [255, 255, 255], options.extrude !== false, scale);
        }
      }
    }
  }

  const rgb = Buffer.alloc(pixelW * pixelH * 3);
  for (let i = 0; i < pixelW * pixelH; i++) {
    rgb[i * 3] = rgba[i * 4];
    rgb[i * 3 + 1] = rgba[i * 4 + 1];
    rgb[i * 3 + 2] = rgba[i * 4 + 2];
  }
  return encodePng(pixelW, pixelH, rgb);
}
