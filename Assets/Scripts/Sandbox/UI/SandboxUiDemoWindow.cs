using System;
using System.Collections.Generic;
using ProjectTwelve.Sandbox.Inventory;
using ProjectTwelve.Sandbox.Registry;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SandboxUiDemoWindow : MonoBehaviour, ISandboxUiScreen
{
    private const int Columns = 5;

    private readonly List<SandboxUiItemSlot> slots = new List<SandboxUiItemSlot>();
    private SandboxUiRoot root;
    private RectTransform view;
    private SandboxUiPanel panel;
    private SandboxUiLabel title;
    private SandboxUiLabel hint;
    private SandboxUiButton closeButton;
    private SandboxUiButton themeButton;
    private SandboxUiButton scaleDownButton;
    private SandboxUiButton scaleAutoButton;
    private SandboxUiButton scaleUpButton;
    private SandboxUiButton aboutButton;
    private SandboxUiScrollView scrollView;
    private SandboxUiTooltip tooltip;
    private SandboxUiModalDialog modal;
    private SandboxInventoryViewAdapter adapter;
    private SandboxHudController hud;
    private SandboxHudPixelPerfectScaler scaler;

    public GameObject View => view != null ? view.gameObject : null;
    public Selectable InitialFocus => slots.Count > 0 && slots[0].Button.interactable
        ? slots[0].Button
        : closeButton != null ? closeButton.Button : null;
    public bool BlocksGameplayInput => true;
    public bool IsModal => false;

    public void Initialize(SandboxUiRoot uiRoot)
    {
        if (view != null)
        {
            return;
        }

        root = uiRoot;
        hud = GetComponent<SandboxHudController>();
        scaler = GetComponent<SandboxHudPixelPerfectScaler>();
        BuildView();
        BindInventory();
        root.ThemeChanged += ApplyTheme;
    }

    private void OnDestroy()
    {
        if (root != null)
        {
            root.ThemeChanged -= ApplyTheme;
        }

        if (adapter != null)
        {
            adapter.SlotChanged -= RefreshSlot;
            adapter.Dispose();
        }
    }

    public void Toggle()
    {
        if (view.gameObject.activeSelf)
        {
            root.HideScreen(this);
        }
        else
        {
            root.ShowScreen(this);
        }
    }

    private void BuildView()
    {
        tooltip = gameObject.AddComponent<SandboxUiTooltip>();
        tooltip.Initialize(root);
        modal = gameObject.AddComponent<SandboxUiModalDialog>();
        modal.Initialize(root);

        panel = SandboxUiBuilder.CreatePanel("InventoryDemoWindow", root.WindowLayer, root.Theme);
        view = panel.transform as RectTransform;
        Vector2 windowSize = SandboxUiLayout.ClampSize(
            new Vector2(520f, 460f),
            new Vector2(360f, 320f),
            root.SafeAreaLayer.rect.size);
        SandboxUiBuilder.SetRect(
            view,
            Vector2.zero,
            windowSize,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f));

        title = SandboxUiBuilder.CreateLabel(
            "Title",
            view,
            root.Theme,
            "Backpack",
            20,
            TextAnchor.MiddleLeft);
        SandboxUiBuilder.SetRect(
            title.transform as RectTransform,
            new Vector2(16f, -12f),
            new Vector2(330f, 34f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f));

        closeButton = SandboxUiBuilder.CreateButton("Close", view, root.Theme, "X", () => root.HideScreen(this));
        SandboxUiBuilder.SetRect(
            closeButton.transform as RectTransform,
            new Vector2(-12f, -12f),
            new Vector2(36f, 32f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f));

        hint = SandboxUiBuilder.CreateLabel(
            "Hint",
            view,
            root.Theme,
            "I: inventory   Esc: close   Hover or focus a slot for details",
            12,
            TextAnchor.MiddleLeft,
            muted: true);
        SandboxUiBuilder.SetRect(
            hint.transform as RectTransform,
            new Vector2(16f, -50f),
            new Vector2(488f, 24f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f));

        SandboxUiScrollViewParts scrollParts = SandboxUiBuilder.CreateScrollView(
            "InventoryScrollView",
            view,
            root.Theme);
        scrollView = scrollParts.View;
        RectTransform scrollRect = scrollView.transform as RectTransform;
        SandboxUiBuilder.SetStretch(scrollRect, 16f, 16f, 118f, 72f);

        GridLayoutGroup grid = scrollParts.Content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(root.Theme.InventorySlotSize, root.Theme.InventorySlotSize);
        grid.spacing = new Vector2(root.Theme.SpacingUnit, root.Theme.SpacingUnit);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Columns;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        int rows = Mathf.CeilToInt(SandboxInventoryConstants.SlotCount / (float)Columns);
        scrollParts.Content.sizeDelta = new Vector2(
            0f,
            rows * root.Theme.InventorySlotSize + (rows - 1) * root.Theme.SpacingUnit);

        for (int i = 0; i < SandboxInventoryConstants.SlotCount; i++)
        {
            SandboxUiItemSlot slot = SandboxUiBuilder.CreateItemSlot(
                $"Slot{i + 1}",
                scrollParts.Content,
                root.Theme,
                i,
                tooltip);
            slots.Add(slot);
        }

        ConfigureSlotNavigation();

        scaleDownButton = SandboxUiBuilder.CreateButton("ScaleDown", view, root.Theme, "-", () => AdjustScale(-1));
        SandboxUiBuilder.SetRect(
            scaleDownButton.transform as RectTransform,
            new Vector2(16f, 16f),
            new Vector2(36f, 32f),
            Vector2.zero,
            Vector2.zero,
            Vector2.zero);

        scaleAutoButton = SandboxUiBuilder.CreateButton("ScaleAuto", view, root.Theme, "Scale Auto", UseAutomaticScale);
        SandboxUiBuilder.SetRect(
            scaleAutoButton.transform as RectTransform,
            new Vector2(58f, 16f),
            new Vector2(112f, 32f),
            Vector2.zero,
            Vector2.zero,
            Vector2.zero);

        scaleUpButton = SandboxUiBuilder.CreateButton("ScaleUp", view, root.Theme, "+", () => AdjustScale(1));
        SandboxUiBuilder.SetRect(
            scaleUpButton.transform as RectTransform,
            new Vector2(176f, 16f),
            new Vector2(36f, 32f),
            Vector2.zero,
            Vector2.zero,
            Vector2.zero);

        themeButton = SandboxUiBuilder.CreateButton("Theme", view, root.Theme, root.ThemeName, ToggleTheme);
        SandboxUiBuilder.SetRect(
            themeButton.transform as RectTransform,
            new Vector2(220f, 16f),
            new Vector2(140f, 32f),
            Vector2.zero,
            Vector2.zero,
            Vector2.zero);

        aboutButton = SandboxUiBuilder.CreateButton("About", view, root.Theme, "Modal", OpenModal);
        SandboxUiBuilder.SetRect(
            aboutButton.transform as RectTransform,
            new Vector2(-16f, 16f),
            new Vector2(112f, 32f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f));

        view.gameObject.SetActive(false);
        RefreshScaleLabel();
    }

    private void BindInventory()
    {
        SandboxPlayerController player = FindAnyObjectByType<SandboxPlayerController>();
        if (player?.Inventory == null)
        {
            return;
        }

        adapter = new SandboxInventoryViewAdapter(player.Inventory, SandboxRegistries.Items);
        adapter.SlotChanged += RefreshSlot;
        for (int i = 0; i < slots.Count; i++)
        {
            RefreshSlot(i);
        }
    }

    private void RefreshSlot(int index)
    {
        if (adapter == null || index < 0 || index >= slots.Count)
        {
            return;
        }

        SandboxInventorySlotViewData data = adapter.GetSlot(index);
        Sprite icon = hud != null ? hud.ResolveInventoryIcon(data.ItemId) : null;
        slots[index].Refresh(data, icon);
    }

    private void ConfigureSlotNavigation()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            Navigation navigation = new Navigation { mode = Navigation.Mode.Explicit };
            navigation.selectOnLeft = SlotSelectable(SandboxUiGridNavigation.Move(i, -1, 0, Columns, slots.Count));
            navigation.selectOnRight = SlotSelectable(SandboxUiGridNavigation.Move(i, 1, 0, Columns, slots.Count));
            navigation.selectOnUp = SlotSelectable(SandboxUiGridNavigation.Move(i, 0, -1, Columns, slots.Count));
            navigation.selectOnDown = SlotSelectable(SandboxUiGridNavigation.Move(i, 0, 1, Columns, slots.Count));
            slots[i].Button.navigation = navigation;
        }
    }

    private Selectable SlotSelectable(int index)
    {
        return index >= 0 && index < slots.Count ? slots[index].Button : null;
    }

    private void AdjustScale(int direction)
    {
        scaler?.AdjustRequestedScale(direction);
        RefreshScaleLabel();
    }

    private void UseAutomaticScale()
    {
        scaler?.UseAutomaticScale();
        RefreshScaleLabel();
    }

    private void RefreshScaleLabel()
    {
        if (scaleAutoButton?.Label != null)
        {
            float value = scaler != null ? scaler.CurrentScale : 1f;
            scaleAutoButton.Label.text = $"Scale {value:0.#}x";
        }
    }

    private void ToggleTheme()
    {
        root.ToggleTheme();
        themeButton.Label.text = root.ThemeName;
    }

    private void OpenModal()
    {
        modal.Open(
            "Framework Ready",
            "This modal traps UI focus and blocks local gameplay input until dismissed.");
    }

    private void ApplyTheme()
    {
        panel.ApplyTheme(root.Theme);
        title.ApplyTheme(root.Theme);
        hint.ApplyTheme(root.Theme);
        closeButton.ApplyTheme(root.Theme);
        themeButton.ApplyTheme(root.Theme);
        scaleDownButton.ApplyTheme(root.Theme);
        scaleAutoButton.ApplyTheme(root.Theme);
        scaleUpButton.ApplyTheme(root.Theme);
        aboutButton.ApplyTheme(root.Theme);
        scrollView.ApplyTheme(root.Theme);
        themeButton.Label.text = root.ThemeName;
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].ApplyTheme(root.Theme);
        }
    }
}
