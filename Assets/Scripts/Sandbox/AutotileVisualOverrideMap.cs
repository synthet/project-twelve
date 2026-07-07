using System;
using System.Collections.Generic;
using ProjectTwelve.Visual.Tiles;
using UnityEngine;

/// <summary>
/// Rendering-only visual overrides keyed by world tile cell, visual layer, and active tileset name.
/// </summary>
[Serializable]
public sealed class AutotileVisualOverrideMap
{
    public const string GroundLayer = AutotileVisualLayerNames.Ground;
    public const string CoverLayer = AutotileVisualLayerNames.Cover;

    private readonly Dictionary<Key, AutotileVisualOverride> overrides = new Dictionary<Key, AutotileVisualOverride>();

    public int Count => overrides.Count;

    public bool HasOverrides => overrides.Count > 0;

    public bool TryGetOverride(Vector2Int cell, string layer, string tilesetName, out AutotileVisualOverride entry)
    {
        return overrides.TryGetValue(new Key(cell, layer, tilesetName), out entry);
    }

    public bool TryGetOverride(Vector2Int cell, AutotileVisualLayer layer, string tilesetName, out AutotileVisualOverride entry)
    {
        return TryGetOverride(cell, AutotileVisualLayerNames.ToName(layer), tilesetName, out entry);
    }

    public void SetOverride(AutotileVisualOverride entry)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        if (string.IsNullOrEmpty(entry.layer))
        {
            throw new ArgumentException("Override layer must be provided.", nameof(entry));
        }

        if (string.IsNullOrEmpty(entry.tileset))
        {
            throw new ArgumentException("Override tileset name must be provided.", nameof(entry));
        }

        if (string.IsNullOrEmpty(entry.overrideSpriteId))
        {
            ClearOverride(entry.Cell, entry.layer, entry.tileset);
            return;
        }

        overrides[new Key(entry.Cell, entry.layer, entry.tileset)] = entry;
    }

    public void SetOverride(Vector2Int cell, string layer, string tilesetName, string overrideSpriteId)
    {
        if (TryGetOverride(cell, layer, tilesetName, out AutotileVisualOverride existing))
        {
            existing.overrideSpriteId = overrideSpriteId ?? string.Empty;
            SetOverride(existing);
            return;
        }

        SetOverride(new AutotileVisualOverride(
            cell,
            AutotileVisualLayerNames.Parse(layer),
            tilesetName,
            autoSpriteId: string.Empty,
            autoFlipX: false,
            overrideSpriteId: overrideSpriteId));
    }

    public bool ClearOverride(Vector2Int cell, string layer, string tilesetName)
    {
        return overrides.Remove(new Key(cell, layer, tilesetName));
    }

    public bool ClearOverride(int x, int y, string layer, string tilesetName)
    {
        return ClearOverride(new Vector2Int(x, y), layer, tilesetName);
    }

    public void ClearAtCell(int x, int y)
    {
        Vector2Int cell = new Vector2Int(x, y);
        List<Key> toRemove = new List<Key>();
        foreach (KeyValuePair<Key, AutotileVisualOverride> pair in overrides)
        {
            if (pair.Key.Cell == cell)
            {
                toRemove.Add(pair.Key);
            }
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            overrides.Remove(toRemove[i]);
        }
    }

    public IEnumerable<AutotileVisualOverride> GetAll()
    {
        return overrides.Values;
    }

    public void Clear()
    {
        overrides.Clear();
    }

    private readonly struct Key : IEquatable<Key>
    {
        public Vector2Int Cell { get; }

        public Key(Vector2Int cell, string layer, string tilesetName)
        {
            Cell = cell;
            Layer = layer ?? string.Empty;
            TilesetName = tilesetName ?? string.Empty;
        }

        private readonly string Layer;
        private readonly string TilesetName;

        public bool Equals(Key other)
        {
            return Cell == other.Cell
                && string.Equals(Layer, other.Layer, StringComparison.Ordinal)
                && string.Equals(TilesetName, other.TilesetName, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is Key other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Cell.GetHashCode();
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Layer);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(TilesetName);
                return hash;
            }
        }
    }
}
