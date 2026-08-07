---
type: Documentation Index
title: Documentation Index
description: Index of all documentation pages in this bundle.
resource: INDEX.md
tags: [docs, index]
timestamp: 2026-06-28T00:00:00Z
okf_version: 0.1
---

# Documentation index

This index groups the repository documentation by purpose. For the recommended reading path, start with [README.md](README.md); for implementation details, start with [wiki/README.md](wiki/README.md).

## Entry points

- [README.md](README.md) — documentation hub and maintenance checklist.
- [wiki/README.md](wiki/README.md) — implementation wiki index and cross-cutting invariants.
- [project/INDEX.md](project/INDEX.md) — project governance index.

## Governance, safety, and conventions

- [CANONICAL_SOURCES.md](CANONICAL_SOURCES.md) — authority map for resolving conflicting references.
- [WIKI_SCHEMA.md](WIKI_SCHEMA.md) — wiki structure, required page conventions, and maintenance rules.
- [OKF_ADOPTION.md](OKF_ADOPTION.md) — Open Knowledge Format profile and lint guidance.
- [security.md](security.md) — security model, secret-handling expectations, and release checklist.
- [PAID_ASSETS.md](PAID_ASSETS.md) — licensed asset policy and public-repo guardrails.

## Agent and contributor workflow

- [ai-workflow/README.md](ai-workflow/README.md) — agent asset map and SDLC loop.
- [agent-asset-inventory.md](agent-asset-inventory.md) — generated inventory of mirrored agent skills, commands, and subagents.
- [EXTERNAL_CLI_REVIEWS.md](EXTERNAL_CLI_REVIEWS.md) — optional review-only external CLI setup.
- [project/00-backlog-workflow.md](project/00-backlog-workflow.md) — backlog, ticket, and board workflow.

## Product, architecture, and visual references

- [terraria-like-unity-design.md](terraria-like-unity-design.md) — canonical product-level architecture plan.
- [terraria-like-unity-design-detailed.md](terraria-like-unity-design-detailed.md) — expanded long-form design companion.
- [VISUAL_BEHAVIOR_SPEC.md](VISUAL_BEHAVIOR_SPEC.md) — visual behavior contracts for tiles, sprites, and avatars.
- [VISUAL_SETUP.md](VISUAL_SETUP.md) — local visual asset setup and generated catalog notes.

## Unity reference snapshots

- [unity-reference/README.md](unity-reference/README.md) — Unity reference bundle overview.
- [unity-reference/unity-manual-index.md](unity-reference/unity-manual-index.md) — Unity Manual index snapshot.
- [unity-reference/unity-scriptreference-index.md](unity-reference/unity-scriptreference-index.md) — Unity Scripting API index snapshot.

## Implementation wiki: prototype-aligned pages

- [wiki/project-brief.md](wiki/project-brief.md) — project goals, scope, and constraints.
- [wiki/architecture-map.md](wiki/architecture-map.md) — current runtime architecture map.
- [wiki/world-and-chunk-data.md](wiki/world-and-chunk-data.md) — world, chunk, and tile data responsibilities.
- [wiki/rendering-and-collision.md](wiki/rendering-and-collision.md) — chunk rendering and collider responsibilities.
- [wiki/simulation-systems.md](wiki/simulation-systems.md) — lighting, liquid, and simulation notes.
- [wiki/generation-and-saving.md](wiki/generation-and-saving.md) — generation and persistence flow.
- [wiki/gameplay-systems.md](wiki/gameplay-systems.md) — player, interaction, and gameplay systems.
- [wiki/multiplayer-and-modding.md](wiki/multiplayer-and-modding.md) — future multiplayer/modding boundaries.
- [wiki/roadmap-and-tasks.md](wiki/roadmap-and-tasks.md) — implementation roadmap and work items.
- [wiki/visual-integration.md](wiki/visual-integration.md) — visual asset integration points.

## Implementation wiki: numbered subsystem reference

- [wiki/00-overview.md](wiki/00-overview.md) — goals, scope, and guiding principles.
- [wiki/01-architecture.md](wiki/01-architecture.md) — system decomposition, ownership, and data flow.
- [wiki/02-data-models.md](wiki/02-data-models.md) — tile, chunk, world, entity, inventory, and coordinate models.
- [wiki/03-chunking.md](wiki/03-chunking.md) — chunk size, streaming, and dirty flags.
- [wiki/04-rendering.md](wiki/04-rendering.md) — tilemap, mesh, and instancing tradeoffs.
- [wiki/05-collision-physics.md](wiki/05-collision-physics.md) — collision generation and physics boundaries.
- [wiki/06-lighting.md](wiki/06-lighting.md) — tile lightmap and propagation design.
- [wiki/07-procedural-generation.md](wiki/07-procedural-generation.md) — terrain, caves, biomes, ores, and structures.
- [wiki/08-liquids.md](wiki/08-liquids.md) — cellular-automaton liquid simulation.
- [wiki/09-pathfinding.md](wiki/09-pathfinding.md) — grid A* and dynamic terrain updates.
- [wiki/10-multiplayer.md](wiki/10-multiplayer.md) — server-authoritative sync and tile deltas.
- [wiki/11-saving-loading.md](wiki/11-saving-loading.md) — chunk saves, diffs, and versioning.
- [wiki/12-modding.md](wiki/12-modding.md) — registries, data-driven content, and Addressables.
- [wiki/13-tooling-testing.md](wiki/13-tooling-testing.md) — debug views, tests, and profiling targets.
- [wiki/14-roadmap.md](wiki/14-roadmap.md) — milestones, sequencing, and priorities.
- [wiki/15-assets-integration.md](wiki/15-assets-integration.md) — sprites, atlases, animations, and Unity asset seams.

## Implementation wiki: supporting pages

- [wiki/architecture-blueprint.md](wiki/architecture-blueprint.md) — text translation of the visual architecture blueprint.
- [wiki/enemy-nav-and-spawn.md](wiki/enemy-nav-and-spawn.md) — P2 spec for grid A* platformer pathfinding and enemy spawning rules.
- [wiki/glossary.md](wiki/glossary.md) — shared vocabulary.
- [wiki/spec-driven-development-tasks.md](wiki/spec-driven-development-tasks.md) — phase-by-phase backlog and verification gates.

## Implementation wiki: HUD and PixelLab assets

- [wiki/hud-assets-manifest.md](wiki/hud-assets-manifest.md) — exact bitmap, Unity import, layout, and generation contract for the creative HUD.
- [wiki/hud-redesign-pixellab.md](wiki/hud-redesign-pixellab.md) — PixelLab capability research and the v3 asset contract plan.
- [wiki/pixellab-api-v2.md](wiki/pixellab-api-v2.md) — PixelLab MCP/REST routing, security, polling, and integration rules.
- [wiki/hud-conversation-summary.md](wiki/hud-conversation-summary.md) — chronological record of the HUD design and generation work.
- [specs/hud-assets.json](specs/hud-assets.json) — machine-readable HUD asset contract (companion to the manifest).

## Activity

- [log.md](log.md) — append-only documentation activity log.
