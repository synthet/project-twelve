using System;

namespace ProjectTwelve.Visual.AutotileDebug
{
    /// <summary>
    /// Play Mode debug modes (F3 cycle; F8 jumps to <see cref="VisualOverrideEdit"/>).
    /// </summary>
    public enum GroundAutotileDebugMode
    {
        Off = 0,
        VisualOverrideEdit = 1,
        SpriteIdLabel = 2,
        GroundCoverSplit = 4,
        VisualOverrideLabel = 5,
    }

    /// <summary>
    /// F3 cycle order, legacy enum normalization, and console log formatting.
    /// </summary>
    public static class GroundAutotileDebugModes
    {
        private static readonly GroundAutotileDebugMode[] CycleOrder =
        {
            GroundAutotileDebugMode.Off,
            GroundAutotileDebugMode.VisualOverrideEdit,
            GroundAutotileDebugMode.SpriteIdLabel,
            GroundAutotileDebugMode.GroundCoverSplit,
            GroundAutotileDebugMode.VisualOverrideLabel,
        };

        /// <summary>
        /// Maps stored inspector values (including removed legacy modes) to a supported mode.
        /// </summary>
        public static GroundAutotileDebugMode Normalize(GroundAutotileDebugMode mode)
        {
            if (Array.IndexOf(CycleOrder, mode) >= 0)
            {
                return mode;
            }

            return (int)mode switch
            {
                1 or 2 or 3 or 8 => GroundAutotileDebugMode.SpriteIdLabel,
                4 or 5 => GroundAutotileDebugMode.GroundCoverSplit,
                6 or 7 or 9 => GroundAutotileDebugMode.VisualOverrideLabel,
                _ => GroundAutotileDebugMode.Off,
            };
        }

        /// <summary>
        /// Returns the next mode in the F3 cycle.
        /// </summary>
        public static GroundAutotileDebugMode Cycle(GroundAutotileDebugMode mode)
        {
            mode = Normalize(mode);
            int index = Array.IndexOf(CycleOrder, mode);
            return CycleOrder[(index + 1) % CycleOrder.Length];
        }

        /// <summary>
        /// True when interactive visual override editing is active (formerly F8-only mode).
        /// </summary>
        public static bool IsVisualOverrideEdit(GroundAutotileDebugMode mode)
        {
            return Normalize(mode) == GroundAutotileDebugMode.VisualOverrideEdit;
        }

        /// <summary>
        /// True when the F3 mesh overlay (and hover HUD) should be active.
        /// </summary>
        public static bool IsOverlayActive(GroundAutotileDebugMode mode)
        {
            mode = Normalize(mode);
            return mode != GroundAutotileDebugMode.Off
                && mode != GroundAutotileDebugMode.VisualOverrideEdit;
        }

        /// <summary>
        /// Human-readable console line for the active mode.
        /// </summary>
        public static string FormatLogLine(GroundAutotileDebugMode mode)
        {
            mode = Normalize(mode);
            int index = Array.IndexOf(CycleOrder, mode);
            string summary = mode switch
            {
                GroundAutotileDebugMode.Off => "overlay hidden",
                GroundAutotileDebugMode.VisualOverrideEdit =>
                    "interactive override editing — Tab layer, [/] sprite, X/Y flip, R rotate, C clear, F5 save sidecar",
                GroundAutotileDebugMode.SpriteIdLabel => "ground sprite id + flip on every solid tile",
                GroundAutotileDebugMode.GroundCoverSplit => "ground + cover sprite ids (bottom / top)",
                GroundAutotileDebugMode.VisualOverrideLabel => "saved visual override ids (tint = status)",
                _ => mode.ToString(),
            };

            return $"[F3] Ground autotile debug: {mode} ({index + 1}/{CycleOrder.Length}) — {summary}";
        }
    }
}
