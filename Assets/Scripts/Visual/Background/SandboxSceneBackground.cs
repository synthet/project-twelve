using UnityEngine;

namespace ProjectTwelve.Visual.Background
{
    /// <summary>
    /// Spawns a tiled parallax backdrop strip when the scene loads and keeps it aligned with the camera.
    /// </summary>
    public sealed class SandboxSceneBackground : MonoBehaviour
    {
        private const string LayerChildName = "BackgroundLayer0";

        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Vector2 tiledSize = new Vector2(128f, 16f);
        [SerializeField] private int sortingOrder = -40;
        [SerializeField] private Transform cameraTransform;

        private Transform layerTransform;

        private void Awake()
        {
            if (backgroundSprite == null)
            {
                Debug.LogWarning(
                    "SandboxSceneBackground: no background sprite assigned; skipping backdrop.");
                return;
            }

            var layerObject = new GameObject(LayerChildName);
            layerTransform = layerObject.transform;
            layerTransform.SetParent(transform, worldPositionStays: false);

            var renderer = layerObject.AddComponent<SpriteRenderer>();
            renderer.sprite = backgroundSprite;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = tiledSize;
            renderer.sortingOrder = sortingOrder;

            SnapToCamera();
        }

        private void LateUpdate()
        {
            if (layerTransform == null)
            {
                return;
            }

            SnapToCamera();
        }

        private void SnapToCamera()
        {
            Transform followTarget = cameraTransform != null
                ? cameraTransform
                : Camera.main != null
                    ? Camera.main.transform
                    : null;

            if (followTarget == null)
            {
                return;
            }

            Vector3 cameraPosition = followTarget.position;
            transform.position = new Vector3(cameraPosition.x, cameraPosition.y, 0f);
        }
    }
}
