using ProjectTwelve.Sandbox.Debug;
using ProjectTwelve.Sandbox.Registry;
using ProjectTwelve.Visual.AutotileDebug;
using ProjectTwelve.Visual.Tiles;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Play Mode keyboard/mouse input for Visual Override Mode (F3 mode 2 / F8 shortcut).
/// </summary>
[DisallowMultipleComponent]
public sealed class SandboxVisualOverrideInput : MonoBehaviour
{
    private static readonly string[] NotePresets = { string.Empty, "wrong flip", "wrong sprite", "check slope" };

    [SerializeField] private SandboxWorld world;
    [SerializeField] private SandboxPlayerController player;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float editRange = 6f;

    internal Camera TargetCamera => targetCamera;

    private Vector2Int selectedCell;
    private AutotileVisualLayer selectedLayer = AutotileVisualLayer.Ground;
    private string selectedTileset;
    private bool hasSelection;
    private bool pointerInRange;
    private int notePresetIndex;

#if ENABLE_INPUT_SYSTEM
    private InputAction prevSpriteAction;
    private InputAction nextSpriteAction;
    private InputAction flipXAction;
    private InputAction flipYAction;
    private InputAction rotateAction;
    private InputAction clearAction;
    private InputAction noteAction;
    private InputAction layerAction;
    private InputAction toggleModeAction;
#endif

    public bool VisualOverrideModeActive =>
        world != null && world.IsVisualOverrideEditActive;

    private void Awake()
    {
        if (world == null)
        {
            world = FindAnyObjectByType<SandboxWorld>();
        }

        if (player == null)
        {
            player = GetComponent<SandboxPlayerController>();
        }

        EnsureTargetCamera();
#if ENABLE_INPUT_SYSTEM
        CreateInputActions();
#endif
    }

    private void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        EnableInputActions(true);
#endif
    }

    private void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        EnableInputActions(false);
#endif
    }

    private void OnDestroy()
    {
#if ENABLE_INPUT_SYSTEM
        DisposeInputActions();
#endif
    }

    private void Update()
    {
        if (world == null)
        {
            return;
        }

        if (player != null)
        {
            player.SetVisualOverrideModeActive(VisualOverrideModeActive);
        }

        HandleF8Shortcut();
        if (!VisualOverrideModeActive || !world.IsDebugOverrideModeEnabled)
        {
            return;
        }

        UpdateSelectionFromPointer();
        HandleEditingKeys();
    }

    private void OnGUI()
    {
        if (!VisualOverrideModeActive || !world.IsDebugOverrideModeEnabled || !pointerInRange)
        {
            return;
        }

        if (!hasSelection && selectedLayer == AutotileVisualLayer.Cover)
        {
            string reason = GetCoverUnavailableReason(world.GetTile(selectedCell.x, selectedCell.y));
            if (!string.IsNullOrEmpty(reason))
            {
                GUI.Label(new Rect(12f, 12f, 900f, 24f), $"Cover n/a ({reason})");
            }

            return;
        }

        if (!hasSelection || string.IsNullOrEmpty(selectedTileset))
        {
            return;
        }

        string autoLabel = "n/a";
        string overrideLabel = "none";
        string transformLabel = string.Empty;
        if (world.TryResolveAutoVisual(
                selectedCell.x,
                selectedCell.y,
                selectedLayer,
                out string autoSpriteId,
                out bool autoFlipX,
                out _))
        {
            autoLabel = autoFlipX ? $"{autoSpriteId}f" : autoSpriteId;
        }

        if (world.TryGetVisualOverride(selectedCell.x, selectedCell.y, selectedLayer, selectedTileset, out AutotileVisualOverride entry))
        {
            overrideLabel = entry.overrideSpriteId;
            transformLabel = $"rot={entry.rotation} flipX={entry.overrideFlipX} flipY={entry.overrideFlipY}";
            if (!string.IsNullOrEmpty(entry.note))
            {
                transformLabel += $" note={entry.note}";
            }
        }

        string layerName = selectedLayer == AutotileVisualLayer.Cover ? "Cover" : "Ground";
        GUI.Label(
            new Rect(12f, 12f, 900f, 24f),
            $"{layerName} {selectedTileset} | auto: {autoLabel} | override: {overrideLabel} {transformLabel}");
    }

    private void HandleF8Shortcut()
    {
        if (!WasToggleModePressed() || world == null)
        {
            return;
        }

        world.ToggleVisualOverrideEditMode();
        if (player != null)
        {
            player.SetVisualOverrideModeActive(VisualOverrideModeActive);
        }
    }

    private void UpdateSelectionFromPointer()
    {
        pointerInRange = false;
        hasSelection = false;
        selectedTileset = null;

        EnsureTargetCamera();
        if (targetCamera == null || !SandboxScreenPointer.TryReadWorldTile(targetCamera, world, out Vector2Int tile))
        {
            return;
        }

        Vector3 center = world.TileToWorldCenter(tile.x, tile.y);
        Transform origin = player != null ? player.transform : transform;
        if (!SandboxDebugEditReach.IsWithinReach(
                center,
                origin.position,
                targetCamera,
                editRange,
                allowVisibleOnScreen: true))
        {
            return;
        }

        pointerInRange = true;
        selectedCell = tile;
        hasSelection = TryResolveSelectedTileset(out selectedTileset);
    }

    private bool TryResolveSelectedTileset(out string tilesetName)
    {
        tilesetName = null;
        SandboxTileVisualCatalog catalog = world.TileVisualCatalog;
        if (catalog == null)
        {
            return false;
        }

        SandboxTile tile = world.GetTile(selectedCell.x, selectedCell.y);
        if (!tile.IsSolid)
        {
            return false;
        }

        if (selectedLayer == AutotileVisualLayer.Cover)
        {
            if (catalog.CanEditCoverAt(world.GetTile, selectedCell.x, selectedCell.y, out AutotileTileset coverTileset))
            {
                tilesetName = coverTileset.Name;
                return true;
            }

            return false;
        }

        if (catalog.TryGetGroundTileset(tile.id, out AutotileTileset groundTileset))
        {
            tilesetName = groundTileset.Name;
            return true;
        }

        return false;
    }

    private static string GetCoverUnavailableReason(SandboxTile tile)
    {
        if (tile.id != SandboxRegistries.GrassIndex)
        {
            return "grass only";
        }

        return "blocked above";
    }

    private void HandleEditingKeys()
    {
        if (WasLayerSwitchPressed())
        {
            selectedLayer = selectedLayer == AutotileVisualLayer.Ground
                ? AutotileVisualLayer.Cover
                : AutotileVisualLayer.Ground;
            hasSelection = TryResolveSelectedTileset(out selectedTileset);
        }

        if (!hasSelection || string.IsNullOrEmpty(selectedTileset))
        {
            return;
        }

        if (WasClearPressed())
        {
            world.ClearVisualOverride(selectedCell.x, selectedCell.y, selectedLayer, selectedTileset);
            return;
        }

        if (WasNotePressed())
        {
            notePresetIndex = (notePresetIndex + 1) % NotePresets.Length;
            if (world.TryGetVisualOverride(selectedCell.x, selectedCell.y, selectedLayer, selectedTileset, out AutotileVisualOverride existing))
            {
                existing.note = NotePresets[notePresetIndex];
                world.SetVisualOverride(existing);
            }

            return;
        }

        if (!world.TryGetVisualOverride(selectedCell.x, selectedCell.y, selectedLayer, selectedTileset, out AutotileVisualOverride entry))
        {
            if (!world.TryResolveAutoVisual(
                    selectedCell.x,
                    selectedCell.y,
                    selectedLayer,
                    out string autoSpriteId,
                    out bool autoFlipX,
                    out _))
            {
                return;
            }

            entry = new AutotileVisualOverride(
                selectedCell,
                selectedLayer,
                selectedTileset,
                autoSpriteId,
                autoFlipX,
                autoSpriteId,
                autoFlipX);
        }

        if (!TryGetSelectedSpriteCount(out int spriteCount))
        {
            spriteCount = AutotileRuleTables.GroundSpriteCount;
        }

        bool shift = IsShiftHeld();
        int stride = shift ? VisualOverrideSpriteStep.GetShiftStride(spriteCount) : 1;
        if (WasPrevSpritePressed())
        {
            entry.overrideSpriteId = VisualOverrideSpriteStep.Step(entry.overrideSpriteId, -stride, spriteCount);
            world.SetVisualOverride(entry);
        }
        else if (WasNextSpritePressed())
        {
            entry.overrideSpriteId = VisualOverrideSpriteStep.Step(entry.overrideSpriteId, stride, spriteCount);
            world.SetVisualOverride(entry);
        }
        else if (WasFlipXPressed())
        {
            entry.overrideFlipX = !entry.overrideFlipX;
            world.SetVisualOverride(entry);
        }
        else if (WasFlipYPressed())
        {
            entry.overrideFlipY = !entry.overrideFlipY;
            world.SetVisualOverride(entry);
        }
        else if (WasRotatePressed())
        {
            entry.rotation = AutotileVisualOverride.NormalizeRotation(entry.rotation + (shift ? -90 : 90));
            world.SetVisualOverride(entry);
        }
    }

    private bool TryGetSelectedSpriteCount(out int spriteCount)
    {
        spriteCount = AutotileRuleTables.GroundSpriteCount;
        SandboxTileVisualCatalog catalog = world?.TileVisualCatalog;
        if (string.IsNullOrEmpty(selectedTileset) || catalog == null)
        {
            return false;
        }

        SandboxTile tile = world.GetTile(selectedCell.x, selectedCell.y);
        if (selectedLayer == AutotileVisualLayer.Cover)
        {
            if (catalog.TryGetCoverTileset(tile.id, out AutotileTileset coverTileset))
            {
                spriteCount = coverTileset.Sprites.Count;
                return true;
            }

            return false;
        }

        if (catalog.TryGetGroundTileset(tile.id, out AutotileTileset groundTileset))
        {
            spriteCount = groundTileset.Sprites.Count;
            return true;
        }

        return false;
    }

    private void EnsureTargetCamera()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

#if ENABLE_INPUT_SYSTEM
    private void CreateInputActions()
    {
        toggleModeAction = new InputAction("ToggleVisualOverrideMode", InputActionType.Button, "<Keyboard>/f8");
        layerAction = new InputAction("SwitchVisualLayer", InputActionType.Button, "<Keyboard>/tab");
        prevSpriteAction = new InputAction("PrevOverrideSprite", InputActionType.Button, "<Keyboard>/leftBracket");
        nextSpriteAction = new InputAction("NextOverrideSprite", InputActionType.Button, "<Keyboard>/rightBracket");
        flipXAction = new InputAction("ToggleOverrideFlipX", InputActionType.Button, "<Keyboard>/x");
        flipYAction = new InputAction("ToggleOverrideFlipY", InputActionType.Button, "<Keyboard>/y");
        rotateAction = new InputAction("RotateOverride", InputActionType.Button, "<Keyboard>/r");
        clearAction = new InputAction("ClearOverride", InputActionType.Button, "<Keyboard>/c");
        noteAction = new InputAction("CycleOverrideNote", InputActionType.Button, "<Keyboard>/n");
    }

    private void EnableInputActions(bool enable)
    {
        if (enable)
        {
            toggleModeAction?.Enable();
            layerAction?.Enable();
            prevSpriteAction?.Enable();
            nextSpriteAction?.Enable();
            flipXAction?.Enable();
            flipYAction?.Enable();
            rotateAction?.Enable();
            clearAction?.Enable();
            noteAction?.Enable();
            return;
        }

        toggleModeAction?.Disable();
        layerAction?.Disable();
        prevSpriteAction?.Disable();
        nextSpriteAction?.Disable();
        flipXAction?.Disable();
        flipYAction?.Disable();
        rotateAction?.Disable();
        clearAction?.Disable();
        noteAction?.Disable();
    }

    private void DisposeInputActions()
    {
        toggleModeAction?.Dispose();
        layerAction?.Dispose();
        prevSpriteAction?.Dispose();
        nextSpriteAction?.Dispose();
        flipXAction?.Dispose();
        flipYAction?.Dispose();
        rotateAction?.Dispose();
        clearAction?.Dispose();
        noteAction?.Dispose();
    }
#endif

    private bool WasToggleModePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (toggleModeAction != null && toggleModeAction.WasPressedThisFrame())
        {
            return true;
        }

        if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.F8);
#else
        return false;
#endif
    }

    private bool WasLayerSwitchPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (layerAction != null && layerAction.WasPressedThisFrame())
        {
            return true;
        }

        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Tab);
#else
        return false;
#endif
    }

    private bool WasPrevSpritePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (prevSpriteAction != null && prevSpriteAction.WasPressedThisFrame())
        {
            return true;
        }

        if (Keyboard.current != null && Keyboard.current.leftBracketKey.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.LeftBracket);
#else
        return false;
#endif
    }

    private bool WasNextSpritePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (nextSpriteAction != null && nextSpriteAction.WasPressedThisFrame())
        {
            return true;
        }

        if (Keyboard.current != null && Keyboard.current.rightBracketKey.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.RightBracket);
#else
        return false;
#endif
    }

    private bool WasFlipXPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (flipXAction != null && flipXAction.WasPressedThisFrame())
        {
            return true;
        }

        if (Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.X);
#else
        return false;
#endif
    }

    private bool WasFlipYPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (flipYAction != null && flipYAction.WasPressedThisFrame())
        {
            return true;
        }

        if (Keyboard.current != null && Keyboard.current.yKey.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Y);
#else
        return false;
#endif
    }

    private bool WasRotatePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (rotateAction != null && rotateAction.WasPressedThisFrame())
        {
            return true;
        }

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.R);
#else
        return false;
#endif
    }

    private bool WasClearPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (clearAction != null && clearAction.WasPressedThisFrame())
        {
            return true;
        }

        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.C);
#else
        return false;
#endif
    }

    private bool WasNotePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (noteAction != null && noteAction.WasPressedThisFrame())
        {
            return true;
        }

        if (Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.N);
#else
        return false;
#endif
    }

    private static bool IsShiftHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#else
        return false;
#endif
    }

    private Vector2 ReadPointerPosition()
    {
        return SandboxScreenPointer.TryReadScreenPosition(out Vector2 screen)
            ? screen
            : Vector2.zero;
    }
}
