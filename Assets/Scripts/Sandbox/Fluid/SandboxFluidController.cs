using UnityEngine;

namespace ProjectTwelve.Sandbox.Fluid
{
    /// <summary>
    /// Play-mode driver that owns a <see cref="SandboxFluidSimulator"/> over the live world and ticks
    /// it on the fixed simulation cadence. Subscribes to <see cref="SandboxWorld.TileFluidWakeRequested"/>
    /// so tile edits (digging a channel, sealing a wall) re-wake the affected fluid cells, and caps
    /// work per tick with <see cref="SandboxFluidConstants.MaxActiveCellsPerTick"/>. Disabled by
    /// default (<see cref="simulateFluids"/>) so scenes without water pay nothing.
    /// </summary>
    public sealed class SandboxFluidController : MonoBehaviour
    {
        [SerializeField] private SandboxWorld world;
        [SerializeField] private bool simulateFluids = true;
        [SerializeField] private int seed;
        [SerializeField] private int maxCellsPerTick = SandboxFluidConstants.MaxActiveCellsPerTick;

        private SandboxFluidSimulator simulator;
        private SandboxWorldFluidGrid grid;

        /// <summary>Awake fluid cells scheduled for the next tick; 0 once every pool has settled.</summary>
        public int ActiveCellCount => simulator?.ActiveCount ?? 0;

        /// <summary>The simulator, for editor tooling and the runtime debug overlay (P2-TOOL-001).</summary>
        public SandboxFluidSimulator Simulator => simulator;

        private void Awake()
        {
            if (world == null)
            {
                world = FindFirstObjectByType<SandboxWorld>();
            }

            if (world != null)
            {
                grid = new SandboxWorldFluidGrid(world);
                simulator = new SandboxFluidSimulator(grid, seed);
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
            if (simulateFluids && simulator != null)
            {
                simulator.ProcessTick(maxCellsPerTick);
            }
        }

        /// <summary>Adds fluid at a world tile (a source or debug pour) and wakes it.</summary>
        public void AddFluid(int x, int y, float amount)
        {
            simulator?.AddFluid(x, y, amount);
        }

        private void OnTileEdited(int x, int y)
        {
            simulator?.Wake(x, y);
        }
    }
}
