/// <summary>
/// Fixed, ordered terrain-generation passes (P2-GEN-001). Each pass has a stable
/// integer id used as the <c>passId</c> component of its per-unit sub-seed
/// (<c>SandboxHash.Hash(seed, passId, coord)</c>), so every pass draws from an
/// independent, coordinate-addressable deterministic stream. Ids are contractual:
/// changing a value re-seeds that pass and changes generated worlds, so treat them
/// as frozen once fixtures exist. See <c>docs/wiki/07-procedural-generation.md</c>
/// and <c>docs/wiki/generation-and-saving.md</c>.
/// </summary>
public enum SandboxGenPass
{
    /// <summary>Pass 1 — per-column surface height. Sub-seed: <c>hash(seed, 1, worldX)</c>.</summary>
    SurfaceHeightmap = 1,

    /// <summary>Pass 2 — layer fill (dirt/stone bands). Pure function of height; no sub-seed draw.</summary>
    LayerFill = 2,

    /// <summary>Pass 3 — caves (thresholded noise + worm tunnels).</summary>
    Caves = 3,

    /// <summary>Pass 4 — biome assignment per region.</summary>
    Biomes = 4,

    /// <summary>Pass 5 — ore veins in stone-class tiles.</summary>
    Ores = 5,

    /// <summary>Pass 6 — structures / features with a single owning anchor.</summary>
    Structures = 6,

    /// <summary>Pass 7 — validation (spawn safety, overlap rejection).</summary>
    Validation = 7,
}
