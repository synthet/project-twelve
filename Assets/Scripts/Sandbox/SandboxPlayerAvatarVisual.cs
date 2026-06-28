using ProjectTwelve.Visual.Characters;
using UnityEngine;

/// <summary>
/// Spawns a modular avatar as a visual child and randomizes appearance at runtime.
/// </summary>
public sealed class SandboxPlayerAvatarVisual : MonoBehaviour
{
    private const string BodyChildName = "Body";

    [SerializeField] private GameObject avatarPrefab;
    [SerializeField] private CharacterLayerCatalog layerCatalog;
    [SerializeField] private Vector3 avatarLocalPosition = Vector3.zero;
    [SerializeField] private int bodySortingOrder = 10;

    [Header("Foot Alignment")]
    [Tooltip("Snap the avatar so the body sprite's foot pivot rests on the player collider's bottom edge.")]
    [SerializeField] private bool alignFeetToCollider = true;

    [Tooltip("Extra vertical nudge (world units) applied after foot alignment. " +
             "Positive raises the avatar; use it when the art's feet differ from the sprite pivot.")]
    [SerializeField] private float footOffset = 0f;

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
            return;
        }

        if (alignFeetToCollider)
        {
            AlignAvatarFeetToCollider();
        }
    }

    /// <summary>
    /// Shifts the avatar vertically so the body sprite's foot pivot coincides with the player
    /// collider's bottom edge (the edge that rests on the tile surface), preventing the avatar
    /// from sinking into or floating above the ground regardless of the sprite's pivot.
    /// </summary>
    private void AlignAvatarFeetToCollider()
    {
        if (avatarTransform == null || !TryGetComponent(out BoxCollider2D playerCollider))
        {
            return;
        }

        Transform footReference = avatarTransform.Find(BodyChildName);
        if (footReference == null)
        {
            return;
        }

        // A SpriteRenderer's transform position maps to the sprite pivot in world space, which the
        // PixelHeroes art authors at the character's ground-contact line.
        float colliderBottomWorldY = transform.TransformPoint(
            new Vector3(playerCollider.offset.x, playerCollider.offset.y - playerCollider.size.y * 0.5f, 0f)).y;
        float footWorldY = footReference.position.y;

        float delta = colliderBottomWorldY - footWorldY + footOffset;
        avatarTransform.position += new Vector3(0f, delta, 0f);
    }
}
