---
type: Source-of-Truth Map
title: Spec-Driven Development Tasks
description: The phase-by-phase spec-driven backlog (P0–P5) with per-task acceptance criteria, verification gates, and the cross-phase task template that the ticket schema expands on.
resource: wiki/spec-driven-development-tasks.md
tags: [wiki, backlog, tasks, planning, roadmap]
timestamp: 2026-06-28T00:00:00Z
okf_version: 0.1
---

# Spec-Driven Development Tasks

> **Status:** Planning backlog.
> **Purpose:** Convert the preliminary roadmap into executable development tasks that preserve a spec-first workflow from discovery through launch.
> **Approach:** Every task starts with an explicit specification, acceptance criteria, and test plan before implementation begins.

## Spec-driven workflow

Use this cycle for every feature, system, bug fix, and content pipeline change:

1. **Intent capture** — define the player outcome, technical objective, constraints, and non-goals.
2. **Specification** — write or update the relevant wiki/design page with data contracts, invariants, lifecycle rules, and edge cases.
3. **Task breakdown** — split the spec into small implementation, test, documentation, and validation tasks.
4. **Implementation** — build only the behavior covered by the accepted spec and keep changes chunk-first.
5. **Verification** — run automated tests, deterministic checks, Unity play-mode/manual checks, and profiling gates listed in the task.
6. **Review and traceability** — link commits, tests, and docs back to the driving spec section.
7. **Retrospective** — capture follow-up tasks, spec gaps, defects, and performance regressions before starting the next cycle.

## Cross-phase task template

Each task should be recorded with the following fields. The canonical ID format, area-code
registry, and full ticket frontmatter/body schema live in the
[Backlog Task Schema & ID Conventions](task-schema.md) page; the table below is the per-row
summary it expands on.

| Field | Requirement |
|-------|-------------|
| **ID** | Stable phase-prefixed identifier `P<phase>-<AREA>-<NNN>`, such as `P0-SPEC-001` (see [task schema](task-schema.md#task-id-format)). |
| **Spec reference** | Wiki/design section that defines the desired behavior. |
| **User/system outcome** | Observable result from the player's or subsystem's perspective. |
| **Acceptance criteria** | Pass/fail statements that can be tested without interpretation. |
| **Implementation notes** | Key files, dependencies, and architectural constraints. |
| **Verification** | Edit-mode tests, play-mode tests, deterministic fixtures, screenshots, profiling, or manual QA. |
| **Docs impact** | Pages that must be updated if the implementation changes the design. |
| **Exit evidence** | Commit, test output, screenshots, profiler captures, and reviewer notes. |

## Phase P0 — Discovery and specification baseline

| ID | Task | Acceptance criteria | Verification |
|----|------|---------------------|--------------|
| P0-SPEC-001 | Consolidate the product brief, architecture overview, and glossary into a single baseline scope statement. | Core genre, target prototype, non-goals, and terminology are unambiguous. | Documentation review against `project-brief.md`, `00-overview.md`, and `glossary.md`. |
| P0-SPEC-002 | Define spec ownership for every subsystem page. | Each subsystem has an owner, status, dependencies, and review cadence. | Wiki index includes current status and cross-links. |
| P0-SPEC-003 | Establish the task schema and ID conventions in the backlog. | New work can be traced from spec to task to commit to verification. | Create sample tasks for the first prototype sprint. |
| P0-SPEC-004 | Define project quality gates. | Required tests, lint/build checks, deterministic seeds, and manual Unity checks are documented. | Dry-run checklist on the existing barebone sandbox. |
| P0-SPEC-005 | Identify architectural risks and spike candidates. | Top risks have mitigations, owners, and decision deadlines. | Risk review covering chunking, rendering, collision, persistence, and networking. |

## Phase P1 — Prototype vertical slice

| ID | Task | Acceptance criteria | Verification |
|----|------|---------------------|--------------|
| P1-WORLD-001 | Specify deterministic world, chunk, and local coordinate conversions, including negative coordinates. | Conversion formulas and boundary cases are documented before code changes. | Edit-mode tests for origin, positive, negative, and boundary positions. |
| P1-WORLD-002 | Implement chunk lifecycle tasks for load, visibility, dirty flags, and unload. | Chunks load around the player, mark render/collision dirtiness independently, and unload outside range. | Play-mode traversal plus dirty-flag unit tests. |
| P1-RENDER-001 | Specify and implement chunk-local render rebuild behavior. | Rebuilds are bounded to dirty visible chunks and do not touch unrelated chunks. | Render rebuild tests and profiler sample during tile edits. |
| P1-COLL-001 | Specify prototype collision rules for solid tiles and player movement. | Player can stand, jump, collide with terrain, and avoid tunneling in target scenarios. | Play-mode movement checklist and collision edge-case tests. |
| P1-EDIT-001 | Specify tile edit flow through a single `SetTile` choke point. | Place/break updates tile data, dirty flags, render, collision, and neighboring border chunks. | Unit tests for central edits and border edits. |
| P1-GEN-001 | Specify deterministic terrain generation inputs and outputs. | Same seed and chunk coordinate produce identical tiles across runs. | Golden-seed deterministic generation tests. |
| P1-QA-001 | Package the prototype vertical-slice demo checklist. | A reviewer can validate movement, generation, rendering, collision, and editing in one scene. | Manual QA checklist with screenshots or short capture. |

## Phase P2 — Core systems alpha

| ID | Task | Acceptance criteria | Verification |
|----|------|---------------------|--------------|
| P2-DATA-001 | Specify tile, item, and entity registry contracts. | IDs, serialization names, mod-safety rules, and migration behavior are documented. | Registry validation tests with duplicate and missing IDs. |
| P2-GEN-001 | Specify biome, cave, structure, and ore generation passes. | Pass order, seed usage, and conflict resolution are deterministic. | Golden-world comparison tests for representative seeds. |
| P2-LIGHT-001 | Specify lighting data layout and propagation rules. | Light updates are chunk-bounded where possible and neighbor propagation is explicit. | Lighting unit tests for edits, borders, and occlusion. |
| P2-FLUID-001 | Specify simple liquid simulation constraints. | Flow rate, update budget, chunk wake/sleep, and save format are defined. | Deterministic simulation fixtures and performance budget checks. |
| P2-INV-001 | Specify inventory-backed placement and pickup rules. | Items are consumed/restored consistently by place/break actions. | Inventory edit tests and play-mode placement checklist. |
| P2-AI-001 | Specify enemy spawn and pathfinding rules. | Navigation respects terrain, jump/fall limits, and loaded-world boundaries. | Pathfinding fixtures across platforms, caves, and blocked routes. |
| P2-SAVE-001 | Specify save/load format using seed plus dirty chunk diffs. | Clean chunks derive from seed; edited chunks persist and migrate safely. | Round-trip save/load tests and corrupted-save handling tests. |
| P2-TOOL-001 | Specify debug tooling for chunks, generation, lighting, and saves. | Tools expose enough state to debug without modifying runtime contracts. | Editor smoke tests and screenshots of inspector/debug views. |

## Phase P3 — Networking alpha

| ID | Task | Acceptance criteria | Verification |
|----|------|---------------------|--------------|
| P3-NET-001 | Specify authoritative server rules for movement, tile edits, inventory, and chunk subscription. | Clients cannot apply persistent world changes without server validation. | Threat-model review and validation tests. |
| P3-NET-002 | Specify chunk snapshot and delta formats. | Initial sync and incremental edits are versioned, ordered, and replayable. | Serialization round-trip and packet-loss/reorder simulation tests. |
| P3-NET-003 | Implement LAN two-player synchronization tasks. | Two players see consistent movement, edits, and chunk state under expected latency. | LAN playtest checklist and network profiler capture. |
| P3-NET-004 | Specify disconnect/reconnect and save consistency behavior. | Reconnected clients converge to server state without duplicating edits/items. | Reconnect scenario tests. |

## Phase P4 — Feature complete and beta

| ID | Task | Acceptance criteria | Verification |
|----|------|---------------------|--------------|
| P4-CONTENT-001 | Specify crafting, progression, combat, and loot loops. | Core gameplay loops are testable and balanced with placeholder content. | Scenario tests and balance review notes. |
| P4-MOD-001 | Specify mod content packaging, loading, and compatibility boundaries. | Mods can add supported data without breaking save or network contracts. | Mod fixture load tests and compatibility matrix. |
| P4-UX-001 | Specify production UI flows for inventory, crafting, settings, and multiplayer. | UI supports keyboard/mouse and controller-ready navigation targets. | UX review checklist and screenshot capture. |
| P4-PERF-001 | Create optimization tasks for chunk streaming, rendering, collision, lighting, and liquids. | Performance budgets are defined for target hardware and content scale. | Profiler captures before and after optimization. |
| P4-QA-001 | Run beta playtest task cycles. | Feedback is triaged into spec changes, bugs, tuning, or deferred scope. | Playtest reports linked to backlog items. |

## Phase P5 — Release candidate and launch

| ID | Task | Acceptance criteria | Verification |
|----|------|---------------------|--------------|
| P5-REL-001 | Specify release criteria and launch-blocking bug classes. | Ship/no-ship decisions are objective and tied to tested scenarios. | Release readiness review. |
| P5-MIG-001 | Specify save migrations and compatibility policy. | Saves from supported versions migrate or fail safely with user-facing messaging. | Migration test suite over archived save fixtures. |
| P5-PLAT-001 | Complete platform, input, resolution, and performance certification tasks. | Target platforms meet minimum functionality and performance criteria. | Platform smoke tests and profiler captures. |
| P5-DOC-001 | Prepare player-facing and developer-facing documentation. | Setup, troubleshooting, controls, modding, and known issues are documented. | Documentation QA pass. |
| P5-LAUNCH-001 | Execute launch checklist and post-launch monitoring plan. | Build artifacts, release notes, rollback plan, and hotfix workflow are ready. | Dry-run release and post-launch incident rehearsal. |

## Backlog governance

- Keep tasks small enough to complete with one focused implementation and verification pass.
- Do not move a task to implementation until its specification and acceptance criteria are reviewed.
- Do not close a task until tests, manual checks, and documentation impacts are recorded.
- Promote repeated defects into new spec requirements rather than one-off fixes.
- Revisit phase scope after every milestone retrospective and update the roadmap when sequencing changes.
