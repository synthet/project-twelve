---
type: Technical Reference
title: "Multiplayer and Modding"
description: "ProjectTwelve Multiplayer and Modding reference — design notes, contracts, and decisions for the multiplayer and modding area of the sandbox prototype."
resource: wiki/multiplayer-and-modding.md
tags: [wiki, multiplayer]
timestamp: 2026-06-28T00:00:00Z
okf_version: 0.1
---

# Multiplayer and Modding

## Multiplayer

The target model is server-authoritative. Clients send input and tile-edit requests. The server validates range, inventory, permissions, and cooldowns before applying edits and broadcasting tile deltas or chunk diffs.

## Modding

Content should move toward registries keyed by stable string IDs for tiles, items, entities, biomes, and recipes. ScriptableObjects or JSON can describe gameplay properties while code implements behavior hooks.
