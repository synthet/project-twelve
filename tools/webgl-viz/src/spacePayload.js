import { loadTileSpaceFromFile, spaceToSampledGrid } from '../../tile-viz/src/io/tileSpace.js';
import { TILE_NAMES, tileColor } from '../../world-viz/src/core/tiles.js';
import { floorDiv } from '../../world-viz/src/core/mathf.js';
import { CHUNK_SIZE } from '../../world-viz/src/core/generator.js';

export function buildViewerPayload(spacePath) {
  const space = loadTileSpaceFromFile(spacePath);
  const region = spaceToSampledGrid(space);
  const tiles = [];
  for (let row = 0; row < region.height; row++) {
    const y = region.maxY - row;
    for (let col = 0; col < region.width; col++) {
      const x = region.minX + col;
      const tile = region.tiles[row][col];
      const [r, g, b] = tileColor(tile.id, tile.light ?? 15);
      tiles.push({
        x,
        y,
        id: tile.id,
        name: TILE_NAMES[tile.id] ?? `Unknown(${tile.id})`,
        light: tile.light ?? 0,
        fluid: tile.fluid ?? 0,
        metadata: tile.metadata ?? 0,
        solid: tile.id !== 0,
        chunkX: floorDiv(x, CHUNK_SIZE),
        chunkY: floorDiv(y, CHUNK_SIZE),
        localX: ((x % CHUNK_SIZE) + CHUNK_SIZE) % CHUNK_SIZE,
        localY: ((y % CHUNK_SIZE) + CHUNK_SIZE) % CHUNK_SIZE,
        color: [r, g, b],
      });
    }
  }
  return {
    format: 'project-twelve/webgl-viz/v1',
    source: spacePath,
    name: space.name,
    kind: space.kind,
    bounds: { xMin: region.minX, xMax: region.maxX, yMin: region.minY, yMax: region.maxY },
    width: region.width,
    height: region.height,
    generatedAt: new Date(0).toISOString(),
    tiles,
  };
}
