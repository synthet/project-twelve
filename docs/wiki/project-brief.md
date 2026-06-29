---
type: Documentation Hub
title: Project Brief
description: Vision, prototype scope, and current-state non-goals for the Unity 2D sandbox prototype.
resource: wiki/project-brief.md
tags: [wiki, scope, prototype, brief]
timestamp: 2026-06-28T00:00:00Z
okf_version: 0.1
---

# Project Brief

## Vision

Build a Unity 2D side-scrolling sandbox inspired by *Terraria* and *Starbound*: a destructible tile world, procedural terrain, player movement, block placement and removal, future inventory, enemies, lighting, liquids, persistence, and optional multiplayer.

## Prototype Scope

The current codebase should stay intentionally small. The prototype target is a vertical slice that proves:

- Chunked world storage.
- Procedural chunk generation.
- Chunk-local rendering and collision rebuilds.
- Basic platformer movement.
- Mouse-driven tile placement and removal.

Systems outside that slice should be represented in documentation before they become production code.

## Non-Goals for Barebone Project State

- No hex-grid demo or unrelated sample gameplay.
- No generated demo scene clutter.
- No committed IDE-generated Unity solution files.
- No production implementation for inventories, enemies, liquids, multiplayer, or modding until their interfaces are designed.
