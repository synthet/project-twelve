# ProjectTwelve - Unity C# Project

A Unity C# project configured for JetBrains Rider IDE.

## Setup

1. Open this project in Unity Editor 6.0.2 or later
2. Unity will automatically regenerate the `.sln` and `.csproj` files on first open
3. In Unity, go to `Edit > Preferences > External Tools`
4. Set `External Script Editor` to your JetBrains Rider installation path (e.g., `C:\Users\<YourUsername>\AppData\Local\JetBrains\Toolbox\apps\Rider\ch-0\<version>\bin\rider64.exe`)
5. Open the regenerated `.sln` file in Rider, or double-click any `.cs` script to open it in Rider automatically

**Note:** The `.sln` and `.csproj` files included in this project are templates. Unity will regenerate them when you open the project, which is normal and expected behavior.

## Project Structure

```
ProjectTwelve/
├── Assets/
│   ├── Scene.unity                 # Minimal default scene (camera + light)
│   └── Scripts/
│       └── PlayerController.cs      # Basic player movement script
├── ProjectSettings/                 # Unity project configuration
├── Packages/                        # Package dependencies
├── docs/                            # Design docs and engineering wiki
└── README.md                        # This file
```

This is intentionally a **barebone** project. The earlier hexagonal-grid "Hello World" demo
(HexGrid/HexTile/MouseControlledObject/SceneSetup) has been removed so the repo is a clean
starting point for the 2D sandbox engine described in the design docs.

### Scripts

- `Assets/Scripts/PlayerController.cs` - Basic player movement script that handles input using Unity's Input system (arrow keys or WASD). Placeholder controller; the sandbox uses manual tile collision (see the wiki).

## Design Documents

- [Engineering Wiki](docs/wiki/README.md) - Navigable, cross-linked knowledge base (architecture, data models, chunking, rendering, lighting, liquids, generation, pathfinding, multiplayer, persistence, modding, tooling, roadmap, glossary).
- [Architecture Blueprint](docs/wiki/architecture-blueprint.md) - Text translation of the visual blueprint canvas (10 figures).
- [Unity 2D Sandbox Architecture Plan](docs/terraria-like-unity-design.md) - Canonical, concise technical plan.
- [Detailed Design Reference](docs/terraria-like-unity-design-detailed.md) - Long-form companion with extended code sketches and tables.

## Requirements

- Unity Editor 6.0.2 or later
- JetBrains Rider 2023.3 or later (recommended for Unity 6 support)

## Getting Started

The project includes a basic `PlayerController` script that handles player movement using arrow keys or WASD. The script uses Unity's built-in `Input.GetAxis()` method for smooth movement input.

For the planned 2D sandbox engine — chunked world data, custom lighting and liquids, procedural generation, collision, pathfinding, multiplayer, persistence, and modding — start with the [Engineering Wiki](docs/wiki/README.md) and follow the [Roadmap](docs/wiki/14-roadmap.md).
