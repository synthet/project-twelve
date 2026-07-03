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

/// <summary>
/// Legacy numeric tile IDs — compatibility shim only (P2-DATA-001). New content identity is the
/// registry string ID (<c>ProjectTwelve.Sandbox.Registry.SandboxCoreContent</c>); these constants
/// map 1:1 onto <c>SandboxCoreContent.LegacyTileIdToStringId</c> and survive until callers
/// migrate to registry runtime indices (P2-DATA-002). Do not add new constants here.
/// </summary>
public static class SandboxTileIds
{
    public const int Air = 0;
    public const int Dirt = 1;
    public const int Grass = 2;
    public const int Stone = 3;
    public const int CopperOre = 4;
    public const int IronOre = 5;
    public const int SilverOre = 6;
    public const int GoldOre = 7;
}
