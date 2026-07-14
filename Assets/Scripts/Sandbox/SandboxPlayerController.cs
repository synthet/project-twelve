using System.IO;
using ProjectTwelve.Sandbox.Inventory;
using ProjectTwelve.Sandbox.Registry;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public sealed class SandboxPlayerController : MonoBehaviour
{
    [SerializeField] private SandboxWorld world;
    [SerializeField] private Camera editCamera;
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float jumpVelocity = 11f;
    [SerializeField] private string placeTileId = "core:dirt";
    [SerializeField] private float editRange = SandboxInventoryConstants.EditRange;
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
    private int placeTileRuntimeIndex = -1;
    private int selectedInventorySlot;
    private float nextEditTime;
    private SandboxInventory inventory;
    private SandboxInventoryEditService inventoryEditService;
    private SandboxWorldInventoryAdapter inventoryWorld;

    private float horizontalInput;
    private float externalMoveInput;
    private float externalMoveUntilTime;
    private bool externalJumpRequested;

    /// <summary>True when tile probes under the player collider hit solid world tiles.</summary>
    public bool IsGrounded => CheckGrounded();

    /// <summary>Current rigidbody velocity (world units per second).</summary>
    public Vector2 Velocity => body != null ? body.linearVelocity : Vector2.zero;

    /// <summary>The registered solid tile selected for creative placement, or null for an empty slot.</summary>
    public string ActivePlacementTileId => placeTileRuntimeIndex >= 0 ? placeTileId : null;

    /// <summary>Fixed-size registry-backed player inventory; the first ten slots are the hotbar.</summary>
    public SandboxInventory Inventory => inventory;

    public int SelectedInventorySlot => selectedInventorySlot;

    /// <summary>
    /// Selects a registered, solid, non-air tile for right-click creative placement. Invalid IDs
    /// leave the current selection unchanged.
    /// </summary>
    public bool TrySetActivePlacementTile(string tileId)
    {
        if (string.IsNullOrEmpty(tileId)
            || !SandboxRegistries.Tiles.TryGet(tileId, out TileDefinition tile)
            || !tile.Solid
            || tileId == SandboxCoreContent.AirTileId
            || !SandboxRegistries.Tiles.TryGetIndex(tileId, out int runtimeIndex))
        {
            return false;
        }

        placeTileId = tileId;
        placeTileRuntimeIndex = runtimeIndex;
        return true;
    }

    /// <summary>Clears creative placement while preserving left-click tile removal.</summary>
    public void ClearActivePlacementTile()
    {
        placeTileId = null;
        placeTileRuntimeIndex = -1;
    }

    /// <summary>Selects a hotbar slot and resolves its placeable tile, if any.</summary>
    public bool SelectInventorySlot(int index)
    {
        if (inventory == null || index < 0 || index >= SandboxInventoryConstants.HotbarSlotCount)
        {
            return false;
        }

        selectedInventorySlot = index;
        SandboxInventory.Slot slot = inventory.GetSlot(index);
        if (slot.IsEmpty
            || !SandboxRegistries.Items.TryGet(slot.ItemId, out ItemDefinition item)
            || string.IsNullOrEmpty(item.PlacesTileId)
            || !TrySetActivePlacementTile(item.PlacesTileId))
        {
            ClearActivePlacementTile();
        }

        return true;
    }

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

        float z = transform.position.z;
        body.position = worldPosition;
        body.linearVelocity = new Vector2(body.linearVelocity.x, 0f);
        transform.position = new Vector3(worldPosition.x, worldPosition.y, z);
        Physics2D.SyncTransforms();
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
        inventory = SandboxInventory.CreatePrototypeLoadout(SandboxRegistries.Items);
        inventory.Changed += OnInventoryChanged;
        inventoryEditService = new SandboxInventoryEditService(SandboxRegistries.Tiles, SandboxRegistries.Items);
        if (!TrySetActivePlacementTile(placeTileId))
        {
            Debug.LogWarning($"SandboxPlayerController: placement tile '{placeTileId}' is not a registered solid tile; placement disabled.");
            ClearActivePlacementTile();
        }

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
            world.SetPlayerInventory(inventory);
            inventoryWorld = new SandboxWorldInventoryAdapter(world);
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
        if (inventory != null)
        {
            inventory.Changed -= OnInventoryChanged;
        }

#if ENABLE_INPUT_SYSTEM
        moveAction?.Dispose();
        jumpAction?.Dispose();
        saveAction?.Dispose();
        loadAction?.Dispose();
#endif
    }

    private void OnInventoryChanged()
    {
        SelectInventorySlot(selectedInventorySlot);
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
        if (world == null || mainCamera == null || inventoryWorld == null || visualOverrideModeActive)
        {
            return;
        }

        bool remove = SandboxScreenPointer.WasLeftButtonPressedThisFrame();
        bool place = SandboxScreenPointer.WasRightButtonPressedThisFrame();
        if ((!remove && !place) || Time.unscaledTime < nextEditTime)
        {
            return;
        }

        if (!SandboxScreenPointer.TryReadWorldTile(mainCamera, world, out Vector2Int tile))
        {
            return;
        }

        Vector3 targetCenter = world.TileToWorldCenter(tile.x, tile.y);
        float maxWorldRange = Mathf.Max(0f, editRange) * world.TileSize;
        bool playerOccluded = place && playerCollider != null && playerCollider.bounds.Contains(targetCenter);
        SandboxInventoryEditResult validation = SandboxInventoryEditService.ValidateControllerRequest(
                transform.position.x,
                transform.position.y,
                targetCenter.x,
                targetCenter.y,
                maxWorldRange,
                playerOccluded);
        if (validation != SandboxInventoryEditResult.Success)
        {
            return;
        }

        SandboxInventoryEditResult result;
        if (remove)
        {
            result = inventoryEditService.TryBreak(inventoryWorld, tile.x, tile.y, out SandboxItemStack drop);
            if (result == SandboxInventoryEditResult.Success)
            {
                SandboxItemPickup.Spawn(
                    drop.ItemId,
                    drop.Count,
                    targetCenter,
                    inventory,
                    transform,
                    world.TileSize);
            }
        }
        else
        {
            result = inventoryEditService.TryPlace(inventoryWorld, inventory, selectedInventorySlot, tile.x, tile.y);
        }

        if (result == SandboxInventoryEditResult.Success)
        {
            nextEditTime = Time.unscaledTime + SandboxInventoryConstants.EditIntervalSeconds;
            SelectInventorySlot(selectedInventorySlot);
        }
    }

    private void EnsureMainCamera()
    {
        if (editCamera != null)
        {
            mainCamera = editCamera;
            return;
        }

        if (visualOverrideInput != null)
        {
            Camera sharedCamera = visualOverrideInput.TargetCamera;
            if (sharedCamera != null)
            {
                mainCamera = sharedCamera;
                return;
            }
        }

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

        // F6 — not F9: Unity Editor binds F9 to Profiler RecordToggle by default.
        if (Keyboard.current != null && Keyboard.current.f6Key.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.F6);
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
        loadAction = new InputAction("Load", InputActionType.Button, "<Keyboard>/f6");
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
