---
type: Technical Reference
title: "Architecture Map"
description: "ProjectTwelve Architecture Map reference — design notes, contracts, and decisions for the architecture map area of the sandbox prototype."
resource: wiki/architecture-map.md
tags: [wiki, architecture]
timestamp: 2026-06-28T00:00:00Z
okf_version: 0.1
---

# Architecture Map

## Runtime Layers

```mermaid
flowchart TD
    Input[Player Input] --> Player[SandboxPlayerController]
    Player --> World[SandboxWorld]
    World --> Chunk[SandboxChunk]
    Chunk --> Tile[SandboxTile]
    World --> Renderer[SandboxChunkRenderer]
    Renderer --> Mesh[Chunk Mesh]
    Renderer --> Collider[Chunk-local BoxCollider2D]
```

## Responsibility Boundaries

| Area | Current owner | Responsibility |
| --- | --- | --- |
| World queries and edits | `SandboxWorld` | Convert world coordinates, load chunks, generate missing chunks, apply tile edits. |
| Chunk storage | `SandboxChunk` | Own a 32×32 tile array and dirty flags. |
| Tile state | `SandboxTile` | Store compact per-tile data: id, light, fluid, metadata. |
| Rendering/collision | `SandboxChunkRenderer` | Build visible tile quads and local collision shapes from chunk data. |
| Player controls | `SandboxPlayerController` | Move, jump, and request valid tile edits. |

## Editing Guidance

- Keep gameplay code aligned with the design document's chunk-first architecture.
- Prefer small, explicit data structures over hidden scene state.
- Do not add demo-only systems unless they directly prove a sandbox milestone.
- Keep render, collider, lighting, and save dirty states separable as the project grows.
