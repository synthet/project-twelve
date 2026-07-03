namespace ProjectTwelve.Sandbox.Registry
{
    /// <summary>
    /// Minimal pure-data entity definition contract; extended by P2-AI-001 (spawn rules,
    /// pathfinding limits) and P2-VISUAL-003 (monster visual keys).
    /// </summary>
    public sealed class EntityDefinition : IContentDefinition
    {
        public EntityDefinition(string id, int maxHealth = 1, string visualKey = null)
        {
            Id = id;
            MaxHealth = maxHealth;
            VisualKey = visualKey;
        }

        public string Id { get; }

        public int MaxHealth { get; }

        /// <summary>Visual catalog key for presentation systems; null when not yet integrated.</summary>
        public string VisualKey { get; }
    }
}
