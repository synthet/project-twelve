#!/usr/bin/env node
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { buildViewerPayload } from './spacePayload.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(__dirname, '..');
const repoRoot = path.resolve(root, '..', '..');

function argValue(name, fallback) {
  const index = process.argv.indexOf(name);
  return index >= 0 ? process.argv[index + 1] : fallback;
}

export function buildViewer({ space = '../tile-viz/test/fixtures/snippets/grass-cover-middle.json', outDir = 'dist' } = {}) {
  const input = path.isAbsolute(space) ? space : path.resolve(process.cwd(), space);
  const resolvedOutDir = path.isAbsolute(outDir) ? outDir : path.resolve(process.cwd(), outDir);
  const payload = buildViewerPayload(input);
  const appJs = fs.readFileSync(path.join(__dirname, 'viewer.js'), 'utf8');
  const html = `<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>ProjectTwelve WebGL Viz</title>
<style>
:root{color-scheme:dark;--bg:#111827;--panel:#1f2937;--text:#e5e7eb;--muted:#9ca3af;--accent:#38bdf8}*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text);font:14px/1.4 system-ui,sans-serif}header{padding:12px 16px;border-bottom:1px solid #374151;background:#0f172a}h1{margin:0;font-size:18px}.layout{display:grid;grid-template-columns:minmax(0,1fr) 320px;height:calc(100vh - 51px)}canvas{display:block;width:100%;height:100%;background:#87ceeb}.side{padding:12px;background:var(--panel);overflow:auto;border-left:1px solid #374151}.hint,.muted{color:var(--muted)}button,label{display:block;margin:8px 0}button{background:#0ea5e9;color:#00111f;border:0;border-radius:6px;padding:8px 10px;cursor:pointer}input[type=file]{width:100%}pre{white-space:pre-wrap;background:#111827;border:1px solid #374151;border-radius:6px;padding:8px}.swatches{display:grid;grid-template-columns:repeat(4,1fr);gap:6px}.swatch{height:18px;border:1px solid #0008}</style>
</head>
<body>
<header><h1>ProjectTwelve WebGL Viz <span class="hint">drag to pan · wheel to zoom · click a tile to inspect</span></h1></header>
<div class="layout"><canvas id="view"></canvas><aside class="side">
<strong id="title"></strong><p class="muted" id="meta"></p>
<label><input id="chunkToggle" type="checkbox" checked> Chunk grid</label>
<label><input id="solidToggle" type="checkbox"> Solid overlay</label>
<label><input id="lightToggle" type="checkbox"> Light heatmap</label>
<input id="fileInput" type="file" accept="application/json,.json"><button id="exportBtn">Export evidence JSON</button>
<h2>Inspector</h2><pre id="inspector">Click a tile.</pre>
<h2>Legend</h2><div class="swatches" id="legend"></div>
</aside></div>
<script type="application/json" id="initial-payload">${JSON.stringify(payload).replace(/</g, '\\u003c')}</script>
<script>${appJs}</script>
</body>
</html>
`;
  fs.mkdirSync(resolvedOutDir, { recursive: true });
  fs.writeFileSync(path.join(resolvedOutDir, 'index.html'), html);
  fs.writeFileSync(path.join(resolvedOutDir, 'payload.json'), `${JSON.stringify(payload, null, 2)}\n`);
  return { htmlPath: path.join(resolvedOutDir, 'index.html'), payloadPath: path.join(resolvedOutDir, 'payload.json'), payload };
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  const result = buildViewer({
    space: argValue('--space', '../tile-viz/test/fixtures/snippets/grass-cover-middle.json'),
    outDir: argValue('--out-dir', 'dist'),
  });
  console.log(`Built WebGL viewer: ${path.relative(repoRoot, result.htmlPath)}`);
}
