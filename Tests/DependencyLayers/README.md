# Dependency-layered test suites

ProjectTwelve separates unit tests by the heaviest dependency each suite is allowed to load. This keeps pure domain rules fast while still preserving Unity-facing coverage where Unity objects are the subject under test.

| Layer | Location | Dependency boundary | Use for |
| --- | --- | --- | --- |
| Pure C# .NET | `Tests/DependencyLayers/PureDotNet/` | Compiles selected production `.cs` files into a normal .NET test assembly. Unity APIs are replaced by local test doubles in `UnityEngineMocks.cs`. | Deterministic data/model rules such as chunks, tiles, coordinates, and terrain. |
| Mocked Unity EditMode | `Assets/Tests/EditMode/` tests that instantiate fakes/stubs instead of scene objects | Runs under Unity Test Runner but avoids real GameObjects, scenes, rendering, physics, or live network endpoints. | Runtime services and dispatchers whose Unity dependencies can be substituted. |
| Real Unity EditMode | `Assets/Tests/EditMode/` tests that intentionally create Unity objects/assets | Runs under Unity Test Runner with the real UnityEngine implementation. | ScriptableObjects, Sprites, Textures, Meshes, and other behavior that needs Unity internals. |

## Commands

```bash
# Pure C# .NET tests; requires a local .NET SDK.
dotnet test Tests/DependencyLayers/PureDotNet/ProjectTwelve.PureDotNet.Tests.csproj

# Unity EditMode tests, including the mocked-Unity and real-Unity layers.
Unity -batchmode -quit -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile Logs/unity-editmode-tests.log
```

When adding a test, choose the lowest layer that can exercise the behavior. Prefer pure .NET tests for production code that does not truly need Unity lifecycle, rendering, physics, asset databases, or scene state.
