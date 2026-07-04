using System;

[Serializable]
public struct SandboxTile
{
    public int id;
    public byte light;
    public float fluid;
    public byte metadata;

    // The registry freeze contract pins the empty definition (core:air) to runtime index 0,
    // matching the legacy Air = 0 convention, so solidity never needs a registry lookup.
    public bool IsSolid => id != 0;

    public SandboxTile(int id, byte light = 0, float fluid = 0f, byte metadata = 0)
    {
        this.id = id;
        this.light = light;
        this.fluid = fluid;
        this.metadata = metadata;
    }
}

/// <summary>
/// Named registry runtime indices for the core tiles, resolved once from the frozen tile
/// registry (P2-DATA-002). These are NOT the fixed legacy numbering — for that, see
/// <c>SandboxCoreContent.LegacyTileIdToStringId</c>, which version-1 saves load through.
/// Convenience identity for tests and editor tooling only: runtime gameplay code must make
/// content decisions via <c>SandboxRegistries</c> / <c>TileDefinition</c> lookups, and
/// persisted data must go through the save palette. Do not add new fields here.
/// </summary>
public static class SandboxTileIds
{
    public static readonly int Air = ProjectTwelve.Sandbox.Registry.SandboxRegistries.AirIndex;
    public static readonly int Dirt = ProjectTwelve.Sandbox.Registry.SandboxRegistries.Tiles.GetIndex("core:dirt");
    public static readonly int Grass = ProjectTwelve.Sandbox.Registry.SandboxRegistries.GrassIndex;
    public static readonly int Stone = ProjectTwelve.Sandbox.Registry.SandboxRegistries.Tiles.GetIndex("core:stone");
    public static readonly int CopperOre = ProjectTwelve.Sandbox.Registry.SandboxRegistries.Tiles.GetIndex("core:copper_ore");
    public static readonly int IronOre = ProjectTwelve.Sandbox.Registry.SandboxRegistries.Tiles.GetIndex("core:iron_ore");
    public static readonly int SilverOre = ProjectTwelve.Sandbox.Registry.SandboxRegistries.Tiles.GetIndex("core:silver_ore");
    public static readonly int GoldOre = ProjectTwelve.Sandbox.Registry.SandboxRegistries.Tiles.GetIndex("core:gold_ore");
}
