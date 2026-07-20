using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectTwelve.UI.Controls
{
    /// <summary>
    /// Marks a control as having a tooltip. On pointer hover or focus it asks the shared
    /// <see cref="TooltipService"/> to show <see cref="Content"/> after the theme delay, and cancels on
    /// exit/deselect. Content is set from data (never baked into decorative art) and can update at runtime.
    /// </summary>
    public sealed class UiTooltipTarget : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField, TextArea] private string content;

        private TooltipService service;

        /// <summary>The tooltip text. Safe to update at runtime (e.g. item stack changes).</summary>
        public string Content
        {
            get => content;
            set
            {
                content = value;
                if (service != null && !string.IsNullOrEmpty(content))
                {
                    service.Request(this, content);
                }
            }
        }

        /// <summary>Binds this target to the shared tooltip service.</summary>
        public void Bind(TooltipService tooltipService)
        {
            service = tooltipService;
        }

        public void OnPointerEnter(PointerEventData eventData) => RequestShow();
        public void OnPointerExit(PointerEventData eventData) => RequestHide();
        public void OnSelect(BaseEventData eventData) => RequestShow();
        public void OnDeselect(BaseEventData eventData) => RequestHide();

        private void OnDisable() => RequestHide();

        private void RequestShow()
        {
            if (service != null && !string.IsNullOrEmpty(content))
            {
                service.Request(this, content);
            }
        }

        private void RequestHide()
        {
            service?.Cancel(this);
        }
    }
}
