using UnityEditor;
using UnityEngine;

/// <summary>Editor-only scratch preview for the pure terrain generator; never owns a live world.</summary>
public sealed class SandboxGenerationTuningWindow : EditorWindow
{
    private const int PreviewWidth = 256;
    private const int PreviewHeight = 128;

    private int seed = 1337;
    private int surfaceHeight = 28;
    private int terrainAmplitude = 8;
    private float terrainFrequency = 0.06f;
    private int dirtDepth = 8;
    private int previewCenterX;
    private int previewCenterY = 24;
    private Texture2D preview;

    [MenuItem("Project Twelve/Debug/Generation Tuning")]
    private static void Open()
    {
        GetWindow<SandboxGenerationTuningWindow>("Generation Tuning");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Scratch world generator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Regenerate builds an in-memory preview from SandboxTerrainGenerator. It never reads or writes the active SandboxWorld or a save file.",
            MessageType.Info);

        seed = EditorGUILayout.IntField("Seed", seed);
        surfaceHeight = EditorGUILayout.IntField("Surface height", surfaceHeight);
        terrainAmplitude = Mathf.Max(0, EditorGUILayout.IntField("Terrain amplitude", terrainAmplitude));
        terrainFrequency = Mathf.Max(0.0001f, EditorGUILayout.FloatField("Terrain frequency", terrainFrequency));
        dirtDepth = Mathf.Max(1, EditorGUILayout.IntField("Dirt depth", dirtDepth));
        previewCenterX = EditorGUILayout.IntField("Preview center X", previewCenterX);
        previewCenterY = EditorGUILayout.IntField("Preview center Y", previewCenterY);

        if (GUILayout.Button("Regenerate scratch preview"))
        {
            Regenerate();
        }

        if (preview != null)
        {
            Rect rect = GUILayoutUtility.GetAspectRect((float)PreviewWidth / PreviewHeight);
            EditorGUI.DrawPreviewTexture(rect, preview, null, ScaleMode.ScaleToFit);
        }
    }

    private void OnDisable()
    {
        if (preview != null)
        {
            DestroyImmediate(preview);
            preview = null;
        }
    }

    private void Regenerate()
    {
        if (preview == null)
        {
            preview = new Texture2D(PreviewWidth, PreviewHeight, TextureFormat.RGBA32, false)
            {
                name = "SandboxGenerationScratchPreview",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        SandboxTerrainGenerator generator = new SandboxTerrainGenerator(
            seed,
            surfaceHeight,
            terrainAmplitude,
            terrainFrequency,
            dirtDepth);
        int xMin = previewCenterX - PreviewWidth / 2;
        int yMin = previewCenterY - PreviewHeight / 2;
        for (int pixelX = 0; pixelX < PreviewWidth; pixelX++)
        {
            int height = generator.GetSurfaceHeight(xMin + pixelX);
            for (int pixelY = 0; pixelY < PreviewHeight; pixelY++)
            {
                int worldY = yMin + pixelY;
                Color color;
                if (worldY > height)
                {
                    color = new Color(0.14f, 0.23f, 0.38f, 1f);
                }
                else if (worldY == height)
                {
                    color = new Color(0.35f, 0.75f, 0.2f, 1f);
                }
                else if (worldY > height - dirtDepth)
                {
                    color = new Color(0.46f, 0.28f, 0.15f, 1f);
                }
                else
                {
                    color = new Color(0.35f, 0.37f, 0.42f, 1f);
                }

                preview.SetPixel(pixelX, pixelY, color);
            }
        }

        preview.Apply(false, false);
        Repaint();
    }
}
