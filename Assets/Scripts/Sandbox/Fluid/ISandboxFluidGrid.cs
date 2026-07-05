namespace ProjectTwelve.Sandbox.Fluid
{
    /// <summary>
    /// Read/write tile view the liquid simulation operates on. Solidity is derived from the same
    /// data collision uses (<see cref="SandboxTile.IsSolid"/>) — never a second grid — and the
    /// fluid amount is the existing <see cref="SandboxTile.fluid"/> field (0.0–1.0). Cells outside
    /// the loaded set report <see cref="IsLoaded"/> false and are treated as impassable: fluid
    /// never flows into an unloaded cell, so simulation pauses at the loaded-set border.
    /// Implementations must not generate chunks on query.
    /// </summary>
    public interface ISandboxFluidGrid
    {
        /// <summary>Whether the tile at the world coordinate is solid. Solid tiles hold no fluid.</summary>
        bool IsSolid(int x, int y);

        /// <summary>Whether the chunk owning the world coordinate is in the loaded set.</summary>
        bool IsLoaded(int x, int y);

        /// <summary>Current fluid amount (0.0–1.0+ under pressure) at the world coordinate.</summary>
        float GetFluid(int x, int y);

        /// <summary>Writes the fluid amount at the world coordinate. Only called for loaded, non-solid cells.</summary>
        void SetFluid(int x, int y, float amount);
    }
}
