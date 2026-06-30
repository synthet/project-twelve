using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Exports a golden fixture of <see cref="SandboxTerrainGenerator"/> output (surface
/// heights plus a few full chunks) to <c>tools/world-viz/test/fixtures/surface.seed1337.json</c>.
///
/// This makes the engine the authority on generation: the standalone Node tool
/// (tools/world-viz) has a parity test that asserts its JS port reproduces this
/// fixture exactly. Running the EditMode tests regenerates the fixture; if the
/// engine generation logic ever changes, re-running these tests updates the
/// fixture and the Node parity test then flags any drift in the port.
///
/// Fixture chunk arrays are row-major with localX as the outer loop and localY
/// inner, matching <see cref="SandboxTerrainGenerator.GenerateChunk"/>.
/// </summary>
public sealed class TerrainFixtureExportTests
{
    private const int Seed = 1337;
    private const int SurfaceHeight = 28;
    private const int TerrainAmplitude = 8;
    private const float TerrainFrequency = 0.06f;
    private const int DirtDepth = 8;

    private const int MinX = -64;
    private const int MaxX = 64;

    private static readonly Vector2Int[] SampleChunks =
    {
        new Vector2Int(0, 0),
        new Vector2Int(1, 0),
        new Vector2Int(-2, 1),
    };

    private static SandboxTerrainGenerator Generator()
    {
        return new SandboxTerrainGenerator(Seed, SurfaceHeight, TerrainAmplitude, TerrainFrequency, DirtDepth);
    }

    [Test]
    public void ExportGoldenFixtureForWorldViz()
    {
        SandboxTerrainGenerator generator = Generator();
        var ci = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();

        sb.Append('{');
        sb.AppendFormat(ci, "\"seed\":{0},", Seed);
        sb.AppendFormat(ci, "\"surfaceHeight\":{0},", SurfaceHeight);
        sb.AppendFormat(ci, "\"terrainAmplitude\":{0},", TerrainAmplitude);
        sb.AppendFormat(ci, "\"terrainFrequency\":{0},", TerrainFrequency.ToString("R", ci));
        sb.AppendFormat(ci, "\"dirtDepth\":{0},", DirtDepth);
        sb.AppendFormat(ci, "\"minX\":{0},\"maxX\":{1},", MinX, MaxX);

        sb.Append("\"surfaceHeights\":[");
        for (int x = MinX; x <= MaxX; x++)
        {
            if (x > MinX)
            {
                sb.Append(',');
            }

            sb.Append(generator.GetSurfaceHeight(x).ToString(ci));
        }

        sb.Append("],\"chunks\":[");
        for (int c = 0; c < SampleChunks.Length; c++)
        {
            if (c > 0)
            {
                sb.Append(',');
            }

            AppendChunk(sb, ci, generator, SampleChunks[c]);
        }

        sb.Append("]}");

        string path = FixturePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, sb.ToString());

        Assert.IsTrue(File.Exists(path), $"Fixture should be written to {path}");
    }

    private static void AppendChunk(
        StringBuilder sb, CultureInfo ci, SandboxTerrainGenerator generator, Vector2Int coord)
    {
        SandboxChunk chunk = generator.GenerateChunk(coord);

        sb.AppendFormat(ci, "{{\"x\":{0},\"y\":{1},\"ids\":[", coord.x, coord.y);
        bool first = true;
        for (int lx = 0; lx < SandboxChunk.Size; lx++)
        {
            for (int ly = 0; ly < SandboxChunk.Size; ly++)
            {
                if (!first)
                {
                    sb.Append(',');
                }

                first = false;
                sb.Append(chunk.GetLocalTile(lx, ly).id.ToString(ci));
            }
        }

        sb.Append("],\"lights\":[");
        first = true;
        for (int lx = 0; lx < SandboxChunk.Size; lx++)
        {
            for (int ly = 0; ly < SandboxChunk.Size; ly++)
            {
                if (!first)
                {
                    sb.Append(',');
                }

                first = false;
                sb.Append(((int)chunk.GetLocalTile(lx, ly).light).ToString(ci));
            }
        }

        sb.Append("]}");
    }

    [Test]
    public void GeneratorIsDeterministicAcrossExportRange()
    {
        SandboxTerrainGenerator a = Generator();
        SandboxTerrainGenerator b = Generator();
        for (int x = MinX; x <= MaxX; x++)
        {
            Assert.AreEqual(a.GetSurfaceHeight(x), b.GetSurfaceHeight(x), $"surface height differs at x={x}");
        }
    }

    private static string FixturePath()
    {
        // Application.dataPath points at the Assets folder; the repo root is its parent.
        return Path.GetFullPath(Path.Combine(
            Application.dataPath, "..", "tools", "world-viz", "test", "fixtures", "surface.seed1337.json"));
    }
}
