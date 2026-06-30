// Numeric helpers that mirror Unity / .NET semantics exactly.
//
// These are the small primitives the engine relies on implicitly. Getting them
// wrong is a classic source of off-by-one terrain drift, so each one documents
// the exact engine behaviour it reproduces. Pure ESM, no Node APIs: shared by
// the CLI and the inlined in-browser generator.

/** Emulate 32-bit float arithmetic (Unity computes terrain math in `float`). */
export const fr = Math.fround;

/** clamp01 — matches UnityEngine.Mathf.Clamp01. */
export function clamp01(v) {
  if (v < 0) return 0;
  if (v > 1) return 1;
  return v;
}

/** Linear interpolation — matches UnityEngine.Mathf.Lerp (t clamped to [0,1]). */
export function lerp(a, b, t) {
  return a + (b - a) * clamp01(t);
}

/**
 * Round-half-to-even ("banker's rounding").
 *
 * UnityEngine.Mathf.RoundToInt(f) == (int)System.Math.Round((double)f), and
 * .NET's Math.Round defaults to MidpointRounding.ToEven. JavaScript's
 * Math.round rounds halves toward +Infinity instead, so it disagrees on exact
 * .5 boundaries (e.g. Math.round(2.5) === 3 but the engine yields 2). Terrain
 * heights are produced through RoundToInt, so we must match the engine here.
 */
export function roundToInt(value) {
  const floor = Math.floor(value);
  const diff = value - floor;
  if (diff < 0.5) return floor;
  if (diff > 0.5) return floor + 1;
  // Exactly halfway: round to the even integer.
  return floor % 2 === 0 ? floor : floor + 1;
}

/**
 * Floor division by a positive divisor.
 * Mirrors SandboxWorld.FloorDiv (Mathf.FloorToInt((float)value / divisor)) so
 * negative world coordinates resolve to the correct chunk (world -1 -> chunk -1).
 */
export function floorDiv(value, divisor) {
  return Math.floor(value / divisor);
}

/**
 * Positive modulo.
 * Mirrors SandboxWorld.Mod: always returns a value in [0, divisor - 1] even for
 * negative inputs, so it is always a valid in-chunk index.
 */
export function mod(value, divisor) {
  // ((v % d) + d) % d keeps the result in [0, d-1] and avoids JS's -0
  // (e.g. -32 % 32 === -0), which the engine's integer Mod never produces.
  return ((value % divisor) + divisor) % divisor;
}
