# ProjectTwelve - Unity 2D Sandbox Prototype

A Unity C# project for prototyping a Terraria-like 2D sandbox with chunked world data, procedural terrain generation, chunk-local rendering, and basic tile editing.

## Requirements

- Unity Editor 6.0.5.1f1 with **Universal Render Pipeline (URP) 2D**
- JetBrains Rider 2023.3 or later, or another Unity-compatible C# editor

## Setup

1. Clone the repository. For full visuals (autotiles, avatars, monsters), include the private assets submodule:

   ```bash
   git clone --recurse-submodules https://github.com/synthet/project-twelve.git
   ```

   Code-only clone (no licensed art): use a normal `git clone`, then run `git submodule update --init --recursive` after you have access to [project-twelve-assets](https://github.com/synthet/project-twelve-assets).

2. Open this project in Unity Editor 6.0.5.1f1.
3. Unity will regenerate solution and project files on first open.
4. Open the regenerated solution in Rider or your preferred editor.
5. Open `Assets/Scene.unity` and press Play for the runnable sandbox prototype.

Licensed content mounts at `Assets/_Licensed/` (git submodule). See [Paid assets policy](docs/PAID_ASSETS.md) and [Visual setup](docs/VISUAL_SETUP.md).

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
project-twelve/                     # Git repo root and Unity -projectPath
├── Assets/
│   ├── _Licensed/                  # Git submodule → project-twelve-assets (private)
│   ├── Scene.unity                 # Runnable sandbox scene (player + camera + world)
│   ├── Materials/                  # Shared tile and player materials
│   └── Scripts/
│       ├── Sandbox/                # World, chunks, player, rendering
│       ├── Visual/                 # Autotile, character, monster presentation
│       └── Integration/            # Avatar factory, import config
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

## Agent / AI workflow

Contributors and coding agents should start with [AGENTS.md](AGENTS.md) for build/test commands, MCP setup, and safety rules. The spec-first SDLC loop and asset map live in [docs/ai-workflow/README.md](docs/ai-workflow/README.md). Backlog work is tracked in [docs/wiki/tickets/](docs/wiki/tickets/) with linked GitHub issues.
