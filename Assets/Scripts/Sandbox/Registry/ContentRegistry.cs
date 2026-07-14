using System;
using System.Collections.Generic;

namespace ProjectTwelve.Sandbox.Registry
{
    /// <summary>Pure-data content definition addressed by a stable string ID.</summary>
    public interface IContentDefinition
    {
        /// <summary>Stable identity in <c>namespace:name</c> form, e.g. <c>core:dirt</c>.</summary>
        string Id { get; }
    }

    /// <summary>
    /// Registry of content definitions keyed by stable <c>namespace:name</c> string IDs.
    /// Lifecycle: <see cref="Register"/> pre-freeze only, then <see cref="Freeze"/> assigns
    /// deterministic runtime indices (declared empty definition pinned to index 0, remainder
    /// sorted by ordinal string ID) and enables index lookups. Runtime indices are process-local;
    /// persist string IDs via <see cref="RegistryPalette"/>, never bare indices.
    /// See <c>docs/wiki/12-modding.md</c> § "Registry contract (P2-DATA-001)".
    /// </summary>
    public sealed class ContentRegistry<TDef> where TDef : class, IContentDefinition
    {
        private readonly Dictionary<string, TDef> definitionsById =
            new Dictionary<string, TDef>(StringComparer.Ordinal);

        private readonly Dictionary<string, string> aliasById =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private readonly string emptyId;
        private TDef[] definitionsByIndex;
        private Dictionary<string, int> indexById;

        /// <param name="emptyId">
        /// Optional string ID pinned to runtime index 0 at freeze (e.g. <c>core:air</c>), keeping
        /// the <c>default(tile)</c>-is-empty invariant. Must be registered before freeze.
        /// </param>
        public ContentRegistry(string emptyId = null)
        {
            if (emptyId != null && !IsValidId(emptyId))
            {
                throw new ArgumentException($"Empty ID '{emptyId}' is not a valid 'namespace:name' ID.", nameof(emptyId));
            }

            this.emptyId = emptyId;
        }

        public bool IsFrozen => definitionsByIndex != null;

        public int Count => definitionsById.Count;

        /// <summary>Stable index order after freeze; index 0 is the empty definition when declared.</summary>
        public IReadOnlyList<TDef> All
        {
            get
            {
                RequireFrozen();
                return definitionsByIndex;
            }
        }

        /// <summary>Adds a definition. Pre-freeze only; duplicate or malformed string IDs throw.</summary>
        public void Register(TDef def)
        {
            if (IsFrozen)
            {
                throw new InvalidOperationException("Registry is frozen; definitions cannot be registered after Freeze().");
            }

            if (def == null)
            {
                throw new ArgumentNullException(nameof(def));
            }

            if (!IsValidId(def.Id))
            {
                throw new ArgumentException($"Definition ID '{def.Id}' is not a valid 'namespace:name' ID (lowercase [a-z0-9_] segments).", nameof(def));
            }

            if (definitionsById.ContainsKey(def.Id))
            {
                throw new ArgumentException($"Duplicate definition ID '{def.Id}'.", nameof(def));
            }

            if (aliasById.ContainsKey(def.Id))
            {
                throw new ArgumentException($"Definition ID '{def.Id}' is already registered as an alias.", nameof(def));
            }

            definitionsById.Add(def.Id, def);
        }

        /// <summary>
        /// Maps a retired string ID to its canonical replacement so persisted data written before
        /// a rename keeps resolving. Pre-freeze only; alias targets are validated at freeze.
        /// </summary>
        public void RegisterAlias(string aliasId, string canonicalId)
        {
            if (IsFrozen)
            {
                throw new InvalidOperationException("Registry is frozen; aliases cannot be registered after Freeze().");
            }

            if (!IsValidId(aliasId))
            {
                throw new ArgumentException($"Alias ID '{aliasId}' is not a valid 'namespace:name' ID.", nameof(aliasId));
            }

            if (!IsValidId(canonicalId))
            {
                throw new ArgumentException($"Canonical ID '{canonicalId}' is not a valid 'namespace:name' ID.", nameof(canonicalId));
            }

            if (definitionsById.ContainsKey(aliasId))
            {
                throw new ArgumentException($"Alias ID '{aliasId}' is already a registered definition.", nameof(aliasId));
            }

            if (aliasById.ContainsKey(aliasId))
            {
                throw new ArgumentException($"Duplicate alias ID '{aliasId}'.", nameof(aliasId));
            }

            aliasById.Add(aliasId, canonicalId);
        }

        /// <summary>
        /// Freezes the definition set and assigns runtime indices: the declared empty definition
        /// gets index 0, all others get 1..N-1 sorted by ordinal string ID. Deterministic for a
        /// fixed definition set regardless of registration order.
        /// </summary>
        public void Freeze()
        {
            if (IsFrozen)
            {
                throw new InvalidOperationException("Registry is already frozen.");
            }

            if (emptyId != null && !definitionsById.ContainsKey(emptyId))
            {
                throw new InvalidOperationException($"Empty definition '{emptyId}' was declared but never registered.");
            }

            foreach (KeyValuePair<string, string> alias in aliasById)
            {
                if (!definitionsById.ContainsKey(alias.Value))
                {
                    throw new InvalidOperationException(
                        $"Alias '{alias.Key}' targets unregistered definition '{alias.Value}'.");
                }
            }

            List<string> orderedIds = new List<string>(definitionsById.Count);
            foreach (string id in definitionsById.Keys)
            {
                if (id != emptyId)
                {
                    orderedIds.Add(id);
                }
            }

            orderedIds.Sort(StringComparer.Ordinal);
            if (emptyId != null)
            {
                orderedIds.Insert(0, emptyId);
            }

            TDef[] byIndex = new TDef[orderedIds.Count];
            Dictionary<string, int> byId = new Dictionary<string, int>(orderedIds.Count, StringComparer.Ordinal);
            for (int i = 0; i < orderedIds.Count; i++)
            {
                byIndex[i] = definitionsById[orderedIds[i]];
                byId.Add(orderedIds[i], i);
            }

            indexById = byId;
            definitionsByIndex = byIndex;
        }

        /// <summary>O(1) hot-path lookup by runtime index. Post-freeze only.</summary>
        public TDef Get(int runtimeIndex)
        {
            RequireFrozen();
            if (runtimeIndex < 0 || runtimeIndex >= definitionsByIndex.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(runtimeIndex), runtimeIndex, $"No definition at runtime index {runtimeIndex} (count {definitionsByIndex.Length}).");
            }

            return definitionsByIndex[runtimeIndex];
        }

        /// <summary>Startup/tools lookup by string ID. Unknown IDs throw; never a silent default.</summary>
        public TDef Get(string id)
        {
            if (!TryGet(id, out TDef def))
            {
                throw new KeyNotFoundException($"Unknown definition ID '{id}'.");
            }

            return def;
        }

        public bool TryGet(string id, out TDef def)
        {
            def = null;
            if (id == null)
            {
                return false;
            }

            if (definitionsById.TryGetValue(id, out def))
            {
                return true;
            }

            return aliasById.TryGetValue(id, out string canonical)
                && definitionsById.TryGetValue(canonical, out def);
        }

        /// <summary>Runtime index of a string ID. Post-freeze only; unknown IDs throw.</summary>
        public int GetIndex(string id)
        {
            if (!TryGetIndex(id, out int index))
            {
                throw new KeyNotFoundException($"Unknown definition ID '{id}'.");
            }

            return index;
        }

        public bool TryGetIndex(string id, out int index)
        {
            RequireFrozen();
            index = 0;
            if (id == null)
            {
                return false;
            }

            if (indexById.TryGetValue(id, out index))
            {
                return true;
            }

            return aliasById.TryGetValue(id, out string canonical)
                && indexById.TryGetValue(canonical, out index);
        }

        /// <summary>
        /// True when <paramref name="id"/> matches the <c>namespace:name</c> grammar: exactly one
        /// colon separating two non-empty lowercase <c>[a-z0-9_]</c> segments.
        /// </summary>
        public static bool IsValidId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            int colon = id.IndexOf(':');
            if (colon <= 0 || colon >= id.Length - 1 || id.IndexOf(':', colon + 1) >= 0)
            {
                return false;
            }

            for (int i = 0; i < id.Length; i++)
            {
                if (i == colon)
                {
                    continue;
                }

                char c = id[i];
                bool valid = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_';
                if (!valid)
                {
                    return false;
                }
            }

            return true;
        }

        private void RequireFrozen()
        {
            if (!IsFrozen)
            {
                throw new InvalidOperationException("Registry is not frozen yet; call Freeze() before index-based lookups.");
            }
        }
    }
}
