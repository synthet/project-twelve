using System.Runtime.CompilerServices;

// Test-only internals (e.g. SandboxRegistries.ResetForTests) stay out of the public runtime API.
[assembly: InternalsVisibleTo("ProjectTwelve.EditModeTests")]
[assembly: InternalsVisibleTo("ProjectTwelve.RuntimeMcp")]
