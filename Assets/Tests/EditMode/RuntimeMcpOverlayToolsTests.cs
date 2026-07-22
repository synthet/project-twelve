using Newtonsoft.Json.Linq;
using NUnit.Framework;
using ProjectTwelve.RuntimeMcp;
using ProjectTwelve.Sandbox.Debug;

public sealed class RuntimeMcpOverlayToolsTests
{
    private McpDispatcher dispatcher;
    private SandboxDebugOverlayState state;

    [SetUp]
    public void SetUp()
    {
        dispatcher = new McpDispatcher();
        state = new SandboxDebugOverlayState();
        DebugOverlayMcpTools.Register(dispatcher, state);
    }

    [Test]
    public void NewState_HasNoEnabledOverlays()
    {
        Assert.IsFalse(state.AnyEnabled);

        foreach (SandboxDebugOverlay overlay in SandboxDebugOverlayState.AllOverlays)
        {
            Assert.IsFalse(state.IsEnabled(overlay));
        }
    }

    [Test]
    public void ToolsList_IncludesOverlaySetAndStateTools()
    {
        string response = dispatcher
            .HandleMessage("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\",\"params\":{}}")
            .ResponseJson;

        StringAssert.Contains("\"name\":\"debug_overlay_set\"", response);
        StringAssert.Contains("\"name\":\"debug_overlay_state\"", response);
    }

    [Test]
    public void ToolsList_DeclaresClosedInputSchemas()
    {
        string response = dispatcher
            .HandleMessage("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\",\"params\":{}}")
            .ResponseJson;
        JArray tools = (JArray)JObject.Parse(response)["result"]["tools"];

        foreach (JObject tool in tools.Children<JObject>())
        {
            Assert.AreEqual(false, tool["inputSchema"]["additionalProperties"]?.Value<bool>());
        }
    }

    [Test]
    public void OverlaySet_WithoutEnabled_TogglesNamedOverlay()
    {
        string response = CallTool("debug_overlay_set", "{\"overlay\":\"light_heatmap\"}");

        StringAssert.Contains("\"isError\":false", response);
        StringAssert.Contains("\\\"overlay\\\":\\\"light_heatmap\\\"", response);
        StringAssert.Contains("\\\"enabled\\\":true", response);
        Assert.IsTrue(state.IsEnabled(SandboxDebugOverlay.LightHeatmap));

        CallTool("debug_overlay_set", "{\"overlay\":\"light_heatmap\"}");
        Assert.IsFalse(state.IsEnabled(SandboxDebugOverlay.LightHeatmap));
    }

    [Test]
    public void OverlaySet_WithEnabled_SetsNamedOverlayIdempotently()
    {
        CallTool("debug_overlay_set", "{\"overlay\":\"fluid\",\"enabled\":true}");
        string response = CallTool("debug_overlay_set", "{\"overlay\":\"fluid\",\"enabled\":true}");

        Assert.IsTrue(state.IsEnabled(SandboxDebugOverlay.Fluid));
        StringAssert.Contains("\\\"enabled\\\":true", response);
    }

    [Test]
    public void OverlaySet_WithEnabledFalse_DisablesNamedOverlay()
    {
        state.SetEnabled(SandboxDebugOverlay.TileSolidity, true);
        string response = CallTool("debug_overlay_set", "{\"overlay\":\"tile_solidity\",\"enabled\":false}");

        Assert.IsFalse(state.IsEnabled(SandboxDebugOverlay.TileSolidity));
        StringAssert.Contains("\\\"enabled\\\":false", response);
    }

    [Test]
    public void OverlayState_ReturnsEveryNamedOverlay()
    {
        state.SetEnabled(SandboxDebugOverlay.ChunkBorders, true);
        state.SetEnabled(SandboxDebugOverlay.DirtyFlags, true);

        string response = CallTool("debug_overlay_state", "{}");

        StringAssert.Contains("\\\"chunk_borders\\\":true", response);
        StringAssert.Contains("\\\"tile_solidity\\\":false", response);
        StringAssert.Contains("\\\"light_heatmap\\\":false", response);
        StringAssert.Contains("\\\"fluid\\\":false", response);
        StringAssert.Contains("\\\"collider_rebuilds\\\":false", response);
        StringAssert.Contains("\\\"dirty_flags\\\":true", response);
    }

    [Test]
    public void OverlaySet_UnknownName_ReturnsToolErrorWithoutMutation()
    {
        string response = CallTool("debug_overlay_set", "{\"overlay\":\"network\"}");

        StringAssert.Contains("\"isError\":true", response);
        StringAssert.Contains("Unknown overlay", response);
        Assert.IsFalse(state.AnyEnabled);
    }

    [Test]
    public void OverlaySet_NonCanonicalName_ReturnsToolErrorWithoutMutation()
    {
        string response = CallTool("debug_overlay_set", "{\"overlay\":\"CHUNK_BORDERS\",\"enabled\":true}");

        StringAssert.Contains("\"isError\":true", response);
        Assert.IsFalse(state.AnyEnabled);
    }

    [Test]
    public void OverlaySet_NonBooleanEnabled_ReturnsToolErrorWithoutMutation()
    {
        string response = CallTool("debug_overlay_set", "{\"overlay\":\"fluid\",\"enabled\":\"true\"}");

        StringAssert.Contains("\"isError\":true", response);
        StringAssert.Contains("enabled must be a boolean", response);
        Assert.IsFalse(state.AnyEnabled);
    }

    [Test]
    public void OverlaySet_UnexpectedArgument_ReturnsToolErrorWithoutMutation()
    {
        string response = CallTool(
            "debug_overlay_set",
            "{\"overlay\":\"fluid\",\"enabled\":true,\"extra\":\"ignored\"}");

        StringAssert.Contains("\"isError\":true", response);
        StringAssert.Contains("Unexpected argument: extra", response);
        Assert.IsFalse(state.AnyEnabled);
    }

    [Test]
    public void OverlayState_UnexpectedArgument_ReturnsToolError()
    {
        string response = CallTool("debug_overlay_state", "{\"extra\":true}");

        StringAssert.Contains("\"isError\":true", response);
        StringAssert.Contains("Unexpected argument: extra", response);
    }

    private string CallTool(string name, string argumentsJson)
    {
        string request =
            "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/call\",\"params\":{\"name\":\"" +
            name + "\",\"arguments\":" + argumentsJson + "}}";
        return dispatcher.HandleMessage(request).ResponseJson;
    }
}
