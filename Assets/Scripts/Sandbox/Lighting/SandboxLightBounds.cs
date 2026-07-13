namespace ProjectTwelve.Sandbox.Lighting
{
    /// <summary>Inclusive world-tile rectangle used by bounded lighting updates.</summary>
    public readonly struct SandboxLightBounds
    {
        public SandboxLightBounds(int minX, int minY, int maxX, int maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        public int MinX { get; }
        public int MinY { get; }
        public int MaxX { get; }
        public int MaxY { get; }

        public bool IsValid => MinX <= MaxX && MinY <= MaxY;

        public bool Contains(int x, int y)
        {
            return x >= MinX && x <= MaxX && y >= MinY && y <= MaxY;
        }

        public SandboxLightBounds Expand(int amount)
        {
            return new SandboxLightBounds(MinX - amount, MinY - amount, MaxX + amount, MaxY + amount);
        }
    }
}
