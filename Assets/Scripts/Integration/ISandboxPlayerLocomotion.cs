/// <summary>
/// Project-owned locomotion presentation API for the sandbox player avatar.
/// </summary>
public interface ISandboxPlayerLocomotion
{
    void Idle();
    void Run();
    void Jump();
    void Fall();
    void Land();
}
