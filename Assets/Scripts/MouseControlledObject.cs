using UnityEngine;

public class MouseControlledObject : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float arrivalDistance = 0.1f;
    
    [Header("Visual Settings")]
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private float highlightIntensity = 1.5f;
    
    private HexGrid hexGrid;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private HexTile currentTile;
    private Camera mainCamera;
    
    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
        
        hexGrid = FindObjectOfType<HexGrid>();
        if (hexGrid == null)
        {
            Debug.LogError("HexGrid not found in scene!");
        }
        
        targetPosition = transform.position;
    }
    
    private void Update()
    {
        HandleMouseInput();
        MoveToTarget();
    }
    
    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                HexTile hexTile = hit.collider.GetComponent<HexTile>();
                if (hexTile != null)
                {
                    MoveToHexTile(hexTile);
                }
            }
        }
    }
    
    private void MoveToHexTile(HexTile hexTile)
    {
        if (hexTile == null) return;
        
        // Reset previous tile highlight
        if (currentTile != null && currentTile != hexTile)
        {
            currentTile.ResetHighlight();
        }
        
        // Highlight new target tile
        hexTile.Highlight(highlightColor);
        currentTile = hexTile;
        
        // Set target position
        targetPosition = hexTile.transform.position + Vector3.up * 0.75f; // Slightly above the tile
        isMoving = true;
    }
    
    private void MoveToTarget()
    {
        if (!isMoving) return;
        
        float distance = Vector3.Distance(transform.position, targetPosition);
        
        if (distance <= arrivalDistance)
        {
            transform.position = targetPosition;
            isMoving = false;
        }
        else
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );
        }
    }
    
    private void OnDestroy()
    {
        if (currentTile != null)
        {
            currentTile.ResetHighlight();
        }
    }
}

