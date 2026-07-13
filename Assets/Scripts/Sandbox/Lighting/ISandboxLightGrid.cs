namespace ProjectTwelve.Sandbox.Lighting
{
    /// <summary>
    /// Finite tile-data view consumed by <see cref="SandboxLightSolver"/>. Implementations must
    /// return false for unavailable cells so relighting never loads or generates world data.
    /// </summary>
    public interface ISandboxLightGrid
    {
        bool IsLoaded(int x, int y);

        SandboxTile GetTile(int x, int y);

        byte GetSourceLight(int x, int y);

        byte GetAttenuation(int x, int y);

        void SetLight(int x, int y, byte light);
    }
}
