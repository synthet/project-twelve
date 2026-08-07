#!/usr/bin/env node
/**
 * Stdio launcher for project-twelve-graphify-mcp.
 * Requires a local graph: run `graphify extract . --code-only` first.
 * Prefers `uv tool run --from graphifyy[mcp]` so Windows PATH quirks do not break clients.
 */
const fs = require('fs');
const path = require('path');
const { spawn, spawnSync } = require('child_process');

const projectRoot = path.resolve(__dirname, '..');
const graphPath = path.join(projectRoot, 'graphify-out', 'graph.json');
const isWin = process.platform === 'win32';

if (!fs.existsSync(graphPath)) {
  console.error(
    'Error: graphify-out/graph.json not found.\n' +
      'Build a local graph first (no API key):\n' +
      '  uv tool install "graphifyy[mcp]"\n' +
      '  graphify extract . --code-only\n' +
      'Then reload the MCP client.'
  );
  process.exit(1);
}

function commandExists(cmd) {
  const probe = isWin ? 'where' : 'which';
  const result = spawnSync(probe, [cmd], {
    encoding: 'utf8',
    shell: isWin,
    windowsHide: true,
  });
  return result.status === 0;
}

function spawnServe(command, args) {
  const child = spawn(command, args, {
    cwd: projectRoot,
    env: process.env,
    stdio: 'inherit',
    shell: isWin,
    windowsHide: true,
  });

  child.on('error', (error) => {
    console.error(`Error: failed to start Graphify MCP: ${error.message}`);
    process.exit(1);
  });

  child.on('exit', (code) => {
    process.exit(code || 0);
  });
}

// Prefer uv-managed package so the serve module matches graphifyy[mcp].
if (commandExists('uv')) {
  const uvCmd = isWin ? 'uv.exe' : 'uv';
  spawnServe(uvCmd, [
    'tool',
    'run',
    '--from',
    'graphifyy[mcp]',
    'python',
    '-m',
    'graphify.serve',
    graphPath,
  ]);
} else if (commandExists('python')) {
  spawnServe(isWin ? 'python.exe' : 'python', ['-m', 'graphify.serve', graphPath]);
} else if (commandExists('python3')) {
  spawnServe('python3', ['-m', 'graphify.serve', graphPath]);
} else {
  console.error(
    'Error: neither `uv` nor `python` found on PATH.\n' +
      'Install uv, then: uv tool install "graphifyy[mcp]"'
  );
  process.exit(1);
}
