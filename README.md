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
| `F5` / `F6` | Save / load the world (and sidecar overrides) to `Application.persistentDataPath` (F6 avoids Unity Profiler’s default F9 RecordToggle) |
| `F8` | Toggle Visual Override Mode (requires `debugOverrideModeEnabled` on `SandboxWorld`) |
| Visual Override Mode + `F5` | Same save path; sidecar `sandbox-world.visual-overrides.json` is written whenever overrides exist |
| `F3` | Cycle autotile debug overlays (`VisualOverrideLabel` shows saved overrides) |

Visual Override Mode editing (after `F8`): `Tab` layer, `[` / `]` sprite, `X`/`Y` flip, `R` rotate, `C` clear, `N` note.

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

GitHub Actions runs docs/agent hygiene checks on pull requests. The Unity EditMode workflow
(`.github/workflows/unit-tests.yml`) is present but **skipped** unless `UNITY_LICENSE` or
`UNITY_SERIAL` is configured — this repo does not provision a Unity license on Actions.
Run EditMode locally before merge (see `docs/wiki/quality-gates.md`).

Optional: if you later add GameCI secrets (`UNITY_LICENSE` or `UNITY_SERIAL`, plus
`UNITY_EMAIL` / `UNITY_PASSWORD` for serial activation), the same workflow runs the full
EditMode suite via [game-ci/unity-test-runner](https://game.ci/docs/github/test-runner).

Local test coverage starts in `Assets/Tests/EditMode` and validates core tile, chunk, and
coordinate-conversion behavior.

## Agent / AI workflow

Contributors and coding agents should start with [AGENTS.md](AGENTS.md) for build/test commands, MCP setup, and safety rules. The spec-first SDLC loop and asset map live in [docs/ai-workflow/README.md](docs/ai-workflow/README.md). Backlog work is tracked in [docs/wiki/tickets/](docs/wiki/tickets/) with linked GitHub issues.
