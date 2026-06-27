# ProjectTwelve - Unity 2D Sandbox Prototype

A Unity C# project for prototyping a Terraria-like 2D sandbox with chunked world data, procedural terrain generation, chunk-local rendering, and basic tile editing.

## Requirements

- Unity Editor 6.0.2 or later
- JetBrains Rider 2023.3 or later, or another Unity-compatible C# editor

## Setup

1. Open this project in Unity Editor 6.0.2 or later.
2. Unity will regenerate solution and project files on first open.
3. Open the regenerated solution in Rider or your preferred editor.
4. Open `Assets/Scene.unity` for the barebone sandbox scene.

## Project Structure

```text
ProjectTwelve/
├── Assets/
│   ├── Scene.unity                 # Minimal sandbox scene
│   └── Scripts/
│       ├── SandboxChunk.cs         # Chunk data and dirty flags
│       ├── SandboxChunkRenderer.cs # Chunk mesh and collider rebuilds
│       ├── SandboxPlayerController.cs # Basic movement and tile editing
│       ├── SandboxTile.cs          # Tile state and IDs
│       └── SandboxWorld.cs         # Chunk loading, generation, and tile edits
├── docs/
│   ├── terraria-like-unity-design.md
│   └── wiki/                       # LLM-facing implementation wiki
├── Packages/
└── ProjectSettings/
```

## Design Documents

- [Unity 2D Sandbox Architecture Plan](docs/terraria-like-unity-design.md) is the product-level technical design.
- [LLM Wiki Structure](docs/wiki/index.md) expands the design into implementation-facing pages for future LLM-assisted work.

## Current Barebone Scope

The project intentionally keeps only sandbox-relevant prototype assets:

- Sparse, chunked world data.
- Simple procedural terrain.
- Chunk mesh rendering with vertex colors.
- Chunk-local collision rebuilds.
- Basic player movement and mouse tile editing scripts.

The previous hex-grid click demo and generated scene artifacts have been removed so the repository stays focused on the sandbox prototype.
