using System;
using UnityEngine;
using UnityEngine.UI;
using ProjectTwelve.UI.Theme;

namespace ProjectTwelve.UI.Controls
{
    /// <summary>
    /// A modal confirmation dialog. As an <see cref="IUiScreen"/> it reports that it blocks gameplay input
    /// and lives on the <see cref="UiLayer.Modal"/> layer; while shown it traps UI focus inside itself via
    /// the <see cref="UiFocusController"/>, so navigation cannot escape behind it. Raises
    /// <see cref="Confirmed"/>/<see cref="Cancelled"/> and pops itself off the modal stack when answered.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class UiModalDialog : MonoBehaviour, IUiScreen, IUiThemeConsumer
    {
        private UiThemeProvider provider;
        private UiFocusController focus;
        private UiFramedPanel panel;
        private UiLabel titleLabel;
        private UiLabel messageLabel;
        private UiButton confirmButton;
        private UiButton cancelButton;
        private RectTransform selfRect;

        /// <summary>Raised when the confirm action is chosen.</summary>
        public event Action Confirmed;

        /// <summary>Raised when the cancel action (or Esc) is chosen.</summary>
        public event Action Cancelled;

        public bool BlocksGameplayInput => true;
        public UiLayer Layer => UiLayer.Modal;

        /// <summary>The confirm button, exposed so callers can set it as the initial focus.</summary>
        public UiButton ConfirmButton => confirmButton;

        public void Bind(UiThemeProvider themeProvider)
        {
            provider = themeProvider;
            panel?.Bind(provider);
            titleLabel?.Bind(provider);
            messageLabel?.Bind(provider);
            confirmButton?.Bind(provider);
            cancelButton?.Bind(provider);
        }

        public void Refresh()
        {
            panel?.Refresh();
            titleLabel?.Refresh();
            messageLabel?.Refresh();
            confirmButton?.Refresh();
            cancelButton?.Refresh();
        }

        /// <summary>Provides the focus controller used to trap focus while shown.</summary>
        public void SetFocusController(UiFocusController focusController)
        {
            focus = focusController;
        }

        /// <summary>Builds the dialog sub-tree (panel, title, message, confirm/cancel) if not already built.</summary>
        public void Build(string confirmText = "Confirm", string cancelText = "Cancel")
        {
            selfRect = GetComponent<RectTransform>();
            if (panel != null)
            {
                return;
            }

            Image panelImage = UiFactory.CreateImage("Panel", transform, null, Color.white);
            UiFactory.SetRect(panelImage.rectTransform, Vector2.zero, new Vector2(360f, 180f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            panel = panelImage.gameObject.AddComponent<UiFramedPanel>();

            Text title = UiFactory.CreateText("Title", panelImage.rectTransform, "Confirm",
                null, 20, TextAnchor.UpperCenter, Color.white);
            UiFactory.SetRect(title.rectTransform, new Vector2(0f, -16f), new Vector2(320f, 28f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            titleLabel = title.gameObject.AddComponent<UiLabel>();

            Text message = UiFactory.CreateText("Message", panelImage.rectTransform, string.Empty,
                null, 15, TextAnchor.MiddleCenter, Color.white);
            UiFactory.SetRect(message.rectTransform, new Vector2(0f, 4f), new Vector2(320f, 72f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            messageLabel = message.gameObject.AddComponent<UiLabel>();

            confirmButton = CreateButton("Confirm", confirmText, new Vector2(-70f, 20f), panelImage.rectTransform);
            confirmButton.Clicked += OnConfirm;
            cancelButton = CreateButton("Cancel", cancelText, new Vector2(70f, 20f), panelImage.rectTransform);
            cancelButton.Clicked += OnCancel;

            // Wire directional navigation between the two buttons for controller/keyboard.
            Navigation nav = confirmButton.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnRight = cancelButton;
            confirmButton.navigation = nav;
            Navigation cancelNav = cancelButton.navigation;
            cancelNav.mode = Navigation.Mode.Explicit;
            cancelNav.selectOnLeft = confirmButton;
            cancelButton.navigation = cancelNav;

            gameObject.SetActive(false);
        }

        /// <summary>Sets the dialog title and message from data.</summary>
        public void Configure(string title, string message)
        {
            if (titleLabel != null)
            {
                titleLabel.Value = title;
            }

            if (messageLabel != null)
            {
                messageLabel.Value = message;
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (focus != null)
            {
                focus.PushTrap(selfRect != null ? selfRect : GetComponent<RectTransform>());
                if (confirmButton != null)
                {
                    focus.RequestFocus(confirmButton.gameObject);
                }
            }
        }

        public void Hide()
        {
            if (focus != null)
            {
                focus.PopTrap(selfRect != null ? selfRect : GetComponent<RectTransform>());
            }

            gameObject.SetActive(false);
        }

        private void OnConfirm()
        {
            Confirmed?.Invoke();
            Hide();
        }

        private void OnCancel()
        {
            Cancelled?.Invoke();
            Hide();
        }

        private UiButton CreateButton(string name, string caption, Vector2 position, Transform parent)
        {
            Image image = UiFactory.CreateImage(name, parent, null, Color.white, raycastTarget: true);
            UiFactory.SetRect(image.rectTransform, position, new Vector2(120f, 32f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            UiButton button = image.gameObject.AddComponent<UiButton>();

            Text text = UiFactory.CreateText("Label", image.rectTransform, caption,
                null, 15, TextAnchor.MiddleCenter, Color.white);
            UiFactory.SetStretch(text.rectTransform, 4f, 4f, 4f, 4f);
            text.gameObject.AddComponent<UiLabel>();
            return button;
        }
    }
}
