using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

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

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction leftMouseAction;
    private InputAction rightMouseAction;
    private InputAction pointerPositionAction;
    private InputAction saveAction;
    private InputAction loadAction;

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

        CreateInputActions();
    }

    private void OnEnable()
    {
        moveAction?.Enable();
        jumpAction?.Enable();
        leftMouseAction?.Enable();
        rightMouseAction?.Enable();
        pointerPositionAction?.Enable();
        saveAction?.Enable();
        loadAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        jumpAction?.Disable();
        leftMouseAction?.Disable();
        rightMouseAction?.Disable();
        pointerPositionAction?.Disable();
        saveAction?.Disable();
        loadAction?.Disable();
    }

    private void OnDestroy()
    {
        moveAction?.Dispose();
        jumpAction?.Dispose();
        leftMouseAction?.Dispose();
        rightMouseAction?.Dispose();
        pointerPositionAction?.Dispose();
        saveAction?.Dispose();
        loadAction?.Dispose();
    }

    private void Update()
    {
        HandleJump();
        HandleTileEditing();
        HandleSaveLoadShortcuts();
    }

    private void FixedUpdate()
    {
        float horizontal = moveAction?.ReadValue<float>() ?? 0f;
        body.linearVelocity = new Vector2(horizontal * moveSpeed, body.linearVelocity.y);
    }

    private void HandleJump()
    {
        if (jumpAction != null && jumpAction.WasPressedThisFrame() && IsGrounded())
        {
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpVelocity);
        }
    }

    private void HandleTileEditing()
    {
        if (world == null || mainCamera == null || pointerPositionAction == null)
        {
            return;
        }

        bool remove = leftMouseAction != null && leftMouseAction.WasPressedThisFrame();
        bool place = rightMouseAction != null && rightMouseAction.WasPressedThisFrame();
        if (!remove && !place)
        {
            return;
        }

        Vector2 pointer = pointerPositionAction.ReadValue<Vector2>();
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
        if (saveAction != null && saveAction.WasPressedThisFrame())
        {
            world.SaveToPath(path);
            Debug.Log($"Saved sandbox world to {path}");
        }

        if (loadAction != null && loadAction.WasPressedThisFrame())
        {
            world.LoadFromPath(path);
            Debug.Log($"Loaded sandbox world from {path}");
        }
    }

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

    private bool IsGrounded()
    {
        Bounds bounds = playerCollider.bounds;
        Vector2 leftFoot = new Vector2(bounds.min.x + 0.05f, bounds.min.y - 0.05f);
        Vector2 rightFoot = new Vector2(bounds.max.x - 0.05f, bounds.min.y - 0.05f);
        return world != null && (world.IsSolidAtWorldPosition(leftFoot) || world.IsSolidAtWorldPosition(rightFoot));
    }
}
