using NUnit.Framework;
using UnityEngine;

namespace ProjectTwelve.Tests.DependencyLayers.PureDotNet;

public sealed class SandboxPureDotNetTests
{
    [Test]
    public void SandboxChunk_UsesRealChunkImplementationWithMockedVector2Int()
    {
        SandboxChunk chunk = new SandboxChunk(new Vector2Int(-2, 3));

        chunk.SetLocalTile(1, 2, new SandboxTile(SandboxTileIds.CopperOre));

        Assert.That(chunk.Coord, Is.EqualTo(new Vector2Int(-2, 3)));
        Assert.That(chunk.GetLocalTile(1, 2).id, Is.EqualTo(SandboxTileIds.CopperOre));
        Assert.That(chunk.NeedsRenderRebuild, Is.True);
        Assert.That(chunk.NeedsColliderRebuild, Is.True);
        Assert.That(chunk.IsDirty, Is.True);
        Assert.That(chunk.HasEdits, Is.True);
    }

    [Test]
    public void SandboxTerrainGenerator_UsesRealGeneratorWithMockedMathf()
    {
        SandboxTerrainGenerator generator = new SandboxTerrainGenerator(
            seed: 1337,
            surfaceHeight: 28,
            terrainAmplitude: 8,
            terrainFrequency: 0.06f,
            dirtDepth: 8);

        SandboxChunk first = generator.GenerateChunk(Vector2Int.zero);
        SandboxChunk second = generator.GenerateChunk(Vector2Int.zero);

        for (int x = 0; x < SandboxChunk.Size; x++)
        {
            for (int y = 0; y < SandboxChunk.Size; y++)
            {
                Assert.That(second.GetLocalTile(x, y).id, Is.EqualTo(first.GetLocalTile(x, y).id));
                Assert.That(second.GetLocalTile(x, y).light, Is.EqualTo(first.GetLocalTile(x, y).light));
            }
        }
    }
}
