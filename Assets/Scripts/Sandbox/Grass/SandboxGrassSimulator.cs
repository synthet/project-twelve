using System.Collections.Generic;
using ProjectTwelve.Sandbox.Registry;
using UnityEngine;

namespace ProjectTwelve.Sandbox.Grass
{
    /// <summary>
    /// Grass-growth simulation over an <see cref="IGrassWorld"/> (EditMode-testable without a scene).
    /// Grass is real tile state — the <c>core:grass</c> tile — not a render-time overlay. Each tick
    /// samples a bounded number of random cells in the loaded set (Terraria-style random tile
    /// updates) and applies four rules:
    /// <list type="bullet">
    /// <item>Grass whose top is buried (solid above) or no longer sunlit reverts to dirt.</item>
    /// <item>Healthy grass has a <c>spreadChance</c> to grow onto an eligible neighbor.</item>
    /// <item>Exposed, sunlit, dry dirt has a small <c>spontaneousChance</c> to sprout grass.</item>
    /// <item>Grass never appears on stone or on non-sunlit (underground / roofed) tiles.</item>
    /// </list>
    /// "Sunlit" is a vertical sky-cast: a clear column of non-solid tiles straight up to open sky.
    /// Every write goes through <see cref="IGrassWorld.SetTile"/> so it persists and repaints.
    /// </summary>
    public sealed class SandboxGrassSimulator
    {
        /// <summary>Grass does not grow under standing liquid; treat a wetter cell above as covered.</summary>
        private const float FluidEpsilon = 0.05f;

        // 8-neighborhood so grass climbs sloped/diagonal surfaces, not just orthogonal runs.
        private static readonly Vector2Int[] SpreadOffsets =
        {
            new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1),
            new Vector2Int(-1, 0), new Vector2Int(1, 0),
            new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1),
        };

        private readonly IGrassWorld world;
        private readonly System.Random rng;
        private readonly int skyScanCap;
        private readonly float spreadChance;
        private readonly float spontaneousChance;

        private readonly List<Vector2Int> loadedScratch = new List<Vector2Int>();

        public SandboxGrassSimulator(
            IGrassWorld world,
            int seed = 0,
            int skyScanCap = 64,
            float spreadChance = 0.25f,
            float spontaneousChance = 0.02f)
        {
            this.world = world;
            rng = new System.Random(seed);
            this.skyScanCap = Mathf.Max(1, skyScanCap);
            this.spreadChance = spreadChance;
            this.spontaneousChance = spontaneousChance;
        }

        /// <summary>
        /// Advances the simulation one step, processing up to <paramref name="maxUpdates"/> random
        /// cells across the loaded chunks. Returns the number of cells visited.
        /// </summary>
        public int ProcessTick(int maxUpdates)
        {
            if (maxUpdates <= 0)
            {
                return 0;
            }

            loadedScratch.Clear();
            foreach (Vector2Int coord in world.LoadedChunkCoords)
            {
                loadedScratch.Add(coord);
            }

            if (loadedScratch.Count == 0)
            {
                return 0;
            }

            for (int i = 0; i < maxUpdates; i++)
            {
                Vector2Int chunkCoord = loadedScratch[rng.Next(loadedScratch.Count)];
                int worldX = chunkCoord.x * SandboxChunk.Size + rng.Next(SandboxChunk.Size);
                int worldY = chunkCoord.y * SandboxChunk.Size + rng.Next(SandboxChunk.Size);
                ProcessCell(worldX, worldY);
            }

            return maxUpdates;
        }

        /// <summary>
        /// Reacts to a tile edit at <paramref name="x"/>,<paramref name="y"/>: if a solid tile was
        /// placed on top of grass, that grass is immediately buried and reverts to dirt. Digging
        /// already leaves bare dirt/air, so it needs no special case. Kept single-step (no cascade)
        /// because the driver suppresses re-entrant wakes from the simulator's own writes.
        /// </summary>
        public void OnTileChanged(int x, int y)
        {
            int belowY = y - 1;
            if (SafeTile(x, belowY).id != SandboxRegistries.GrassIndex)
            {
                return;
            }

            if (SafeTile(x, y).IsSolid)
            {
                world.SetTile(x, belowY, SandboxRegistries.DirtIndex);
            }
        }

        /// <summary>
        /// Vertical sky-cast: true when a clear column of non-solid tiles runs from just above
        /// <paramref name="x"/>,<paramref name="y"/> up to open sky. Reaching an unloaded chunk (the
        /// top of the streamed window) or clearing <see cref="skyScanCap"/> tiles counts as sky; the
        /// first solid tile encountered means the cell is roofed and not sunlit.
        /// </summary>
        public bool IsSunlit(int x, int y)
        {
            for (int scan = 1; scan <= skyScanCap; scan++)
            {
                int worldY = y + scan;
                if (!world.IsLoaded(x, worldY))
                {
                    return true;
                }

                if (world.GetTile(x, worldY).IsSolid)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Whether grass may grow on the cell: bare <c>core:dirt</c>, top-exposed (no solid tile
        /// above), dry, and sunlit. Excludes stone, buried dirt, and underground/roofed dirt.
        /// </summary>
        public bool CanGrassGrow(int x, int y)
        {
            if (SafeTile(x, y).id != SandboxRegistries.DirtIndex)
            {
                return false;
            }

            SandboxTile above = SafeTile(x, y + 1);
            if (above.IsSolid || above.fluid > FluidEpsilon)
            {
                return false;
            }

            return IsSunlit(x, y);
        }

        /// <summary>
        /// Applies the grass rules to a single cell (grass death/spread, dirt spontaneous growth).
        /// Internal so EditMode tests can drive a specific cell deterministically.
        /// </summary>
        internal void ProcessCell(int x, int y)
        {
            SandboxTile tile = world.GetTile(x, y);

            if (tile.id == SandboxRegistries.GrassIndex)
            {
                if (SafeTile(x, y + 1).IsSolid || !IsSunlit(x, y))
                {
                    world.SetTile(x, y, SandboxRegistries.DirtIndex);
                    return;
                }

                if (rng.NextDouble() < spreadChance)
                {
                    TrySpread(x, y);
                }
            }
            else if (tile.id == SandboxRegistries.DirtIndex)
            {
                if (rng.NextDouble() < spontaneousChance && CanGrassGrow(x, y))
                {
                    world.SetTile(x, y, SandboxRegistries.GrassIndex);
                }
            }
        }

        /// <summary>
        /// Grows grass onto one random eligible neighbor of a healthy grass tile. Reservoir sampling
        /// keeps the pick uniform across eligible neighbors in a single pass.
        /// </summary>
        private void TrySpread(int x, int y)
        {
            int eligible = 0;
            Vector2Int target = default;
            foreach (Vector2Int offset in SpreadOffsets)
            {
                int nx = x + offset.x;
                int ny = y + offset.y;
                if (!world.IsLoaded(nx, ny) || !CanGrassGrow(nx, ny))
                {
                    continue;
                }

                eligible++;
                if (rng.Next(eligible) == 0)
                {
                    target = new Vector2Int(nx, ny);
                }
            }

            if (eligible > 0)
            {
                world.SetTile(target.x, target.y, SandboxRegistries.GrassIndex);
            }
        }

        /// <summary>Tile read that never generates a chunk: unloaded cells read as air (open sky).</summary>
        private SandboxTile SafeTile(int x, int y)
        {
            return world.IsLoaded(x, y) ? world.GetTile(x, y) : default;
        }
    }
}
