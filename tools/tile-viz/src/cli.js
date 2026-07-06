#!/usr/bin/env node
// tile-viz CLI: resolve and render autotile tile spaces from JSON without Unity.

import fs from 'node:fs';
import path from 'node:path';

import { loadTileSpaceFromFile, exportMcpToSpace } from './io/tileSpace.js';
import { buildAutotileReport, assertExpectations } from './report/autotileJson.js';
import { listVisualOverrides, renderAutotilePng } from './render/autotilePng.js';

const USAGE = `tile-viz — offline autotile resolver and sprite compositor

Usage:
  tile-viz resolve --space <file.json> [--json <out|-]
  tile-viz render --space <file.json> --assets-root <dir> --png <out> [options]
  tile-viz test-fixture --space <file.json>
  tile-viz import-mcp --file <mcp.json> --out <space.json>
  tile-viz list-visual-overrides --visual-overrides <file>

Options:
  --scale <int>           pixels per tile for PNG (default 16)
  --manifest <file>       tileset manifest (default data/tileset-manifest.json)
  --visual-overrides <file> sidecar JSON with per-cell ground/cover sprite overrides
  --assets-root <dir>     licensed Tiles root (Ground/, Cover/)
  --flat-light            skip underground light dimming in PNG
  --no-cover              skip grass cover layer in PNG
  --no-extrude            disable 1px sprite bleed when compositing PNG
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

function main() {
  const args = parseArgs(process.argv.slice(2));
  const cmd = args._[0];
  if (args.help || !cmd) {
    process.stdout.write(USAGE);
    process.exit(args.help ? 0 : 1);
  }

  switch (cmd) {
    case 'resolve':
      cmdResolve(args);
      break;
    case 'render':
      cmdRender(args);
      break;
    case 'test-fixture':
      cmdTestFixture(args);
      break;
    case 'import-mcp':
      cmdImportMcp(args);
      break;
    case 'list-visual-overrides':
      cmdListVisualOverrides(args);
      break;
    default:
      fail(`unknown command "${cmd}"`);
  }
}

function requireSpace(args) {
  if (!args.space || args.space === true) {
    fail(`${args._[0]} requires --space <file.json>`);
  }
  return loadTileSpaceFromFile(args.space);
}

function cmdResolve(args) {
  const space = requireSpace(args);
  const report = buildAutotileReport(space, {
    manifest: args.manifest ? JSON.parse(fs.readFileSync(args.manifest, 'utf8')) : undefined,
  });
  const text = `${JSON.stringify(report, null, 2)}\n`;
  if (args.json === '-' || args.json === true || !args.json) {
    process.stdout.write(text);
  } else {
    fs.mkdirSync(path.dirname(path.resolve(args.json)), { recursive: true });
    fs.writeFileSync(args.json, text);
    process.stderr.write(`wrote ${args.json}\n`);
  }
}

function loadVisualOverrides(args) {
  if (!args['visual-overrides']) {
    return undefined;
  }
  if (args['visual-overrides'] === true) {
    fail(`${args._[0]} requires --visual-overrides <file>`);
  }
  return JSON.parse(fs.readFileSync(args['visual-overrides'], 'utf8'));
}

function cmdRender(args) {
  const space = requireSpace(args);
  if (!args['assets-root'] || args['assets-root'] === true) {
    fail('render requires --assets-root');
  }
  if (!args.png || args.png === true) {
    fail('render requires --png <file>');
  }
  const scale = args.scale ? Number(args.scale) | 0 : 16;
  const visualOverrides = loadVisualOverrides(args);
  const buf = renderAutotilePng(space, {
    assetsRoot: args['assets-root'],
    manifest: args.manifest,
    scale,
    flatLight: args['flat-light'] === true,
    noCover: args['no-cover'] === true,
    extrude: args['no-extrude'] !== true,
    visualOverrides,
  });
  fs.mkdirSync(path.dirname(path.resolve(args.png)), { recursive: true });
  fs.writeFileSync(args.png, buf);
  process.stderr.write(`wrote ${args.png}\n`);
}

function cmdTestFixture(args) {
  const space = requireSpace(args);
  const report = buildAutotileReport(space);
  const errors = assertExpectations(space, report);
  if (errors.length) {
    for (const e of errors) {
      process.stderr.write(`${e}\n`);
    }
    process.exit(1);
  }
  process.stderr.write(`OK ${space.name ?? args.space}\n`);
}

function cmdImportMcp(args) {
  if (!args.file || args.file === true) {
    fail('import-mcp requires --file');
  }
  if (!args.out || args.out === true) {
    fail('import-mcp requires --out');
  }
  exportMcpToSpace(args.file, args.out);
  process.stderr.write(`wrote ${args.out}\n`);
}

function cmdListVisualOverrides(args) {
  if (!args['visual-overrides']) {
    fail('list-visual-overrides requires --visual-overrides <file>');
  }
  const visualOverrides = loadVisualOverrides(args);
  const entries = listVisualOverrides(visualOverrides);
  for (const entry of entries) {
    process.stdout.write(`${entry.x},${entry.y}: ${entry.layers.join('; ')}\n`);
  }
}

main();
