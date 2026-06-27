---
type: System Concept
title: Multiplayer and Modding
description: Server-authoritative multiplayer and data-driven modding direction.
resource: ../terraria-like-unity-design.md
tags: [unity, multiplayer, modding, registries, okf]
timestamp: 2026-06-27T00:00:00Z
---

# Multiplayer and Modding

## Multiplayer

The target model is server-authoritative. Clients send input and tile-edit requests. The server validates range, inventory, permissions, and cooldowns before applying edits and broadcasting tile deltas or chunk diffs.

## Modding

Content should move toward registries keyed by stable string IDs for tiles, items, entities, biomes, and recipes. ScriptableObjects or JSON can describe gameplay properties while code implements behavior hooks.
