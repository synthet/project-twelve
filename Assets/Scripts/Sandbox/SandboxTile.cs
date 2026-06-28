using System;

[Serializable]
public struct SandboxTile
{
    public int id;
    public byte light;
    public float fluid;
    public byte metadata;

    public bool IsSolid => id != SandboxTileIds.Air;

    public SandboxTile(int id, byte light = 0, float fluid = 0f, byte metadata = 0)
    {
        this.id = id;
        this.light = light;
        this.fluid = fluid;
        this.metadata = metadata;
    }
}

public static class SandboxTileIds
{
    public const int Air = 0;
    public const int Dirt = 1;
    public const int Grass = 2;
    public const int Stone = 3;
    public const int CopperOre = 4;
}
