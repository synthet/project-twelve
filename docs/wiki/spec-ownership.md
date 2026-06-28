---
type: Source-of-Truth Map
title: Spec Ownership Registry
description: The single P0 registry that assigns every subsystem page an owner (DRI role), spec status, upstream dependencies, and review cadence so spec-driven work has clear accountability.
resource: wiki/spec-ownership.md
tags: [wiki, ownership, spec, p0, governance]
timestamp: 2026-06-28T00:00:00Z
okf_version: 0.1
---

# Spec Ownership Registry

> **Status:** Baseline (P0). This page is the single agreed map of *who owns which spec*.
> **Owns:** The owner, spec status, dependencies, and review cadence of every subsystem page.
> **Gated by:** [P0-SPEC-002](tickets/p0-spec-002-define-spec-ownership-for-every-subsystem-page.md).

This page exists so that every subsystem spec has a **single accountable owner**, a **known
maturity**, an explicit set of **upstream dependencies**, and a **review cadence** — instead of
that knowledge living only in commit history or a maintainer's head. The GitHub issue, an
implementation branch, a reviewer, and a tool can all trace a subsystem back to one row here.

## Ownership model

ProjectTwelve is currently a **single-maintainer** project, so every spec has the same
accountable owner ([@synthet](https://github.com/synthet)). To keep the registry useful as the
team grows, ownership is recorded as a **DRI role** (Directly Responsible Individual) per
subsystem rather than only a person: the role is the durable unit of ownership, and the person
column is whoever currently fills it. Re-assigning a subsystem later means changing the person,
not restructuring the table.

Each registry row answers four questions:

| Field | Question it answers |
|-------|---------------------|
| **Owner (DRI role)** | Who is accountable for keeping this spec correct and for approving changes to it? |
| **Spec status** | How mature is the page's *contract* (independent of whether prototype code exists)? |
| **Depends on** | Which other specs must be read first / must agree, because this page builds on their contracts? |
| **Review cadence** | How often is this page deliberately re-read against the code and roadmap? |

### Spec status vocabulary

Status describes the maturity of the **page's documented contract**, not the maturity of the
runtime code (the prototype implements parts of several P1 pages whose specs are still `Draft`).

| Status | Meaning |
|--------|---------|
| **Baseline** | Reviewed and ratified as a P0 reference; changes require an explicit decision. |
| **Draft** | Content exists and is usable, but the contract is not yet ratified and may shift. |
| **Stub** | Placeholder or thin page that still needs its contract written out. |

### Review cadence vocabulary

| Cadence | When the owner re-reviews the page |
|---------|-----------------------------------|
| **Baseline gate** | At every phase boundary, because downstream work depends on it staying stable. |
| **Per sprint** | At the end of each prototype sprint, because the subsystem is under active P1 work. |
| **Phase gate** | When its owning roadmap phase opens, and at each later phase boundary. |
| **On dependency change** | Whenever an upstream spec it depends on changes its contract (in addition to the cadence above). |

## Registry — numbered subsystem reference (Set B)

These are the canonical [detailed subsystem pages](README.md#b-detailed-subsystem-reference-numbered).
"Depends on" lists the upstream specs whose contracts this page assumes; it is *in addition to*
the [cross-cutting invariants](README.md#cross-cutting-invariants), which every page inherits.

| Page | Owner (DRI role) | Owner | Spec status | Depends on | Review cadence |
|------|------------------|-------|-------------|------------|----------------|
| [00 Overview & Principles](00-overview.md) | Architecture Owner | @synthet | Baseline | [Baseline Scope](baseline-scope.md) | Baseline gate |
| [01 Architecture](01-architecture.md) | Architecture Owner | @synthet | Draft | 00, 02 | Baseline gate |
| [02 Data Models](02-data-models.md) | World & Data Owner | @synthet | Draft | 01 | Per sprint |
| [03 Chunking](03-chunking.md) | World & Data Owner | @synthet | Draft | 02 | Per sprint |
| [04 Rendering](04-rendering.md) | Rendering Owner | @synthet | Draft | 02, 03 | Per sprint |
| [05 Collision & Physics](05-collision-physics.md) | Physics Owner | @synthet | Draft | 02, 03 | Per sprint |
| [06 Lighting](06-lighting.md) | Lighting Owner | @synthet | Draft | 02, 03 | Phase gate (P2) |
| [07 Procedural Generation](07-procedural-generation.md) | Generation Owner | @synthet | Draft | 02, 03 | Per sprint |
| [08 Liquids](08-liquids.md) | Simulation Owner | @synthet | Draft | 02, 03 | Phase gate (P2) |
| [09 Pathfinding](09-pathfinding.md) | AI Owner | @synthet | Draft | 03, 05 | Phase gate (P2) |
| [10 Multiplayer](10-multiplayer.md) | Netcode Owner | @synthet | Draft | 02, 11 | Phase gate (P3) |
| [11 Saving & Loading](11-saving-loading.md) | Persistence Owner | @synthet | Draft | 02, 03, 07 | Phase gate (P2) |
| [12 Modding & Content](12-modding.md) | Content & Modding Owner | @synthet | Draft | 02 | Phase gate (P4) |
| [13 Tooling, Testing & Profiling](13-tooling-testing.md) | Tooling & QA Owner | @synthet | Draft | 03, 04, 05, 07 | Per sprint |
| [14 Roadmap](14-roadmap.md) | Product & Roadmap Owner | @synthet | Draft | all | Baseline gate |
| [15 Asset Integration Requirements](15-assets-integration.md) | Assets Owner | @synthet | Draft | 02, 04 | Phase gate (P4) |

## Registry — cross-cutting and prototype-aligned pages

The [prototype-aligned pages (Set A)](README.md#a-prototype-aligned-knowledge-pages) summarize one
or more numbered specs; they inherit the owner of the spec they summarize and are reviewed on the
same cadence. The cross-cutting P0 pages own project-wide scope and process.

| Page | Owner (DRI role) | Owner | Spec status | Mirrors / scope | Review cadence |
|------|------------------|-------|-------------|-----------------|----------------|
| [Baseline Scope Statement](baseline-scope.md) | Product & Roadmap Owner | @synthet | Baseline | Core genre, prototype target, non-goals | Baseline gate |
| [Spec-Driven Development Tasks](spec-driven-development-tasks.md) | Product & Roadmap Owner | @synthet | Baseline | Phase backlog and acceptance gates | Baseline gate |
| [Backlog Task Schema & ID Conventions](task-schema.md) | Product & Roadmap Owner | @synthet | Baseline | Task ID format, area codes, ticket schema, traceability | Baseline gate |
| [Glossary](glossary.md) | Architecture Owner | @synthet | Draft | Shared vocabulary index | Baseline gate |
| [Architecture Blueprint](architecture-blueprint.md) | Architecture Owner | @synthet | Draft | Visual blueprint translation | Phase gate (P2) |
| [Project Brief](project-brief.md) | Product & Roadmap Owner | @synthet | Draft | Vision narrative | Baseline gate |
| [Architecture Map](architecture-map.md) | Architecture Owner | @synthet | Draft | Mirrors 01 | Baseline gate |
| [World and Chunk Data](world-and-chunk-data.md) | World & Data Owner | @synthet | Draft | Mirrors 02, 03 | Per sprint |
| [Rendering and Collision](rendering-and-collision.md) | Rendering Owner | @synthet | Draft | Mirrors 04, 05 | Per sprint |
| [Simulation Systems](simulation-systems.md) | Simulation Owner | @synthet | Draft | Mirrors 06, 08 | Phase gate (P2) |
| [Generation and Saving](generation-and-saving.md) | Generation Owner | @synthet | Draft | Mirrors 07, 11 | Per sprint |
| [Gameplay Systems](gameplay-systems.md) | AI Owner | @synthet | Draft | Mirrors 09 | Per sprint |
| [Multiplayer and Modding](multiplayer-and-modding.md) | Netcode Owner | @synthet | Draft | Mirrors 10, 12 | Phase gate (P3) |
| [Roadmap and Tasks](roadmap-and-tasks.md) | Product & Roadmap Owner | @synthet | Draft | Mirrors 14 | Baseline gate |
| [Visual integration](visual-integration.md) | Assets Owner | @synthet | Draft | Sandbox tile/avatar mapping | Phase gate (P4) |

> The two [visual specs](README.md#c-visual-presentation) outside `docs/wiki/`
> ([Visual behavior spec](../VISUAL_BEHAVIOR_SPEC.md), [Visual setup](../VISUAL_SETUP.md)) are
> owned by the **Assets Owner** and are governed by [`docs/CANONICAL_SOURCES.md`](../CANONICAL_SOURCES.md);
> they are listed there rather than duplicated here.

## DRI role definitions

| DRI role | Spec area |
|----------|-----------|
| Product & Roadmap Owner | Scope, brief, roadmap, phase backlog, and acceptance gates. |
| Architecture Owner | System decomposition, data flow, glossary, and blueprint. |
| World & Data Owner | Tile/chunk/world data models, coordinates, and chunk lifecycle. |
| Rendering Owner | Chunk-local mesh/tilemap rebuilds. |
| Physics Owner | Tile collision and player movement. |
| Lighting Owner | Tile lightmap and propagation. |
| Generation Owner | Deterministic terrain, biome, cave, ore, and structure passes. |
| Simulation Owner | Liquids and other active-cell simulations. |
| AI Owner | Enemy spawn, pathfinding, and gameplay loops. |
| Netcode Owner | Server-authoritative sync, snapshots, and deltas. |
| Persistence Owner | Save/load format, diffs, versioning, and migrations. |
| Content & Modding Owner | Registries, data-driven content, and mod boundaries. |
| Tooling & QA Owner | Debug views, tests, profiling, and quality gates. |
| Assets Owner | Sprites, atlases, animation, and Unity asset seams. |

## How to use and maintain this registry

- **Picking up a spec task?** Find the page here first: the owner approves contract changes, and
  the "Depends on" column tells you which upstream specs must agree before you edit.
- **Changing a contract?** Update the dependent pages' rows' status to `Draft` if the change
  reopens their contracts, and review every page that lists the changed page under "Depends on"
  (cadence **On dependency change**).
- **Promoting a spec to `Baseline`?** Do it at a phase gate with the owner's sign-off, and record
  the decision in the page's own Status block.
- **Adding a new subsystem page?** Add a row here in the same change, with an owner, status,
  dependencies, and cadence — a page without a registry row is treated as unowned.

## See also

- [Open Knowledge Base index](README.md) — the page sets this registry covers.
- [Baseline Scope Statement](baseline-scope.md) — the P0 scope this ownership map complements.
- [Spec-Driven Development Tasks](spec-driven-development-tasks.md) — the backlog each owner draws
  from.
- [`docs/CANONICAL_SOURCES.md`](../CANONICAL_SOURCES.md) — authority map for non-wiki documents.
