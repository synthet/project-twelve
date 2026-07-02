---
type: Task
id: P1-QA-001
title: "[P1-QA-001] Package the prototype vertical-slice demo checklist."
description: Reviewer-executable demo script validating movement, generation, rendering, collision, editing, autotile terrain, and the composed avatar in one scene.
status: in_progress
phase: "Phase P1 — Prototype vertical slice"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/28"
github_issue_status: created
resource: wiki/tickets/p1-qa-001-package-the-prototype-vertical-slice-demo-checklist.md
tags: [docs, wiki, ticket, qa, p1]
timestamp: 2026-07-01T00:00:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/spec-driven-development-tasks.md"
  - "docs/wiki/13-tooling-testing.md"
  - "docs/wiki/quality-gates.md"
  - "docs/wiki/visual-integration.md"
  - "docs/VISUAL_SETUP.md"
---

# [P1-QA-001] Package the prototype vertical-slice demo checklist.

## Open knowledge summary

This ticket packages the P1 vertical slice into a single reviewer-executable demo runbook. It turns
the scattered play-mode checks from `docs/wiki/quality-gates.md` § Manual Unity checks and
`docs/VISUAL_SETUP.md` into one ordered script with a fixed seed, so any reviewer can validate the
prototype (movement, deterministic generation, chunk-local rendering, collision, tile editing,
autotiled terrain, composed avatar) in one session and produce comparable evidence.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** [synthet/project-twelve#28](https://github.com/synthet/project-twelve/issues/28)
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a reviewer of the P1 milestone, I want a single ordered demo checklist with a pinned scene and
seed so that I can verify every vertical-slice capability in one play session and attach
reproducible evidence, without needing tribal knowledge about which systems exist or how to
exercise them.

## Requirements

### Functional requirements

1. Deliver a demo runbook (new page `docs/wiki/p1-vertical-slice-demo.md`, type `Runbook`) that a
   reviewer can execute top-to-bottom in one play session.
2. The runbook pins the demo configuration: scene path, world seed, and required local setup
   (`Assets/_Licensed` submodule initialized; character catalog and avatar prefab configured per
   `docs/VISUAL_SETUP.md`).
3. Every P1 capability has at least one numbered step with an observable pass/fail outcome:
   movement, chunk streaming, deterministic generation, chunk-local render rebuilds, collision,
   tile place/break, autotiled terrain, composed avatar animation (Idle/Run/Jump/Fall/Land).
4. The runbook defines the evidence to capture per step (screenshot, short capture, or note) and
   where to attach it (PR / issue / this ticket's exit evidence).
5. Degraded-setup behavior is specified: without `Assets/_Licensed`, terrain must still render via
   the fallback path and the runbook must mark visual steps as skipped, not failed.

### Non-functional requirements

1. A reviewer unfamiliar with the codebase completes the runbook in under 30 minutes.
2. Steps rely only on shipped input bindings and debug toggles — no code edits or inspector surgery
   mid-run.
3. The checklist stays deterministic: the pinned seed and start position produce the same terrain
   and the same first-screen composition on every run.
4. The runbook does not duplicate subsystem specs; it links to them (`quality-gates.md`,
   `13-tooling-testing.md`, `visual-integration.md`).

## Acceptance criteria

- A reviewer can validate movement, generation, rendering, collision, and editing in one scene by
  following the runbook without outside help.
- Reloading the same seed reproduces identical terrain (spot-checked at three documented world
  positions, including at least one negative-coordinate position).
- Tile place/break updates render and collision, including edits on a chunk border affecting the
  neighbor chunk.
- With `Assets/_Licensed` initialized, autotiled terrain renders on chunk meshes (not
  vertex-color fallback only).
- With character catalog and prefab configured, a composed player avatar spawns and animates
  Idle/Run/Jump/Fall/Land during movement.
- Visual verification steps align with the `docs/VISUAL_SETUP.md` play-mode checklist.
- The GitHub issue and this markdown ticket link to each other.
- Exit evidence records the commit, verification commands, manual QA notes, and reviewer findings.

## Detailed technical specifications

### Scope

- Author `docs/wiki/p1-vertical-slice-demo.md` (Runbook) and link it from
  `docs/wiki/13-tooling-testing.md` and the tickets README.
- Pin demo constants in the runbook: scene (`Assets/Scenes/` prototype scene), world seed, expected
  spawn area description.
- Out of scope: new debug tooling (P2-TOOL-001), automated PlayMode coverage of these steps
  (exists partially in `Assets/Tests/PlayMode/SandboxCollisionPlayModeTests.cs`), and any gameplay
  changes.

### Inputs and dependencies

- Manual QA checklist and profiler targets: `docs/wiki/quality-gates.md` § Manual Unity checks.
- Visual setup and play-mode checklist: `docs/VISUAL_SETUP.md`, `docs/wiki/visual-integration.md`.
- Runtime state inspection (optional steps): in-game MCP tools (`player_state`, `world_info`,
  `tile_at`) per `docs/wiki/13-tooling-testing.md`.
- Implemented P1 behavior: `SandboxWorld.cs`, `SandboxPlayerController.cs`,
  `SandboxChunkRenderer.cs`, `SandboxPlayerAvatarVisual.cs`.

### Demo script outline (to be finalized in the runbook)

| # | Step | Pass condition |
|---|------|----------------|
| 1 | Open pinned scene, set pinned seed, press Play | Player spawns on solid ground, not inside terrain/liquid |
| 2 | Move left/right, jump | Responsive control; avatar plays Idle/Run/Jump/Fall/Land |
| 3 | Traverse 3+ chunk borders each direction | New chunks stream in; no hitches; far chunks unload |
| 4 | Break and place tiles mid-chunk | Render + collision update on the edited chunk only |
| 5 | Break and place tiles on a chunk border | Neighbor chunk's mesh/collider updates too |
| 6 | Verify autotile seams around edits | Edges/corners re-resolve; no missing or mismatched sprites |
| 7 | Stop Play; replay with same seed | Terrain identical at 3 documented positions (incl. negative x) |
| 8 | 5-minute free play | Stable frame rate; no GC spikes; no errors in console |

### Verification plan

- Manual QA checklist execution with screenshots or a short capture per the evidence table in the
  runbook.
- Deterministic spot-check: same-seed reload comparison at documented positions.
- `python3 scripts/check_markdown_links.py` and OKF lint for the new runbook page.

## Documentation impact

- New page: `docs/wiki/p1-vertical-slice-demo.md` (Runbook, OKF frontmatter required).
- `docs/wiki/13-tooling-testing.md` — link the runbook from the manual-checks section.
- Update `docs/wiki/spec-driven-development-tasks.md` if task scope or sequencing changes.
- Keep this ticket synchronized with the final GitHub issue URL and outcome.

## Exit evidence checklist

- [x] GitHub issue URL is recorded in this ticket.
- [ ] GitHub issue links back to this markdown ticket.
- [x] Demo runbook authored with pinned scene/seed and evidence table:
      [`docs/wiki/p1-vertical-slice-demo.md`](../p1-vertical-slice-demo.md).
- [ ] Full checklist executed by someone other than the author, with evidence attached.
- [ ] Degraded-setup path (no `Assets/_Licensed`) executed and documented.
- [ ] Follow-up tasks created for any failed or flaky steps.
