using UnityEngine;

/// <summary>
/// Drives sandbox player avatar locomotion animations from movement state.
/// </summary>
[RequireComponent(typeof(SandboxPlayerController), typeof(SandboxPlayerAvatarVisual))]
public sealed class SandboxPlayerAvatarAnimation : MonoBehaviour
{
    private const float MoveThreshold = 0.1f;
    private const float VerticalRiseThreshold = 0.1f;

    [SerializeField] private SandboxPlayerController controller;
    [SerializeField] private SandboxPlayerAvatarVisual avatarVisual;

    private ISandboxPlayerLocomotion locomotion;
    private bool wasGrounded;
    private bool wasAirborne;

    private void Awake()
    {
        if (controller == null)
        {
            controller = GetComponent<SandboxPlayerController>();
        }

        if (avatarVisual == null)
        {
            avatarVisual = GetComponent<SandboxPlayerAvatarVisual>();
        }
    }

    private void Start()
    {
        locomotion = avatarVisual != null ? avatarVisual.Locomotion : null;
        wasGrounded = controller != null && controller.IsGrounded;
    }

    private void FixedUpdate()
    {
        if (controller == null || locomotion == null || avatarVisual?.AvatarTransform == null)
        {
            return;
        }

        Vector2 velocity = controller.Velocity;
        bool grounded = controller.IsGrounded;

        UpdateFacing(velocity.x);

        bool landed = grounded && !wasGrounded && wasAirborne;
        if (landed)
        {
            locomotion.Land();
            wasAirborne = false;
        }
        else
        {
            UpdateLocomotion(velocity, grounded);
            if (!grounded)
            {
                wasAirborne = true;
            }
        }

        wasGrounded = grounded;
    }

    private void UpdateFacing(float horizontalVelocity)
    {
        if (Mathf.Abs(horizontalVelocity) <= MoveThreshold)
        {
            return;
        }

        Transform avatarTransform = avatarVisual.AvatarTransform;
        Vector3 scale = avatarTransform.localScale;
        scale.x = Mathf.Sign(horizontalVelocity) * Mathf.Abs(scale.x);
        avatarTransform.localScale = scale;
    }

    private void UpdateLocomotion(Vector2 velocity, bool grounded)
    {
        if (grounded)
        {
            if (Mathf.Abs(velocity.x) > MoveThreshold)
            {
                locomotion.Run();
            }
            else
            {
                locomotion.Idle();
            }

            return;
        }

        if (velocity.y > VerticalRiseThreshold)
        {
            locomotion.Jump();
        }
        else
        {
            locomotion.Fall();
        }
    }
}
