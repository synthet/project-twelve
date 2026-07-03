using System;
using System.Collections.Generic;

namespace ProjectTwelve.Sandbox.Registry
{
    /// <summary>
    /// String ID → runtime index map captured at save time and written into the save header
    /// (P2-SAVE-001), so persisted tiles survive registry reordering and insertions.
    /// See docs/wiki/12-modding.md § "Save palette".
    /// </summary>
    [Serializable]
    public sealed class RegistryPalette
    {
        [Serializable]
        public struct Entry
        {
            public string id;
            public int runtimeIndex;

            public Entry(string id, int runtimeIndex)
            {
                this.id = id;
                this.runtimeIndex = runtimeIndex;
            }
        }

        public List<Entry> entries = new List<Entry>();

        /// <summary>Captures the full string ID → runtime index map of a frozen registry.</summary>
        public static RegistryPalette Capture<TDef>(ContentRegistry<TDef> registry)
            where TDef : class, IContentDefinition
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            RegistryPalette palette = new RegistryPalette();
            IReadOnlyList<TDef> all = registry.All;
            for (int i = 0; i < all.Count; i++)
            {
                palette.entries.Add(new Entry(all[i].Id, i));
            }

            return palette;
        }

        /// <summary>
        /// Resolves this palette against the current frozen registry, returning a
        /// saved-index → current-index remap array. A palette entry whose string ID no longer
        /// resolves, or with a negative/duplicate saved index, is an explicit error — persisted
        /// tiles are never silently remapped to a default.
        /// </summary>
        public int[] BuildRemap<TDef>(ContentRegistry<TDef> registry)
            where TDef : class, IContentDefinition
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            int maxIndex = -1;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].runtimeIndex < 0)
                {
                    throw new InvalidOperationException($"Palette entry '{entries[i].id}' has negative saved index {entries[i].runtimeIndex}.");
                }

                maxIndex = Math.Max(maxIndex, entries[i].runtimeIndex);
            }

            int[] remap = new int[maxIndex + 1];
            bool[] seen = new bool[maxIndex + 1];
            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (seen[entry.runtimeIndex])
                {
                    throw new InvalidOperationException($"Palette contains duplicate saved index {entry.runtimeIndex}.");
                }

                seen[entry.runtimeIndex] = true;
                remap[entry.runtimeIndex] = registry.GetIndex(entry.id);
            }

            for (int i = 0; i <= maxIndex; i++)
            {
                if (!seen[i])
                {
                    throw new InvalidOperationException($"Palette has no entry for saved index {i}; refusing to guess a mapping.");
                }
            }

            return remap;
        }
    }
}
