using UnityEngine;

[ExecuteInEditMode]
public class HexGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 5;
    [SerializeField] private int gridHeight = 5;
    [SerializeField] private float hexSize = 1f;
    [SerializeField] private float hexHeight = 0.1f;
    [SerializeField] private Material hexMaterial;
    [SerializeField] private Color hexColor = new Color(0.8f, 0.8f, 0.9f, 1f);
    
    [Header("Prefabs")]
    [SerializeField] private GameObject hexTilePrefab;
    
    private GameObject[,] hexTiles;
    private bool hasGenerated = false;
    
    private void OnEnable()
    {
        if (!hasGenerated)
        {
            GenerateHexGrid();
            hasGenerated = true;
        }
    }
    
    private void Start()
    {
        if (!hasGenerated)
        {
            GenerateHexGrid();
            hasGenerated = true;
        }
    }
    
    [ContextMenu("Regenerate Grid")]
    private void RegenerateGrid()
    {
        // Clear existing tiles
        if (hexTiles != null)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (hexTiles[x, y] != null)
                    {
                        if (Application.isPlaying)
                            Destroy(hexTiles[x, y]);
                        else
                            DestroyImmediate(hexTiles[x, y]);
                    }
                }
            }
        }
        
        hasGenerated = false;
        GenerateHexGrid();
        hasGenerated = true;
    }
    
    private void GenerateHexGrid()
    {
        hexTiles = new GameObject[gridWidth, gridHeight];
        
        float hexWidth = hexSize * 2f;
        float hexHeight = hexSize * Mathf.Sqrt(3f);
        float horizontalOffset = hexWidth * 0.75f;
        float verticalOffset = hexHeight;
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 position = CalculateHexPosition(x, y, horizontalOffset, verticalOffset);
                GameObject hexTile = CreateHexTile(position, x, y);
                hexTiles[x, y] = hexTile;
            }
        }
    }
    
    private Vector3 CalculateHexPosition(int x, int y, float horizontalOffset, float verticalOffset)
    {
        float xPos = x * horizontalOffset;
        float yPos = y * verticalOffset + (x % 2 == 1 ? verticalOffset * 0.5f : 0f);
        return new Vector3(xPos, 0f, yPos);
    }
    
    private GameObject CreateHexTile(Vector3 position, int x, int y)
    {
        GameObject hexTile;
        
        if (hexTilePrefab != null)
        {
            hexTile = Instantiate(hexTilePrefab, position, Quaternion.identity, transform);
        }
        else
        {
            hexTile = new GameObject($"HexTile_{x}_{y}");
            hexTile.transform.SetParent(transform);
            hexTile.transform.position = position;
            
            MeshFilter meshFilter = hexTile.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = hexTile.AddComponent<MeshRenderer>();
            MeshCollider meshCollider = hexTile.AddComponent<MeshCollider>();
            
            meshFilter.mesh = CreateHexMesh();
            meshRenderer.material = hexMaterial != null ? hexMaterial : CreateDefaultMaterial();
            meshCollider.sharedMesh = meshFilter.mesh;
            
            HexTile hexTileComponent = hexTile.AddComponent<HexTile>();
            hexTileComponent.Initialize(x, y);
        }
        
        return hexTile;
    }
    
    private Mesh CreateHexMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Hexagon";
        
        // Create a hexagon with height (extruded)
        // Top face: 7 vertices (center + 6 outer)
        // Bottom face: 7 vertices (center + 6 outer)
        // 6 side faces: 4 vertices each = 24 vertices
        // Total: 7 + 7 + 24 = 38 vertices
        
        Vector3[] vertices = new Vector3[38];
        int[] triangles = new int[72]; // 6 top + 6 bottom + 6*4 sides = 36 triangles * 2 vertices per triangle
        
        // Top face center
        vertices[0] = new Vector3(0, hexHeight, 0);
        
        // Top face outer vertices
        for (int i = 0; i < 6; i++)
        {
            float angle = 60f * i * Mathf.Deg2Rad;
            vertices[i + 1] = new Vector3(
                Mathf.Cos(angle) * hexSize,
                hexHeight,
                Mathf.Sin(angle) * hexSize
            );
        }
        
        // Bottom face center
        vertices[7] = Vector3.zero;
        
        // Bottom face outer vertices
        for (int i = 0; i < 6; i++)
        {
            float angle = 60f * i * Mathf.Deg2Rad;
            vertices[i + 8] = new Vector3(
                Mathf.Cos(angle) * hexSize,
                0f,
                Mathf.Sin(angle) * hexSize
            );
        }
        
        // Side faces (6 sides, each with 4 vertices)
        int sideStartIndex = 14;
        for (int i = 0; i < 6; i++)
        {
            float angle1 = 60f * i * Mathf.Deg2Rad;
            float angle2 = 60f * (i + 1) * Mathf.Deg2Rad;
            
            int sideIdx = sideStartIndex + i * 4;
            vertices[sideIdx] = new Vector3(Mathf.Cos(angle1) * hexSize, hexHeight, Mathf.Sin(angle1) * hexSize);
            vertices[sideIdx + 1] = new Vector3(Mathf.Cos(angle1) * hexSize, 0f, Mathf.Sin(angle1) * hexSize);
            vertices[sideIdx + 2] = new Vector3(Mathf.Cos(angle2) * hexSize, 0f, Mathf.Sin(angle2) * hexSize);
            vertices[sideIdx + 3] = new Vector3(Mathf.Cos(angle2) * hexSize, hexHeight, Mathf.Sin(angle2) * hexSize);
        }
        
        int triIndex = 0;
        
        // Top face triangles (fan from center)
        for (int i = 0; i < 6; i++)
        {
            triangles[triIndex++] = 0;
            triangles[triIndex++] = i + 1;
            triangles[triIndex++] = (i + 1) % 6 + 1;
        }
        
        // Bottom face triangles (fan from center, reversed order)
        for (int i = 0; i < 6; i++)
        {
            triangles[triIndex++] = 7;
            triangles[triIndex++] = (i + 1) % 6 + 8;
            triangles[triIndex++] = i + 8;
        }
        
        // Side faces (6 sides, 2 triangles each)
        for (int i = 0; i < 6; i++)
        {
            int baseIdx = sideStartIndex + i * 4;
            // First triangle of side
            triangles[triIndex++] = baseIdx;
            triangles[triIndex++] = baseIdx + 1;
            triangles[triIndex++] = baseIdx + 2;
            // Second triangle of side
            triangles[triIndex++] = baseIdx;
            triangles[triIndex++] = baseIdx + 2;
            triangles[triIndex++] = baseIdx + 3;
        }
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }
    
    private Material CreateDefaultMaterial()
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = hexColor;
        mat.SetFloat("_Metallic", 0.1f);
        mat.SetFloat("_Glossiness", 0.3f);
        return mat;
    }
    
    public Vector3 GetHexWorldPosition(int x, int y)
    {
        if (hexTiles == null || x < 0 || x >= gridWidth || y < 0 || y >= gridHeight)
            return Vector3.zero;
        
        if (hexTiles[x, y] != null)
            return hexTiles[x, y].transform.position;
        
        return Vector3.zero;
    }
    
    public bool GetHexCoordinates(Vector3 worldPosition, out int x, out int y)
    {
        x = -1;
        y = -1;
        
        if (hexTiles == null) return false;
        
        float minDistance = float.MaxValue;
        
        for (int i = 0; i < gridWidth; i++)
        {
            for (int j = 0; j < gridHeight; j++)
            {
                if (hexTiles[i, j] != null)
                {
                    float distance = Vector3.Distance(worldPosition, hexTiles[i, j].transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        x = i;
                        y = j;
                    }
                }
            }
        }
        
        return x >= 0 && y >= 0;
    }
}

