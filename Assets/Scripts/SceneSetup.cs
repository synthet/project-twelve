using UnityEngine;

[ExecuteInEditMode]
public class SceneSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    [SerializeField] private bool autoSetupOnStart = true;
    [SerializeField] private bool setupOnEnable = true;
    
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 5;
    [SerializeField] private int gridHeight = 5;
    [SerializeField] private float hexSize = 1f;
    [SerializeField] private Color hexColor = Color.white;
    
    [Header("Movable Object Settings")]
    [SerializeField] private GameObject movableObjectPrefab;
    [SerializeField] private Color movableObjectColor = Color.red;
    
    private bool hasSetup = false;
    
    private void OnEnable()
    {
        if (setupOnEnable && !hasSetup)
        {
            SetupScene();
            hasSetup = true;
        }
    }
    
    private void Start()
    {
        if (autoSetupOnStart && !hasSetup)
        {
            SetupScene();
            hasSetup = true;
        }
    }
    
    [ContextMenu("Setup Scene")]
    public void SetupScene()
    {
        // Check if HexGrid already exists
        HexGrid existingGrid = FindObjectOfType<HexGrid>();
        if (existingGrid == null)
        {
            // Create HexGrid
            GameObject hexGridObj = new GameObject("HexGrid");
            HexGrid hexGrid = hexGridObj.AddComponent<HexGrid>();
        }
        
        // Check if MovableObject already exists
        MouseControlledObject existingController = FindObjectOfType<MouseControlledObject>();
        if (existingController == null)
        {
            // Create Movable Object
            GameObject movableObj;
            if (movableObjectPrefab != null)
            {
                movableObj = Instantiate(movableObjectPrefab);
            }
            else
            {
                // Create a default sphere
                movableObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                movableObj.name = "MovableObject";
                
                // Make it stand out
                Renderer renderer = movableObj.GetComponent<Renderer>();
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = movableObjectColor;
                mat.SetFloat("_Metallic", 0.5f);
                mat.SetFloat("_Glossiness", 0.8f);
                renderer.material = mat;
                
                // Scale it to be visible
                movableObj.transform.localScale = Vector3.one * 0.5f;
            }
            
            movableObj.transform.position = new Vector3(0, 0.75f, 0);
            
            // Add MouseControlledObject component (only in play mode)
            if (Application.isPlaying)
            {
                MouseControlledObject mouseController = movableObj.AddComponent<MouseControlledObject>();
            }
        }
        
        // Setup camera if needed
        SetupCamera();
    }
    
    private void SetupCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            mainCam = FindObjectOfType<Camera>();
        }
        
        if (mainCam != null)
        {
            // Position camera to see the grid nicely
            mainCam.transform.position = new Vector3(0, 8, -8);
            mainCam.transform.LookAt(new Vector3(0, 0, 0));
            
            // Set background color
            mainCam.backgroundColor = new Color(0.2f, 0.2f, 0.25f);
        }
    }
    
    // Method to be called from HexGrid to set properties
    public void ConfigureHexGrid(HexGrid hexGrid)
    {
        // This will be called by HexGrid if it detects SceneSetup
        // We'll update HexGrid to support this
    }
}

