using ProjectTwelve.Sandbox.Registry;
using UnityEngine;

/// <summary>
/// Pure, deterministic terrain generator for the prototype sandbox.
///
/// Given identical settings (seed plus the terrain shaping parameters) and a chunk
/// coordinate, generation always produces identical tile data, independent of Unity
/// lifecycle, scene state, or load order. Untouched chunks can therefore be regenerated
/// from the seed instead of being persisted (see <c>docs/wiki/generation-and-saving.md</c>).
///
/// The struct carries no mutable state: it is a read-only view over the generation
/// inputs, which keeps the contract easy to reason about and to test with golden seeds.
/// </summary>
public readonly struct SandboxTerrainGenerator
{
    public readonly int Seed;
    public readonly int SurfaceHeight;
    public readonly int TerrainAmplitude;
    public readonly float TerrainFrequency;
    public readonly int DirtDepth;

    // Registry runtime indices resolved once per generator instance; generation loops stay
    // free of per-tile string lookups (P2-DATA-002 hot-path requirement).
    private readonly int airTileIndex;
    private readonly int grassTileIndex;
    private readonly int dirtTileIndex;
    private readonly int stoneTileIndex;

    public SandboxTerrainGenerator(int seed, int surfaceHeight, int terrainAmplitude, float terrainFrequency, int dirtDepth)
    {
        Seed = seed;
        SurfaceHeight = surfaceHeight;
        TerrainAmplitude = terrainAmplitude;
        TerrainFrequency = terrainFrequency;
        DirtDepth = dirtDepth;

        ContentRegistry<TileDefinition> tiles = SandboxRegistries.Tiles;
        airTileIndex = SandboxRegistries.AirIndex;
        grassTileIndex = tiles.GetIndex(SandboxCoreContent.GrassTileId);
        dirtTileIndex = tiles.GetIndex("core:dirt");
        stoneTileIndex = tiles.GetIndex("core:stone");
    }

    /// <summary>
    /// Generates the full tile grid for the chunk at <paramref name="chunkCoord"/>.
    /// </summary>
    public SandboxChunk GenerateChunk(Vector2Int chunkCoord)
    {
        SandboxChunk chunk = new SandboxChunk(chunkCoord);

        for (int localX = 0; localX < SandboxChunk.Size; localX++)
        {
            int worldX = chunkCoord.x * SandboxChunk.Size + localX;
            int height = GetSurfaceHeight(worldX);

            for (int localY = 0; localY < SandboxChunk.Size; localY++)
            {
                int worldY = chunkCoord.y * SandboxChunk.Size + localY;
                chunk.Tiles[localX, localY] = GenerateTile(worldY, height);
            }
        }

        return chunk;
    }

    /// <summary>
    /// Surface height in world tiles for the given world column. Determined solely by
    /// the seed, the column, and the terrain shaping parameters.
    ///
    /// Pass 1 (<see cref="SandboxGenPass.SurfaceHeightmap"/>) value noise: a smooth
    /// interpolation between per-lattice random samples drawn from the deterministic
    /// integer hash <c>SandboxHash.Hash(seed, passId, latticeX)</c>. This replaces the
    /// engine-native <c>Mathf.PerlinNoise</c> so the standalone JS port
    /// (<c>tools/world-viz</c>) can reproduce generation bit-for-bit — Unity's native
    /// perlin tables could not be matched offline. <see cref="TerrainFrequency"/> sets
    /// the lattice spacing (tiles per noise cell = 1 / frequency).
    /// </summary>
    public int GetSurfaceHeight(int worldX)
    {
        const int passId = (int)SandboxGenPass.SurfaceHeightmap;

        float t = worldX * TerrainFrequency;
        int latticeX = Mathf.FloorToInt(t);
        float frac = t - latticeX;

        float a = SandboxHash.UnitFloat(SandboxHash.Hash((uint)Seed, (uint)passId, (uint)latticeX));
        float b = SandboxHash.UnitFloat(SandboxHash.Hash((uint)Seed, (uint)passId, (uint)(latticeX + 1)));
        float noise = a + (b - a) * SmoothStep(frac);

        return SurfaceHeight + Mathf.RoundToInt((noise - 0.5f) * TerrainAmplitude * 2f);
    }

    /// <summary>
    /// Quintic smootherstep (6t^5 - 15t^4 + 10t^3), the C2-continuous ease Ken Perlin
    /// uses for improved noise. Applied to the in-cell fraction so surface height is
    /// smooth across lattice boundaries. Evaluated in <c>float</c> to match the JS port.
    /// </summary>
    private static float SmoothStep(float t)
    {
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    /// <summary>
    /// Generates a single tile from its world row and the surface height of its column,
    /// including the prototype light seed used by rendering.
    /// </summary>
    public SandboxTile GenerateTile(int worldY, int height)
    {
        int tileId = GetGeneratedTileId(worldY, height);
        return new SandboxTile(tileId, GetPrototypeLight(worldY, height));
    }

    /// <summary>
    /// Prototype sky-exposure light from a world row and that column's surface height.
    /// </summary>
    public static byte GetPrototypeLight(int worldY, int surfaceHeightForColumn)
    {
        return worldY >= surfaceHeightForColumn ? (byte)15 : (byte)4;
    }

    /// <summary>
    /// Resolves the registry runtime tile index for a world row relative to its column
    /// surface height.
    /// </summary>
    public int GetGeneratedTileId(int worldY, int height)
    {
        if (worldY > height)
        {
            return airTileIndex;
        }

        if (worldY == height)
        {
            return grassTileIndex;
        }

        return worldY > height - DirtDepth ? dirtTileIndex : stoneTileIndex;
    }
}
