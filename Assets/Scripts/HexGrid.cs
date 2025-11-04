using UnityEngine;

public class HexGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 5;
    [SerializeField] private int gridHeight = 5;
    [SerializeField] private float hexSize = 1f;
    [SerializeField] private Material hexMaterial;
    [SerializeField] private Color hexColor = Color.white;
    
    [Header("Prefabs")]
    [SerializeField] private GameObject hexTilePrefab;
    
    private GameObject[,] hexTiles;
    
    private void Start()
    {
        GenerateHexGrid();
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
        
        Vector3[] vertices = new Vector3[7];
        int[] triangles = new int[18];
        
        // Center vertex
        vertices[0] = Vector3.zero;
        
        // Outer vertices (6 points of hexagon)
        for (int i = 0; i < 6; i++)
        {
            float angle = 60f * i * Mathf.Deg2Rad;
            vertices[i + 1] = new Vector3(
                Mathf.Cos(angle) * hexSize,
                0f,
                Mathf.Sin(angle) * hexSize
            );
        }
        
        // Create triangles (fan from center)
        for (int i = 0; i < 6; i++)
        {
            int baseIndex = i * 3;
            triangles[baseIndex] = 0;
            triangles[baseIndex + 1] = i + 1;
            triangles[baseIndex + 2] = (i + 1) % 6 + 1;
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

