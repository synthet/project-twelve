using System.Collections.Generic;
using UnityEngine;

namespace ProjectTwelve.Sandbox.Nav
{
    /// <summary>A queued path request identified by the requesting agent.</summary>
    public readonly struct SandboxNavRequest
    {
        public readonly int AgentId;
        public readonly Vector2Int Start;
        public readonly Vector2Int Goal;

        public SandboxNavRequest(int agentId, Vector2Int start, Vector2Int goal)
        {
            AgentId = agentId;
            Start = start;
            Goal = goal;
        }
    }

    /// <summary>A completed request paired with its computed path.</summary>
    public readonly struct SandboxNavResult
    {
        public readonly SandboxNavRequest Request;
        public readonly SandboxNavPath Path;

        public SandboxNavResult(SandboxNavRequest request, SandboxNavPath path)
        {
            Request = request;
            Path = path;
        }
    }

    /// <summary>
    /// FIFO path-request scheduler bounding search work per simulation tick: at most
    /// <see cref="SandboxNavConstants.MaxRequestsPerTick"/> requests are processed per
    /// <see cref="ProcessTick"/>, each capped at
    /// <see cref="SandboxNavConstants.MaxExpansionsPerRequest"/> expansions, so path recomputes
    /// are staggered across frames instead of stalling one. An agent has at most one pending
    /// request; re-enqueues while pending are ignored.
    /// </summary>
    public sealed class SandboxNavRequestScheduler
    {
        private readonly Queue<SandboxNavRequest> pending = new Queue<SandboxNavRequest>();
        private readonly HashSet<int> pendingAgentIds = new HashSet<int>();

        public int PendingCount => pending.Count;

        /// <summary>Queues a request unless the agent already has one pending.</summary>
        public bool Enqueue(SandboxNavRequest request)
        {
            if (!pendingAgentIds.Add(request.AgentId))
            {
                return false;
            }

            pending.Enqueue(request);
            return true;
        }

        /// <summary>
        /// Processes up to <paramref name="maxRequests"/> queued requests against the grid and
        /// appends their results to <paramref name="results"/>. Returns the number processed.
        /// </summary>
        public int ProcessTick(
            ISandboxNavGrid grid,
            List<SandboxNavResult> results,
            ISandboxNavVersionSource versions = null,
            int maxRequests = SandboxNavConstants.MaxRequestsPerTick)
        {
            int processed = 0;
            while (processed < maxRequests && pending.Count > 0)
            {
                SandboxNavRequest request = pending.Dequeue();
                pendingAgentIds.Remove(request.AgentId);
                SandboxNavPath path = SandboxNavPathfinder.FindPath(grid, request.Start, request.Goal, versions);
                results.Add(new SandboxNavResult(request, path));
                processed++;
            }

            return processed;
        }
    }
}
