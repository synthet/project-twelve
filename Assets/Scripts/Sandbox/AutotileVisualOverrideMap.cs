using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rendering-only sprite overrides for autotile visuals.
/// Overrides are keyed by world tile cell, visual layer, and active tileset name so tile identity,
/// generation, collision, navigation, fluid, and save palette code remain unaware of them.
/// </summary>
[Serializable]
public sealed class AutotileVisualOverrideMap
{
    public const string GroundLayer = "ground";
    public const string CoverLayer = "cover";

    private readonly Dictionary<Key, string> overrides = new Dictionary<Key, string>();

    public bool TryGetOverride(Vector2Int cell, string layer, string tilesetName, out string spriteId)
    {
        return overrides.TryGetValue(new Key(cell, layer, tilesetName), out spriteId);
    }

    public void SetOverride(Vector2Int cell, string layer, string tilesetName, string spriteId)
    {
        if (string.IsNullOrEmpty(layer))
        {
            throw new ArgumentException("Override layer must be provided.", nameof(layer));
        }

        if (string.IsNullOrEmpty(tilesetName))
        {
            throw new ArgumentException("Override tileset name must be provided.", nameof(tilesetName));
        }

        if (string.IsNullOrEmpty(spriteId))
        {
            ClearOverride(cell, layer, tilesetName);
            return;
        }

        overrides[new Key(cell, layer, tilesetName)] = spriteId;
    }

    public bool ClearOverride(Vector2Int cell, string layer, string tilesetName)
    {
        return overrides.Remove(new Key(cell, layer, tilesetName));
    }

    public void Clear()
    {
        overrides.Clear();
    }

    private readonly struct Key : IEquatable<Key>
    {
        private readonly Vector2Int cell;
        private readonly string layer;
        private readonly string tilesetName;

        public Key(Vector2Int cell, string layer, string tilesetName)
        {
            this.cell = cell;
            this.layer = layer ?? string.Empty;
            this.tilesetName = tilesetName ?? string.Empty;
        }

        public bool Equals(Key other)
        {
            return cell == other.cell
                && string.Equals(layer, other.layer, StringComparison.Ordinal)
                && string.Equals(tilesetName, other.tilesetName, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is Key other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = cell.GetHashCode();
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(layer);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(tilesetName);
                return hash;
            }
        }
    }
}
