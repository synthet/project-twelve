using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Shared screen pointer reads for sandbox debug tile editing and visual override selection.
/// </summary>
public static class SandboxScreenPointer
{
#if ENABLE_INPUT_SYSTEM
    private static InputAction leftClickAction;
    private static InputAction rightClickAction;
    private static InputAction positionAction;
    private static bool actionsInitialized;
#endif

    public static bool TryReadScreenPosition(out Vector2 screenPosition)
    {
        EnsureActions();

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        if (positionAction != null)
        {
            screenPosition = positionAction.ReadValue<Vector2>();
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        screenPosition = Input.mousePosition;
        return true;
#else
        screenPosition = default;
        return false;
#endif
    }

    public static bool WasLeftButtonPressedThisFrame()
    {
        EnsureActions();

#if ENABLE_INPUT_SYSTEM
        if (leftClickAction != null && leftClickAction.WasPressedThisFrame())
        {
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }

    public static bool WasRightButtonPressedThisFrame()
    {
        EnsureActions();

#if ENABLE_INPUT_SYSTEM
        if (rightClickAction != null && rightClickAction.WasPressedThisFrame())
        {
            return true;
        }

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(1);
#else
        return false;
#endif
    }

    public static Vector3 ScreenToWorld2D(Camera camera, Vector2 screenPosition)
    {
        float zDistance = camera.orthographic
            ? Mathf.Abs(camera.transform.position.z)
            : camera.WorldToScreenPoint(Vector3.zero).z;
        Vector3 world = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, zDistance));
        world.z = 0f;
        return world;
    }

    public static bool TryReadWorldTile(Camera camera, SandboxWorld world, out Vector2Int tile)
    {
        tile = default;
        if (camera == null || world == null || !TryReadScreenPosition(out Vector2 screen))
        {
            return false;
        }

        Vector3 worldPoint = ScreenToWorld2D(camera, screen);
        tile = world.WorldPositionToTile(worldPoint);
        return true;
    }

#if ENABLE_INPUT_SYSTEM
    private static void EnsureActions()
    {
        if (actionsInitialized)
        {
            return;
        }

        actionsInitialized = true;
        leftClickAction = new InputAction("SandboxLeftClick", InputActionType.Button, "<Mouse>/leftButton");
        rightClickAction = new InputAction("SandboxRightClick", InputActionType.Button, "<Mouse>/rightButton");
        positionAction = new InputAction("SandboxPointerPosition", InputActionType.Value, "<Mouse>/position");
        leftClickAction.Enable();
        rightClickAction.Enable();
        positionAction.Enable();
    }
#else
    private static void EnsureActions()
    {
    }
#endif
}
