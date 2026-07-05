import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { loadTileSpaceFromFile } from './src/io/tileSpace.js';
import { renderAutotilePng } from './src/render/autotilePng.js';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const snippetPath = path.join(__dirname, 'test', 'fixtures', 'snippets', 'dirt-stone-reentrant-west.json');
const space = loadTileSpaceFromFile(snippetPath);
const assetsRoot = path.join(__dirname, '..', '..', 'Assets', '_Licensed', 'PixelFantasy', 'PixelTileEngine', 'Tiles');

const pngTest = renderAutotilePng(space, { assetsRoot, scale: 32, flatLight: true });
fs.writeFileSync(path.join(__dirname, 'out-slope', 'test-output.png'), pngTest);

console.log('Done rendering test-output.png');
