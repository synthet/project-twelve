using System.Collections.Generic;
using ProjectTwelve.Visual.Monsters;
using UnityEngine;

namespace ProjectTwelve.Sandbox.Nav
{
    /// <summary>
    /// Area spawn controller for the P2 walker archetype. On a
    /// <see cref="SandboxNavConstants.SpawnInterval"/> cadence it samples candidate foot cells in
    /// the spawn band around the player, validates them with <see cref="SandboxSpawnRules"/>, and
    /// spawns through the P2-VISUAL-003 contract (<see cref="MonsterSpawnHelper"/>). Also owns the
    /// shared <see cref="SandboxNavRequestScheduler"/> so all agents' path recomputes respect the
    /// per-tick request budget.
    /// </summary>
    public sealed class SandboxEnemySpawner : MonoBehaviour
    {
        [SerializeField] private SandboxWorld world;
        [SerializeField] private MonsterVisualCatalog monsterCatalog;
        [SerializeField] private string monsterId = "walker";
        [SerializeField] private Camera viewCamera;
        [SerializeField] private int spawnAttemptsPerCycle = 32;

        private readonly List<SandboxEnemyAgent> liveAgents = new List<SandboxEnemyAgent>();
        private readonly Dictionary<int, SandboxEnemyAgent> agentsById = new Dictionary<int, SandboxEnemyAgent>();
        private readonly List<SandboxNavResult> resultScratch = new List<SandboxNavResult>();
        private readonly SandboxNavRequestScheduler scheduler = new SandboxNavRequestScheduler();

        private SandboxWorldNavGrid grid;
        private float nextSpawnTime;
        private int nextAgentId;

        public int LiveEnemyCount => liveAgents.Count;

        private void Awake()
        {
            if (world == null)
            {
                world = FindAnyObjectByType<SandboxWorld>();
            }

            if (viewCamera == null)
            {
                viewCamera = Camera.main;
            }

            grid = new SandboxWorldNavGrid(world);
        }

        private void Update()
        {
            if (world == null)
            {
                return;
            }

            ProcessPathRequests();

            if (Time.time >= nextSpawnTime)
            {
                nextSpawnTime = Time.time + SandboxNavConstants.SpawnInterval;
                TrySpawnOne();
            }
        }

        /// <summary>Queues a path request for an agent; results arrive via the per-tick budget.</summary>
        public void RequestPath(SandboxEnemyAgent agent, Vector2Int start, Vector2Int goal)
        {
            scheduler.Enqueue(new SandboxNavRequest(agent.AgentId, start, goal));
        }

        public void NotifyDespawned(SandboxEnemyAgent agent)
        {
            liveAgents.Remove(agent);
            agentsById.Remove(agent.AgentId);
        }

        private void ProcessPathRequests()
        {
            resultScratch.Clear();
            scheduler.ProcessTick(grid, resultScratch, grid);
            foreach (SandboxNavResult result in resultScratch)
            {
                if (agentsById.TryGetValue(result.Request.AgentId, out SandboxEnemyAgent agent) && agent != null)
                {
                    agent.OnPathComputed(result.Path);
                }
            }
        }

        /// <summary>
        /// Drops agents destroyed outside <see cref="NotifyDespawned"/> (scene teardown, external
        /// Destroy) so dead references never pin the population cap.
        /// </summary>
        private void PruneDeadAgents()
        {
            for (int i = liveAgents.Count - 1; i >= 0; i--)
            {
                if (liveAgents[i] == null)
                {
                    liveAgents.RemoveAt(i);
                }
            }
        }

        private void TrySpawnOne()
        {
            PruneDeadAgents();
            if (!SandboxSpawnRules.CanSpawn(liveAgents.Count)
                || monsterCatalog == null
                || !world.TryGetPlayerWorldPosition(out Vector2 playerPosition))
            {
                return;
            }

            Vector2Int playerFoot = world.WorldPositionToTile(playerPosition);
            RectInt cameraRect = GetCameraTileRect();
            SandboxTerrainGenerator generator = world.CreateTerrainGenerator();
            int SurfaceHeightAt(int x) => generator.GetSurfaceHeight(x);
            int LightAt(Vector2Int cell) => SandboxSpawnRules.InterimLightAt(cell, SurfaceHeightAt);

            for (int attempt = 0; attempt < spawnAttemptsPerCycle; attempt++)
            {
                Vector2Int cell = new Vector2Int(
                    playerFoot.x + Random.Range(-SandboxNavConstants.MaxSpawnDistance, SandboxNavConstants.MaxSpawnDistance + 1),
                    playerFoot.y + Random.Range(-SandboxNavConstants.MaxSpawnDistance, SandboxNavConstants.MaxSpawnDistance + 1));

                if (SandboxSpawnRules.IsValidSpawnCell(grid, cell, playerFoot, cameraRect, LightAt, SurfaceHeightAt))
                {
                    Spawn(cell);
                    return;
                }
            }
        }

        private void Spawn(Vector2Int cell)
        {
            Vector3 position = world.TileToWorldCenter(cell.x, cell.y);
            MonsterVisual visual = MonsterSpawnHelper.Spawn(monsterCatalog, monsterId, position, transform);
            if (visual == null)
            {
                return;
            }

            SandboxEnemyAgent agent = visual.gameObject.AddComponent<SandboxEnemyAgent>();
            agent.Initialize(this, world, grid, world.PlayerTarget, nextAgentId++);
            liveAgents.Add(agent);
            agentsById.Add(agent.AgentId, agent);
        }

        private RectInt GetCameraTileRect()
        {
            if (viewCamera == null || !viewCamera.orthographic)
            {
                return new RectInt(0, 0, 0, 0);
            }

            float halfHeight = viewCamera.orthographicSize;
            float halfWidth = halfHeight * viewCamera.aspect;
            Vector3 center = viewCamera.transform.position;
            Rect worldRect = new Rect(center.x - halfWidth, center.y - halfHeight, halfWidth * 2f, halfHeight * 2f);
            return SandboxSpawnRules.CameraWorldRectToTileRect(worldRect, world.TileSize);
        }
    }
}
