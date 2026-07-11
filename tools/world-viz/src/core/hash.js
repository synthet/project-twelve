// Deterministic 32-bit integer hash — 1:1 port of Assets/Scripts/Sandbox/SandboxHash.cs.
//
// FNV-1a (offset 2166136261, prime 16777619) over three 32-bit words plus an
// xorshift-multiply finalizer. Pure integer math via Math.imul / `>>> 0`, so this
// agrees bit-for-bit with the engine with no float black box — the reason terrain
// generation no longer depends on the engine-native Mathf.PerlinNoise (whose native
// tables could not be reproduced offline; see docs/wiki/07-procedural-generation.md).
//
// The known-answer vectors in test/hash.test.js are shared verbatim with the C#
// SandboxHashTests suite. Keep this file, SandboxHash.cs, and both test suites in sync.
// Pure ESM (no Node APIs) so it also inlines into the classic-script HTML view.

import { fr } from './mathf.js';

/**
 * FNV-1a over (a, b, c) with an xorshift-multiply finalizer. Inputs are treated as
 * unsigned 32-bit; the result is an unsigned 32-bit integer (0 .. 2^32-1).
 */
export function hash(a, b, c) {
  let h = 2166136261; // 0x811C9DC5
  h = Math.imul(h ^ (a >>> 0), 16777619) >>> 0;
  h = Math.imul(h ^ (b >>> 0), 16777619) >>> 0;
  h = Math.imul(h ^ (c >>> 0), 16777619) >>> 0;
  h = (h ^ (h >>> 15)) >>> 0;
  h = Math.imul(h, 2246822519) >>> 0; // 0x85EBCA77
  h = (h ^ (h >>> 13)) >>> 0;
  return h >>> 0;
}

/**
 * Maps a hash word to a float in [0, 1] by dividing by 2^32 and narrowing to
 * float32 (Math.fround), matching C# SandboxHash.UnitFloat. The maximum word
 * (0xFFFFFFFF) narrows to exactly 1.
 */
export function unitFloat(h) {
  return fr((h >>> 0) * (1 / 4294967296)); // 4294967296 = 2^32
}
