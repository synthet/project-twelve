using System;
using System.Collections.Generic;
using System.Linq;
using ProjectTwelve.Visual.Creatures;
using UnityEngine;
using UnityEngine.U2D.Animation;
using Random = UnityEngine.Random;

namespace ProjectTwelve.Visual.Characters
{
    /// <summary>
    /// Runtime hero layer composer: merges equipment layers into a sprite library asset.
    /// </summary>
    public sealed class CharacterComposer : MonoBehaviour
    {
        [SerializeField] internal CharacterLayerCatalog layerCatalog;
        [SerializeField] internal LayeredCharacterVisual character;
        [SerializeField] internal bool rebuildOnStart = true;

        [Header("Equipment")]
        public string Body = "Human";
        public string Head = "Human";
        public string Ears = "Human";
        public string Eyes = "Human";
        public string Hair;
        public string Armor;
        public string Helmet;
        public string Weapon;
        public string Firearm;
        public string Shield;
        public string Cape;
        public string Back;
        public string Mask;

        private Texture2D texture;
        private Dictionary<string, Sprite> sprites;

        public Texture2D Texture => texture;

        private void Awake()
        {
            if (rebuildOnStart)
            {
                Rebuild();
            }
        }

        /// <summary>
        /// Rebuilds the combined hero texture and sprite library.
        /// </summary>
        public void Rebuild()
        {
            if (layerCatalog == null || character == null)
            {
                return;
            }

            Dictionary<string, Color32[]> layers = BuildLayers();
            if (layers.Count == 0)
            {
                return;
            }

            if (texture == null)
            {
                texture = new Texture2D(CharacterSheetLayout.Width, CharacterSheetLayout.Height)
                {
                    filterMode = FilterMode.Point
                };
            }

            Color32[] merged = new Color32[CharacterSheetLayout.Width * CharacterSheetLayout.Height];
            LayerTextureComposer.MergeLayers(merged, layers.Values.ToList());
            texture.SetPixels32(merged);
            LayerTextureComposer.ClearRect(merged, CharacterSheetLayout.Width, 0, CharacterSheetLayout.Height - 32, 32, 32);
            texture.SetPixels32(merged);
            texture.Apply();

            ApplySpriteLibrary();
            ApplyFirearm(layers);
        }

        /// <summary>
        /// Builds layer pixel buffers in merge order.
        /// </summary>
        public Dictionary<string, Color32[]> BuildLayers()
        {
            Dictionary<string, Color32[]> layers = new Dictionary<string, Color32[]>();
            if (layerCatalog == null)
            {
                return layers;
            }

            if (Head.Contains("Lizard"))
            {
                Hair = Helmet = Mask = string.Empty;
            }

            TryAddLayer(layers, "Back", Back);
            TryAddLayer(layers, "Shield", Shield);
            TryAddLayer(layers, "Body", Body);
            if (string.IsNullOrEmpty(Firearm))
            {
                TryAddLayer(layers, "Arms", Body);
            }

            TryAddLayer(layers, "Head", Head);
            if (string.IsNullOrEmpty(Helmet) || Helmet.Contains("[ShowEars]"))
            {
                TryAddLayer(layers, "Ears", Ears);
            }

            TryAddLayer(layers, "Armor", Armor);
            if (string.IsNullOrEmpty(Firearm))
            {
                TryAddLayer(layers, "Bracers", Armor);
            }

            TryAddLayer(layers, "Eyes", Eyes);
            TryAddLayer(layers, "Hair", Hair, layers.ContainsKey("Head") ? layers["Head"] : null);
            TryAddLayer(layers, "Cape", Cape);
            TryAddLayer(layers, "Helmet", Helmet);
            TryAddLayer(layers, "Weapon", Weapon);
            TryAddLayer(layers, "Mask", Mask);

            return layers;
        }

        public void RandomizeEquipment(bool helmet = true, bool armor = true, bool weapon = true, bool shield = true)
        {
            if (helmet)
            {
                Helmet = RandomizeLayer("Helmet", 20);
            }

            if (armor)
            {
                Armor = RandomizeLayer("Armor", 20);
            }

            if (weapon)
            {
                Weapon = RandomizeLayer("Weapon");
            }

            bool bow = Weapon != null && Weapon.Contains("Bow");
            bool gun = Weapon != null && Weapon.Contains("Gun");
            Shield = bow || gun ? string.Empty : shield ? RandomizeLayer("Shield", 50) : Shield;
            Back = bow ? "LeatherQuiver" : string.Empty;
        }

        public void RandomizeHumanAppearance()
        {
            string[] colors =
            {
                "3D3D3D", "5D5D5D", "858585", "C7CFDD", "5D2C28", "8A4836", "BF6F4A", "E69C69", "F6CA9F"
            };
            string color = colors[Random.Range(0, colors.Length)];
            Hair = $"{RandomizeLayer("Hair", 20)}#{color}";
        }

        public void RandomizeRace()
        {
            Body = RandomizeLayer("Body");
            string race = System.Text.RegularExpressions.Regex.Replace(Body, @"\d", string.Empty);
            Head = RandomizeMatching("Head", race);
            Eyes = RandomizeMatching("Eyes", race);
            Ears = RandomizeMatching("Ears", race);
        }

        private void TryAddLayer(
            Dictionary<string, Color32[]> layers,
            string layerName,
            string layerData,
            Color32[] mask = null)
        {
            if (string.IsNullOrEmpty(layerData) || !layerCatalog.TryGetLayer(layerName, out CharacterLayerEntry entry))
            {
                return;
            }

            Color32[] pixels = entry.GetPixels(
                layerData,
                CharacterSheetLayout.Width,
                CharacterSheetLayout.Height,
                mask);
            if (pixels != null)
            {
                layers[layerName] = pixels;
            }
        }

        private string RandomizeLayer(string layerName, int emptyChance = 0)
        {
            if (!layerCatalog.TryGetLayer(layerName, out CharacterLayerEntry entry)
                || entry.Textures.Count == 0)
            {
                return string.Empty;
            }

            if (emptyChance > 0 && Random.Range(0, 100) < emptyChance)
            {
                return string.Empty;
            }

            int index = Random.Range(0, entry.Textures.Count);
            return entry.Textures[index].name;
        }

        private string RandomizeMatching(string layerName, string prefix)
        {
            if (!layerCatalog.TryGetLayer(layerName, out CharacterLayerEntry entry))
            {
                return string.Empty;
            }

            List<Texture2D> matches = entry.Textures.Where(t => t != null && t.name.StartsWith(prefix)).ToList();
            if (matches.Count == 0)
            {
                return RandomizeLayer(layerName);
            }

            return matches[Random.Range(0, matches.Count)].name;
        }

        private void ApplySpriteLibrary()
        {
            sprites ??= new Dictionary<string, Sprite>();
            sprites.Clear();

            Dictionary<string, Rect> frames = CharacterSheetLayout.BuildFrameRects();
            foreach (KeyValuePair<string, Rect> frame in frames)
            {
                sprites[frame.Key] = Sprite.Create(
                    texture,
                    frame.Value,
                    CharacterSheetLayout.Pivot,
                    CharacterSheetLayout.PixelsPerUnit,
                    0,
                    SpriteMeshType.FullRect);
            }

            SpriteLibraryAsset library = ScriptableObject.CreateInstance<SpriteLibraryAsset>();
            foreach (KeyValuePair<string, Sprite> sprite in sprites)
            {
                string[] split = sprite.Key.Split('_');
                library.AddCategoryLabel(sprite.Value, split[0], split[1]);
            }

            character.ApplySpriteLibrary(library);
        }

        private void ApplyFirearm(Dictionary<string, Color32[]> layers)
        {
            FirearmVisual firearmVisual = character.Firearm;
            if (firearmVisual?.Renderer == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(Firearm))
            {
                firearmVisual.Renderer.enabled = false;
                return;
            }

            if (!layerCatalog.TryGetLayer("Firearm", out CharacterLayerEntry entry))
            {
                firearmVisual.Renderer.enabled = false;
                return;
            }

            Color32[] pixels = entry.GetPixels(Firearm, CharacterSheetLayout.Width, CharacterSheetLayout.Height);
            if (pixels == null)
            {
                firearmVisual.Renderer.enabled = false;
                return;
            }

            Texture2D firearmTexture = new Texture2D(64, 64) { filterMode = FilterMode.Point };
            for (int x = 0; x < 64; x++)
            {
                for (int y = 0; y < 64; y++)
                {
                    int sourceIndex = x + (y + 12 * 64) * CharacterSheetLayout.Width;
                    firearmTexture.SetPixel(x, y, pixels[sourceIndex]);
                }
            }

            firearmTexture.Apply();
            firearmVisual.Renderer.enabled = true;
            firearmVisual.Renderer.sprite = Sprite.Create(
                firearmTexture,
                new Rect(0, 0, 64, 64),
                CharacterSheetLayout.Pivot,
                CharacterSheetLayout.PixelsPerUnit);
            firearmVisual.SetDetachedActive(firearmVisual.Detached);
        }
    }
}
