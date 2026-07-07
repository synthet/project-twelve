using System.IO;
using Newtonsoft.Json.Linq;
using ProjectTwelve.Sandbox.Debug;
using ProjectTwelve.Sandbox.Registry;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class SandboxPlayerController : MonoBehaviour
{
    [SerializeField] private SandboxWorld world;
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float jumpVelocity = 11f;
    [SerializeField] private string placeTileId = "core:dirt";
    [SerializeField] private float editRange = 6f;
    [SerializeField] private string saveFileName = "sandbox-world.json";

    private bool visualOverrideModeActive;
    private SandboxVisualOverrideInput visualOverrideInput;

    public bool VisualOverrideModeActive => visualOverrideModeActive;

    public void SetVisualOverrideModeActive(bool active)
    {
        visualOverrideModeActive = active;
    }

    private const float GroundProbeDistance = 0.12f;
    private const float GroundNormalThreshold = 0.5f;
    private const float WallProbeDistance = 0.08f;
    private const float WallNormalThreshold = 0.5f;
    private const float FootInset = 0.05f;

    private Rigidbody2D body;
    private BoxCollider2D playerCollider;
    private Camera mainCamera;
    private int placeTileRuntimeIndex;

    private float horizontalInput;
    private float externalMoveInput;
    private float externalMoveUntilTime;
    private bool externalJumpRequested;

    /// <summary>True when tile probes under the player collider hit solid world tiles.</summary>
    public bool IsGrounded => CheckGrounded();

    /// <summary>Current rigidbody velocity (world units per second).</summary>
    public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;

    /// <summary>
    /// Sets external horizontal movement input for MCP or automation callers.
    /// Local player input takes priority when non-zero.
    /// </summary>
    /// <param name="direction">-1 left, 0 none, 1 right.</param>
    /// <param name="durationSeconds">When greater than zero, clears external input after this many seconds.</param>
    public void SetExternalMoveInput(float direction, float durationSeconds = 0f)
    {
        externalMoveInput = Mathf.Clamp(direction, -1f, 1f);
        externalMoveUntilTime = durationSeconds > 0f ? Time.time + durationSeconds : 0f;
    }

    /// <summary>Requests a jump on the next Update when the player is grounded.</summary>
    public void RequestJump()
    {
        externalJumpRequested = true;
    }

    /// <summary>Teleports the player rigidbody to a world-space position.</summary>
    /// <param name="worldPosition">Target position in world units.</param>
    public void TeleportTo(Vector2 worldPosition)
    {
        if (body == null)
        {
            return;
        }

        body.position = worldPosition;
        body.linearVelocity = new Vector2(body.linearVelocity.x, 0f);
    }

#if ENABLE_INPUT_SYSTEM
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction saveAction;
    private InputAction loadAction;
#endif

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<BoxCollider2D>();
        mainCamera = Camera.main;
        placeTileRuntimeIndex = SandboxRegistries.Tiles.GetIndex(placeTileId);

        PhysicsMaterial2D frictionless = SandboxPhysicsMaterials.ZeroFriction;
        body.sharedMaterial = frictionless;
        playerCollider.sharedMaterial = frictionless;

        if (world == null)
        {
            world = FindAnyObjectByType<SandboxWorld>();
        }

        if (world != null)
        {
            world.SetPlayerTarget(transform);
        }

        visualOverrideInput = GetComponent<SandboxVisualOverrideInput>();

#if ENABLE_INPUT_SYSTEM
        CreateInputActions();
#endif
    }

    private void Start()
    {
        visualOverrideModeActive = visualOverrideInput != null && visualOverrideInput.VisualOverrideModeActive;
    }

    private void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        moveAction?.Enable();
        jumpAction?.Enable();
        saveAction?.Enable();
        loadAction?.Enable();
#endif
    }

    private void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        moveAction?.Disable();
        jumpAction?.Disable();
        saveAction?.Disable();
        loadAction?.Disable();
#endif
    }

    private void OnDestroy()
    {
#if ENABLE_INPUT_SYSTEM
        moveAction?.Dispose();
        jumpAction?.Dispose();
        saveAction?.Dispose();
        loadAction?.Dispose();
#endif
    }

    private void Update()
    {
        UpdateExternalMoveInput();
        horizontalInput = ReadHorizontalInput();
        if (Mathf.Approximately(horizontalInput, 0f))
        {
            horizontalInput = externalMoveInput;
        }

        HandleJump();
        HandleTileEditing();
        HandleSaveLoadShortcuts();
    }

    private void FixedUpdate()
    {
        float targetVelocityX = horizontalInput * moveSpeed;
        if (!Mathf.Approximately(horizontalInput, 0f) && IsBlockedHorizontally(Mathf.Sign(horizontalInput)))
        {
            targetVelocityX = 0f;
        }

        body.linearVelocity = new Vector2(targetVelocityX, body.linearVelocity.y);
    }

    private void HandleJump()
    {
        bool jumpPressed = WasJumpPressed() || externalJumpRequested;
        externalJumpRequested = false;

        if (!jumpPressed || !IsGrounded)
        {
            return;
        }

        body.linearVelocity = new Vector2(body.linearVelocity.x, jumpVelocity);
    }

    private void UpdateExternalMoveInput()
    {
        if (externalMoveUntilTime > 0f && Time.time >= externalMoveUntilTime)
        {
            externalMoveInput = 0f;
            externalMoveUntilTime = 0f;
        }
    }

    private void HandleTileEditing()
    {
        EnsureMainCamera();

        bool remove = SandboxScreenPointer.WasLeftButtonPressedThisFrame();
        bool place = SandboxScreenPointer.WasRightButtonPressedThisFrame();
        if (remove || place)
        {
            // #region agent log
            AgentDebugLog.Write(
                "A",
                "SandboxPlayerController.HandleTileEditing",
                "mouse button edge detected",
                new JObject
                {
                    ["remove"] = remove,
                    ["place"] = place,
                    ["worldNull"] = world == null,
                    ["cameraNull"] = mainCamera == null,
                    ["debugEnabled"] = world != null && world.IsDebugOverrideModeEnabled,
                    ["appFocused"] = Application.isFocused,
                });
            // #endregion
        }

        if (world == null || mainCamera == null || !world.IsDebugOverrideModeEnabled)
        {
            if (remove || place)
            {
                // #region agent log
                AgentDebugLog.Write(
                    "B",
                    "SandboxPlayerController.HandleTileEditing",
                    "early exit before edit",
                    new JObject
                    {
                        ["worldNull"] = world == null,
                        ["cameraNull"] = mainCamera == null,
                        ["debugEnabled"] = world != null && world.IsDebugOverrideModeEnabled,
                    });
                // #endregion
            }

            return;
        }

        if (!remove && !place)
        {
            return;
        }

        if (!SandboxScreenPointer.TryReadWorldTile(mainCamera, world, out Vector2Int tile))
        {
            // #region agent log
            AgentDebugLog.Write(
                "C",
                "SandboxPlayerController.HandleTileEditing",
                "TryReadWorldTile failed",
                new JObject { ["remove"] = remove, ["place"] = place });
            // #endregion
            return;
        }

        SandboxScreenPointer.TryReadScreenPosition(out Vector2 screen);
        Vector3 worldPoint = SandboxScreenPointer.ScreenToWorld2D(mainCamera, screen);
        int tileIdBefore = world.GetTile(tile.x, tile.y).id;
        int targetTileId = remove ? SandboxRegistries.AirIndex : placeTileRuntimeIndex;
        bool edited = world.TrySetDebugOverrideTile(tile.x, tile.y, targetTileId);
        int tileIdAfter = world.GetTile(tile.x, tile.y).id;

        // #region agent log
        AgentDebugLog.Write(
            "F",
            "SandboxPlayerController.HandleTileEditing",
            "edit attempt completed",
            new JObject
            {
                ["runId"] = "post-fix",
                ["screenX"] = screen.x,
                ["screenY"] = screen.y,
                ["worldX"] = worldPoint.x,
                ["worldY"] = worldPoint.y,
                ["cameraX"] = mainCamera.transform.position.x,
                ["cameraY"] = mainCamera.transform.position.y,
                ["tileX"] = tile.x,
                ["tileY"] = tile.y,
                ["remove"] = remove,
                ["place"] = place,
                ["targetTileId"] = targetTileId,
                ["tileIdBefore"] = tileIdBefore,
                ["tileIdAfter"] = tileIdAfter,
                ["edited"] = edited,
                ["placeTileRuntimeIndex"] = placeTileRuntimeIndex,
            },
            "post-fix");
        // #endregion
    }

    private void EnsureMainCamera()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void HandleSaveLoadShortcuts()
    {
        if (world == null)
        {
            return;
        }

        string path = Path.Combine(Application.persistentDataPath, saveFileName);
        SandboxSaveLoadShortcutRouter.ShortcutCommand command = SandboxSaveLoadShortcutRouter.Resolve(
            WasSavePressed(),
            WasLoadPressed(),
            visualOverrideModeActive);

        if (command == SandboxSaveLoadShortcutRouter.ShortcutCommand.SaveWorld
            || command == SandboxSaveLoadShortcutRouter.ShortcutCommand.SaveWorldAndVisualOverrideSidecar)
        {
            world.SaveToPath(path);
            Debug.Log($"Saved sandbox world and player position to {path}");
            return;
        }

        if (command == SandboxSaveLoadShortcutRouter.ShortcutCommand.LoadWorld)
        {
            world.LoadFromPath(path);
            Debug.Log($"Loaded sandbox world and player position from {path}");
        }
    }

    private float ReadHorizontalInput()
    {
        float horizontal = 0f;

#if ENABLE_INPUT_SYSTEM
        horizontal = moveAction?.ReadValue<float>() ?? 0f;
        if (!Mathf.Approximately(horizontal, 0f))
        {
            return horizontal;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                horizontal -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                horizontal += 1f;
            }
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Mathf.Approximately(horizontal, 0f))
        {
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                horizontal -= 1f;
            }

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                horizontal += 1f;
            }
        }
#endif

        return horizontal;
    }

    private bool WasJumpPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (jumpAction != null && jumpAction.WasPressedThisFrame())
        {
            return true;
        }

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Space);
#else
        return false;
#endif
    }

    private bool WasSavePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (saveAction != null && saveAction.WasPressedThisFrame())
        {
            return true;
        }

        if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.F5);
#else
        return false;
#endif
    }

    private bool WasLoadPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (loadAction != null && loadAction.WasPressedThisFrame())
        {
            return true;
        }

        if (Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.F9);
#else
        return false;
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private void CreateInputActions()
    {
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/a")
            .With("Positive", "<Keyboard>/d");
        moveAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/leftArrow")
            .With("Positive", "<Keyboard>/rightArrow");

        jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
        saveAction = new InputAction("Save", InputActionType.Button, "<Keyboard>/f5");
        loadAction = new InputAction("Load", InputActionType.Button, "<Keyboard>/f9");
    }
#endif

    private bool CheckGrounded()
    {
        if (playerCollider == null)
        {
            return false;
        }

        Bounds bounds = playerCollider.bounds;
        float probeY = bounds.min.y - 0.01f;
        Vector2 leftFoot = new Vector2(bounds.min.x + FootInset, probeY);
        Vector2 rightFoot = new Vector2(bounds.max.x - FootInset, probeY);
        return ProbeGround(leftFoot) || ProbeGround(rightFoot);
    }

    private bool ProbeGround(Vector2 origin)
    {
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, GroundProbeDistance);
        return IsTerrainHit(hit) && hit.normal.y > GroundNormalThreshold;
    }

    private bool IsBlockedHorizontally(float direction)
    {
        if (playerCollider == null || Mathf.Approximately(direction, 0f))
        {
            return false;
        }

        Bounds bounds = playerCollider.bounds;
        float probeX = direction > 0f ? bounds.max.x + 0.01f : bounds.min.x - 0.01f;
        Vector2 origin = new Vector2(probeX, bounds.min.y + bounds.size.y * 0.25f);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right * direction, WallProbeDistance);
        return IsTerrainHit(hit) && Mathf.Abs(hit.normal.x) > WallNormalThreshold;
    }

    private bool IsTerrainHit(RaycastHit2D hit)
    {
        return hit.collider != null && hit.collider != playerCollider;
    }
}

/// <summary>
/// Shared 2D physics materials for sandbox gameplay colliders.
/// </summary>
public static class SandboxPhysicsMaterials
{
    private static PhysicsMaterial2D zeroFriction;

    /// <summary>Frictionless material used for player and terrain to prevent wall sticking.</summary>
    public static PhysicsMaterial2D ZeroFriction
    {
        get
        {
            if (zeroFriction == null)
            {
                zeroFriction = new PhysicsMaterial2D("SandboxZeroFriction")
                {
                    friction = 0f,
                    bounciness = 0f
                };
            }

            return zeroFriction;
        }
    }
}

/// <summary>
/// Pure shortcut router for sandbox save/load keys so EditMode tests can cover key conflicts
/// without synthesizing Unity input events.
/// </summary>
internal static class SandboxSaveLoadShortcutRouter
{
    internal enum ShortcutCommand
    {
        None,
        SaveWorld,
        SaveWorldAndVisualOverrideSidecar,
        LoadWorld
    }

    internal static ShortcutCommand Resolve(bool savePressed, bool loadPressed, bool visualOverrideModeActive)
    {
        if (savePressed)
        {
            return visualOverrideModeActive
                ? ShortcutCommand.SaveWorldAndVisualOverrideSidecar
                : ShortcutCommand.SaveWorld;
        }

        if (loadPressed)
        {
            return ShortcutCommand.LoadWorld;
        }

        return ShortcutCommand.None;
    }
}
