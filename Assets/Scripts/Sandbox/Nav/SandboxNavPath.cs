using System.Collections.Generic;
using UnityEngine;

namespace ProjectTwelve.Sandbox.Nav
{
    /// <summary>Movement-model edge type used to reach a path step.</summary>
    public enum SandboxNavMove
    {
        Walk,
        Jump,
        Fall
    }

    /// <summary>Result classification of a path request.</summary>
    public enum SandboxNavStatus
    {
        /// <summary>A followable path to the goal was found.</summary>
        Found,

        /// <summary>The reachable node set was exhausted without reaching the goal.</summary>
        NoPath,

        /// <summary>
        /// The expansion budget was hit before the search concluded. Treated as no-path by
        /// agents, which fall back to local steering until the next scheduled recompute.
        /// </summary>
        BudgetExhausted
    }

    /// <summary>One step of a computed path: the foot cell to reach and the edge type used.</summary>
    public readonly struct SandboxNavStep
    {
        public readonly Vector2Int Cell;
        public readonly SandboxNavMove Move;

        public SandboxNavStep(Vector2Int cell, SandboxNavMove move)
        {
            Cell = cell;
            Move = move;
        }
    }

    /// <summary>
    /// Immutable computed path plus the per-chunk nav-version snapshot taken at compute time.
    /// A path is stale once any crossed chunk's version has moved on (its cells changed after
    /// the path was computed); agents check staleness on their next step and recompute lazily —
    /// never synchronously inside the tile edit.
    /// </summary>
    public sealed class SandboxNavPath
    {
        private readonly List<SandboxNavStep> steps;
        private readonly Dictionary<Vector2Int, int> chunkVersionSnapshot;

        public SandboxNavStatus Status { get; }
        public IReadOnlyList<SandboxNavStep> Steps => steps;

        /// <summary>Chunk coordinates crossed by the start cell and every step cell.</summary>
        public IReadOnlyCollection<Vector2Int> CrossedChunks => chunkVersionSnapshot.Keys;

        public SandboxNavPath(SandboxNavStatus status, Vector2Int start, List<SandboxNavStep> steps, ISandboxNavVersionSource versions)
        {
            Status = status;
            this.steps = steps ?? new List<SandboxNavStep>();
            chunkVersionSnapshot = new Dictionary<Vector2Int, int>();
            SnapshotChunk(start, versions);
            foreach (SandboxNavStep step in this.steps)
            {
                SnapshotChunk(step.Cell, versions);
            }
        }

        /// <summary>Whether any chunk crossed by this path changed since the path was computed.</summary>
        public bool IsStale(ISandboxNavVersionSource versions)
        {
            foreach (KeyValuePair<Vector2Int, int> pair in chunkVersionSnapshot)
            {
                if (versions.GetNavVersion(pair.Key) != pair.Value)
                {
                    return true;
                }
            }

            return false;
        }

        private void SnapshotChunk(Vector2Int cell, ISandboxNavVersionSource versions)
        {
            Vector2Int chunkCoord = SandboxWorld.WorldToChunkCoord(cell.x, cell.y);
            if (!chunkVersionSnapshot.ContainsKey(chunkCoord))
            {
                chunkVersionSnapshot.Add(chunkCoord, versions?.GetNavVersion(chunkCoord) ?? 0);
            }
        }
    }
}
