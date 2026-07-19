using UnityEditor;
using UnityEngine;

/// <summary>Read-only Play Mode inspector over SandboxWorld debug snapshots and tile copies.</summary>
public sealed class SandboxChunkInspectorWindow : EditorWindow
{
    private const float CellSize = 28f;

    private Vector2Int chunkCoord;
    private Vector2Int selectedLocal;
    private Vector2 scroll;
    private bool selectBySceneClick = true;

    [MenuItem("Project Twelve/Debug/Chunk Inspector")]
    private static void Open()
    {
        GetWindow<SandboxChunkInspectorWindow>("Chunk Inspector");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGui;
        EditorApplication.update += Repaint;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGui;
        EditorApplication.update -= Repaint;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Read-only chunk snapshot", EditorStyles.boldLabel);
        chunkCoord = EditorGUILayout.Vector2IntField("Chunk coordinate", chunkCoord);
        selectBySceneClick = EditorGUILayout.Toggle("Select by Scene click", selectBySceneClick);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to inspect the live SandboxWorld.", MessageType.Info);
            return;
        }

        SandboxWorld world = Object.FindAnyObjectByType<SandboxWorld>();
        if (world == null)
        {
            EditorGUILayout.HelpBox("SandboxWorld not found in the active scene.", MessageType.Warning);
            return;
        }

        if (!world.TryGetChunkDebugState(chunkCoord, out SandboxChunkDebugState state))
        {
            EditorGUILayout.HelpBox("This chunk has not been generated. Inspection never generates it.", MessageType.Info);
            return;
        }

        DrawFlags(state);
        DrawSelectedTile(world);
        DrawTileGrid(world);
    }

    private static void DrawFlags(SandboxChunkDebugState state)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Snapshot", EditorStyles.boldLabel);
        EditorGUILayout.Toggle("Renderer loaded", state.RendererLoaded);
        EditorGUILayout.Toggle("Render dirty", state.NeedsRenderRebuild);
        EditorGUILayout.Toggle("Collider dirty", state.NeedsColliderRebuild);
        EditorGUILayout.Toggle("Save dirty", state.IsSaveDirty);
        EditorGUILayout.Toggle("Has edits", state.HasEdits);
        EditorGUILayout.IntField("Nav version", state.NavVersion);
    }

    private void DrawSelectedTile(SandboxWorld world)
    {
        int worldX = chunkCoord.x * SandboxChunk.Size + selectedLocal.x;
        int worldY = chunkCoord.y * SandboxChunk.Size + selectedLocal.y;
        if (!world.TryGetExistingTile(worldX, worldY, out SandboxTile tile))
        {
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            $"Tile local ({selectedLocal.x}, {selectedLocal.y}) / world ({worldX}, {worldY})",
            EditorStyles.boldLabel);
        EditorGUILayout.IntField("Runtime id", tile.id);
        EditorGUILayout.Toggle("Solid", tile.IsSolid);
        EditorGUILayout.IntField("Light", tile.light);
        EditorGUILayout.FloatField("Fluid", tile.fluid);
        EditorGUILayout.IntField("Metadata", tile.metadata);
    }

    private void DrawTileGrid(SandboxWorld world)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Tile array (click a runtime id for details)", EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(260f));
        GUILayout.BeginVertical(GUILayout.Width((SandboxChunk.Size + 1) * CellSize));
        for (int localY = SandboxChunk.Size - 1; localY >= 0; localY--)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(localY.ToString(), GUILayout.Width(CellSize));
            for (int localX = 0; localX < SandboxChunk.Size; localX++)
            {
                int worldX = chunkCoord.x * SandboxChunk.Size + localX;
                int worldY = chunkCoord.y * SandboxChunk.Size + localY;
                string label = world.TryGetExistingTile(worldX, worldY, out SandboxTile tile)
                    ? tile.id.ToString()
                    : "-";
                if (GUILayout.Button(label, GUILayout.Width(CellSize), GUILayout.Height(CellSize)))
                {
                    selectedLocal = new Vector2Int(localX, localY);
                }
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
        EditorGUILayout.EndScrollView();
    }

    private void OnSceneGui(SceneView sceneView)
    {
        if (!Application.isPlaying)
        {
            return;
        }

        SandboxWorld world = Object.FindAnyObjectByType<SandboxWorld>();
        if (world == null)
        {
            return;
        }

        float chunkWorldSize = SandboxChunk.Size * world.TileSize;
        Vector3 center = new Vector3(
            (chunkCoord.x + 0.5f) * chunkWorldSize,
            (chunkCoord.y + 0.5f) * chunkWorldSize,
            0f);
        Handles.color = Color.cyan;
        Handles.DrawWireCube(center, new Vector3(chunkWorldSize, chunkWorldSize, 0f));

        Event current = Event.current;
        if (!selectBySceneClick || current.type != EventType.MouseDown || current.button != 0 || current.alt)
        {
            return;
        }

        Ray ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);
        Plane plane = new Plane(Vector3.forward, Vector3.zero);
        if (!plane.Raycast(ray, out float distance))
        {
            return;
        }

        Vector2Int tile = world.WorldPositionToTile(ray.GetPoint(distance));
        chunkCoord = SandboxWorld.WorldToChunkCoord(tile.x, tile.y);
        selectedLocal = SandboxWorld.WorldToLocalCoord(tile.x, tile.y);
        current.Use();
        Repaint();
    }
}
