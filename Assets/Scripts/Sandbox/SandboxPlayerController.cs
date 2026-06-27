using System.IO;
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
    [SerializeField] private int placeTileId = SandboxTileIds.Dirt;
    [SerializeField] private float editRange = 6f;
    [SerializeField] private string saveFileName = "sandbox-world.json";

    private Rigidbody2D body;
    private BoxCollider2D playerCollider;
    private Camera mainCamera;

    private float horizontalInput;

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
        horizontalInput = ReadHorizontalInput();
        HandleJump();
        HandleTileEditing();
        HandleSaveLoadShortcuts();
    }

    private void FixedUpdate()
    {
        body.linearVelocity = new Vector2(horizontalInput * moveSpeed, body.linearVelocity.y);
    }

    private void HandleJump()
    {
        if (!WasJumpPressed() || !IsGrounded())
        {
            return;
        }

        body.linearVelocity = new Vector2(body.linearVelocity.x, jumpVelocity);
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

        world.SetTile(tile.x, tile.y, remove ? SandboxTileIds.Air : placeTileId);
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
            Debug.Log($"Saved sandbox world to {path}");
        }

        if (WasLoadPressed())
        {
            world.LoadFromPath(path);
            Debug.Log($"Loaded sandbox world from {path}");
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

    private bool IsGrounded()
    {
        Bounds bounds = playerCollider.bounds;
        Vector2 leftFoot = new Vector2(bounds.min.x + 0.05f, bounds.min.y - 0.05f);
        Vector2 rightFoot = new Vector2(bounds.max.x - 0.05f, bounds.min.y - 0.05f);
        return world != null && (world.IsSolidAtWorldPosition(leftFoot) || world.IsSolidAtWorldPosition(rightFoot));
    }
}
