namespace ProjectTwelve.Sandbox.Fluid
{
    /// <summary>
    /// Read/write fluid view the simulator operates on. Implementations must derive solidity from
    /// the same data collision uses (<see cref="SandboxTile.IsSolid"/>) — never a second fluid mask
    /// — and must not generate chunks on query: cells outside the loaded set report
    /// <see cref="IsLoaded"/> false, read solid, and are treated as impassable to flow. Fluid amount
    /// is the <see cref="SandboxTile.fluid"/> field (0 for dry cells; pressurized cells may exceed
    /// <see cref="SandboxFluidConstants.MaxFill"/>).
    /// </summary>
    public interface ISandboxFluidGrid
    {
        /// <summary>Whether the tile at the world coordinate is solid (holds no fluid, blocks flow).</summary>
        bool IsSolid(int x, int y);

        /// <summary>Whether the chunk owning the world coordinate is in the loaded (simulated) set.</summary>
        bool IsLoaded(int x, int y);

        /// <summary>Current fluid amount at the world coordinate; 0 for solid, dry, or unloaded cells.</summary>
        float GetFluid(int x, int y);

        /// <summary>Writes the fluid amount at the world coordinate. Only called for loaded, non-solid cells.</summary>
        void SetFluid(int x, int y, float amount);
    }
}
