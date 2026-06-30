#!/usr/bin/env node
// world-viz CLI: render a ProjectTwelve world to PNG / interactive HTML / ASCII
// from a random seed or a JSON save game, without the Unity engine.

import fs from 'node:fs';
import path from 'node:path';

import { TerrainGenerator, DEFAULT_PARAMS, CHUNK_SIZE } from './core/generator.js';
import { World, editsFromChunks, sampleRegion, editedChunksBounds } from './core/world.js';
import { tileColor } from './core/tiles.js';
import { parseSaveText } from './io/saveLoad.js';
import { renderAscii } from './render/ascii.js';
import { renderPng } from './render/png.js';
import { renderHtml } from './render/html.js';

const USAGE = `world-viz — offline world visualization / debug tool

Usage:
  world-viz generate [options]
  world-viz load --save <file.json> [options]

Generation params (generate; override save seed on load):
  --seed <int>            (default ${DEFAULT_PARAMS.seed})
  --surface <int>         surface height        (default ${DEFAULT_PARAMS.surfaceHeight})
  --amplitude <int>       terrain amplitude     (default ${DEFAULT_PARAMS.terrainAmplitude})
  --frequency <float>     terrain frequency     (default ${DEFAULT_PARAMS.terrainFrequency})
  --dirt-depth <int>      dirt layer depth      (default ${DEFAULT_PARAMS.dirtDepth})

Region (world tile coords, inclusive):
  --min-x <int> --max-x <int> --min-y <int> --max-y <int>
    generate default: x[-64..64] y[0..48]
    load default:     bounding box of edited chunks (+1 chunk padding)

Output (at least one):
  --png <file>            write a PNG raster
  --html <file>           write a self-contained interactive HTML page
  --ascii <file|->        write a text dump ('-' prints to stdout)
  --scale <int>           pixels per tile for PNG / initial HTML zoom (default 4)

  -h, --help
`;

function fail(msg) {
  process.stderr.write(`error: ${msg}\n\n${USAGE}`);
  process.exit(1);
}

function parseArgs(argv) {
  const out = { _: [] };
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a === '-h' || a === '--help') {
      out.help = true;
    } else if (a.startsWith('--')) {
      const key = a.slice(2);
      const next = argv[i + 1];
      if (next === undefined || next.startsWith('--')) {
        out[key] = true;
      } else {
        out[key] = next;
        i++;
      }
    } else {
      out._.push(a);
    }
  }
  return out;
}

function num(v, name) {
  const n = Number(v);
  if (!Number.isFinite(n)) fail(`--${name} must be a number (got "${v}")`);
  return n;
}

function main() {
  const args = parseArgs(process.argv.slice(2));
  const cmd = args._[0];
  if (args.help || !cmd) {
    process.stdout.write(USAGE);
    process.exit(args.help ? 0 : 1);
  }
  if (cmd !== 'generate' && cmd !== 'load') {
    fail(`unknown command "${cmd}" (expected "generate" or "load")`);
  }

  // Resolve generation params (+ save seed/edits for load).
  const params = { ...DEFAULT_PARAMS };
  let edits = new Map();
  let editChunks = [];

  if (cmd === 'load') {
    if (!args.save || args.save === true) fail('load requires --save <file.json>');
    let text;
    try {
      text = fs.readFileSync(args.save, 'utf8');
    } catch (err) {
      fail(`cannot read save file: ${err.message}`);
    }
    const save = parseSaveText(text);
    params.seed = save.seed;
    editChunks = save.chunks;
    edits = editsFromChunks(save.chunks);
  }

  if (args.seed !== undefined) params.seed = num(args.seed, 'seed');
  if (args.surface !== undefined) params.surfaceHeight = num(args.surface, 'surface');
  if (args.amplitude !== undefined) params.terrainAmplitude = num(args.amplitude, 'amplitude');
  if (args.frequency !== undefined) params.terrainFrequency = num(args.frequency, 'frequency');
  if (args['dirt-depth'] !== undefined) params.dirtDepth = num(args['dirt-depth'], 'dirt-depth');

  // Resolve region.
  let region = defaultRegion(cmd, editChunks);
  if (args['min-x'] !== undefined) region.minX = num(args['min-x'], 'min-x') | 0;
  if (args['max-x'] !== undefined) region.maxX = num(args['max-x'], 'max-x') | 0;
  if (args['min-y'] !== undefined) region.minY = num(args['min-y'], 'min-y') | 0;
  if (args['max-y'] !== undefined) region.maxY = num(args['max-y'], 'max-y') | 0;
  if (region.maxX < region.minX || region.maxY < region.minY) {
    fail('region max must be >= min on each axis');
  }

  const scale = args.scale !== undefined ? Math.max(1, num(args.scale, 'scale') | 0) : 4;

  const wantPng = args.png && args.png !== true;
  const wantHtml = args.html && args.html !== true;
  const wantAscii = args.ascii !== undefined;
  if (!wantPng && !wantHtml && !wantAscii) {
    fail('specify at least one output: --png, --html, or --ascii');
  }

  // Build world + sampled grid.
  const world = new World(new TerrainGenerator(params), edits);
  const sampled = sampleRegion(world, region.minX, region.maxX, region.minY, region.maxY);

  const summary = [];
  summary.push(
    `${cmd}: seed=${params.seed} surface=${params.surfaceHeight} amp=${params.terrainAmplitude} ` +
      `freq=${params.terrainFrequency} dirtDepth=${params.dirtDepth}`,
  );
  summary.push(
    `region x[${region.minX}..${region.maxX}] y[${region.minY}..${region.maxY}] ` +
      `(${sampled.width}x${sampled.height} tiles)` +
      (editChunks.length ? `  edits: ${edits.size} tiles in ${editChunks.length} chunk(s)` : ''),
  );

  if (wantAscii) {
    const text = renderAscii(sampled, world);
    if (args.ascii === '-' || args.ascii === true) {
      process.stdout.write(text);
    } else {
      writeFile(args.ascii, text);
      summary.push(`wrote ${args.ascii}`);
    }
  }
  if (wantPng) {
    const buf = renderPng(sampled, tileColor, scale);
    writeFile(args.png, buf);
    summary.push(`wrote ${args.png} (${sampled.width * scale}x${sampled.height * scale}px)`);
  }
  if (wantHtml) {
    const htmlEdits = [];
    for (const c of editChunks) {
      for (const e of c.edits) {
        htmlEdits.push({
          worldX: c.x * CHUNK_SIZE + e.localX,
          worldY: c.y * CHUNK_SIZE + e.localY,
          id: e.tile.id,
          light: e.tile.light,
          fluid: e.tile.fluid,
          metadata: e.tile.metadata,
        });
      }
    }
    const html = renderHtml({ params, region, edits: htmlEdits, scale: Math.max(scale, 4), title: `world-viz ${cmd}` });
    writeFile(args.html, html);
    summary.push(`wrote ${args.html}`);
  }

  process.stderr.write(summary.join('\n') + '\n');
}

function defaultRegion(cmd, editChunks) {
  if (cmd === 'load') {
    const b = editedChunksBounds(editChunks);
    if (b) {
      return {
        minX: b.minX - CHUNK_SIZE,
        maxX: b.maxX + CHUNK_SIZE,
        minY: b.minY - CHUNK_SIZE,
        maxY: b.maxY + CHUNK_SIZE,
      };
    }
  }
  return { minX: -64, maxX: 64, minY: 0, maxY: 48 };
}

function writeFile(file, data) {
  const dir = path.dirname(path.resolve(file));
  fs.mkdirSync(dir, { recursive: true });
  fs.writeFileSync(file, data);
}

main();
