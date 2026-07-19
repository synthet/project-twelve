---
type: Concept
title: Multiplayer And Modding
description: Documentation for Multiplayer And Modding.
resource: docs/wiki/multiplayer-and-modding.md
tags: [docs, wiki]
timestamp: 2026-07-19T01:28:50Z
okf_version: 0.1
---

# Multiplayer and Modding

## Multiplayer

The target model is server-authoritative. Clients send input and tile-edit requests. The server validates range, inventory, permissions, and cooldowns before applying edits and broadcasting tile deltas or chunk diffs.

## Modding

Content should move toward registries keyed by stable string IDs for tiles, items, entities, biomes, and recipes. ScriptableObjects or JSON can describe gameplay properties while code implements behavior hooks.
