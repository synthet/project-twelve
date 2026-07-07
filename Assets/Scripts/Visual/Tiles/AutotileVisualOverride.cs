using System;
using UnityEngine;

namespace ProjectTwelve.Visual.Tiles
{
    /// <summary>
    /// Debug-only visual override for one autotile cell, layer, and tileset.
    /// </summary>
    [Serializable]
    public sealed class AutotileVisualOverride
    {
        public int x;
        public int y;
        public string layer = AutotileVisualLayerNames.Ground;
        public string tileset;
        public string autoSpriteId;
        public bool autoFlipX;
        public string overrideSpriteId;
        public bool overrideFlipX;
        public bool overrideFlipY;
        public int rotation;
        public string note;

        public Vector2Int Cell => new Vector2Int(x, y);

        public AutotileVisualLayer Layer => AutotileVisualLayerNames.Parse(layer);

        public AutotileVisualOverride()
        {
        }

        public AutotileVisualOverride(
            Vector2Int cell,
            AutotileVisualLayer layer,
            string tilesetName,
            string autoSpriteId,
            bool autoFlipX,
            string overrideSpriteId,
            bool overrideFlipX = false,
            bool overrideFlipY = false,
            int rotationDegrees = 0,
            string note = null)
        {
            x = cell.x;
            y = cell.y;
            this.layer = AutotileVisualLayerNames.ToName(layer);
            tileset = tilesetName ?? string.Empty;
            this.autoSpriteId = autoSpriteId ?? string.Empty;
            this.autoFlipX = autoFlipX;
            this.overrideSpriteId = overrideSpriteId ?? string.Empty;
            this.overrideFlipX = overrideFlipX;
            this.overrideFlipY = overrideFlipY;
            rotation = NormalizeRotation(rotationDegrees);
            this.note = note ?? string.Empty;
        }

        public static int NormalizeRotation(int rotationDegrees)
        {
            int normalized = rotationDegrees % 360;
            if (normalized < 0)
            {
                normalized += 360;
            }

            return normalized switch
            {
                0 or 90 or 180 or 270 => normalized,
                _ => throw new ArgumentOutOfRangeException(nameof(rotationDegrees), rotationDegrees,
                    "Visual override rotation must normalize to 0, 90, 180, or 270.")
            };
        }

        public SandboxVisualOverrideEntrySaveData ToSaveData()
        {
            return new SandboxVisualOverrideEntrySaveData
            {
                x = x,
                y = y,
                layer = layer,
                tileset = tileset,
                autoSpriteId = autoSpriteId,
                autoFlipX = autoFlipX,
                overrideSpriteId = overrideSpriteId,
                overrideFlipX = overrideFlipX,
                overrideFlipY = overrideFlipY,
                rotation = rotation,
                note = note ?? string.Empty,
            };
        }

        public static AutotileVisualOverride FromSaveData(SandboxVisualOverrideEntrySaveData saveData)
        {
            if (saveData == null)
            {
                throw new ArgumentNullException(nameof(saveData));
            }

            return new AutotileVisualOverride(
                saveData.ToCoord(),
                AutotileVisualLayerNames.Parse(saveData.layer),
                saveData.tileset,
                saveData.autoSpriteId,
                saveData.autoFlipX,
                saveData.overrideSpriteId,
                saveData.overrideFlipX,
                saveData.overrideFlipY,
                saveData.rotation,
                saveData.note);
        }
    }
}
