using System.IO;
using UnityEngine;

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

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<BoxCollider2D>();
        mainCamera = Camera.main;

        if (world == null)
        {
            world = FindObjectOfType<SandboxWorld>();
        }
    }

    private void Update()
    {
        HandleJump();
        HandleTileEditing();
        HandleSaveLoadShortcuts();
    }

    private void FixedUpdate()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        body.velocity = new Vector2(horizontal * moveSpeed, body.velocity.y);
    }

    private void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && IsGrounded())
        {
            body.velocity = new Vector2(body.velocity.x, jumpVelocity);
        }
    }

    private void HandleTileEditing()
    {
        if (world == null || mainCamera == null)
        {
            return;
        }

        bool remove = Input.GetMouseButtonDown(0);
        bool place = Input.GetMouseButtonDown(1);
        if (!remove && !place)
        {
            return;
        }

        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
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
        if (Input.GetKeyDown(KeyCode.F5))
        {
            world.SaveToPath(path);
            Debug.Log($"Saved sandbox world to {path}");
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            world.LoadFromPath(path);
            Debug.Log($"Loaded sandbox world from {path}");
        }
    }

    private bool IsGrounded()
    {
        Bounds bounds = playerCollider.bounds;
        Vector2 leftFoot = new Vector2(bounds.min.x + 0.05f, bounds.min.y - 0.05f);
        Vector2 rightFoot = new Vector2(bounds.max.x - 0.05f, bounds.min.y - 0.05f);
        return world != null && (world.IsSolidAtWorldPosition(leftFoot) || world.IsSolidAtWorldPosition(rightFoot));
    }
}
