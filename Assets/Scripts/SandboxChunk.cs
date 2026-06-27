using UnityEngine;

public sealed class SandboxChunk
{
    public const int Size = 32;

    private readonly SandboxTile[,] tiles = new SandboxTile[Size, Size];

    public Vector2Int Coord { get; }
    public bool NeedsRenderRebuild { get; set; }
    public bool NeedsColliderRebuild { get; set; }
    public bool IsDirty { get; set; }

    public SandboxTile[,] Tiles => tiles;

    public SandboxChunk(Vector2Int coord)
    {
        Coord = coord;
        NeedsRenderRebuild = true;
        NeedsColliderRebuild = true;
    }

    public SandboxTile GetLocalTile(int x, int y)
    {
        if (!IsLocalInBounds(x, y))
        {
            return default;
        }

        return tiles[x, y];
    }

    public void SetLocalTile(int x, int y, SandboxTile tile)
    {
        if (!IsLocalInBounds(x, y))
        {
            return;
        }

        tiles[x, y] = tile;
        NeedsRenderRebuild = true;
        NeedsColliderRebuild = true;
        IsDirty = true;
    }

    public static bool IsLocalInBounds(int x, int y)
    {
        return x >= 0 && x < Size && y >= 0 && y < Size;
    }
}
