using NUnit.Framework;
using ProjectTwelve.RuntimeMcp;

public sealed class RuntimeMcpDispatcherTests
{
    [Test]
    public void HandleMessage_Initialize_ReturnsServerInfoAndToolsCapability()
    {
        McpDispatcher dispatcher = new McpDispatcher();
        string request = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{}}";

        McpHandleResult result = dispatcher.HandleMessage(request);

        Assert.IsFalse(result.IsNotification);
        StringAssert.Contains("\"jsonrpc\":\"2.0\"", result.ResponseJson);
        StringAssert.Contains("\"id\":1", result.ResponseJson);
        StringAssert.Contains("\"name\":\"project-twelve-ingame-mcp\"", result.ResponseJson);
        StringAssert.Contains("\"tools\":", result.ResponseJson);
    }

    [Test]
    public void HandleMessage_NotificationsInitialized_IsNotification()
    {
        McpDispatcher dispatcher = new McpDispatcher();
        string request = "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\",\"params\":{}}";

        McpHandleResult result = dispatcher.HandleMessage(request);

        Assert.IsTrue(result.IsNotification);
        Assert.IsNull(result.ResponseJson);
    }

    [Test]
    public void HandleMessage_ToolsList_ReturnsRegisteredTools()
    {
        McpDispatcher dispatcher = new McpDispatcher();
        dispatcher.RegisterTool(McpTool.CreateEchoToolForTests());

        string request = "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/list\",\"params\":{}}";
        McpHandleResult result = dispatcher.HandleMessage(request);

        StringAssert.Contains("\"name\":\"echo\"", result.ResponseJson);
        StringAssert.Contains("\"description\":\"Echoes a value.\"", result.ResponseJson);
    }

    [Test]
    public void HandleMessage_ToolsCall_ReturnsToolPayload()
    {
        McpDispatcher dispatcher = new McpDispatcher();
        dispatcher.RegisterTool(McpTool.CreateEchoToolForTests());

        string request =
            "{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"tools/call\",\"params\":{\"name\":\"echo\",\"arguments\":{\"value\":\"hello\"}}}";
        McpHandleResult result = dispatcher.HandleMessage(request);

        StringAssert.Contains("\"isError\":false", result.ResponseJson);
        StringAssert.Contains("\"value\":\"hello\"", result.ResponseJson);
    }

    [Test]
    public void HandleMessage_ToolsCall_UnknownTool_ReturnsToolError()
    {
        McpDispatcher dispatcher = new McpDispatcher();
        string request =
            "{\"jsonrpc\":\"2.0\",\"id\":4,\"method\":\"tools/call\",\"params\":{\"name\":\"missing\",\"arguments\":{}}}";
        McpHandleResult result = dispatcher.HandleMessage(request);

        StringAssert.Contains("\"isError\":true", result.ResponseJson);
        StringAssert.Contains("Unknown tool: missing", result.ResponseJson);
    }

    [Test]
    public void HandleMessage_UnknownMethod_ReturnsMethodNotFoundError()
    {
        McpDispatcher dispatcher = new McpDispatcher();
        string request = "{\"jsonrpc\":\"2.0\",\"id\":5,\"method\":\"does/not/exist\",\"params\":{}}";
        McpHandleResult result = dispatcher.HandleMessage(request);

        StringAssert.Contains("\"code\":-32601", result.ResponseJson);
    }

    [Test]
    public void RequiresMainThread_OnlyToolsCallNeedsMainThread()
    {
        Assert.IsTrue(McpDispatcher.RequiresMainThread("tools/call"));
        Assert.IsFalse(McpDispatcher.RequiresMainThread("initialize"));
        Assert.IsFalse(McpDispatcher.RequiresMainThread("tools/list"));
    }

    [Test]
    public void TryParseRequestMethod_ParsesSingleObjectAndBatch()
    {
        Assert.IsTrue(McpDispatcher.TryParseRequestMethod(
            "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{}}",
            out bool isNotification,
            out string method));
        Assert.IsFalse(isNotification);
        Assert.AreEqual("initialize", method);

        Assert.IsTrue(McpDispatcher.TryParseRequestMethod(
            "[{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\",\"params\":{}}]",
            out isNotification,
            out method));
        Assert.IsTrue(isNotification);
        Assert.AreEqual("notifications/initialized", method);
    }
}
