// Minimal, dependency-free PNG encoder (truecolor RGB, 8-bit) built on Node's
// built-in zlib. Sufficient for rasterising debug world views.

import zlib from 'node:zlib';

// CRC-32 table (PNG chunk checksums).
const CRC_TABLE = (() => {
  const table = new Int32Array(256);
  for (let n = 0; n < 256; n++) {
    let c = n;
    for (let k = 0; k < 8; k++) {
      c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
    }
    table[n] = c;
  }
  return table;
})();

function crc32(buf) {
  let c = 0xffffffff;
  for (let i = 0; i < buf.length; i++) {
    c = CRC_TABLE[(c ^ buf[i]) & 0xff] ^ (c >>> 8);
  }
  return (c ^ 0xffffffff) >>> 0;
}

function chunk(type, data) {
  const typeBuf = Buffer.from(type, 'ascii');
  const lenBuf = Buffer.alloc(4);
  lenBuf.writeUInt32BE(data.length, 0);
  const crcBuf = Buffer.alloc(4);
  crcBuf.writeUInt32BE(crc32(Buffer.concat([typeBuf, data])), 0);
  return Buffer.concat([lenBuf, typeBuf, data, crcBuf]);
}

/**
 * Encode an RGB pixel buffer to a PNG Buffer.
 * @param {number} width
 * @param {number} height
 * @param {Uint8Array|Buffer} rgb  width*height*3 bytes, row-major top-to-bottom
 * @returns {Buffer}
 */
export function encodePng(width, height, rgb) {
  const signature = Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]);

  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(width, 0);
  ihdr.writeUInt32BE(height, 4);
  ihdr[8] = 8; // bit depth
  ihdr[9] = 2; // color type 2 = truecolor RGB
  ihdr[10] = 0; // compression
  ihdr[11] = 0; // filter
  ihdr[12] = 0; // interlace

  // Prepend the per-scanline filter byte (0 = none).
  const stride = width * 3;
  const raw = Buffer.alloc((stride + 1) * height);
  for (let y = 0; y < height; y++) {
    raw[y * (stride + 1)] = 0;
    rgb.subarray
      ? Buffer.from(rgb.subarray(y * stride, y * stride + stride)).copy(
          raw,
          y * (stride + 1) + 1,
        )
      : Buffer.from(rgb.slice(y * stride, y * stride + stride)).copy(
          raw,
          y * (stride + 1) + 1,
        );
  }

  const idat = zlib.deflateSync(raw, { level: 9 });

  return Buffer.concat([
    signature,
    chunk('IHDR', ihdr),
    chunk('IDAT', idat),
    chunk('IEND', Buffer.alloc(0)),
  ]);
}

/**
 * Rasterise a sampled region to a PNG Buffer at the given integer pixel scale.
 * @param {object} region  from world.sampleRegion (tiles ordered top-down)
 * @param {(id:number, light:number)=>[number,number,number]} colorFn
 * @param {number} scale  pixels per tile (>=1)
 */
export function renderPng(region, colorFn, scale = 4) {
  const s = Math.max(1, scale | 0);
  const pxW = region.width * s;
  const pxH = region.height * s;
  const rgb = Buffer.alloc(pxW * pxH * 3);

  for (let row = 0; row < region.height; row++) {
    for (let col = 0; col < region.width; col++) {
      const tile = region.tiles[row][col];
      const [r, g, b] = colorFn(tile.id, tile.light);
      for (let dy = 0; dy < s; dy++) {
        const py = row * s + dy;
        let idx = (py * pxW + col * s) * 3;
        for (let dx = 0; dx < s; dx++) {
          rgb[idx++] = r;
          rgb[idx++] = g;
          rgb[idx++] = b;
        }
      }
    }
  }
  return encodePng(pxW, pxH, rgb);
}
