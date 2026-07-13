#!/usr/bin/env node
const fs = require('fs');
const path = require('path');
const { spawn } = require('child_process');

const projectRoot = path.resolve(__dirname, '..');
const envPath = path.join(projectRoot, '.env');

let apiKey = process.env.PIXELLAB_API_KEY;

if (!apiKey && fs.existsSync(envPath)) {
  const envContent = fs.readFileSync(envPath, 'utf8');
  const match = envContent.match(/^PIXELLAB_API_KEY=(.*)$/m);
  if (match) {
    apiKey = match[1].trim().replace(/^['"]|['"]$/g, '');
  }
}

if (!apiKey) {
  console.error("Error: PIXELLAB_API_KEY not found in environment or .env file.");
  process.exit(1);
}

const isWin = process.platform === 'win32';
const npxCmd = isWin ? 'npx.cmd' : 'npx';
const childEnv = {
  ...process.env,
  PIXELLAB_AUTH_HEADER: `Bearer ${apiKey}`,
};

const child = spawn(
  npxCmd,
  [
    '-y',
    'mcp-remote',
    'https://api.pixellab.ai/mcp',
    '--header',
    'Authorization:${PIXELLAB_AUTH_HEADER}',
  ],
  { env: childEnv, stdio: 'inherit', shell: isWin }
);

child.on('error', (error) => {
  console.error(`Error: failed to start PixelLab MCP bridge: ${error.message}`);
  process.exit(1);
});

child.on('exit', (code) => {
  process.exit(code || 0);
});
