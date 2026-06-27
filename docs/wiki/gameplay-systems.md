---
type: System Concept
title: Gameplay Systems
description: Player, inventory, item, enemy, and pathfinding boundaries for future work.
resource: ../terraria-like-unity-design.md
tags: [unity, gameplay, inventory, pathfinding, okf]
timestamp: 2026-06-27T00:00:00Z
status: active
---

# Gameplay Systems

## Player

The player controller owns movement input and translates tile edit inputs into world requests. Validation such as edit range belongs near the player/controller boundary, while authoritative world mutation belongs in `SandboxWorld` or a future server layer.

## Inventory and Items

Future inventory should be data-driven and registry-backed. Tile placement should consume item stacks after validation. Tool strength, range, cooldown, and tile damage should be item/tool properties rather than hard-coded player logic.

## Enemies and Pathfinding

Enemy movement should use chunk-aware navigation. Basic enemies can query solid tiles directly; advanced platformer pathfinding needs jump height, fall distance, doors, ladders, and terrain edits that invalidate paths.

## Source Alignment

This page defines gameplay boundaries for future work and should stay aligned with player, inventory, item, enemy, and pathfinding source changes.
