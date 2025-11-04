using UnityEngine;

public class HexTile : MonoBehaviour
{
    private int gridX;
    private int gridY;
    private Material originalMaterial;
    private Renderer tileRenderer;
    
    public int GridX => gridX;
    public int GridY => gridY;
    
    public void Initialize(int x, int y)
    {
        gridX = x;
        gridY = y;
        tileRenderer = GetComponent<Renderer>();
        if (tileRenderer != null)
        {
            originalMaterial = tileRenderer.material;
        }
    }
    
    public void Highlight(Color highlightColor)
    {
        if (tileRenderer != null)
        {
            Material highlightMat = new Material(originalMaterial);
            highlightMat.color = highlightColor;
            tileRenderer.material = highlightMat;
        }
    }
    
    public void ResetHighlight()
    {
        if (tileRenderer != null && originalMaterial != null)
        {
            tileRenderer.material = originalMaterial;
        }
    }
}

