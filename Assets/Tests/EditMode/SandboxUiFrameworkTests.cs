using System.Collections.Generic;
using NUnit.Framework;
using ProjectTwelve.Sandbox.Inventory;
using ProjectTwelve.Sandbox.Registry;
using UnityEngine;

public sealed class SandboxUiFrameworkTests
{
    [TestCase(1920f, 1080f, 1, 0f, 1f)]
    [TestCase(1920f, 1080f, 2, 0f, 1.5f)]
    [TestCase(3840f, 2160f, 2, 2.5f, 2.5f)]
    [TestCase(1024f, 576f, 2, 0f, 1f)]
    public void ScalePolicy_UsesOnlyThemeSupportedSteps(
        float screenWidth,
        float screenHeight,
        int pixelModule,
        float requestedScale,
        float expected)
    {
        Assert.AreEqual(expected, SandboxUiScalePolicy.ComputeScale(
            screenWidth,
            screenHeight,
            new Vector2(1280f, 720f),
            pixelModule,
            requestedScale), 0.001f);
    }

    [Test]
    public void Layout_ClampsPanelSizeAndPositionInsideSafeArea()
    {
        Rect safeArea = new Rect(0f, 0f, 100f, 80f);
        Rect desired = new Rect(90f, 70f, 50f, 40f);

        Rect result = SandboxUiLayout.ClampPanel(
            desired,
            safeArea,
            new Vector2(20f, 20f),
            new Vector2(90f, 70f));

        Assert.AreEqual(new Rect(50f, 40f, 50f, 40f), result);
        Assert.AreEqual(new Vector2(40f, 70f), SandboxUiLayout.ClampSize(
            new Vector2(10f, 200f),
            new Vector2(40f, 30f),
            new Vector2(100f, 70f)));
    }

    [TestCase(33, 0, 1, 38)]
    [TestCase(34, 0, 1, 39)]
    [TestCase(39, 1, 0, 39)]
    [TestCase(0, -1, 0, 0)]
    [TestCase(5, 0, -1, 0)]
    public void GridNavigation_IsDeterministicAtEdges(
        int current,
        int columnDelta,
        int rowDelta,
        int expected)
    {
        Assert.AreEqual(expected, SandboxUiGridNavigation.Move(
            current,
            columnDelta,
            rowDelta,
            columns: 5,
            itemCount: 40));
    }

    [Test]
    public void InventoryAdapter_EmitsOnlyActuallyChangedSlotIndices()
    {
        SandboxInventory inventory = new SandboxInventory(SandboxRegistries.Items);
        using SandboxInventoryViewAdapter adapter = new SandboxInventoryViewAdapter(
            inventory,
            SandboxRegistries.Items);
        List<int> changed = new List<int>();
        adapter.SlotChanged += changed.Add;

        inventory.SetSlot(5, "core:dirt", 3);
        CollectionAssert.AreEqual(new[] { 5 }, changed);
        Assert.AreEqual("core:dirt", adapter.GetSlot(5).ItemId);
        Assert.AreEqual(3, adapter.GetSlot(5).Count);
        Assert.AreEqual(SandboxInventoryConstants.DefaultMaxStack, adapter.GetSlot(5).MaximumCount);

        changed.Clear();
        inventory.SetSlot(5, "core:dirt", 3);
        CollectionAssert.IsEmpty(changed);

        inventory.SetSlot(5, "core:dirt", 2);
        CollectionAssert.AreEqual(new[] { 5 }, changed);
    }

    [Test]
    public void ScreenStack_ModalTrapsFocusAndBlocksGameplay()
    {
        SandboxUiScreenStack stack = new SandboxUiScreenStack();
        object window = new object();
        object modal = new object();

        stack.Push(window, blocksGameplayInput: true, isModal: false);
        stack.Push(modal, blocksGameplayInput: true, isModal: true);

        Assert.IsTrue(stack.GameplayInputBlocked);
        Assert.IsFalse(stack.IsFocusAllowed(window));
        Assert.IsTrue(stack.IsFocusAllowed(modal));
        Assert.AreSame(modal, stack.TopOwner);

        Assert.IsTrue(stack.Pop(modal));
        Assert.IsTrue(stack.IsFocusAllowed(window));
        Assert.IsTrue(stack.GameplayInputBlocked);
        Assert.IsTrue(stack.Pop(window));
        Assert.IsFalse(stack.GameplayInputBlocked);
    }

    [Test]
    public void DragState_RejectedDropRefreshesBothEndpointsAndClearsDrag()
    {
        SandboxUiDragState drag = new SandboxUiDragState();
        RejectingInventoryCommands commands = new RejectingInventoryCommands();
        List<int> refreshes = new List<int>();
        drag.RefreshRequested += refreshes.Add;

        Assert.IsTrue(drag.Begin(2, 4));
        Assert.AreEqual(SandboxInventoryOperationResult.Rejected, drag.Drop(7, commands));

        Assert.IsFalse(drag.IsDragging);
        CollectionAssert.AreEqual(new[] { 2, 7 }, refreshes);
    }

    [Test]
    public void DragState_CancelRefreshesSourceAndClearsDrag()
    {
        SandboxUiDragState drag = new SandboxUiDragState();
        List<int> refreshes = new List<int>();
        drag.RefreshRequested += refreshes.Add;

        Assert.IsTrue(drag.Begin(3, 1));
        Assert.IsTrue(drag.Cancel());

        Assert.IsFalse(drag.IsDragging);
        CollectionAssert.AreEqual(new[] { 3 }, refreshes);
    }

    [TestCase(1.34f, 1f, 0.35f, false)]
    [TestCase(1.35f, 1f, 0.35f, true)]
    public void TooltipTiming_UsesConfiguredDelay(float now, float enteredAt, float delay, bool expected)
    {
        Assert.AreEqual(expected, SandboxUiTooltipTiming.ShouldShow(now, enteredAt, delay));
    }

    [Test]
    public void ThemeFallback_HasUsableTokensAndNoRequiredAssetDependency()
    {
        SandboxUiTheme theme = SandboxUiTheme.CreateRuntimeFallback();
        try
        {
            Assert.AreEqual(4f, theme.SpacingUnit);
            Assert.AreEqual(48f, theme.InventorySlotSize);
            Assert.AreEqual(1, theme.PixelModule);
            Assert.Greater(theme.TextColor.a, 0f);
            Assert.Greater(theme.SurfaceColor.a, 0f);
        }
        finally
        {
            Object.DestroyImmediate(theme);
        }
    }

    private sealed class RejectingInventoryCommands : ISandboxInventoryCommandHandler
    {
        public SandboxInventoryOperationResult TryMove(int sourceIndex, int destinationIndex, int amount)
        {
            return SandboxInventoryOperationResult.Rejected;
        }
    }
}
