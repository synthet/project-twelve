---
type: Source-of-Truth Map
title: Backlog Task Schema & ID Conventions
description: The single P0 reference that fixes the backlog task ID format, area codes, required ticket schema, and the spec→task→issue→commit→verification trace, with sample first-sprint tasks.
resource: wiki/task-schema.md
tags: [wiki, backlog, schema, ids, p0, governance]
timestamp: 2026-06-28T00:00:00Z
okf_version: 0.1
---

# Backlog Task Schema & ID Conventions

> **Status:** Baseline (P0). This page is the single agreed definition of *how a backlog item
> is identified and structured*.
> **Owns:** The task ID format, the area-code registry, the required ticket schema, and the
> end-to-end traceability chain.
> **Gated by:** [P0-SPEC-003](tickets/p0-spec-003-establish-the-task-schema-and-id-conventions-in-the-backlog.md).
> **Complements:** the [Spec-Driven Development Tasks](spec-driven-development-tasks.md) backlog
> (the *list* of work) and the [Backlog Workflow](../project/00-backlog-workflow.md) runbook (the
> *process* of moving work). This page defines the *shape* both of them assume.

This page exists so that every backlog item — present and future — carries a **stable identity**
and a **predictable structure**. With those two things fixed, a GitHub issue, an implementation
branch, a reviewer, and a tool can all trace one piece of work from the spec that motivated it to
the verification that closed it, without reverse-engineering ad-hoc conventions from commit
history.

## The traceability chain

Every backlog item must be traceable in both directions along this chain:

```
spec page  →  wiki ticket  →  GitHub issue  →  branch  →  commit(s)  →  PR  →  verification
   ▲              │   ▲            │  ▲                        │            │          │
   │              │   └── github_issue ──┘                    │            │          │
   └── spec_references                                        │            │          │
                  └──────────────── ticket ID (P<n>-AREA-NNN) ┴── Closes #N ┴── Exit evidence
```

The **ticket ID** is the join key. It appears in the ticket filename and frontmatter, in the
GitHub issue title, in the branch's PR (`Closes #N` resolves issue→ticket), and in the exit
evidence. Given any one link, the others are reachable.

| Link | Carried by | Points to |
|------|-----------|-----------|
| spec → ticket | ticket `spec_references` | the wiki/design page(s) defining desired behavior |
| ticket → issue | ticket `github_issue` | the tracking issue URL |
| issue → ticket | issue body backlink | the ticket markdown path |
| issue → PR | `Closes #N` in PR body | the PR that satisfies the issue |
| PR → commits | the merge commit / branch | the implementing diff |
| commits → verification | ticket **Exit evidence** | commands, tests, screenshots, reviewer notes |

## Task ID format

```
P<phase>-<AREA>-<NNN>
```

- **`P<phase>`** — roadmap phase `P0`–`P5` (see [Roadmap](14-roadmap.md) / the
  [phase backlog](spec-driven-development-tasks.md)). The phase is the item's *home*: where it
  first becomes ready, not necessarily where it is touched again.
- **`<AREA>`** — an uppercase subsystem/area code from the [area registry](#area-code-registry)
  below.
- **`<NNN>`** — a zero-padded three-digit sequence number, unique **within** a `P<phase>-<AREA>`
  pair, assigned in creation order (`001`, `002`, …).

Examples: `P0-SPEC-003`, `P1-WORLD-002`, `P2-SAVE-001`.

### ID rules (invariants)

1. **Immutable.** Once assigned and linked to an issue, an ID never changes — not when scope is
   trimmed, not when the item is re-prioritized, not when it moves sprints. Renaming breaks every
   inbound link in the chain above.
2. **Unique.** No two tickets share an ID. Sequence numbers are never reused, even after a ticket
   is closed or abandoned (a `done`/abandoned `NNN` is retired, not recycled).
3. **Phase is fixed at creation.** An item keeps its `P<phase>` prefix even if work on it
   continues into a later phase; the prefix records where it entered the backlog. Genuinely new
   later-phase work gets a new ID.
4. **One ID per deliverable.** If a ticket grows two independent deliverables, split it into two
   IDs rather than overloading one. Follow-up scope discovered mid-task becomes a *new* ticket
   (recorded in the parent's Exit evidence), not a silent expansion of the original.
5. **Casing is canonical.** Phase and area codes are uppercase in IDs and titles; the ticket
   *filename* is lowercase-slugged (`p0-spec-003-…`). Tools match on the uppercase ID in
   frontmatter, not the filename.

### Choosing the area code

Pick the area that **owns the contract being changed**, using the
[Spec Ownership Registry](spec-ownership.md) as the tiebreaker — the area whose owner must approve
the change is the area code. If a task spans areas, file it under the area that owns the *primary*
deliverable and reference the others in `spec_references`; prefer splitting over a multi-area
ticket.

## Area code registry

Area codes are stable abbreviations of subsystem/area names. The set below is the current
registry; add a new code only when no existing area fits, and add it here in the same change.

| Code | Area | Owning spec(s) |
|------|------|----------------|
| `SPEC` | Cross-cutting discovery, scope, process, and quality gates | [Baseline Scope](baseline-scope.md), [Spec Ownership](spec-ownership.md), this page |
| `WORLD` | World/chunk data model, coordinates, chunk lifecycle | [02 Data Models](02-data-models.md), [03 Chunking](03-chunking.md) |
| `RENDER` | Chunk-local mesh/tilemap rebuilds | [04 Rendering](04-rendering.md) |
| `COLL` | Tile collision and player movement | [05 Collision & Physics](05-collision-physics.md) |
| `EDIT` | Tile edit flow (`SetTile` choke point) | [02 Data Models](02-data-models.md), [03 Chunking](03-chunking.md) |
| `GEN` | Deterministic terrain, biome, cave, ore, structure passes | [07 Procedural Generation](07-procedural-generation.md) |
| `LIGHT` | Tile lightmap and propagation | [06 Lighting](06-lighting.md) |
| `FLUID` | Liquid simulation | [08 Liquids](08-liquids.md) |
| `INV` | Inventory-backed placement and pickup | [02 Data Models](02-data-models.md) |
| `AI` | Enemy spawn, pathfinding, gameplay loops | [09 Pathfinding](09-pathfinding.md) |
| `DATA` | Tile/item/entity registry contracts | [02 Data Models](02-data-models.md) |
| `SAVE` | Save/load format, diffs, versioning | [11 Saving & Loading](11-saving-loading.md) |
| `MIG` | Save migrations and compatibility policy | [11 Saving & Loading](11-saving-loading.md) |
| `NET` | Server-authoritative sync, snapshots, deltas | [10 Multiplayer](10-multiplayer.md) |
| `CONTENT` | Crafting, progression, combat, loot loops | [14 Roadmap](14-roadmap.md) |
| `MOD` | Mod packaging, loading, compatibility boundaries | [12 Modding & Content](12-modding.md) |
| `UX` | Production UI flows | [14 Roadmap](14-roadmap.md) |
| `TOOL` | Debug tooling, inspectors | [13 Tooling, Testing & Profiling](13-tooling-testing.md) |
| `PERF` | Profiling and optimization tasks | [13 Tooling, Testing & Profiling](13-tooling-testing.md) |
| `QA` | Playtest checklists and beta cycles | [13 Tooling, Testing & Profiling](13-tooling-testing.md) |
| `DOC` | Player-facing and developer-facing documentation | [14 Roadmap](14-roadmap.md) |
| `REL` | Release criteria and launch-blocking bug classes | [14 Roadmap](14-roadmap.md) |
| `PLAT` | Platform/input/resolution/performance certification | [14 Roadmap](14-roadmap.md) |
| `LAUNCH` | Launch checklist and post-launch monitoring | [14 Roadmap](14-roadmap.md) |

## Ticket schema

A backlog item is a markdown file under [`docs/wiki/tickets/`](tickets/) with YAML frontmatter
and a fixed set of body sections. The schema below is the contract; existing tickets such as
[P0-SPEC-001](tickets/p0-spec-001-consolidate-the-product-brief-architecture-overview-and-glos.md)
are reference instances.

### Frontmatter fields

| Field | Required | Purpose |
|-------|:--------:|---------|
| `id` | ✅ | The canonical `P<phase>-<AREA>-<NNN>` ID — the join key for the whole trace. |
| `type` | ✅ | OKF document type (`Feature Spec` for tickets). Required by the project OKF lint profile. |
| `title` | ✅ | `"[<ID>] <Task sentence>"` — the same sentence used in the issue title. |
| `description` | ✅ | One-sentence summary (OKF profile field). |
| `resource` | ✅ | Bundle-relative path to this file (OKF profile field). |
| `tags` | ✅ | OKF tag list (include `ticket`, the phase, and the area). |
| `timestamp` | ✅ | OKF timestamp. |
| `okf_version` | ✅ | OKF schema version (`0.1`). |
| `status` | ✅ | Lifecycle state: `open` → `claimed` → `in_progress` → (`blocked`) → `done`. |
| `phase` | ✅ | Human-readable phase label (e.g. `"Phase P0 — …"`). |
| `github_issue` | ✅ | URL of the linked tracking issue. |
| `github_issue_status` | ✅ | `created` \| `closed` — mirror of the issue state. |
| `spec_references` | ✅ | List of spec/design pages this item is traceable to (≥1). |
| `github_project` | optional | Project board URL, when used. |

> The OKF fields (`type`, `description`, `resource`, `tags`, `timestamp`, `okf_version`) are
> enforced by `python scripts/okf_lint.py --profile project`. New tickets should carry them from
> creation so they pass the changed-docs gate without a follow-up. (Several pre-baseline tickets
> predate this requirement and still lint with `missing_*` findings; backfilling them is tracked
> separately and is **not** part of creating a new ticket.)

### Status lifecycle

| Status | Set when | By |
|--------|----------|----|
| `open` | The ticket and issue exist and are ready to pick. | author |
| `claimed` | The issue is assigned via [`/task-claim <N>`](../../.claude/commands/task-claim.md). | implementer |
| `in_progress` | The first commit for the task is pushed. | implementer |
| `blocked` | Work cannot proceed; a blocker comment is on the issue with an unblock condition. | implementer |
| `done` | The PR (`Closes #N`) is merged. | implementer / on merge |

### Required body sections

In order: **Open knowledge summary**, **GitHub project linkage**, **User story**, **Requirements**
(Functional + Non-functional), **Acceptance criteria**, **Detailed technical specifications**
(Scope, Inputs and dependencies, Implementation notes, Verification plan), **Documentation
impact**, **Exit evidence checklist**, and — once delivered — **Exit evidence**.

The **cross-phase task template** in
[Spec-Driven Development Tasks](spec-driven-development-tasks.md#cross-phase-task-template)
defines the per-row summary fields (ID, Spec reference, Outcome, Acceptance criteria,
Implementation notes, Verification, Docs impact, Exit evidence); the ticket body is the long-form
expansion of that row.

## Sample tasks — first prototype sprint (P1)

The first prototype sprint is **Phase P1 — Prototype vertical slice**. Its tickets are the worked
examples that validate this schema end-to-end: each one instantiates the ID format, carries the
required frontmatter, links a spec and an issue, and closes with Exit evidence. Reading down the
table demonstrates the trace `spec → ticket → issue → verification` for a complete sprint.

| ID | Spec reference | Issue | Acceptance criterion (abbrev.) | Verification |
|----|----------------|:-----:|--------------------------------|--------------|
| `P1-WORLD-001` | [02](02-data-models.md) / [03](03-chunking.md) | [#30](https://github.com/synthet/project-twelve/issues/30) | Coordinate conversions documented incl. negatives | Edit-mode boundary tests |
| `P1-WORLD-002` | [03 Chunking](03-chunking.md) | [#31](https://github.com/synthet/project-twelve/issues/31) | Chunks load/dirty/unload independently | Play-mode traversal + dirty-flag tests |
| `P1-RENDER-001` | [04 Rendering](04-rendering.md) | [#29](https://github.com/synthet/project-twelve/issues/29) | Rebuilds bounded to dirty visible chunks | Render rebuild tests + profiler sample |
| `P1-COLL-001` | [05 Collision](05-collision-physics.md) | [#25](https://github.com/synthet/project-twelve/issues/25) | Stand/jump/collide without tunneling | Movement checklist + edge-case tests |
| `P1-EDIT-001` | [02](02-data-models.md) / [03](03-chunking.md) | [#26](https://github.com/synthet/project-twelve/issues/26) | `SetTile` updates data/flags/render/collision/borders | Central + border edit unit tests |
| `P1-GEN-001` | [07 Generation](07-procedural-generation.md) | [#27](https://github.com/synthet/project-twelve/issues/27) | Same seed+coord ⇒ identical tiles | Golden-seed determinism tests |
| `P1-QA-001` | [13 Tooling/QA](13-tooling-testing.md) | [#28](https://github.com/synthet/project-twelve/issues/28) | One scene exercises the full slice | Manual QA checklist + capture |

These IDs all share `P1`, use distinct area codes, and number from `001` within each area — a
direct application of the [ID format](#task-id-format). New first-sprint work follows the same
pattern: the next rendering task is `P1-RENDER-002`, the next world task `P1-WORLD-003`, and so on.

## How to use and maintain this page

- **Filing a new ticket?** Assign the next free `P<phase>-<AREA>-<NNN>`, copy the
  [frontmatter fields](#frontmatter-fields) and [body sections](#required-body-sections) from an
  existing ticket, link ≥1 `spec_references`, and create the issue with a backlink. The
  [`backlog-queue`](../../.claude/skills/backlog-queue/SKILL.md) skill automates the process steps.
- **Need a new area code?** Add a row to the [area registry](#area-code-registry) in the same
  change that creates the first ticket using it, and pick the owning spec from the
  [Spec Ownership Registry](spec-ownership.md).
- **Renaming/re-scoping?** Never change an existing `id`. Trim scope inside the ticket and spin
  follow-up scope into a new ID instead.
- **Tooling:** `python scripts/okf_lint.py --profile project` enforces the frontmatter schema;
  `python3 scripts/check_markdown_links.py` enforces the cross-links that make the trace navigable.

## See also

- [Spec-Driven Development Tasks](spec-driven-development-tasks.md) — the phase backlog this
  schema shapes, including the cross-phase task template.
- [Backlog Workflow](../project/00-backlog-workflow.md) — the five-step pick/claim/PR process.
- [Spec Ownership Registry](spec-ownership.md) — who owns each area code's contract.
- [Baseline Scope Statement](baseline-scope.md) — the P0 scope these tasks deliver against.
- [`backlog-queue` skill](../../.claude/skills/backlog-queue/SKILL.md) — the agent-facing
  automation of this contract.
