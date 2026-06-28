using ProjectTwelve.Visual.Characters;
using UnityEngine;

/// <summary>
/// Spawns a modular avatar as a visual child and randomizes appearance at runtime.
/// </summary>
public sealed class SandboxPlayerAvatarVisual : MonoBehaviour
{
    [SerializeField] private GameObject avatarPrefab;
    [SerializeField] private CharacterLayerCatalog layerCatalog;
    [SerializeField] private Vector3 avatarLocalPosition = Vector3.zero;
    [SerializeField] private int bodySortingOrder = 10;

    private Transform avatarTransform;
    private ISandboxPlayerLocomotion locomotion;

    /// <summary>Root transform of the instantiated avatar (used for facing flips).</summary>
    public Transform AvatarTransform => avatarTransform;

    /// <summary>Locomotion presentation driver for the avatar instance.</summary>
    public ISandboxPlayerLocomotion Locomotion => locomotion;

    private void Awake()
    {
        if (!PlayerAvatarFactory.TryCreateRandomAvatar(
                transform,
                avatarPrefab,
                layerCatalog,
                avatarLocalPosition,
                bodySortingOrder,
                out avatarTransform,
                out locomotion))
        {
            Debug.LogWarning(
                "SandboxPlayerAvatarVisual: failed to create avatar. Assign CharacterLayerCatalog and prefab.");
        }
    }
}
