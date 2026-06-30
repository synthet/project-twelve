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
        [SerializeField] private bool syncCameraClearColor = true;
        [SerializeField] private Color cameraClearColor = new Color(0.102f, 0.098f, 0.196f, 1f);

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
            ApplyCameraClearColor();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (backgroundSprite == null || !syncCameraClearColor)
            {
                return;
            }

            cameraClearColor = BackdropClearColorSampler.SampleBackdropClearColor(backgroundSprite);
        }
#endif

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

        private void ApplyCameraClearColor()
        {
            if (!syncCameraClearColor)
            {
                return;
            }

            Camera camera = ResolveCamera();
            if (camera == null)
            {
                return;
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = cameraClearColor;
        }

        private Camera ResolveCamera()
        {
            if (cameraTransform != null &&
                cameraTransform.TryGetComponent(out Camera assignedCamera))
            {
                return assignedCamera;
            }

            return Camera.main;
        }
    }
}
