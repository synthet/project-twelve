using UnityEngine;
using UnityEngine.UI;
using ProjectTwelve.UI.Controls;
using ProjectTwelve.UI.Theme;

namespace ProjectTwelve.UI
{
    /// <summary>
    /// Root of the framework's UI. Owns the layer roots (so every screen parents into an explicit, ordered
    /// band instead of juggling per-prefab sorting constants), the theme provider, the modal stack, the
    /// focus controller, and the shared tooltip service. Binds all theme consumers and tooltip targets in
    /// its subtree once, event-driven — no per-frame walk.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public sealed class HudRoot : MonoBehaviour
    {
        [SerializeField] private UiTheme theme;

        private readonly RectTransform[] layerRoots = new RectTransform[UiLayers.Count];

        private UiThemeProvider themeProvider;
        private ModalStack modalStack;
        private UiFocusController focusController;
        private TooltipService tooltipService;

        public UiThemeProvider ThemeProvider => themeProvider;
        public ModalStack ModalStack => modalStack;
        public UiFocusController FocusController => focusController;
        public TooltipService TooltipService => tooltipService;

        /// <summary>Returns the root transform for a UI layer band; screens parent their content here.</summary>
        public RectTransform GetLayer(UiLayer layer)
        {
            EnsureBuilt();
            return layerRoots[(int)layer];
        }

        /// <summary>Swaps the active theme at runtime; all bound controls re-skin.</summary>
        public void SetTheme(UiTheme next)
        {
            EnsureBuilt();
            themeProvider.SetTheme(next);
        }

        /// <summary>
        /// Binds every theme consumer and tooltip target under <paramref name="root"/> to this HUD's shared
        /// services. Call after instantiating a screen so its controls pick up the theme and tooltip service.
        /// </summary>
        public void BindTree(Transform root)
        {
            EnsureBuilt();

            IUiThemeConsumer[] consumers = root.GetComponentsInChildren<IUiThemeConsumer>(true);
            for (int i = 0; i < consumers.Length; i++)
            {
                consumers[i].Bind(themeProvider);
            }

            UiTooltipTarget[] targets = root.GetComponentsInChildren<UiTooltipTarget>(true);
            for (int i = 0; i < targets.Length; i++)
            {
                targets[i].Bind(tooltipService);
            }

            UiModalDialog[] dialogs = root.GetComponentsInChildren<UiModalDialog>(true);
            for (int i = 0; i < dialogs.Length; i++)
            {
                dialogs[i].SetFocusController(focusController);
            }
        }

        private void Awake()
        {
            EnsureBuilt();
        }

        private void EnsureBuilt()
        {
            if (themeProvider != null)
            {
                return;
            }

            themeProvider = new UiThemeProvider(theme);
            modalStack = new ModalStack();
            focusController = new UiFocusController();

            for (int i = 0; i < UiLayers.Count; i++)
            {
                RectTransform root = UiFactory.CreateRect(((UiLayer)i).ToString() + "Layer", transform);
                UiFactory.SetStretch(root, 0f, 0f, 0f, 0f);
                layerRoots[i] = root;
            }

            GameObject serviceGo = new GameObject("TooltipService");
            serviceGo.transform.SetParent(transform, false);
            tooltipService = serviceGo.AddComponent<TooltipService>();
            tooltipService.Initialize(layerRoots[(int)UiLayer.Tooltips], themeProvider);
        }
    }
}
