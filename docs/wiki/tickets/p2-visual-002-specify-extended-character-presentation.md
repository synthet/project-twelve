---
type: Task
id: P2-VISUAL-002
title: "[P2-VISUAL-002] Specify extended character presentation (effects, firearms, locomotion)."
description: Specify and implement extended character VFX, detached firearms, and locomotion gaps vs vendor reference behavior.
status: open
phase: "Phase P2 — Core systems alpha"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/77"
github_issue_status: created
resource: wiki/tickets/p2-visual-002-specify-extended-character-presentation.md
tags: [docs, wiki, ticket, visual, characters, p2]
timestamp: 2026-06-30T12:00:00Z
okf_version: 0.1
spec_references:
  - "docs/VISUAL_BEHAVIOR_SPEC.md"
  - "docs/wiki/visual-integration.md"
---

# [P2-VISUAL-002] Specify extended character presentation (effects, firearms, locomotion).

## Open knowledge summary

This ticket specifies and implements gaps between ProjectTwelve character presentation and the Pixel Heroes Hub reference: sprite effects (dust, muzzle), detached firearms, Walk vs Run locomotion threshold, combat animator triggers, and open spec items (Horns layer, firearm head-row shift, shield overlay rules).

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** Pending creation via `scripts/sync_wiki_tickets_to_github.py`.
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a developer working on the P2 milestone, I want extended character presentation specified and partially wired so that VFX, firearms, and additional locomotion states match the documented visual behavior contract.

## Requirements

### Functional requirements

1. Wire `EffectCatalog` in the sandbox scene; Run/Jump/Land dust visible when catalog is present.
2. Detached firearm mode: `PlayerAvatarFactory` wires `FirearmVisual` transforms; muzzle socket documented.
3. Locomotion: Walk vs Run velocity threshold; combat triggers (`Slash`, `Shot`, etc.) documented as simulation-driven (not sandbox-wired until combat exists).
4. Close spec gaps in `VISUAL_BEHAVIOR_SPEC.md`: implement Horns in merge order, firearm head-row shift, and shield overlay — or mark explicit non-goals with rationale.

### Non-functional requirements

1. Presentation changes must not alter `SandboxPlayerController` physics contracts.
2. VFX remain client-side presentation unless gameplay explicitly depends on them.

## Acceptance criteria

- `EffectCatalog` reference in scene enables dust on Run/Jump/Land state transitions.
- `FirearmVisual` child transforms are wired when `CharacterComposer.Firearm` is set.
- Walk vs Run threshold is documented and implemented in `SandboxPlayerAvatarAnimation` or driver.
- Spec gaps (Horns, head-row shift, shield overlay) are implemented or recorded as non-goals in `visual-integration.md`.
- Animator parameter checklist validated against `VISUAL_BEHAVIOR_SPEC.md` section 5.
- The GitHub issue and this markdown ticket link to each other.

## Detailed technical specifications

### Scope

- `CharacterComposer`, `PlayerAvatarFactory`, `CharacterLocomotionDriver`, `SandboxPlayerAvatarAnimation`, `EffectCatalog`.
- Combat trigger wiring deferred until combat simulation ticket exists; spec documents the contract only.

### Inputs and dependencies

- P1-VISUAL-001 (baseline avatar integration).
- Pixel Heroes Hub wiki sections on firearms and sprite effects (external reference).

### Verification plan

- Play-mode VFX smoke test with `EffectCatalog` in scene.
- Manual animator parameter checklist for extended states.
- Firearm spawn test with equipment string including firearm layer.

## Documentation impact

- `docs/VISUAL_BEHAVIOR_SPEC.md` — update if non-goals chosen.
- `docs/wiki/visual-integration.md` — implementation status matrix.

## Exit evidence checklist

- [ ] GitHub issue URL is recorded in this ticket.
- [ ] GitHub issue links back to this markdown ticket.
- [ ] Play-mode VFX screenshots or notes attached.
- [ ] Spec gaps resolved or explicitly deferred.
