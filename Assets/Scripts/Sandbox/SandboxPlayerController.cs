using System.IO;
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
    private InputAction leftMouseAction;
    private InputAction rightMouseAction;
    private InputAction pointerPositionAction;
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

#if ENABLE_INPUT_SYSTEM
        CreateInputActions();
#endif
    }

    private void OnEnable()
    {
#if ENABLE_INPUT_SYSTEM
        moveAction?.Enable();
        jumpAction?.Enable();
        leftMouseAction?.Enable();
        rightMouseAction?.Enable();
        pointerPositionAction?.Enable();
        saveAction?.Enable();
        loadAction?.Enable();
#endif
    }

    private void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM
        moveAction?.Disable();
        jumpAction?.Disable();
        leftMouseAction?.Disable();
        rightMouseAction?.Disable();
        pointerPositionAction?.Disable();
        saveAction?.Disable();
        loadAction?.Disable();
#endif
    }

    private void OnDestroy()
    {
#if ENABLE_INPUT_SYSTEM
        moveAction?.Dispose();
        jumpAction?.Dispose();
        leftMouseAction?.Dispose();
        rightMouseAction?.Dispose();
        pointerPositionAction?.Dispose();
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
        if (world == null || mainCamera == null)
        {
            return;
        }

        bool remove = WasRemoveTilePressed();
        bool place = WasPlaceTilePressed();
        if (!remove && !place)
        {
            return;
        }

        Vector2 pointer = ReadPointerPosition();
        Vector3 mouseScreen = new Vector3(pointer.x, pointer.y, -mainCamera.transform.position.z);
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(mouseScreen);
        Vector2Int tile = world.WorldPositionToTile(mouseWorld);
        Vector3 center = world.TileToWorldCenter(tile.x, tile.y);
        if (Vector2.Distance(transform.position, center) > editRange)
        {
            return;
        }

        world.SetTile(tile.x, tile.y, remove ? SandboxRegistries.AirIndex : placeTileRuntimeIndex);
    }

    private void HandleSaveLoadShortcuts()
    {
        if (world == null)
        {
            return;
        }

        string path = Path.Combine(Application.persistentDataPath, saveFileName);
        if (WasSavePressed())
        {
            world.SaveToPath(path);
            Debug.Log($"Saved sandbox world and player position to {path}");
        }

        if (WasLoadPressed())
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

    private bool WasRemoveTilePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (leftMouseAction != null && leftMouseAction.WasPressedThisFrame())
        {
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }

    private bool WasPlaceTilePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (rightMouseAction != null && rightMouseAction.WasPressedThisFrame())
        {
            return true;
        }

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(1);
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

    private Vector2 ReadPointerPosition()
    {
#if ENABLE_INPUT_SYSTEM
        Vector2 pointer = pointerPositionAction?.ReadValue<Vector2>() ?? Vector2.zero;
        if (pointer != Vector2.zero)
        {
            return pointer;
        }

        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#else
        return Vector2.zero;
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
        leftMouseAction = new InputAction("RemoveTile", InputActionType.Button, "<Mouse>/leftButton");
        rightMouseAction = new InputAction("PlaceTile", InputActionType.Button, "<Mouse>/rightButton");
        pointerPositionAction = new InputAction("Pointer", InputActionType.Value, "<Mouse>/position");
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
