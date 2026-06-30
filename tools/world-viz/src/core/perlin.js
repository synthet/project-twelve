// Perlin noise targeting UnityEngine.Mathf.PerlinNoise.
//
// IMPORTANT — fidelity caveat:
// Unity's Mathf.PerlinNoise is implemented in native code. This is a faithful
// reimplementation of Ken Perlin's "improved noise" (2002 reference algorithm,
// the canonical 256-entry permutation table) evaluated in 2D with z = 0 and
// remapped to ~[0, 1], which is the basis Unity's generator is built on. It is
// computed with float32 emulation (Math.fround) to track the engine's `float`
// math as closely as possible.
//
// Exact bit-for-bit parity with the engine is NOT asserted by this file — it is
// guaranteed by the Unity-exported golden fixture
// (tools/world-viz/test/fixtures/surface.seed1337.json) and the parity test that
// checks the generator against it. If that test ever reports a mismatch, THIS is
// the single file to adjust (swap in the exact gradient/permutation Unity uses);
// nothing else in the tool depends on the noise internals.

import { fr } from './mathf.js';

// Canonical Ken Perlin (2002) permutation table, doubled to 512 to avoid
// index wrapping in the hash lookups.
const PERM_BASE = [
  151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225, 140, 36,
  103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148, 247, 120, 234, 75, 0,
  26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32, 57, 177, 33, 88, 237, 149, 56,
  87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165, 71, 134, 139, 48, 27, 166, 77,
  146, 158, 231, 83, 111, 229, 122, 60, 211, 133, 230, 220, 105, 92, 41, 55, 46,
  245, 40, 244, 102, 143, 54, 65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132, 187,
  208, 89, 18, 169, 200, 196, 135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173,
  186, 3, 64, 52, 217, 226, 250, 124, 123, 5, 202, 38, 147, 118, 126, 255, 82, 85,
  212, 207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42, 223, 183, 170, 213, 119,
  248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9, 129, 22, 39,
  253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104, 218, 246, 97, 228,
  251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162, 241, 81, 51, 145, 235, 249,
  14, 239, 107, 49, 192, 214, 31, 181, 199, 106, 157, 184, 84, 204, 176, 115, 121,
  50, 45, 127, 4, 150, 254, 138, 236, 205, 93, 222, 114, 67, 29, 24, 72, 243, 141,
  128, 195, 78, 66, 215, 61, 156, 180,
];

const PERM = new Int32Array(512);
for (let i = 0; i < 512; i++) {
  PERM[i] = PERM_BASE[i & 255];
}

function fade(t) {
  return t * t * t * (t * (t * 6 - 15) + 10);
}

// Local interpolation helper. Named distinctly from mathf's `lerp` so the core
// modules can be safely concatenated into one classic <script> for the HTML view.
function plerp(t, a, b) {
  return a + t * (b - a);
}

function grad(hash, x, y, z) {
  const h = hash & 15;
  const u = h < 8 ? x : y;
  const v = h < 4 ? y : h === 12 || h === 14 ? x : z;
  return ((h & 1) === 0 ? u : -u) + ((h & 2) === 0 ? v : -v);
}

/** Improved-noise sample in [-1, 1]-ish, evaluated in 3D. */
function noise3(x, y, z) {
  const X = Math.floor(x) & 255;
  const Y = Math.floor(y) & 255;
  const Z = Math.floor(z) & 255;
  x -= Math.floor(x);
  y -= Math.floor(y);
  z -= Math.floor(z);
  const u = fade(x);
  const v = fade(y);
  const w = fade(z);
  const A = PERM[X] + Y;
  const AA = PERM[A] + Z;
  const AB = PERM[A + 1] + Z;
  const B = PERM[X + 1] + Y;
  const BA = PERM[B] + Z;
  const BB = PERM[B + 1] + Z;
  return plerp(
    w,
    plerp(
      v,
      plerp(u, grad(PERM[AA], x, y, z), grad(PERM[BA], x - 1, y, z)),
      plerp(u, grad(PERM[AB], x, y - 1, z), grad(PERM[BB], x - 1, y - 1, z)),
    ),
    plerp(
      v,
      plerp(u, grad(PERM[AA + 1], x, y, z - 1), grad(PERM[BA + 1], x - 1, y, z - 1)),
      plerp(
        u,
        grad(PERM[AB + 1], x, y - 1, z - 1),
        grad(PERM[BB + 1], x - 1, y - 1, z - 1),
      ),
    ),
  );
}

/**
 * Reproduces UnityEngine.Mathf.PerlinNoise(x, y): a 2D sample remapped to ~[0, 1].
 * Inputs are float32-emulated to match the engine's `float` argument widths.
 */
export function perlinNoise(x, y) {
  const n = noise3(fr(x), fr(y), 0);
  return fr(n * 0.5 + 0.5);
}
