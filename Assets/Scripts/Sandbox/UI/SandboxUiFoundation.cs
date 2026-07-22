using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Pure scale policy shared by the runtime scaler and EditMode tests.</summary>
public static class SandboxUiScalePolicy
{
    public static float ComputeScale(
        float screenWidth,
        float screenHeight,
        Vector2 referenceResolution,
        int pixelModule,
        float requestedScale = 0f)
    {
        if (screenWidth <= 0f || screenHeight <= 0f ||
            referenceResolution.x <= 0f || referenceResolution.y <= 0f)
        {
            return 1f;
        }

        int module = Mathf.Max(1, pixelModule);
        float quantum = 1f / module;
        float fit = Mathf.Min(screenWidth / referenceResolution.x, screenHeight / referenceResolution.y);
        float maximum = Mathf.Max(1f, Mathf.Floor((fit + 0.0001f) / quantum) * quantum);
        if (requestedScale <= 0f)
        {
            return maximum;
        }

        float snappedRequest = Mathf.Floor((requestedScale + 0.0001f) / quantum) * quantum;
        return Mathf.Clamp(snappedRequest, 1f, maximum);
    }
}

/// <summary>Pure viewport and resize math for safe-area-aware panels.</summary>
public static class SandboxUiLayout
{
    public static Vector2 ClampSize(Vector2 desired, Vector2 minimum, Vector2 maximum)
    {
        float maxWidth = Mathf.Max(0f, maximum.x);
        float maxHeight = Mathf.Max(0f, maximum.y);
        float minWidth = Mathf.Clamp(minimum.x, 0f, maxWidth);
        float minHeight = Mathf.Clamp(minimum.y, 0f, maxHeight);
        return new Vector2(
            Mathf.Clamp(desired.x, minWidth, maxWidth),
            Mathf.Clamp(desired.y, minHeight, maxHeight));
    }

    public static Rect ClampPanel(Rect desired, Rect safeArea, Vector2 minimum, Vector2 maximum)
    {
        Vector2 available = new Vector2(Mathf.Max(0f, safeArea.width), Mathf.Max(0f, safeArea.height));
        Vector2 cappedMaximum = new Vector2(
            Mathf.Min(maximum.x, available.x),
            Mathf.Min(maximum.y, available.y));
        Vector2 size = ClampSize(desired.size, minimum, cappedMaximum);
        float x = Mathf.Clamp(desired.x, safeArea.xMin, safeArea.xMax - size.x);
        float y = Mathf.Clamp(desired.y, safeArea.yMin, safeArea.yMax - size.y);
        return new Rect(x, y, size.x, size.y);
    }
}

/// <summary>Deterministic navigation for fixed-column item grids.</summary>
public static class SandboxUiGridNavigation
{
    public static int Move(int current, int columnDelta, int rowDelta, int columns, int itemCount)
    {
        if (columns <= 0 || itemCount <= 0)
        {
            return -1;
        }

        int index = Mathf.Clamp(current, 0, itemCount - 1);
        int row = index / columns;
        int column = index % columns;
        int lastRow = (itemCount - 1) / columns;

        if (columnDelta != 0)
        {
            int targetColumn = Mathf.Clamp(column + Math.Sign(columnDelta), 0, columns - 1);
            int target = row * columns + targetColumn;
            index = target < itemCount ? target : itemCount - 1;
            row = index / columns;
            column = index % columns;
        }

        if (rowDelta != 0)
        {
            int targetRow = Mathf.Clamp(row + Math.Sign(rowDelta), 0, lastRow);
            int target = targetRow * columns + column;
            index = Mathf.Min(target, itemCount - 1);
        }

        return index;
    }
}

/// <summary>Small presentation stack that owns dismissal, modal focus, and gameplay blocking state.</summary>
public sealed class SandboxUiScreenStack
{
    private readonly List<Entry> entries = new List<Entry>();

    public event Action Changed;

    public int Count => entries.Count;
    public object TopOwner => entries.Count > 0 ? entries[entries.Count - 1].Owner : null;

    public bool GameplayInputBlocked
    {
        get
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].BlocksGameplayInput)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public void Push(object owner, bool blocksGameplayInput, bool isModal)
    {
        if (owner == null)
        {
            throw new ArgumentNullException(nameof(owner));
        }

        Pop(owner);
        entries.Add(new Entry(owner, blocksGameplayInput, isModal));
        Changed?.Invoke();
    }

    public bool Pop(object owner)
    {
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            if (!ReferenceEquals(entries[i].Owner, owner))
            {
                continue;
            }

            entries.RemoveAt(i);
            Changed?.Invoke();
            return true;
        }

        return false;
    }

    public bool IsFocusAllowed(object owner)
    {
        object modalOwner = null;
        for (int i = entries.Count - 1; i >= 0; i--)
        {
            if (entries[i].IsModal)
            {
                modalOwner = entries[i].Owner;
                break;
            }
        }

        return modalOwner == null || ReferenceEquals(modalOwner, owner);
    }

    private readonly struct Entry
    {
        public Entry(object owner, bool blocksGameplayInput, bool isModal)
        {
            Owner = owner;
            BlocksGameplayInput = blocksGameplayInput;
            IsModal = isModal;
        }

        public object Owner { get; }
        public bool BlocksGameplayInput { get; }
        public bool IsModal { get; }
    }
}

/// <summary>Read-only global gate consumed by gameplay input code.</summary>
public static class SandboxUiInputGate
{
    public static bool IsGameplayInputBlocked { get; private set; }

    public static void SetGameplayInputBlocked(bool blocked)
    {
        IsGameplayInputBlocked = blocked;
    }
}

public static class SandboxUiTooltipTiming
{
    public static bool ShouldShow(float currentTime, float enteredAt, float delay)
    {
        return currentTime >= enteredAt + Mathf.Max(0f, delay);
    }
}
