# ProjectTwelve - Unity 2D Sandbox Prototype

A Unity C# project for prototyping a Terraria-like 2D sandbox with chunked world data, procedural terrain generation, chunk-local rendering, and basic tile editing.

## Requirements

- Unity Editor 6.0.2 or later
- JetBrains Rider 2023.3 or later, or another Unity-compatible C# editor

## Setup

1. Open this project in Unity Editor 6.0.2 or later.
2. Unity will regenerate solution and project files on first open.
3. Open the regenerated solution in Rider or your preferred editor.
4. Open `Assets/Scene.unity` and press Play for the runnable sandbox prototype.

## Running the Prototype

`Assets/Scene.unity` is a self-contained vertical slice. Pressing Play spawns the
player above procedurally generated terrain; chunks stream in around it and the camera
follows. Controls:

| Input | Action |
|-------|--------|
| `A` / `D` or `←` / `→` | Move left / right |
| `Space` | Jump (when grounded) |
| Left mouse button | Break the tile under the cursor (within edit range) |
| Right mouse button | Place a tile under the cursor (within edit range) |
| `F5` / `F9` | Save / load the world to `Application.persistentDataPath` |

## Project Structure

```text
ProjectTwelve/
├── Assets/
│   ├── Scene.unity                 # Runnable sandbox scene (player + camera + world)
│   ├── Materials/                  # Shared tile and player materials
│   └── Scripts/
│       └── Sandbox/
│           ├── SandboxChunk.cs            # Chunk data and dirty flags
│           ├── SandboxChunkRenderer.cs    # Chunk mesh and collider rebuilds
│           ├── SandboxTile.cs             # Tile state and IDs
│           ├── SandboxTerrainGenerator.cs # Deterministic terrain generation
│           ├── SandboxWorld.cs            # Chunk loading, generation, and tile edits
│           ├── SandboxPlayerController.cs # Movement, jump, and mouse tile editing
│           ├── SandboxCameraFollow.cs     # Camera follow for the player
│           └── SandboxSaveData.cs         # Serializable save records
├── docs/
│   ├── terraria-like-unity-design.md
│   └── wiki/                       # open implementation knowledge base
├── Packages/
└── ProjectSettings/
```

## Design Documents

- [Unity 2D Sandbox Architecture Plan](docs/terraria-like-unity-design.md) is the product-level technical design.
- [Detailed Design Reference](docs/terraria-like-unity-design-detailed.md) is a long-form companion with extended code sketches and comparison tables.
- [Open Knowledge Base](docs/wiki/README.md) expands the design into implementation-facing pages for contributors and automation. It holds two complementary page sets: a prototype-aligned wiki and a deeper numbered subsystem reference (see the wiki index).
- [Architecture Blueprint](docs/wiki/architecture-blueprint.md) is a text translation of the visual blueprint canvas (10 figures), cross-linked to the wiki.

## Current Barebone Scope

The project intentionally keeps only sandbox-relevant prototype assets:

- Sparse, chunked world data.
- Simple procedural terrain.
- Chunk mesh rendering with vertex colors.
- Chunk-local collision rebuilds.
- A playable scene wiring the player, camera follow, and world together.

The previous hex-grid click demo and generated scene artifacts have been removed so the repository stays focused on the sandbox prototype.

## Continuous Integration

GitHub Actions runs the Unity EditMode unit-test suite on pushes to `main`, pull requests, and manual workflow dispatches via `.github/workflows/unit-tests.yml`.

The workflow uses GameCI's Unity Test Runner. It skips the Unity execution step when no Unity license secret is configured, which keeps pull requests from failing before CI credentials are installed. Configure either `UNITY_LICENSE` for a personal license file or `UNITY_SERIAL` for a serial-based license to enable test execution. If using a serial-based license, also configure:

- `UNITY_EMAIL`
- `UNITY_PASSWORD`

Local test coverage starts in `Assets/Tests/EditMode` and currently validates core tile, chunk, and coordinate-conversion behavior.
