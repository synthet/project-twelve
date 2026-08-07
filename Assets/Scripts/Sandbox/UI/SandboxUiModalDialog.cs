using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SandboxUiModalDialog : MonoBehaviour, ISandboxUiScreen
{
    private SandboxUiRoot root;
    private RectTransform view;
    private SandboxUiPanel dialogPanel;
    private SandboxUiLabel title;
    private SandboxUiLabel body;
    private SandboxUiButton closeButton;

    public GameObject View => view != null ? view.gameObject : null;
    public Selectable InitialFocus => closeButton != null ? closeButton.Button : null;
    public bool BlocksGameplayInput => true;
    public bool IsModal => true;

    public void Initialize(SandboxUiRoot uiRoot)
    {
        if (view != null)
        {
            return;
        }

        root = uiRoot;
        view = SandboxUiBuilder.CreateRect("FrameworkModal", root.ModalLayer);
        SandboxUiBuilder.SetStretch(view, 0f, 0f, 0f, 0f);
        Image blocker = view.gameObject.AddComponent<Image>();
        blocker.color = new Color(0f, 0f, 0f, 0.55f);
        blocker.raycastTarget = true;

        dialogPanel = SandboxUiBuilder.CreatePanel("Dialog", view, root.Theme);
        RectTransform dialogRect = dialogPanel.transform as RectTransform;
        Vector2 dialogSize = SandboxUiLayout.ClampSize(
            new Vector2(380f, 180f),
            new Vector2(280f, 160f),
            root.SafeAreaLayer.rect.size);
        SandboxUiBuilder.SetRect(
            dialogRect,
            Vector2.zero,
            dialogSize,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f));

        title = SandboxUiBuilder.CreateLabel(
            "Title",
            dialogRect,
            root.Theme,
            "Framework",
            18,
            TextAnchor.MiddleCenter);
        SandboxUiBuilder.SetRect(
            title.transform as RectTransform,
            new Vector2(12f, -10f),
            new Vector2(356f, 32f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f));

        body = SandboxUiBuilder.CreateLabel(
            "Body",
            dialogRect,
            root.Theme,
            string.Empty,
            14,
            TextAnchor.UpperCenter);
        SandboxUiBuilder.SetRect(
            body.transform as RectTransform,
            new Vector2(20f, -52f),
            new Vector2(340f, 72f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f));

        closeButton = SandboxUiBuilder.CreateButton("Close", dialogRect, root.Theme, "Close", Close);
        SandboxUiBuilder.SetRect(
            closeButton.transform as RectTransform,
            new Vector2(0f, 14f),
            new Vector2(112f, root.Theme.ButtonHeight),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f));

        view.gameObject.SetActive(false);
        root.ThemeChanged += ApplyTheme;
    }

    private void OnDestroy()
    {
        if (root != null)
        {
            root.ThemeChanged -= ApplyTheme;
        }
    }

    public void Open(string heading, string message)
    {
        title.Text.text = heading;
        body.Text.text = message;
        root.ShowScreen(this);
    }

    public void Close()
    {
        root.HideScreen(this);
    }

    private void ApplyTheme()
    {
        dialogPanel.ApplyTheme(root.Theme);
        title.ApplyTheme(root.Theme);
        body.ApplyTheme(root.Theme);
        closeButton.ApplyTheme(root.Theme);
    }
}
