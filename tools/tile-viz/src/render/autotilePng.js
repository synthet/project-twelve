// Composite autotile layers to RGBA then encode PNG via world-viz encoder.

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { encodePng } from '../../../world-viz/src/render/png.js';
import { tileColor, lightBrightness, TileId } from '../../../world-viz/src/core/tiles.js';
import { buildAutotileReport } from '../report/autotileJson.js';
import { getTile, spaceToSampledGrid } from '../io/tileSpace.js';
import { blitSprite, loadTilesetsFromManifest, spriteRect } from './spriteSheet.js';
import { drawCellLabel, formatSpriteLabel } from './cellLabel.js';

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
  applyVisualOverrides(report, options.visualOverrides);
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

      if (options.annotateGround && info?.autotile?.ground?.spriteId != null) {
        const label = formatSpriteLabel(info.autotile.ground.spriteId, info.autotile.ground.flipX);
        const labelScale = Math.max(2, Math.floor(scale / 16));
        drawCellLabel(rgba, pixelW, px + 2, py + 2, label, [255, 255, 80], labelScale);
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


function normalizeVisualOverrideLayer(layer) {
  if (!layer || typeof layer !== 'object') {
    return null;
  }
  const out = {};
  for (const key of ['tileset', 'spriteId', 'flipX', 'rendered']) {
    if (layer[key] !== undefined) {
      out[key] = layer[key];
    }
  }
  if (out.spriteId !== undefined) {
    out.spriteId = Number(out.spriteId);
  }
  if (out.flipX !== undefined) {
    out.flipX = Boolean(out.flipX);
  }
  if (out.rendered !== undefined) {
    out.rendered = Boolean(out.rendered);
  }
  return Object.keys(out).length ? out : null;
}

function visualOverrideEntries(sidecar) {
  if (!sidecar) {
    return [];
  }
  const source = Array.isArray(sidecar) ? sidecar : (sidecar.overrides ?? sidecar.tiles ?? sidecar.visualOverrides ?? sidecar);
  if (Array.isArray(source)) {
    return source;
  }
  if (source && typeof source === 'object') {
    return Object.entries(source).map(([key, value]) => {
      const [x, y] = key.split(',').map((v) => Number(v.trim()));
      return { x, y, ...value };
    });
  }
  return [];
}

/**
 * Apply sidecar visual overrides to an autotile report after normal resolver output.
 * Supported shapes:
 *   { "overrides": [{ "x": 0, "y": 1, "ground": { "tileset": "Humus", "spriteId": 4, "flipX": false } }] }
 *   { "0,1": { "cover": { "tileset": "GrassA", "spriteId": 2, "flipX": true, "rendered": true } } }
 * @param {object} report
 * @param {object|Array<object>|undefined} sidecar
 * @returns {object}
 */
export function applyVisualOverrides(report, sidecar) {
  const entries = visualOverrideEntries(sidecar);
  if (!entries.length) {
    return report;
  }

  const reportByKey = new Map(report.tiles.map((t) => [`${t.x},${t.y}`, t]));
  for (const entry of entries) {
    const x = Number(entry.x);
    const y = Number(entry.y);
    if (!Number.isFinite(x) || !Number.isFinite(y)) {
      continue;
    }
    const tile = reportByKey.get(`${x},${y}`);
    if (!tile) {
      continue;
    }
    tile.autotile ??= {};
    for (const layerName of ['ground', 'cover']) {
      const layer = normalizeVisualOverrideLayer(entry[layerName]);
      if (!layer) {
        continue;
      }
      tile.autotile[layerName] = {
        ...(tile.autotile[layerName] ?? {}),
        ...layer,
        visualOverride: true,
      };
    }
  }
  return report;
}

export function listVisualOverrides(sidecar) {
  return visualOverrideEntries(sidecar)
    .map((entry) => {
      const layers = [];
      for (const layerName of ['ground', 'cover']) {
        const layer = normalizeVisualOverrideLayer(entry[layerName]);
        if (layer) {
          const bits = [`${layerName}`];
          if (layer.tileset !== undefined) bits.push(`tileset=${layer.tileset}`);
          if (layer.spriteId !== undefined) bits.push(`spriteId=${layer.spriteId}`);
          if (layer.flipX !== undefined) bits.push(`flipX=${layer.flipX}`);
          if (layer.rendered !== undefined) bits.push(`rendered=${layer.rendered}`);
          layers.push(bits.join(' '));
        }
      }
      return { x: Number(entry.x), y: Number(entry.y), layers };
    })
    .filter((entry) => Number.isFinite(entry.x) && Number.isFinite(entry.y) && entry.layers.length);
}
