---
type: Task
id: P1-VISUAL-001
title: "[P1-VISUAL-001] Specify sandbox player avatar visual integration."
description: Document and validate sandbox player avatar composition, locomotion, and vendor parity for the P1 vertical slice.
status: open
phase: "Phase P1 — Prototype vertical slice"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/74"
github_issue_status: created
resource: wiki/tickets/p1-visual-001-specify-sandbox-player-avatar-visual-integration.md
tags: [docs, wiki, ticket, visual, characters, p1]
timestamp: 2026-06-30T12:00:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/visual-integration.md"
  - "docs/VISUAL_BEHAVIOR_SPEC.md"
  - "docs/VISUAL_SETUP.md"
---

# [P1-VISUAL-001] Specify sandbox player avatar visual integration.

## Open knowledge summary

This ticket documents and validates the sandbox player avatar presentation path: `PlayerAvatarFactory` spawns a composed character, foot alignment works against the player collider, and `SandboxPlayerAvatarAnimation` drives Idle/Run/Jump/Fall/Land from controller velocity. It also records vendor-to-project parity and extends the P1 vertical-slice QA checklist with visual steps.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** Pending creation via `scripts/sync_wiki_tickets_to_github.py`.
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a developer or reviewer working on the P1 milestone, I want sandbox player avatar visual integration specified and validated so that play mode demonstrates composed characters with correct locomotion without vendor demo scripts.

## Requirements

### Functional requirements

1. Document the vendor-to-project parity table in `docs/wiki/visual-integration.md` (CharacterBuilder → `CharacterComposer`, SpriteLibrary → runtime asset, CharacterAnimation → `CharacterLocomotionDriver`).
2. Extend P1-QA-001 acceptance criteria with autotiled terrain and composed avatar checks from `docs/VISUAL_SETUP.md`.
3. Confirm `PlayerAvatarFactory`, `SandboxPlayerAvatarVisual`, and `SandboxPlayerAvatarAnimation` work together in play mode when the assets submodule is initialized.
4. Record non-goals deferred to P2-VISUAL-002: combat triggers, detached firearms, Walk vs Run threshold.

### Non-functional requirements

1. No runtime references to vendor demo scripts (`CharacterBuilder`, `CharacterAnimation`, `CharacterControls`).
2. Verification steps are reproducible with submodule setup documented in `docs/VISUAL_SETUP.md`.

## Acceptance criteria

- Play mode spawns a composed avatar via `PlayerAvatarFactory` when catalog and prefab are available.
- Foot alignment places avatar feet at the bottom of the player `BoxCollider2D`.
- `SandboxPlayerAvatarAnimation` drives Idle, Run, Jump, Fall, and Land from `SandboxPlayerController` state.
- Vendor parity table and implementation status matrix exist in `docs/wiki/visual-integration.md`.
- P1-QA-001 checklist includes autotile terrain and avatar visual verification steps.
- The GitHub issue and this markdown ticket link to each other.

## Detailed technical specifications

### Scope

- Documentation and QA checklist updates; no requirement to implement P2 features (firearms, VFX, combat).
- Align with `docs/VISUAL_BEHAVIOR_SPEC.md` sections 4–5.

### Inputs and dependencies

- `Assets/Scripts/Integration/PlayerAvatarFactory.cs`
- `Assets/Scripts/Sandbox/SandboxPlayerAvatarVisual.cs`
- `Assets/Scripts/Sandbox/SandboxPlayerAvatarAnimation.cs`
- `Assets/_Licensed` submodule with `CharacterLayerCatalog` and character prefab.

### Verification plan

- Follow play-mode steps in `docs/VISUAL_SETUP.md` (terrain autotiles, randomized avatar).
- Manual QA: move, jump, fall, land; confirm animator states change with controller velocity.

## Documentation impact

- `docs/wiki/visual-integration.md` — parity table and status matrix (may already be updated).
- `docs/wiki/tickets/p1-qa-001-package-the-prototype-vertical-slice-demo-checklist.md` — visual checklist items.

## Exit evidence checklist

- [ ] GitHub issue URL is recorded in this ticket.
- [ ] GitHub issue links back to this markdown ticket.
- [ ] Spec references reviewed and updated if needed.
- [ ] Acceptance criteria validated in play mode with submodule.
- [ ] P1-QA-001 updated with visual verification steps.
