---
type: Reference
title: Source References
description: External and internal references used to structure the Unity sandbox OKF bundle.
tags: [okf, llm, references]
timestamp: 2026-06-27T00:00:00Z
---
# Internal Sources

| Source | Role |
| --- | --- |
| [`../terraria-like-unity-design.md`](../terraria-like-unity-design.md) | Product-level architecture source of truth for the sandbox. |
| [`../../README.md`](../../README.md) | Repository setup, current scope, and project structure. |
| [`../../Assets/Scripts/SandboxWorld.cs`](../../Assets/Scripts/SandboxWorld.cs) | Current world/chunk loading and tile edit implementation. |
| [`../../Assets/Scripts/SandboxChunk.cs`](../../Assets/Scripts/SandboxChunk.cs) | Current chunk storage and dirty flag implementation. |
| [`../../Assets/Scripts/SandboxTile.cs`](../../Assets/Scripts/SandboxTile.cs) | Current tile fields and tile ID constants. |
| [`../../Assets/Scripts/SandboxChunkRenderer.cs`](../../Assets/Scripts/SandboxChunkRenderer.cs) | Current chunk mesh and collider rebuild implementation. |
| [`../../Assets/Scripts/SandboxPlayerController.cs`](../../Assets/Scripts/SandboxPlayerController.cs) | Current player movement and tile editing interface. |

# External Sources

| Source | Applied convention |
| --- | --- |
| [Google Cloud: Introducing the Open Knowledge Format](https://cloud.google.com/blog/products/data-analytics/how-the-open-knowledge-format-can-improve-data-sharing) | Treat the wiki as portable markdown plus YAML frontmatter for human and agent consumption. |
| [Open Knowledge Format repository](https://github.com/GoogleCloudPlatform/knowledge-catalog/tree/main/okf) | Use `index.md`, optional `log.md`, concept documents, links, and permissive consumption expectations. |
| [Open Knowledge Format v0.1 specification](https://github.com/GoogleCloudPlatform/knowledge-catalog/blob/main/okf/SPEC.md) | Require `type` on concept documents and use recommended frontmatter fields where helpful. |
| [Andrej Karpathy: LLM Wiki](https://gist.github.com/karpathy/442a6bf555914893e9891c11519de94f) | Model this directory as the maintained wiki layer between raw sources and future LLM work. |

# Notes

The OKF references are about format and maintenance conventions, not gameplay architecture. The Unity sandbox design document remains the source of truth for game-specific decisions.
