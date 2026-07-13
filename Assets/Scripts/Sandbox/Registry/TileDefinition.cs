namespace ProjectTwelve.Sandbox.Registry
{
    /// <summary>
    /// Pure-data tile definition per docs/wiki/12-modding.md § "Definitions are pure data".
    /// Consumed by collision (Solid), lighting (Opaque, LightEmission), rendering (AtlasSprite),
    /// and inventory (DropItemId, Hardness placeholders for P2-INV-001).
    /// </summary>
    public sealed class TileDefinition : IContentDefinition
    {
        public TileDefinition(
            string id,
            bool solid,
            bool opaque = false,
            byte lightEmission = 0,
            byte? lightAttenuation = null,
            string atlasSprite = null,
            string dropItemId = null,
            float hardness = 1f)
        {
            Id = id;
            Solid = solid;
            Opaque = opaque;
            LightEmission = lightEmission;
            LightAttenuation = lightAttenuation ?? (opaque ? (byte)3 : (byte)1);
            AtlasSprite = atlasSprite;
            DropItemId = dropItemId;
            Hardness = hardness;
        }

        public string Id { get; }

        /// <summary>Blocks movement and pathfinding; feeds collider run-merging.</summary>
        public bool Solid { get; }

        /// <summary>Attenuates light propagation (P2-LIGHT-001).</summary>
        public bool Opaque { get; }

        /// <summary>Emitted light level 0–15 (P2-LIGHT-001).</summary>
        public byte LightEmission { get; }

        /// <summary>Light lost when propagation enters this tile; core opaque tiles use 3.</summary>
        public byte LightAttenuation { get; }

        /// <summary>Visual key resolved by the rendering catalog (ground tileset name today).</summary>
        public string AtlasSprite { get; }

        /// <summary>Item string ID granted on break; null for no drop (P2-INV-001).</summary>
        public string DropItemId { get; }

        /// <summary>Relative break-time placeholder (P2-INV-001).</summary>
        public float Hardness { get; }
    }
}
