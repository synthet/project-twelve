using System;

namespace ProjectTwelve.Visual.Tiles
{
    /// <summary>
    /// Autotile neighbor exposure rules. Matches tile-viz: coordinates below the exposure floor
    /// are treated as air for south-row solid support so procedural fill under the world bottom
    /// does not force interior sprites on exposed underside cells.
    /// </summary>
    public static class AutotileExposure
    {
        /// <summary>Sentinel: no exposure floor clipping (EditMode tests and legacy callers).</summary>
        public const int NoFloor = int.MinValue;

        /// <summary>
        /// Default floor for sandbox scenes aligned with tile-space captures (yMin = 0).
        /// </summary>
        public const int DefaultSandboxFloorY = 0;

        /// <summary>
        /// Builds an <c>isSolid</c> predicate for solid-mask debug and retained normalizer helpers.
        /// Tiles below <paramref name="exposureFloorY"/> never count as solid (tile-viz off-space air parity).
        /// </summary>
        public static Func<int, int, bool> CreateIsSolid(
            Func<int, int, SandboxTile> tileLookup,
            int exposureFloorY)
        {
            if (exposureFloorY == NoFloor)
            {
                return (x, y) => tileLookup(x, y).IsSolid;
            }

            return (x, y) => y >= exposureFloorY && tileLookup(x, y).IsSolid;
        }
    }
}
