import fs from 'fs';
import { loadTileSpaceFromFile, getTile } from './src/io/tileSpace.js';
import { buildConnectivityGroundMask } from './src/visual/maskBuilder.js';
import { DEFAULT_CATALOG, sharesGroundAutotileGroup } from './src/visual/catalog.js';

const space = loadTileSpaceFromFile('./test/fixtures/captures/sandbox-scene-mountain.json');
const catalog = DEFAULT_CATALOG;

const x = -113;
const y = 26;
const tile = getTile(space, x, y);

const sharesGround = (nx, ny) => {
  const n = getTile(space, nx, ny);
  return sharesGroundAutotileGroup(tile.id, n.id, catalog);
};
const isSolid = (nx, ny) => getTile(space, nx, ny).id !== 0;

const rawMask = buildConnectivityGroundMask(sharesGround, x, y, isSolid);
console.log('rawMask array raw:', rawMask);
console.log('rawMask[0][0]:', rawMask[0][0]);
console.log('rawMask[0][1]:', rawMask[0][1]);
console.log('rawMask[0][2]:', rawMask[0][2]);
console.log('rawMask[1][0]:', rawMask[1][0]);
console.log('rawMask[1][1]:', rawMask[1][1]);
console.log('rawMask[1][2]:', rawMask[1][2]);
console.log('rawMask[2][0]:', rawMask[2][0]);
console.log('rawMask[2][1]:', rawMask[2][1]);
console.log('rawMask[2][2]:', rawMask[2][2]);
