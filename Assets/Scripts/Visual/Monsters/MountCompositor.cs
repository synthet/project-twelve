using System.Collections.Generic;
using ProjectTwelve.Visual.Characters;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace ProjectTwelve.Visual.Monsters
{
    /// <summary>
    /// Composites hero layer pixels onto mount textures at authored frame positions.
    /// </summary>
    public sealed class MountCompositor : MonoBehaviour
    {
        [SerializeField] private Texture2D mountTexture;
        [SerializeField] private SpriteLibrary mountLibrary;
        [SerializeField] private LayeredCharacterVisual rider;

        private static readonly Dictionary<Vector2Int, string> FrameTargets = BuildFrameTargets();

        /// <summary>
        /// Applies rider layers onto the mount texture and updates the mount sprite library.
        /// </summary>
        public void ApplyMount(CharacterComposer composer)
        {
            if (mountTexture == null || composer == null || rider == null)
            {
                return;
            }

            Dictionary<string, Color32[]> layers = composer.BuildLayers();
            Texture2D composed = new Texture2D(mountTexture.width, mountTexture.height)
            {
                filterMode = FilterMode.Point
            };
            composed.SetPixels32(mountTexture.GetPixels32());

            Dictionary<string, Rect> frames = CharacterSheetLayout.BuildFrameRects();
            foreach (KeyValuePair<Vector2Int, string> target in FrameTargets)
            {
                if (!frames.TryGetValue(target.Value, out Rect block))
                {
                    continue;
                }

                BlitFrame(composed, composer.Texture, block, target.Key, layers, target.Value);
            }

            composed.Apply();
            ApplyMountLibrary(composed);
        }

        private static void BlitFrame(
            Texture2D destination,
            Texture2D sourceSheet,
            Rect block,
            Vector2Int targetOrigin,
            Dictionary<string, Color32[]> layers,
            string frameKey)
        {
            Color32[] sheet = sourceSheet.GetPixels32();
            int blockWidth = (int)block.width;
            int blockHeight = (int)block.height;

            for (int y = 0; y < blockHeight; y++)
            {
                for (int x = 0; x < blockWidth; x++)
                {
                    int sourceIndex = (int)block.x + x + ((int)block.y + y) * sourceSheet.width;
                    Color32 pixel = sheet[sourceIndex];
                    int dx = targetOrigin.x + x;
                    int dy = targetOrigin.y + y;
                    if (pixel.a > 0 && dx >= 0 && dy >= 0 && dx < destination.width && dy < destination.height)
                    {
                        destination.SetPixel(dx, dy, pixel);
                    }
                }
            }

            if (frameKey.Contains("Idle") || frameKey.Contains("Ready"))
            {
                OverlayLayer(destination, layers, block, targetOrigin, "Weapon");
            }

            if (frameKey.Contains("Jab") || frameKey.Contains("Slash") || frameKey.Contains("Jump"))
            {
                OverlayLayer(destination, layers, block, targetOrigin, "Arms");
                OverlayLayer(destination, layers, block, targetOrigin, "Bracers");
                OverlayLayer(destination, layers, block, targetOrigin, "Weapon");
            }
        }

        private static void OverlayLayer(
            Texture2D destination,
            Dictionary<string, Color32[]> layers,
            Rect block,
            Vector2Int targetOrigin,
            string layerName)
        {
            if (!layers.TryGetValue(layerName, out Color32[] layerPixels))
            {
                return;
            }

            int blockWidth = (int)block.width;
            int blockHeight = (int)block.height;
            for (int y = 0; y < blockHeight; y++)
            {
                for (int x = 0; x < blockWidth; x++)
                {
                    int sourceIndex = (int)block.x + x + ((int)block.y + y) * CharacterSheetLayout.Width;
                    Color32 pixel = layerPixels[sourceIndex];
                    if (pixel.a == 0)
                    {
                        continue;
                    }

                    int dx = targetOrigin.x + x;
                    int dy = targetOrigin.y + y;
                    if (dx >= 0 && dy >= 0 && dx < destination.width && dy < destination.height)
                    {
                        destination.SetPixel(dx, dy, pixel);
                    }
                }
            }
        }

        private void ApplyMountLibrary(Texture2D composed)
        {
            if (mountLibrary == null)
            {
                return;
            }

            SpriteLibraryAsset asset = ScriptableObject.CreateInstance<SpriteLibraryAsset>();
            Sprite sprite = Sprite.Create(
                composed,
                new Rect(0, 0, composed.width, composed.height),
                new Vector2(0.5f, 0.5f),
                CharacterSheetLayout.PixelsPerUnit);
            asset.AddCategoryLabel(sprite, "Mount", "Default");
            mountLibrary.spriteLibraryAsset = asset;
        }

        private static Dictionary<Vector2Int, string> BuildFrameTargets()
        {
            return new Dictionary<Vector2Int, string>
            {
                { new Vector2Int(0, 259), "Idle_0" },
                { new Vector2Int(64, 259), "Idle_0" },
                { new Vector2Int(128, 259), "Idle_1" },
                { new Vector2Int(192, 259), "Idle_1" },
                { new Vector2Int(0, 196), "Ready_1" },
                { new Vector2Int(64, 196), "Ready_0" },
                { new Vector2Int(0, 131), "Jab_0" },
                { new Vector2Int(0, 67), "Slash_0" },
                { new Vector2Int(0, 2), "Jump_1" },
            };
        }
    }
}
