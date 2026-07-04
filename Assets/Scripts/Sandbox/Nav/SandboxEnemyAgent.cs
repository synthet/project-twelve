using ProjectTwelve.Visual.Monsters;
using UnityEngine;

namespace ProjectTwelve.Sandbox.Nav
{
    /// <summary>
    /// Prototype walker enemy controller: follows paths from <see cref="SandboxNavPathfinder"/>
    /// step by step, falls back to local steering (walk toward the target's X, stop at ledges)
    /// when no path is available, and despawns after
    /// <see cref="SandboxNavConstants.DespawnGraceSeconds"/> once its target leaves the loaded
    /// set. Movement is kinematic toward step cell centers, so a returned path is followed
    /// without teleports. Visuals are driven only through the P2-VISUAL-003 locomotion contract
    /// (<see cref="MonsterLocomotionDriver"/>) — never vendor sprites directly.
    /// </summary>
    public sealed class SandboxEnemyAgent : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float repathInterval = 0.75f;

        private SandboxWorld world;
        private SandboxWorldNavGrid grid;
        private SandboxEnemySpawner spawner;
        private Transform target;
        private MonsterLocomotionDriver locomotion;
        private int agentId;

        private SandboxNavPath path;
        private int stepIndex;
        private float nextRepathTime;
        private float unloadedTargetSince = -1f;
        private SandboxNavMove? lastLocomotionMove;

        public int AgentId => agentId;

        public void Initialize(SandboxEnemySpawner owner, SandboxWorld world, SandboxWorldNavGrid grid, Transform target, int agentId)
        {
            spawner = owner;
            this.world = world;
            this.grid = grid;
            this.target = target;
            this.agentId = agentId;
            locomotion = GetComponent<MonsterLocomotionDriver>();
        }

        /// <summary>Receives a completed path request from the spawner's scheduler.</summary>
        public void OnPathComputed(SandboxNavPath computedPath)
        {
            path = computedPath.Status == SandboxNavStatus.Found ? computedPath : null;
            stepIndex = 0;
        }

        private void Update()
        {
            if (world == null || target == null)
            {
                return;
            }

            if (HandleDespawnWhenTargetUnloaded())
            {
                return;
            }

            InvalidateStalePath();
            RequestPathIfDue();

            if (path != null && stepIndex < path.Steps.Count)
            {
                FollowPath();
            }
            else
            {
                SteerLocally();
            }
        }

        /// <summary>
        /// Despawn rule: once the target's chunk leaves the loaded set the agent idles, then
        /// despawns after the grace period. Returns true when the agent is idling or gone.
        /// </summary>
        private bool HandleDespawnWhenTargetUnloaded()
        {
            Vector2Int targetTile = world.WorldPositionToTile(target.position);
            bool targetLoaded = world.IsChunkLoaded(SandboxWorld.WorldToChunkCoord(targetTile.x, targetTile.y));
            if (targetLoaded)
            {
                unloadedTargetSince = -1f;
                return false;
            }

            if (unloadedTargetSince < 0f)
            {
                unloadedTargetSince = Time.time;
                SetLocomotionIdle();
            }

            if (Time.time - unloadedTargetSince >= SandboxNavConstants.DespawnGraceSeconds)
            {
                Despawn();
            }

            return true;
        }

        private void InvalidateStalePath()
        {
            if (path != null && path.IsStale(grid))
            {
                path = null;
                nextRepathTime = 0f; // recompute lazily on this agent's next step, via the scheduler
            }
        }

        private void RequestPathIfDue()
        {
            bool pathConsumed = path == null || stepIndex >= path.Steps.Count;
            if (!pathConsumed || Time.time < nextRepathTime)
            {
                return;
            }

            nextRepathTime = Time.time + repathInterval;
            Vector2Int start = world.WorldPositionToTile(transform.position);
            Vector2Int goal = world.WorldPositionToTile(target.position);
            spawner.RequestPath(this, start, goal);
        }

        private void FollowPath()
        {
            SandboxNavStep step = path.Steps[stepIndex];
            Vector3 destination = world.TileToWorldCenter(step.Cell.x, step.Cell.y);
            MoveTowards(destination);
            SetLocomotionForMove(step.Move);

            if (Vector2.Distance(transform.position, destination) < 0.05f)
            {
                stepIndex++;
                if (stepIndex >= path.Steps.Count)
                {
                    path = null;
                }
            }
        }

        /// <summary>
        /// Local steering fallback: walk horizontally toward the target's X and stop at ledges
        /// or gaps (the next foot cell is not standable), never walking off into unpathable air.
        /// </summary>
        private void SteerLocally()
        {
            Vector2Int foot = world.WorldPositionToTile(transform.position);
            int direction = target.position.x > transform.position.x ? 1 : -1;
            if (Mathf.Abs(target.position.x - transform.position.x) < world.TileSize * 0.5f
                || !SandboxNavPathfinder.IsStandable(grid, foot.x + direction, foot.y))
            {
                SetLocomotionIdle();
                return;
            }

            Vector3 destination = world.TileToWorldCenter(foot.x + direction, foot.y);
            MoveTowards(destination);
            SetLocomotionForMove(SandboxNavMove.Walk);
        }

        private void MoveTowards(Vector3 destination)
        {
            transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
            float dx = destination.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.01f)
            {
                Vector3 scale = transform.localScale;
                scale.x = Mathf.Abs(scale.x) * (dx < 0f ? -1f : 1f);
                transform.localScale = scale;
            }
        }

        /// <summary>
        /// Drives the locomotion contract only on state changes — <see cref="MonsterLocomotionDriver"/>
        /// spawns one-shot effects on Run/Jump transitions, so calling it every frame would spam them.
        /// </summary>
        private void SetLocomotionForMove(SandboxNavMove move)
        {
            if (locomotion == null || lastLocomotionMove == move)
            {
                return;
            }

            lastLocomotionMove = move;
            if (move == SandboxNavMove.Jump)
            {
                locomotion.Jump();
            }
            else
            {
                locomotion.Run();
            }
        }

        private void SetLocomotionIdle()
        {
            if (locomotion == null || lastLocomotionMove == null)
            {
                return;
            }

            lastLocomotionMove = null;
            locomotion.Idle();
        }

        private void Despawn()
        {
            spawner?.NotifyDespawned(this);
            Destroy(gameObject);
        }
    }
}
