using UnityEngine;

namespace ProjectTwelve.Sandbox.Grass
{
    /// <summary>
    /// Play-mode driver that owns a <see cref="SandboxGrassSimulator"/> over the live world and ticks
    /// it on a slow cadence (grass grows over seconds, not frames). Subscribes to
    /// <see cref="SandboxWorld.TileFluidWakeRequested"/> so a tile placed on top of grass buries it
    /// immediately. A re-entrancy guard keeps the simulator's own grass ⇄ dirt writes — which re-fire
    /// the same edit event — from being treated as player edits (which would cascade). Enabled by
    /// default; a scene with no ground pays only the empty per-interval scan.
    /// </summary>
    public sealed class SandboxGrassController : MonoBehaviour
    {
        [SerializeField] private SandboxWorld world;
        [SerializeField] private bool simulateGrass = true;
        [SerializeField] private int seed = 1337;

        [Header("Growth")]
        [Tooltip("Seconds between grass update passes. Larger = slower spread.")]
        [SerializeField] private float growthInterval = 0.5f;
        [Tooltip("Random cells sampled from the loaded set each pass.")]
        [SerializeField] private int maxUpdatesPerTick = 64;
        [Tooltip("Chance (0–1) a healthy grass tile spreads to an eligible neighbor per pass.")]
        [SerializeField, Range(0f, 1f)] private float spreadChance = 0.25f;
        [Tooltip("Chance (0–1) an exposed, sunlit, bare dirt tile spontaneously sprouts grass per pass.")]
        [SerializeField, Range(0f, 1f)] private float spontaneousChance = 0.02f;
        [Tooltip("How many tiles the sunlight sky-cast scans upward before treating the column as open.")]
        [SerializeField] private int skyScanCap = 64;

        private SandboxGrassSimulator simulator;
        private float timer;
        private bool applyingGrass;

        private void Awake()
        {
            if (world == null)
            {
                world = FindFirstObjectByType<SandboxWorld>();
            }

            if (world != null)
            {
                simulator = new SandboxGrassSimulator(
                    new SandboxWorldGrassAdapter(world),
                    seed,
                    skyScanCap,
                    spreadChance,
                    spontaneousChance);
            }
        }

        private void OnEnable()
        {
            if (world != null)
            {
                world.TileFluidWakeRequested += OnTileEdited;
            }
        }

        private void OnDisable()
        {
            if (world != null)
            {
                world.TileFluidWakeRequested -= OnTileEdited;
            }
        }

        private void FixedUpdate()
        {
            if (!simulateGrass || simulator == null)
            {
                return;
            }

            timer += Time.fixedDeltaTime;
            if (timer < growthInterval)
            {
                return;
            }

            timer = 0f;
            applyingGrass = true;
            try
            {
                simulator.ProcessTick(maxUpdatesPerTick);
            }
            finally
            {
                applyingGrass = false;
            }
        }

        private void OnTileEdited(int x, int y)
        {
            // Ignore the edit events the simulator's own writes raise; only external edits (player,
            // MCP) bury grass here. Without this guard a grass→dirt write would re-enter and cascade.
            if (applyingGrass || simulator == null)
            {
                return;
            }

            applyingGrass = true;
            try
            {
                simulator.OnTileChanged(x, y);
            }
            finally
            {
                applyingGrass = false;
            }
        }
    }
}
