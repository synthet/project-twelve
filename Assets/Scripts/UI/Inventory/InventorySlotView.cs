using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ProjectTwelve.UI.Controls;
using ProjectTwelve.UI.Theme;

namespace ProjectTwelve.UI.Inventory
{
    /// <summary>
    /// Presentation of a single inventory slot. Derives from <see cref="Selectable"/> so it participates in
    /// uGUI directional navigation (deterministic controller/keyboard traversal), and raises interaction
    /// events (click, drag begin/end, drop) that the owning grid routes to the command handler. It holds no
    /// inventory rules — it only draws <see cref="InventorySlotViewData"/> and reports intent.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class InventorySlotView : Selectable, IUiThemeConsumer,
        IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        private UiThemeProvider provider;
        private Image icon;
        private Image selection;
        private Text count;
        private UiLabel countLabel;
        private UiTooltipTarget tooltip;
        private CanvasGroup canvasGroup;
        private InventorySlotViewData data;

        /// <summary>This slot's index within the grid model.</summary>
        public int Index { get; private set; }

        /// <summary>Current view data bound to this slot.</summary>
        public InventorySlotViewData Data => data;

        /// <summary>The tooltip target, so the grid can bind it to the shared tooltip service.</summary>
        public UiTooltipTarget Tooltip => tooltip;

        public event Action<int> Clicked;
        public event Action<int> DragBegan;
        public event Action<int> DragEnded;

        /// <summary>Raised on a drop: (sourceIndex, destinationIndex).</summary>
        public event Action<int, int> Dropped;

        public void Bind(UiThemeProvider themeProvider)
        {
            provider = themeProvider;
            Refresh();
        }

        public void Refresh()
        {
            ApplyVisual();
        }

        /// <summary>Builds the slot sub-tree (background, icon, count, selection, tooltip). Idempotent.</summary>
        public void Build()
        {
            if (icon != null)
            {
                return;
            }

            targetGraphic = GetComponent<Image>();
            targetGraphic.raycastTarget = true;
            transition = Transition.None; // Theme drives slot color; selection uses the overlay image.
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

            icon = UiFactory.CreateImage("Icon", transform, null, Color.white);
            UiFactory.SetRect(icon.rectTransform, Vector2.zero, new Vector2(32f, 32f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            icon.preserveAspect = true;

            selection = UiFactory.CreateImage("Selection", transform, null, Color.white);
            UiFactory.SetStretch(selection.rectTransform, 0f, 0f, 0f, 0f);
            selection.gameObject.SetActive(false);

            count = UiFactory.CreateText("Count", transform, string.Empty, null, 15,
                TextAnchor.LowerRight, Color.white);
            UiFactory.SetStretch(count.rectTransform, 3f, 4f, 3f, 3f);
            countLabel = count.gameObject.AddComponent<UiLabel>();

            tooltip = gameObject.AddComponent<UiTooltipTarget>();
        }

        /// <summary>Binds this pooled slot to a model index and its view data.</summary>
        public void SetData(int index, InventorySlotViewData slotData)
        {
            Index = index;
            data = slotData;
            interactable = !slotData.IsLocked;
            ApplyVisual();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsInteractable() && eventData.button == PointerEventData.InputButton.Left)
            {
                Clicked?.Invoke(Index);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!IsInteractable() || data.IsEmpty)
            {
                return;
            }

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false; // Let the drop target below receive the pointer.
                canvasGroup.alpha = 0.6f;
            }

            DragBegan?.Invoke(Index);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Position feedback is handled by the drag preview layer; nothing per-frame here.
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.alpha = 1f;
            }

            DragEnded?.Invoke(Index);
        }

        public void OnDrop(PointerEventData eventData)
        {
            GameObject dragged = eventData.pointerDrag;
            if (dragged == null)
            {
                return;
            }

            InventorySlotView source = dragged.GetComponent<InventorySlotView>();
            if (source != null && source != this)
            {
                Dropped?.Invoke(source.Index, Index);
            }
        }

        private void ApplyVisual()
        {
            if (icon == null)
            {
                return;
            }

            UiTheme theme = provider != null ? provider.Active : new UiThemeProvider().Active;

            Image bg = targetGraphic as Image;
            if (bg != null)
            {
                Sprite slotSprite = theme.ResolveSprite(UiSpriteRole.Slot);
                bg.sprite = slotSprite;
                bg.type = slotSprite != null ? Image.Type.Sliced : Image.Type.Simple;
                bg.color = data.IsLocked
                    ? theme.ResolveColor(UiColorRole.Disabled)
                    : (slotSprite != null ? Color.white : theme.ResolveColor(UiColorRole.Normal));
            }

            icon.sprite = data.Icon;
            icon.enabled = data.Icon != null;
            icon.color = data.IsUsable ? Color.white : theme.ResolveColor(UiColorRole.Disabled);

            if (count != null)
            {
                count.text = data.Count > 1 ? data.Count.ToString() : string.Empty;
            }

            if (selection != null)
            {
                Sprite selSprite = theme.ResolveSprite(UiSpriteRole.Selection);
                selection.sprite = selSprite;
                selection.type = selSprite != null ? Image.Type.Sliced : Image.Type.Simple;
                selection.color = selSprite != null ? Color.white : theme.ResolveColor(UiColorRole.Selected);
                selection.gameObject.SetActive(data.IsSelected);
            }

            if (tooltip != null)
            {
                tooltip.Content = data.IsEmpty ? string.Empty : ComposeTooltip();
            }
        }

        private string ComposeTooltip()
        {
            if (!string.IsNullOrEmpty(data.TooltipBody))
            {
                return string.IsNullOrEmpty(data.TooltipTitle)
                    ? data.TooltipBody
                    : data.TooltipTitle + "\n" + data.TooltipBody;
            }

            return data.TooltipTitle ?? data.ItemId;
        }
    }
}
