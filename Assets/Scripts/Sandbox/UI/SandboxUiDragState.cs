using System;

public enum SandboxInventoryOperationResult
{
    Success,
    InvalidSource,
    InvalidDestination,
    InvalidAmount,
    Rejected
}

public interface ISandboxInventoryCommandHandler
{
    SandboxInventoryOperationResult TryMove(int sourceIndex, int destinationIndex, int amount);
}

/// <summary>Presentation-only drag state. Inventory rules remain behind the command handler.</summary>
public sealed class SandboxUiDragState
{
    private int sourceIndex = -1;
    private int amount;

    public event Action<int> RefreshRequested;

    public bool IsDragging => sourceIndex >= 0;
    public int SourceIndex => sourceIndex;
    public int Amount => amount;

    public bool Begin(int source, int dragAmount)
    {
        if (source < 0 || dragAmount <= 0)
        {
            return false;
        }

        sourceIndex = source;
        amount = dragAmount;
        return true;
    }

    public SandboxInventoryOperationResult Drop(int destination, ISandboxInventoryCommandHandler commands)
    {
        if (!IsDragging)
        {
            return SandboxInventoryOperationResult.InvalidSource;
        }

        if (commands == null)
        {
            throw new ArgumentNullException(nameof(commands));
        }

        int originalSource = sourceIndex;
        int originalAmount = amount;
        Clear();
        SandboxInventoryOperationResult result = commands.TryMove(originalSource, destination, originalAmount);
        RefreshRequested?.Invoke(originalSource);
        if (destination != originalSource)
        {
            RefreshRequested?.Invoke(destination);
        }

        return result;
    }

    public bool Cancel()
    {
        if (!IsDragging)
        {
            return false;
        }

        int originalSource = sourceIndex;
        Clear();
        RefreshRequested?.Invoke(originalSource);
        return true;
    }

    private void Clear()
    {
        sourceIndex = -1;
        amount = 0;
    }
}
