/// <summary>
/// Deterministic 32-bit integer hash and derived helpers shared by seeded
/// generation (P2-GEN-001) and the fluid scan-order (<c>SandboxFluidSimulator</c>).
///
/// The mix is FNV-1a (offset basis 2166136261, prime 16777619) over three 32-bit
/// words followed by an xorshift-multiply finalizer. It is pure integer math, so
/// C# and the JavaScript port (<c>tools/world-viz/src/core/hash.js</c>) agree
/// bit-for-bit with no float black box — the reason terrain no longer depends on
/// the engine-native <c>Mathf.PerlinNoise</c>, whose native tables could not be
/// reproduced offline. Any change here MUST be mirrored in the JS port and in the
/// shared known-answer vectors (<c>SandboxHashTests</c> / <c>hash.test.js</c>).
///
/// Sub-seed convention: a pass unit draws from <c>Hash(seed, passId, coord)</c>
/// (see <see cref="SandboxGenPass"/>); coordinate-addressable, never dependent on
/// iteration order.
/// </summary>
public static class SandboxHash
{
    /// <summary>
    /// FNV-1a over (<paramref name="a"/>, <paramref name="b"/>, <paramref name="c"/>)
    /// with an xorshift-multiply finalizer. Deterministic and platform-independent.
    /// </summary>
    public static uint Hash(uint a, uint b, uint c)
    {
        unchecked
        {
            uint h = 2166136261u;
            h = (h ^ a) * 16777619u;
            h = (h ^ b) * 16777619u;
            h = (h ^ c) * 16777619u;
            h ^= h >> 15;
            h *= 2246822519u;
            h ^= h >> 13;
            return h;
        }
    }

    /// <summary>
    /// Maps a hash word to a <c>float</c> in [0, 1] by dividing by 2^32 and
    /// narrowing to <c>float</c>. Computed in <c>double</c> then cast so the JS
    /// port (<c>fround(h / 2^32)</c>) matches exactly. The maximum word
    /// (<c>uint.MaxValue</c>) narrows to exactly <c>1f</c>.
    /// </summary>
    public static float UnitFloat(uint h)
    {
        return (float)(h * (1.0 / 4294967296.0));
    }
}
