using System.Collections.Generic;

namespace ProjectTwelve.Sandbox.Lighting
{
    /// <summary>
    /// Pure breadth-first tile-light propagation. Removal uses clear-then-refill and preserves
    /// only light entering from outside the bounded update, preventing stale-light feedback.
    /// </summary>
    public static class SandboxLightSolver
    {
        public const byte MaxLight = 15;
        public const byte AirAttenuation = 1;
        public const byte DefaultOpaqueAttenuation = 3;
        public const int MaxRange = MaxLight;

        private static readonly int[] DirectionX = { -1, 1, 0, 0 };
        private static readonly int[] DirectionY = { 0, 0, -1, 1 };

        public static void RelightAll(ISandboxLightGrid grid, SandboxLightBounds bounds)
        {
            Relight(grid, bounds, seedFromOutside: false);
        }

        public static void RelightAfterEdit(ISandboxLightGrid grid, int x, int y)
        {
            Relight(
                grid,
                new SandboxLightBounds(x - MaxRange, y - MaxRange, x + MaxRange, y + MaxRange),
                seedFromOutside: true);
        }

        public static void RelightAfterChunkLoad(
            ISandboxLightGrid grid,
            int chunkX,
            int chunkY,
            int chunkSize)
        {
            int minX = chunkX * chunkSize;
            int minY = chunkY * chunkSize;
            SandboxLightBounds chunkBounds = new SandboxLightBounds(
                minX,
                minY,
                minX + chunkSize - 1,
                minY + chunkSize - 1);
            Relight(grid, chunkBounds.Expand(MaxRange), seedFromOutside: true);
        }

        private static void Relight(
            ISandboxLightGrid grid,
            SandboxLightBounds bounds,
            bool seedFromOutside)
        {
            if (grid == null || !bounds.IsValid)
            {
                return;
            }

            Queue<LightNode> queue = new Queue<LightNode>();
            for (int x = bounds.MinX; x <= bounds.MaxX; x++)
            {
                for (int y = bounds.MinY; y <= bounds.MaxY; y++)
                {
                    if (grid.IsLoaded(x, y))
                    {
                        grid.SetLight(x, y, 0);
                    }
                }
            }

            for (int x = bounds.MinX; x <= bounds.MaxX; x++)
            {
                for (int y = bounds.MinY; y <= bounds.MaxY; y++)
                {
                    if (!grid.IsLoaded(x, y))
                    {
                        continue;
                    }

                    Seed(grid, queue, x, y, ClampLight(grid.GetSourceLight(x, y)));
                    if (seedFromOutside && IsBoundary(bounds, x, y))
                    {
                        SeedFromOutsideNeighbors(grid, bounds, queue, x, y);
                    }
                }
            }

            while (queue.Count > 0)
            {
                LightNode current = queue.Dequeue();
                byte currentLight = grid.GetTile(current.X, current.Y).light;
                if (currentLight == 0)
                {
                    continue;
                }

                for (int i = 0; i < DirectionX.Length; i++)
                {
                    int nextX = current.X + DirectionX[i];
                    int nextY = current.Y + DirectionY[i];
                    if (!bounds.Contains(nextX, nextY) || !grid.IsLoaded(nextX, nextY))
                    {
                        continue;
                    }

                    byte candidate = Attenuate(currentLight, grid.GetAttenuation(nextX, nextY));
                    Seed(grid, queue, nextX, nextY, candidate);
                }
            }
        }

        private static void SeedFromOutsideNeighbors(
            ISandboxLightGrid grid,
            SandboxLightBounds bounds,
            Queue<LightNode> queue,
            int x,
            int y)
        {
            for (int i = 0; i < DirectionX.Length; i++)
            {
                int outsideX = x + DirectionX[i];
                int outsideY = y + DirectionY[i];
                if (bounds.Contains(outsideX, outsideY) || !grid.IsLoaded(outsideX, outsideY))
                {
                    continue;
                }

                byte outsideLight = grid.GetTile(outsideX, outsideY).light;
                Seed(grid, queue, x, y, Attenuate(outsideLight, grid.GetAttenuation(x, y)));
            }
        }

        private static void Seed(
            ISandboxLightGrid grid,
            Queue<LightNode> queue,
            int x,
            int y,
            byte light)
        {
            if (light == 0 || light <= grid.GetTile(x, y).light)
            {
                return;
            }

            grid.SetLight(x, y, light);
            queue.Enqueue(new LightNode(x, y));
        }

        private static byte Attenuate(byte light, byte attenuation)
        {
            int cost = attenuation < 1 ? AirAttenuation : attenuation;
            return light > cost ? (byte)(light - cost) : (byte)0;
        }

        private static byte ClampLight(byte light)
        {
            return light > MaxLight ? MaxLight : light;
        }

        private static bool IsBoundary(SandboxLightBounds bounds, int x, int y)
        {
            return x == bounds.MinX || x == bounds.MaxX || y == bounds.MinY || y == bounds.MaxY;
        }

        private readonly struct LightNode
        {
            public LightNode(int x, int y)
            {
                X = x;
                Y = y;
            }

            public int X { get; }
            public int Y { get; }
        }
    }
}
