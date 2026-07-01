using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure geometry helpers for building chunk terrain colliders from solid tiles.
/// The run-merge and rect math live here — free of any Unity component creation — so the
/// collision geometry is unit-testable in EditMode, while <see cref="SandboxChunkRenderer"/>
/// owns turning the runs into actual <c>BoxCollider2D</c> components at runtime.
/// See <c>docs/wiki/rendering-and-collision.md</c> § "Prototype collision rules (P1-COLL-001)".
/// </summary>
public static class SandboxColliderGeometry
{
    /// <summary>A merged horizontal run of solid tiles within one chunk row.</summary>
    public readonly struct SolidRun
    {
        /// <summary>Local row (Y) the run belongs to, in <c>[0, SandboxChunk.Size)</c>.</summary>
        public readonly int Row;

        /// <summary>Local X of the first solid tile in the run.</summary>
        public readonly int Start;

        /// <summary>Number of contiguous solid tiles in the run (always &gt;= 1).</summary>
        public readonly int Length;

        public SolidRun(int row, int start, int length)
        {
            Row = row;
            Start = start;
            Length = length;
        }
    }

    /// <summary>
    /// Enumerates the merged solid runs of a chunk, row by row (bottom to top, left to right).
    /// Contiguous solid tiles in a single row merge into one run; runs never span rows, so a
    /// solid column produces one length-1 run per row rather than a single vertical box.
    /// </summary>
    /// <param name="chunk">Chunk to scan; a null chunk yields an empty list.</param>
    public static List<SolidRun> BuildSolidRuns(SandboxChunk chunk)
    {
        List<SolidRun> runs = new List<SolidRun>();
        if (chunk == null)
        {
            return runs;
        }

        for (int y = 0; y < SandboxChunk.Size; y++)
        {
            int runStart = -1;
            for (int x = 0; x <= SandboxChunk.Size; x++)
            {
                bool isSolid = x < SandboxChunk.Size && chunk.GetLocalTile(x, y).IsSolid;
                if (isSolid && runStart < 0)
                {
                    runStart = x;
                }

                if ((!isSolid || x == SandboxChunk.Size) && runStart >= 0)
                {
                    runs.Add(new SolidRun(y, runStart, x - runStart));
                    runStart = -1;
                }
            }
        }

        return runs;
    }

    /// <summary>
    /// Converts a solid run to the chunk-local box <paramref name="offset"/> and
    /// <paramref name="size"/> for a <c>BoxCollider2D</c>. Matches tile <c>(x, y)</c> spanning
    /// world <c>[x, x+1) x [y, y+1)</c> at the default tile size of 1.
    /// </summary>
    public static void GetColliderRect(SolidRun run, float tileSize, out Vector2 offset, out Vector2 size)
    {
        offset = new Vector2((run.Start + run.Length * 0.5f) * tileSize, (run.Row + 0.5f) * tileSize);
        size = new Vector2(run.Length * tileSize, tileSize);
    }
}
