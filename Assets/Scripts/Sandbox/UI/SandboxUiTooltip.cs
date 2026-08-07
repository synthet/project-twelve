using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SandboxUiTooltip : MonoBehaviour
{
    private SandboxUiRoot root;
    private SandboxUiPanel panel;
    private RectTransform panelRect;
    private SandboxUiLabel title;
    private SandboxUiLabel body;
    private object pendingOwner;
    private RectTransform pendingTarget;
    private string pendingTitle;
    private string pendingBody;
    private float enteredAt;

    public bool Visible => panelRect != null && panelRect.gameObject.activeSelf;

    public void Initialize(SandboxUiRoot uiRoot)
    {
        if (panelRect != null)
        {
            return;
        }

        root = uiRoot;
        panel = SandboxUiBuilder.CreatePanel("SharedTooltip", root.TooltipLayer, root.Theme);
        panelRect = panel.transform as RectTransform;
        SandboxUiBuilder.SetRect(
            panelRect,
            Vector2.zero,
            new Vector2(236f, 72f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 1f));
        panel.GetComponent<Image>().raycastTarget = false;

        title = SandboxUiBuilder.CreateLabel(
            "Title",
            panelRect,
            root.Theme,
            string.Empty,
            15,
            TextAnchor.UpperLeft);
        SandboxUiBuilder.SetRect(
            title.transform as RectTransform,
            new Vector2(8f, -6f),
            new Vector2(220f, 22f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f));

        body = SandboxUiBuilder.CreateLabel(
            "Body",
            panelRect,
            root.Theme,
            string.Empty,
            12,
            TextAnchor.UpperLeft,
            muted: true);
        SandboxUiBuilder.SetRect(
            body.transform as RectTransform,
            new Vector2(8f, -30f),
            new Vector2(220f, 34f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f));

        panelRect.gameObject.SetActive(false);
        root.ThemeChanged += ApplyTheme;
    }

    private void Update()
    {
        if (pendingOwner == null || Visible ||
            !SandboxUiTooltipTiming.ShouldShow(Time.unscaledTime, enteredAt, root.Theme.TooltipDelaySeconds))
        {
            return;
        }

        ShowPending();
    }

    private void OnDestroy()
    {
        if (root != null)
        {
            root.ThemeChanged -= ApplyTheme;
        }
    }

    public void Request(
        object owner,
        string heading,
        string description,
        RectTransform target,
        bool immediate)
    {
        pendingOwner = owner;
        pendingTitle = heading;
        pendingBody = description;
        pendingTarget = target;
        enteredAt = immediate ? Time.unscaledTime - root.Theme.TooltipDelaySeconds : Time.unscaledTime;
        panelRect.gameObject.SetActive(false);
        if (immediate)
        {
            ShowPending();
        }
    }

    public void Cancel(object owner)
    {
        if (!ReferenceEquals(owner, pendingOwner))
        {
            return;
        }

        pendingOwner = null;
        pendingTarget = null;
        panelRect.gameObject.SetActive(false);
    }

    private void ShowPending()
    {
        if (pendingOwner == null || pendingTarget == null)
        {
            return;
        }

        title.Text.text = pendingTitle;
        body.Text.text = pendingBody;
        PositionNear(pendingTarget);
        panelRect.gameObject.SetActive(true);
        panelRect.SetAsLastSibling();
    }

    private void PositionNear(RectTransform target)
    {
        RectTransform parent = panelRect.parent as RectTransform;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, target.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, null, out Vector2 local);
        local += new Vector2(18f, 42f);
        Rect bounds = parent.rect;
        float x = Mathf.Clamp(local.x, bounds.xMin, bounds.xMax - panelRect.rect.width);
        float y = Mathf.Clamp(local.y, bounds.yMin + panelRect.rect.height, bounds.yMax);
        panelRect.anchoredPosition = new Vector2(x, y);
    }

    private void ApplyTheme()
    {
        panel.ApplyTheme(root.Theme);
        title.ApplyTheme(root.Theme);
        body.ApplyTheme(root.Theme);
    }
}
