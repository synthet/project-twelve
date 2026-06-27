using UnityEngine;

/// <summary>
/// Smoothly follows a target transform in the XY plane while keeping a fixed camera depth.
/// Used by the prototype scene so the camera tracks the player as it moves and jumps.
/// The target can be assigned in the inspector or wired at runtime via <see cref="SetTarget"/>.
/// </summary>
[RequireComponent(typeof(Camera))]
public sealed class SandboxCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private float zOffset = -10f;

    private Vector3 followVelocity;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        SnapToTarget();
    }

    private void Start()
    {
        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desired = new Vector3(target.position.x, target.position.y, zOffset);
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref followVelocity, smoothTime);
    }

    private void SnapToTarget()
    {
        if (target == null)
        {
            return;
        }

        followVelocity = Vector3.zero;
        transform.position = new Vector3(target.position.x, target.position.y, zOffset);
    }
}
