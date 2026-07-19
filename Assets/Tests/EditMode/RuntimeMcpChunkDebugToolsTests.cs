using NUnit.Framework;
using ProjectTwelve.RuntimeMcp;
using UnityEngine;

/// <summary>
/// EditMode coverage for the P2-TOOL-001 chunk/light/fluid debug MCP tools: dispatch through raw
/// JSON-RPC, response shape, and the read-only guarantee (debug reads never generate chunks).
/// </summary>
public sealed class RuntimeMcpChunkDebugToolsTests
{
    private GameObject worldGo;
    private SandboxWorld world;
    private McpDispatcher dispatcher;

    [SetUp]
    public void SetUp()
    {
        worldGo = new GameObject("World");
        world = worldGo.AddComponent<SandboxWorld>();
        dispatcher = new McpDispatcher();
        GameplayMcpTools.Register(dispatcher, null);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(worldGo);
    }

    private string CallTool(string name, string argumentsJson)
    {
        string request =
            "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/call\",\"params\":{\"name\":\"" +
            name + "\",\"arguments\":" + argumentsJson + "}}";
        return dispatcher.HandleMessage(request).ResponseJson;
    }

    [Test]
    public void ChunkInfo_UngeneratedChunk_ReportsNotGeneratedAndDoesNotGenerate()
    {
        string response = CallTool("chunk_info", "{\"chunkX\":100,\"chunkY\":100}");

        StringAssert.Contains("\"isError\":false", response);
        StringAssert.Contains("\\\"generated\\\":false", response);
        Assert.IsFalse(world.TryGetExistingTile(100 * SandboxChunk.Size, 100 * SandboxChunk.Size, out _));
        Assert.AreEqual(0, world.GetNavVersion(new Vector2Int(100, 100)));
    }

    [Test]
    public void ChunkInfo_GeneratedChunk_ReportsFlags()
    {
        world.GetTile(0, 0);

        string response = CallTool("chunk_info", "{\"chunkX\":0,\"chunkY\":0}");

        StringAssert.Contains("\"isError\":false", response);
        StringAssert.Contains("\\\"generated\\\":true", response);
        StringAssert.Contains("rendererLoaded", response);
        StringAssert.Contains("needsRenderRebuild", response);
        StringAssert.Contains("needsColliderRebuild", response);
        StringAssert.Contains("saveDirty", response);
        StringAssert.Contains("hasEdits", response);
        StringAssert.Contains("navVersion", response);
    }

    [Test]
    public void ChunkInfo_NegativeCoordinateChunk_ReportsFlags()
    {
        world.GetTile(-1, -1);

        string response = CallTool("chunk_info", "{\"chunkX\":-1,\"chunkY\":-1}");

        StringAssert.Contains("\\\"generated\\\":true", response);
        StringAssert.Contains("\\\"chunkX\\\":-1", response);
        StringAssert.Contains("\\\"chunkY\\\":-1", response);
    }

    [Test]
    public void ChunkInfo_WorldTileArgs_ResolveToContainingChunk()
    {
        world.GetTile(-1, -1);

        string response = CallTool("chunk_info", "{\"x\":-1,\"y\":-1}");

        StringAssert.Contains("\\\"chunkX\\\":-1", response);
        StringAssert.Contains("\\\"chunkY\\\":-1", response);
        StringAssert.Contains("\\\"generated\\\":true", response);
    }

    [Test]
    public void ChunkInfo_MissingArgs_ReturnsToolError()
    {
        string response = CallTool("chunk_info", "{}");

        StringAssert.Contains("\"isError\":true", response);
        StringAssert.Contains("chunkX/chunkY or world tile x/y", response);
    }

    [Test]
    public void LightAt_UngeneratedChunk_ReportsNotGeneratedAndDoesNotGenerate()
    {
        int farTile = 100 * SandboxChunk.Size;
        string response = CallTool("light_at", "{\"x\":" + farTile + ",\"y\":" + farTile + "}");

        StringAssert.Contains("\"isError\":false", response);
        StringAssert.Contains("\\\"generated\\\":false", response);
        Assert.IsFalse(world.TryGetExistingTile(farTile, farTile, out _));
    }

    [Test]
    public void LightAt_GeneratedChunk_ReturnsLightValue()
    {
        world.GetTile(0, 0);

        string response = CallTool("light_at", "{\"x\":0,\"y\":0}");

        StringAssert.Contains("\\\"generated\\\":true", response);
        StringAssert.Contains("\\\"light\\\":", response);
    }

    [Test]
    public void FluidAt_UngeneratedChunk_ReportsNotGeneratedAndDoesNotGenerate()
    {
        int farTile = 100 * SandboxChunk.Size;
        string response = CallTool("fluid_at", "{\"x\":" + farTile + ",\"y\":" + farTile + "}");

        StringAssert.Contains("\"isError\":false", response);
        StringAssert.Contains("\\\"generated\\\":false", response);
        Assert.IsFalse(world.TryGetExistingTile(farTile, farTile, out _));
    }

    [Test]
    public void FluidAt_GeneratedChunk_ReturnsFluidAmount()
    {
        world.GetTile(0, 0);
        world.SetTileFluid(0, 0, 0.5f);

        string response = CallTool("fluid_at", "{\"x\":0,\"y\":0}");

        StringAssert.Contains("\\\"generated\\\":true", response);
        StringAssert.Contains("\\\"fluid\\\":0.5", response);
    }

    [Test]
    public void FluidAt_NoFluidControllerInScene_ReportsNullActiveSet()
    {
        world.GetTile(0, 0);

        string response = CallTool("fluid_at", "{\"x\":0,\"y\":0}");

        StringAssert.Contains("\\\"awake\\\":null", response);
        StringAssert.Contains("\\\"activeCellCount\\\":null", response);
    }

    [Test]
    public void ToolsList_IncludesChunkDebugTools()
    {
        string response = dispatcher
            .HandleMessage("{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/list\",\"params\":{}}")
            .ResponseJson;

        StringAssert.Contains("\"name\":\"chunk_info\"", response);
        StringAssert.Contains("\"name\":\"light_at\"", response);
        StringAssert.Contains("\"name\":\"fluid_at\"", response);
    }
}
