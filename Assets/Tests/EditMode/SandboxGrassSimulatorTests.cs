using System.Collections.Generic;
using NUnit.Framework;
using ProjectTwelve.Sandbox.Grass;
using ProjectTwelve.Sandbox.Registry;
using UnityEngine;

public sealed class SandboxGrassSimulatorTests
{
    private static int Air => SandboxRegistries.AirIndex;
    private static int Dirt => SandboxRegistries.DirtIndex;
    private static int Grass => SandboxRegistries.GrassIndex;
    private static int Stone => SandboxRegistries.StoneIndex;

    private static SandboxGrassSimulator MakeSimulator(
        IGrassWorld world,
        float spreadChance = 0f,
        float spontaneousChance = 0f)
    {
        // Small sky-scan cap keeps roof/sunlight setups compact and fast.
        return new SandboxGrassSimulator(
            world,
            seed: 1,
            skyScanCap: 8,
            spreadChance: spreadChance,
            spontaneousChance: spontaneousChance);
    }

    [Test]
    public void IsSunlit_ClearColumnAboveIsSunlit()
    {
        FakeGrassWorld world = new FakeGrassWorld();
        world.Set(0, 0, Dirt);

        Assert.IsTrue(MakeSimulator(world).IsSunlit(0, 0));
    }

    [Test]
    public void IsSunlit_SolidRoofBlocksSunlight()
    {
        FakeGrassWorld world = new FakeGrassWorld();
        world.Set(0, 0, Dirt);
        world.Set(0, 3, Stone); // roof within the sky-scan cap

        Assert.IsFalse(MakeSimulator(world).IsSunlit(0, 0));
    }

    [Test]
    public void CanGrassGrow_ExposedSunlitDirtIsEligible()
    {
        FakeGrassWorld world = new FakeGrassWorld();
        world.Set(0, 0, Dirt);

        Assert.IsTrue(MakeSimulator(world).CanGrassGrow(0, 0));
    }

    [Test]
    public void CanGrassGrow_StoneIsNeverEligible()
    {
        FakeGrassWorld world = new FakeGrassWorld();
        world.Set(0, 0, Stone);

        Assert.IsFalse(MakeSimulator(world).CanGrassGrow(0, 0));
    }

    [Test]
    public void CanGrassGrow_BuriedDirtIsNotEligible()
    {
        FakeGrassWorld world = new FakeGrassWorld();
        world.Set(0, 0, Dirt);
        world.Set(0, 1, Dirt); // solid tile directly above

        Assert.IsFalse(MakeSimulator(world).CanGrassGrow(0, 0));
    }

    [Test]
    public void CanGrassGrow_UnderwaterDirtIsNotEligible()
    {
        FakeGrassWorld world = new FakeGrassWorld();
        world.Set(0, 0, Dirt);
        world.Set(0, 1, Air, fluid: 0.8f); // standing liquid above

        Assert.IsFalse(MakeSimulator(world).CanGrassGrow(0, 0));
    }

    [Test]
    public void CanGrassGrow_UndergroundDirtNotSunlitIsNotEligible()
    {
        FakeGrassWorld world = new FakeGrassWorld();
        world.Set(0, 0, Dirt); // top exposed (air above) but roofed higher up
        world.Set(0, 3, Stone);

        Assert.IsFalse(MakeSimulator(world).CanGrassGrow(0, 0));
    }

    [Test]
    public void ProcessCell_BuriedGrassRevertsToDirt()
    {
        FakeGrassWorld world = new FakeGrassWorld();
        world.Set(0, 0, Grass);
        world.Set(0, 1, Stone); // buries the grass

        MakeSimulator(world).ProcessCell(0, 0);

        Assert.AreEqual(Dirt, world.GetTile(0, 0).id);
    }

    [Test]
    public void ProcessCell_UnlitGrassRevertsToDirt()
    {
        FakeGrassWorld world = new FakeGrassWorld();
        world.Set(0, 0, Grass); // exposed but roofed → not sunlit
        world.Set(0, 3, Stone);

        MakeSimulator(world).ProcessCell(0, 0);

        Assert.AreEqual(Dirt, world.GetTile(0, 0).id);
    }

    [Test]
    public void ProcessCell_GrassSpreadsToEligibleNeighbor()
    {
        FakeGrassWorld world = new FakeGrassWorld();
        world.Set(0, 0, Grass); // healthy, sunlit
        world.Set(1, 0, Dirt);  // eligible neighbor

        MakeSimulator(world, spreadChance: 1f).ProcessCell(0, 0);

        Assert.AreEqual(Grass, world.GetTile(1, 0).id);
    }

    [Test]
    public void ProcessCell_GrassDoesNotSpreadToStone()
    {
        FakeGrassWorld world = new FakeGrassWorld();
        world.Set(0, 0, Grass);
        world.Set(1, 0, Stone); // the only neighbor, ineligible

        MakeSimulator(world, spreadChance: 1f).ProcessCell(0, 0);

        Assert.AreEqual(Stone, world.GetTile(1, 0).id);
    }

    [Test]
    public void ProcessCell_DirtSpontaneouslyGrowsWhenSunlit()
    {
        FakeGrassWorld world = new FakeGrassWorld();
        world.Set(0, 0, Dirt); // exposed, sunlit

        MakeSimulator(world, spontaneousChance: 1f).ProcessCell(0, 0);

        Assert.AreEqual(Grass, world.GetTile(0, 0).id);
    }

    [Test]
    public void ProcessCell_DirtDoesNotGrowUnderground()
    {
        FakeGrassWorld world = new FakeGrassWorld();
        world.Set(0, 0, Dirt); // exposed but roofed → not sunlit
        world.Set(0, 2, Stone);

        MakeSimulator(world, spontaneousChance: 1f).ProcessCell(0, 0);

        Assert.AreEqual(Dirt, world.GetTile(0, 0).id);
    }

    [Test]
    public void OnTileChanged_PlacingSolidOnGrassBuriesIt()
    {
        FakeGrassWorld world = new FakeGrassWorld();
        world.Set(0, 0, Grass);
        world.Set(0, 1, Stone); // a tile was just placed on top

        MakeSimulator(world).OnTileChanged(0, 1);

        Assert.AreEqual(Dirt, world.GetTile(0, 0).id);
    }

    [Test]
    public void OnTileChanged_DiggingAboveGrassLeavesItUntouched()
    {
        FakeGrassWorld world = new FakeGrassWorld();
        world.Set(0, 0, Grass);
        world.Set(0, 1, Air); // dug out (non-solid) above the grass

        MakeSimulator(world).OnTileChanged(0, 1);

        Assert.AreEqual(Grass, world.GetTile(0, 0).id);
    }

    /// <summary>In-memory <see cref="IGrassWorld"/>; every cell is treated as loaded.</summary>
    private sealed class FakeGrassWorld : IGrassWorld
    {
        private readonly Dictionary<Vector2Int, SandboxTile> tiles = new Dictionary<Vector2Int, SandboxTile>();

        public void Set(int x, int y, int id, float fluid = 0f)
        {
            tiles[new Vector2Int(x, y)] = new SandboxTile(id, 0, fluid);
        }

        public SandboxTile GetTile(int x, int y)
        {
            return tiles.TryGetValue(new Vector2Int(x, y), out SandboxTile tile) ? tile : default;
        }

        public void SetTile(int x, int y, int tileId)
        {
            tiles[new Vector2Int(x, y)] = new SandboxTile(tileId);
        }

        public bool IsLoaded(int x, int y) => true;

        public IEnumerable<Vector2Int> LoadedChunkCoords => new[] { Vector2Int.zero };
    }
}
