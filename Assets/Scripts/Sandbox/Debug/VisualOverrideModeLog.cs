namespace ProjectTwelve.Sandbox.Debug
{
    /// <summary>
    /// Pure formatting for Visual Override Mode console messages (F3 mode 2 / F8 shortcut).
    /// </summary>
    public static class VisualOverrideModeLog
    {
        public const string UnavailableMessage =
            "Visual Override Mode unavailable (enable debugOverrideModeEnabled in Editor or use a development build).";

        public const string OnControlsMessage =
            "Visual Override Mode: ON — Tab layer, [/] sprite, X/Y flip, R rotate, C clear, F5 save sidecar.";

        public const string OffMessage = "Visual Override Mode: OFF";

        public static string FormatToggleMessage(
            bool turnedOn,
            bool gateOpen,
            int? cellX = null,
            int? cellY = null,
            string layer = null,
            string tileset = null)
        {
            if (!gateOpen)
            {
                return UnavailableMessage;
            }

            if (!turnedOn)
            {
                return OffMessage;
            }

            if (cellX.HasValue && cellY.HasValue && !string.IsNullOrEmpty(tileset))
            {
                string layerLabel = string.IsNullOrEmpty(layer) ? "Ground" : layer;
                return $"Visual Override Mode: ON at ({cellX.Value}, {cellY.Value}) {layerLabel} {tileset} — Tab layer, [/] sprite, X/Y flip, R rotate, C clear, F5 save sidecar.";
            }

            return OnControlsMessage;
        }
    }
}
