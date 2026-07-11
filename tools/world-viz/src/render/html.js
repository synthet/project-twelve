// Self-contained interactive HTML view.
//
// The core/ modules are inlined verbatim (import/export stripped) into a single
// classic <script>, so the in-browser generator is the EXACT SAME CODE the CLI
// runs — there is no second JS port to drift. The viewer adds pan/zoom,
// hover-to-inspect, a light heatmap toggle, and live parameter sliders that
// regenerate the world in the browser.

import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const CORE_DIR = path.join(__dirname, '..', 'core');

// Dependency order matters: dependencies before dependents.
const CORE_FILES = ['mathf.js', 'tiles.js', 'hash.js', 'generator.js', 'world.js'];

/** Read core modules and strip ESM import/export so they concatenate cleanly. */
function inlineCore() {
  return CORE_FILES.map((file) => {
    const src = fs.readFileSync(path.join(CORE_DIR, file), 'utf8');
    return src
      .split('\n')
      .filter((line) => !/^\s*import\s.+from\s.+;?\s*$/.test(line))
      // Drop bare re-export statements like `export { floorDiv };`.
      .filter((line) => !/^\s*export\s*\{[^}]*\}\s*;?\s*$/.test(line))
      // Strip the `export ` keyword from declarations.
      .map((line) => line.replace(/^(\s*)export\s+/, '$1'))
      .join('\n');
  }).join('\n\n');
}

/**
 * Build the HTML document.
 * @param {object} opts
 * @param {object} opts.params   generator params {seed,surfaceHeight,...}
 * @param {object} opts.region   { minX, maxX, minY, maxY }
 * @param {Array}  opts.edits    [{worldX,worldY,id,light,fluid,metadata}] (may be empty)
 * @param {number} opts.scale    initial pixels per tile
 * @param {string} opts.title
 */
export function renderHtml({ params, region, edits = [], scale = 6, title = 'world-viz' }) {
  const core = inlineCore();
  const data = JSON.stringify({ params, region, edits, scale });

  return `<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>${escapeHtml(title)}</title>
<style>
  :root { color-scheme: dark; }
  * { box-sizing: border-box; }
  body { margin: 0; font: 13px/1.4 system-ui, sans-serif; background: #14171c; color: #e6e6e6; }
  header { padding: 8px 12px; background: #1c2128; border-bottom: 1px solid #2b313a; }
  header h1 { font-size: 14px; margin: 0 0 6px; }
  .controls { display: flex; flex-wrap: wrap; gap: 12px; align-items: center; }
  .ctrl { display: flex; flex-direction: column; font-size: 11px; min-width: 120px; }
  .ctrl label { color: #9aa4b2; margin-bottom: 2px; }
  .ctrl output { color: #8fd0ff; font-variant-numeric: tabular-nums; }
  .row { display: flex; align-items: center; gap: 6px; }
  button { background: #2b313a; color: #e6e6e6; border: 1px solid #3a424d; border-radius: 4px; padding: 4px 8px; cursor: pointer; }
  button.on { background: #2f6feb; border-color: #2f6feb; }
  #stage { position: relative; }
  canvas { display: block; background: #0c0e12; cursor: crosshair; image-rendering: pixelated; }
  #tip { position: fixed; pointer-events: none; background: rgba(12,14,18,.95); border: 1px solid #3a424d;
         border-radius: 4px; padding: 6px 8px; font-size: 12px; white-space: pre; display: none; z-index: 10; }
  #legend { padding: 6px 12px; color: #9aa4b2; border-top: 1px solid #2b313a; background: #1c2128; }
  .swatch { display: inline-block; width: 11px; height: 11px; margin: 0 3px -1px 10px; border: 1px solid #000; vertical-align: middle; }
  .hint { color: #6b7280; }
</style>
</head>
<body>
<header>
  <h1>world-viz &mdash; offline world debug view <span class="hint">(drag to pan, scroll to zoom)</span></h1>
  <div class="controls" id="controls">
    <div class="ctrl"><label>seed <output id="o-seed"></output></label><input type="range" id="seed" min="0" max="100000" step="1"></div>
    <div class="ctrl"><label>surfaceHeight <output id="o-surfaceHeight"></output></label><input type="range" id="surfaceHeight" min="0" max="128" step="1"></div>
    <div class="ctrl"><label>terrainAmplitude <output id="o-terrainAmplitude"></output></label><input type="range" id="terrainAmplitude" min="0" max="64" step="1"></div>
    <div class="ctrl"><label>terrainFrequency <output id="o-terrainFrequency"></output></label><input type="range" id="terrainFrequency" min="0.005" max="0.3" step="0.005"></div>
    <div class="ctrl"><label>dirtDepth <output id="o-dirtDepth"></output></label><input type="range" id="dirtDepth" min="0" max="32" step="1"></div>
    <div class="row">
      <button id="heatmap" title="Toggle light heatmap">light heatmap</button>
      <button id="reset" title="Reset view and params">reset</button>
    </div>
  </div>
</header>
<div id="stage"><canvas id="cv"></canvas></div>
<div id="tip"></div>
<div id="legend"></div>

<script>
// ---- inlined core modules (identical to the CLI's generator) ----
${core}
</script>

<script>
(function () {
  "use strict";
  const DATA = ${data};
  const cv = document.getElementById('cv');
  const ctx = cv.getContext('2d');
  const tip = document.getElementById('tip');

  const params = Object.assign({}, DATA.params);
  const DEFAULT_PARAMS_COPY = Object.assign({}, DATA.params);

  // Build the absolute-coordinate edit overlay once (independent of params).
  const editMap = new Map();
  for (const e of DATA.edits) {
    editMap.set(worldKey(e.worldX, e.worldY), { id: e.id, light: e.light, fluid: e.fluid, metadata: e.metadata });
  }

  // View: world tile coordinate at canvas centre, plus pixels-per-tile.
  let scale = DATA.scale;
  let centerX = (DATA.region.minX + DATA.region.maxX) / 2;
  let centerY = (DATA.region.minY + DATA.region.maxY) / 2;
  let heatmap = false;
  let world = null;

  function rebuildWorld() {
    const gen = new TerrainGenerator(params);
    world = new World(gen, editMap);
  }

  function resize() {
    cv.width = Math.max(320, window.innerWidth);
    cv.height = Math.max(240, window.innerHeight - cv.getBoundingClientRect().top - 40);
    draw();
  }

  // Convert a canvas pixel to a world tile coordinate (y up).
  function pxToWorld(px, py) {
    const tilesW = cv.width / scale;
    const tilesH = cv.height / scale;
    const left = centerX - tilesW / 2;
    const top = centerY + tilesH / 2; // higher y at the top
    return { x: Math.floor(left + px / scale), y: Math.floor(top - py / scale) };
  }

  function heatColor(light) {
    const t = Math.max(0, Math.min(1, light / 15));
    return [Math.round(255 * t), Math.round(40 + 80 * t), Math.round(255 * (1 - t))];
  }

  function draw() {
    if (!world) rebuildWorld();
    const tilesW = Math.ceil(cv.width / scale) + 2;
    const tilesH = Math.ceil(cv.height / scale) + 2;
    const left = Math.floor(centerX - tilesW / 2);
    const top = Math.ceil(centerY + tilesH / 2);

    const img = ctx.createImageData(cv.width, cv.height);
    const buf = img.data;
    for (let py = 0; py < cv.height; py++) {
      const worldY = Math.floor(top - py / scale);
      for (let px = 0; px < cv.width; px++) {
        const worldX = Math.floor(left + px / scale);
        const tile = world.tileAt(worldX, worldY);
        let rgb;
        if (heatmap && tile.id !== TileId.Air) rgb = heatColor(tile.light);
        else rgb = tileColor(tile.id, tile.light);
        const o = (py * cv.width + px) * 4;
        buf[o] = rgb[0]; buf[o + 1] = rgb[1]; buf[o + 2] = rgb[2]; buf[o + 3] = 255;
      }
    }
    ctx.putImageData(img, 0, 0);
  }

  // ---- interaction ----
  let dragging = false, lastX = 0, lastY = 0;
  cv.addEventListener('mousedown', (e) => { dragging = true; lastX = e.clientX; lastY = e.clientY; });
  window.addEventListener('mouseup', () => { dragging = false; });
  cv.addEventListener('mousemove', (e) => {
    if (dragging) {
      centerX -= (e.clientX - lastX) / scale;
      centerY += (e.clientY - lastY) / scale;
      lastX = e.clientX; lastY = e.clientY;
      draw();
    }
    const r = cv.getBoundingClientRect();
    const w = pxToWorld(e.clientX - r.left, e.clientY - r.top);
    const tile = world.tileAt(w.x, w.y);
    const cx = Math.floor(w.x / CHUNK_SIZE), cy = Math.floor(w.y / CHUNK_SIZE);
    tip.style.display = 'block';
    tip.style.left = (e.clientX + 14) + 'px';
    tip.style.top = (e.clientY + 14) + 'px';
    tip.textContent =
      'world (' + w.x + ', ' + w.y + ')\\n' +
      'chunk (' + cx + ', ' + cy + ')\\n' +
      'tile  ' + TILE_NAMES[tile.id] + ' (id ' + tile.id + ')\\n' +
      'light ' + tile.light + '   fluid ' + tile.fluid +
      (editMap.has(worldKey(w.x, w.y)) ? '\\n[edited]' : '');
  });
  cv.addEventListener('mouseleave', () => { tip.style.display = 'none'; });
  cv.addEventListener('wheel', (e) => {
    e.preventDefault();
    const r = cv.getBoundingClientRect();
    const before = pxToWorld(e.clientX - r.left, e.clientY - r.top);
    scale = Math.max(1, Math.min(40, scale * (e.deltaY < 0 ? 1.15 : 1 / 1.15)));
    const after = pxToWorld(e.clientX - r.left, e.clientY - r.top);
    centerX += before.x - after.x;
    centerY += before.y - after.y;
    draw();
  }, { passive: false });

  // ---- sliders ----
  const FIELDS = ['seed', 'surfaceHeight', 'terrainAmplitude', 'terrainFrequency', 'dirtDepth'];
  function syncOutputs() {
    for (const f of FIELDS) {
      document.getElementById(f).value = params[f];
      document.getElementById('o-' + f).textContent = params[f];
    }
  }
  for (const f of FIELDS) {
    document.getElementById(f).addEventListener('input', (e) => {
      params[f] = f === 'terrainFrequency' ? parseFloat(e.target.value) : parseInt(e.target.value, 10);
      document.getElementById('o-' + f).textContent = params[f];
      rebuildWorld();
      draw();
    });
  }
  document.getElementById('heatmap').addEventListener('click', (e) => {
    heatmap = !heatmap; e.target.classList.toggle('on', heatmap); draw();
  });
  document.getElementById('reset').addEventListener('click', () => {
    Object.assign(params, DEFAULT_PARAMS_COPY);
    scale = DATA.scale;
    centerX = (DATA.region.minX + DATA.region.maxX) / 2;
    centerY = (DATA.region.minY + DATA.region.maxY) / 2;
    syncOutputs(); rebuildWorld(); draw();
  });

  // ---- legend ----
  (function () {
    const ids = [0, 1, 2, 3, 4, 5, 6, 7];
    let html = 'tiles:';
    for (const id of ids) {
      const c = tileColor(id, 15);
      html += '<span class="swatch" style="background:rgb(' + c[0] + ',' + c[1] + ',' + c[2] + ')"></span>' + TILE_NAMES[id];
    }
    document.getElementById('legend').innerHTML = html;
  })();

  syncOutputs();
  rebuildWorld();
  window.addEventListener('resize', resize);
  resize();
})();
</script>
</body>
</html>
`;
}

function escapeHtml(s) {
  return String(s).replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
}
