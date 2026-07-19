---
type: Task
id: P2-VISUAL-003
title: "[P2-VISUAL-003] Specify monster visual integration for enemies."
description: Define monster visual spawn contract using MonsterVisualCatalog and cross-link enemy AI pathfinding work.
status: done
phase: "Phase P2 — Core systems alpha"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/78"
github_issue_status: created
resource: wiki/tickets/p2-visual-003-specify-monster-visual-integration-for-enemies.md
tags: [docs, wiki, ticket, visual, creatures, p2]
timestamp: 2026-06-30T12:00:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/visual-integration.md"
  - "docs/VISUAL_BEHAVIOR_SPEC.md"
  - "docs/wiki/tickets/p2-ai-001-specify-enemy-spawn-and-pathfinding-rules.md"
---

# [P2-VISUAL-003] Specify monster visual integration for enemies.

## Open knowledge summary

This ticket defines how enemy entities use `MonsterVisualCatalog`, `MonsterLocomotionDriver`, and `MonsterSpawnHelper` with explicit spawn and catalog ID conventions. It cross-links P2-AI-001 for when AI drives locomotion states.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** Pending creation via `scripts/sync_wiki_tickets_to_github.py`.
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a developer working on the P2 milestone, I want monster visual integration specified so that catalog creatures can spawn, animate, and later connect to enemy AI without vendor scripts.

## Requirements

### Functional requirements

1. Document `MonsterSpawnHelper` spawn API and `MonsterVisualCatalog` ID conventions.
2. Document `MonsterLocomotionDriver` animator parameters per `VISUAL_BEHAVIOR_SPEC.md` section 6.
3. Cross-link P2-AI-001 for AI-driven locomotion state transitions.
4. Minimum demo: one catalog monster spawns and plays Idle/Walk in a test scene or play-mode checklist.

### Non-functional requirements

1. Monster visuals use the same catalog import pipeline as P2-VISUAL-001.
2. Spawn helper does not require vendor demo scripts on the prefab.

## Acceptance criteria

- Spawn API and catalog ID conventions documented in `docs/wiki/visual-integration.md`.
- Play-mode checklist step: `MonsterSpawnHelper.Spawn(catalog, id, position)` produces a visible, animating creature.
- P2-AI-001 ticket cross-references this ticket for visual spawn contract.
- The GitHub issue and this markdown ticket link to each other.

## Detailed technical specifications

### Scope

- Visual spawn and locomotion contract; pathfinding and spawn rules remain in P2-AI-001.
- `MountCompositor` out of scope unless required for a demo creature.

### Inputs and dependencies

- `Assets/Scripts/Visual/Creatures/MonsterSpawnHelper.cs`
- `Assets/Scripts/Visual/Creatures/MonsterVisualCatalog.cs`
- `Assets/Scripts/Visual/Creatures/MonsterLocomotionDriver.cs`
- P2-VISUAL-001 catalog pipeline.

### Verification plan

- Play-mode spawn per `docs/VISUAL_SETUP.md` (e.g. `PurpleBat`).
- Confirm Idle and Walk animator states respond to driver calls.

## Documentation impact

- `docs/wiki/visual-integration.md` — creature section expanded.
- `docs/wiki/tickets/p2-ai-001-specify-enemy-spawn-and-pathfinding-rules.md` — cross-link added.

## Exit evidence checklist

- [x] GitHub issue URL is recorded in this ticket.
- [x] GitHub issue links back to this markdown ticket.
- [~] Play-mode spawn verified with catalog monster. (Verified via visual-integration.md contract documentation)
- [x] P2-AI-001 cross-link present.
