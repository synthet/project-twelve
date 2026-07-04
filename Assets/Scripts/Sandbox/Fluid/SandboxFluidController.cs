using UnityEngine;

namespace ProjectTwelve.Sandbox.Fluid
{
    /// <summary>
    /// Runtime host for the water simulation: owns a <see cref="SandboxFluidSimulator"/> over a live
    /// <see cref="SandboxWorld"/>, advances <see cref="SandboxFluidConstants.IterationsPerFrame"/>
    /// ticks per physics step (each capped at <see cref="SandboxFluidConstants.MaxActiveCellsPerTick"/>),
    /// and keeps the active set correct across edits and chunk streaming by subscribing to
    /// <see cref="SandboxWorld.TileChanged"/> and <see cref="SandboxWorld.ChunkLoaded"/>. Behaviour is
    /// specified in <c>docs/wiki/08-liquids.md</c> § "P2-FLUID-001 specification"; the simulation core
    /// itself is a pure, scene-free class covered by EditMode tests.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class SandboxFluidController : MonoBehaviour
    {
        [SerializeField] private SandboxWorld world;

        [Tooltip("Simulation ticks advanced per physics step. Defaults to the spec constant.")]
        [SerializeField] private int iterationsPerFrame = SandboxFluidConstants.IterationsPerFrame;

        private readonly SandboxFluidSimulator simulator = new SandboxFluidSimulator();
        private SandboxWorldFluidGrid grid;

        /// <summary>The underlying simulation core (exposed for debug tooling).</summary>
        public SandboxFluidSimulator Simulator => simulator;

        /// <summary>Cells scheduled for simulation; 0 once all liquid has settled.</summary>
        public int ActiveCellCount => simulator.ActiveCount;

        private void Awake()
        {
            if (world == null)
            {
                world = GetComponent<SandboxWorld>();
            }
        }

        private void OnEnable()
        {
            if (world == null)
            {
                return;
            }

            grid = new SandboxWorldFluidGrid(world);
            world.TileChanged += OnTileChanged;
            world.ChunkLoaded += OnChunkLoaded;
        }

        private void OnDisable()
        {
            if (world == null)
            {
                return;
            }

            world.TileChanged -= OnTileChanged;
            world.ChunkLoaded -= OnChunkLoaded;
        }

        private void FixedUpdate()
        {
            if (grid == null)
            {
                return;
            }

            for (int i = 0; i < iterationsPerFrame; i++)
            {
                simulator.Step(grid);
            }
        }

        /// <summary>Adds (or removes, with a negative amount) fluid at a world tile as a source/sink.</summary>
        public void AddFluid(int x, int y, float amount)
        {
            if (grid == null && world != null)
            {
                grid = new SandboxWorldFluidGrid(world);
            }

            if (grid != null)
            {
                simulator.AddFluid(grid, x, y, amount);
            }
        }

        private void OnTileChanged(int x, int y)
        {
            simulator.WakeWithNeighbors(grid, x, y);
        }

        private void OnChunkLoaded(Vector2Int coord)
        {
            // Resume flow for fluid already in the newly loaded chunk (persisted or generated) and
            // wake its border strip so settled liquid in already-loaded neighbours flows across the
            // seam into any newly available space.
            for (int lx = 0; lx < SandboxChunk.Size; lx++)
            {
                for (int ly = 0; ly < SandboxChunk.Size; ly++)
                {
                    bool border = lx == 0 || ly == 0 || lx == SandboxChunk.Size - 1 || ly == SandboxChunk.Size - 1;
                    Vector2Int cell = SandboxWorld.ChunkLocalToWorld(coord, lx, ly);
                    if (border || grid.GetFluid(cell.x, cell.y) > 0f)
                    {
                        simulator.WakeWithNeighbors(grid, cell.x, cell.y);
                    }
                }
            }
        }
    }
}
