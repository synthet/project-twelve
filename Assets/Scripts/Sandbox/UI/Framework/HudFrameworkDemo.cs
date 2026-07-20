using ProjectTwelve.Sandbox.Inventory;
using ProjectTwelve.Sandbox.Registry;
using ProjectTwelve.UI;
using ProjectTwelve.UI.Controls;
using ProjectTwelve.UI.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectTwelve.Sandbox.UI
{
    /// <summary>
    /// Minimal vertical slice exercising the HUD framework end-to-end: a framed window with a scrollable,
    /// configurable inventory grid bound to a real <see cref="SandboxInventory"/> through the view-model /
    /// command-handler boundary, a global UI-scale stepper, item tooltips, and a modal confirmation dialog
    /// that blocks gameplay input and traps focus. Existing HUD screens are untouched — this is a demo of
    /// the reusable pieces, not a replacement.
    /// </summary>
    [RequireComponent(typeof(HudRoot))]
    public sealed class HudFrameworkDemo : MonoBehaviour
    {
        [SerializeField] private int gridColumns = 5;

        private HudRoot hudRoot;
        private UiScaleController scaleController;
        private SandboxInventory inventory;
        private SandboxInventoryViewModel viewModel;
        private SandboxInventoryCommandHandler commandHandler;
        private InventoryGridView grid;
        private UiModalDialog confirmDialog;
        private UiLabel scaleLabel;
        private int selectedSlot = -1;

        private void Awake()
        {
            hudRoot = GetComponent<HudRoot>();
            scaleController = GetComponent<UiScaleController>();

            inventory = SandboxInventory.CreatePrototypeLoadout(SandboxRegistries.Items);
            viewModel = new SandboxInventoryViewModel(inventory);
            commandHandler = new SandboxInventoryCommandHandler(inventory);

            BuildWindow();
            BuildConfirmDialog();

            hudRoot.BindTree(transform);
            if (scaleController != null)
            {
                scaleController.ScaleChanged += OnScaleChanged;
                OnScaleChanged(scaleController.AppliedScale);
            }
        }

        private void OnDestroy()
        {
            viewModel?.Dispose();
            if (scaleController != null)
            {
                scaleController.ScaleChanged -= OnScaleChanged;
            }
        }

        private void BuildWindow()
        {
            RectTransform layer = hudRoot.GetLayer(UiLayer.Windows);

            Image windowImage = UiFactory.CreateImage("InventoryWindow", layer, null, Color.white);
            UiFactory.SetRect(windowImage.rectTransform, new Vector2(24f, 0f), new Vector2(300f, 320f),
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));
            windowImage.gameObject.AddComponent<UiFramedPanel>();

            Text title = UiFactory.CreateText("Title", windowImage.rectTransform, "Inventory",
                null, 20, TextAnchor.UpperLeft, Color.white);
            UiFactory.SetRect(title.rectTransform, new Vector2(16f, -12f), new Vector2(260f, 26f),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            title.gameObject.AddComponent<UiLabel>();

            // Scrollable region holding the grid.
            ScrollRect scrollRect = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect)).GetComponent<ScrollRect>();
            RectTransform scrollTransform = scrollRect.GetComponent<RectTransform>();
            scrollTransform.SetParent(windowImage.rectTransform, false);
            UiFactory.SetRect(scrollTransform, new Vector2(16f, -48f), new Vector2(268f, 210f),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            UiScrollView scrollView = scrollRect.gameObject.AddComponent<UiScrollView>();
            scrollView.EnsureStructure();

            GameObject gridGo = new GameObject("InventoryGrid", typeof(RectTransform));
            gridGo.layer = UiFactory.UiGameObjectLayer;
            RectTransform gridRect = gridGo.GetComponent<RectTransform>();
            gridRect.SetParent(scrollView.Content, false);
            gridRect.anchorMin = new Vector2(0f, 1f);
            gridRect.anchorMax = new Vector2(0f, 1f);
            gridRect.pivot = new Vector2(0f, 1f);
            gridRect.anchoredPosition = Vector2.zero;
            grid = gridGo.AddComponent<InventoryGridView>();
            grid.SlotSelected += OnSlotSelected;
            grid.Initialize(viewModel, commandHandler, hudRoot.ThemeProvider, hudRoot.TooltipService,
                new InventoryGridConfig { columns = gridColumns, slotSize = 48f, spacing = 4f });

            BuildFooter(windowImage.rectTransform);
        }

        private void BuildFooter(RectTransform window)
        {
            UiButton minus = CreateTextButton("ScaleMinus", "UI -", new Vector2(16f, 14f), window);
            minus.Clicked += () => AdjustScale(-1);
            UiButton plus = CreateTextButton("ScalePlus", "UI +", new Vector2(88f, 14f), window);
            plus.Clicked += () => AdjustScale(1);

            Text scaleText = UiFactory.CreateText("ScaleValue", window, "Scale x1",
                null, 15, TextAnchor.MiddleLeft, Color.white);
            UiFactory.SetRect(scaleText.rectTransform, new Vector2(150f, 14f), new Vector2(90f, 28f),
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
            scaleLabel = scaleText.gameObject.AddComponent<UiLabel>();

            UiButton clear = CreateTextButton("ClearSlot", "Clear", new Vector2(232f, 14f), window);
            clear.Clicked += OpenClearConfirm;
        }

        private void BuildConfirmDialog()
        {
            RectTransform modalLayer = hudRoot.GetLayer(UiLayer.Modal);
            GameObject dialogGo = new GameObject("ConfirmDialog", typeof(RectTransform));
            dialogGo.layer = UiFactory.UiGameObjectLayer;
            RectTransform dialogRect = dialogGo.GetComponent<RectTransform>();
            dialogRect.SetParent(modalLayer, false);
            UiFactory.SetStretch(dialogRect, 0f, 0f, 0f, 0f);

            confirmDialog = dialogGo.AddComponent<UiModalDialog>();
            confirmDialog.Build();
            confirmDialog.SetFocusController(hudRoot.FocusController);
            confirmDialog.Confirmed += OnClearConfirmed;
            confirmDialog.Confirmed += () => hudRoot.ModalStack.Remove(confirmDialog);
            confirmDialog.Cancelled += () => hudRoot.ModalStack.Remove(confirmDialog);
        }

        private void OpenClearConfirm()
        {
            if (selectedSlot < 0)
            {
                return;
            }

            confirmDialog.Configure("Clear slot?", $"Remove the item in slot {selectedSlot + 1}?");
            hudRoot.ModalStack.Push(confirmDialog);
        }

        private void OnClearConfirmed()
        {
            if (selectedSlot >= 0)
            {
                commandHandler.TryClear(selectedSlot);
            }
        }

        private void OnSlotSelected(int index)
        {
            selectedSlot = index;
        }

        private void AdjustScale(int delta)
        {
            if (scaleController == null)
            {
                return;
            }

            int next = Mathf.Max(1, scaleController.AppliedScale + delta);
            scaleController.SetUserScale(next);
        }

        private void OnScaleChanged(int scale)
        {
            if (scaleLabel != null)
            {
                scaleLabel.Value = $"Scale x{scale}";
            }
        }

        private UiButton CreateTextButton(string name, string caption, Vector2 position, Transform parent)
        {
            Image image = UiFactory.CreateImage(name, parent, null, Color.white, raycastTarget: true);
            UiFactory.SetRect(image.rectTransform, position, new Vector2(64f, 28f),
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
            UiButton button = image.gameObject.AddComponent<UiButton>();

            Text text = UiFactory.CreateText("Label", image.rectTransform, caption,
                null, 15, TextAnchor.MiddleCenter, Color.white);
            UiFactory.SetStretch(text.rectTransform, 2f, 2f, 2f, 2f);
            text.gameObject.AddComponent<UiLabel>();
            return button;
        }
    }
}
