namespace UnityEngine;

public readonly struct Vector2Int : System.IEquatable<Vector2Int>
{
    public static Vector2Int zero => new Vector2Int(0, 0);

    public readonly int x;
    public readonly int y;

    public Vector2Int(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public bool Equals(Vector2Int other) => x == other.x && y == other.y;

    public override bool Equals(object? obj) => obj is Vector2Int other && Equals(other);

    public override int GetHashCode() => System.HashCode.Combine(x, y);

    public override string ToString() => $"({x}, {y})";

    public static bool operator ==(Vector2Int left, Vector2Int right) => left.Equals(right);

    public static bool operator !=(Vector2Int left, Vector2Int right) => !left.Equals(right);
}

public static class Mathf
{
    public static int RoundToInt(float value) => (int)System.MathF.Round(value, System.MidpointRounding.AwayFromZero);

    public static float PerlinNoise(float x, float y)
    {
        int hash = Hash(System.BitConverter.SingleToInt32Bits(x), System.BitConverter.SingleToInt32Bits(y));
        return (hash & 0x00ffffff) / 16777215f;
    }

    private static int Hash(int x, int y)
    {
        unchecked
        {
            int hash = 17;
            hash = (hash * 31) ^ x;
            hash = (hash * 31) ^ y;
            hash ^= hash >> 16;
            hash *= unchecked((int)0x7feb352d);
            hash ^= hash >> 15;
            hash *= unchecked((int)0x846ca68b);
            hash ^= hash >> 16;
            return hash;
        }
    }
}
