---
type: Task
id: P2-DATA-002
title: "[P2-DATA-002] Migrate sandbox callers to registry runtime indices and complete item/entity registries."
description: Swap SandboxTile.id from legacy constants to registry runtime indices, persist the save palette, and complete item/entity definitions and mod load order.
status: claimed
phase: "Phase P2 — Core systems alpha"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/86"
github_issue_status: created
resource: wiki/tickets/p2-data-002-migrate-sandbox-callers-to-registry-runtime-indices.md
tags: [docs, wiki, ticket, registry, modding, p2]
timestamp: 2026-07-03T00:00:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/12-modding.md"
  - "docs/wiki/11-saving-loading.md"
  - "docs/wiki/02-data-models.md"
  - "docs/wiki/tickets/p2-data-001-specify-tile-item-and-entity-registry-contracts.md"
---

# [P2-DATA-002] Migrate sandbox callers to registry runtime indices and complete item/entity registries.

## Open knowledge summary

Follow-up to [[P2-DATA-001]](p2-data-001-specify-tile-item-and-entity-registry-contracts.md). The
registry contract, `ContentRegistry<TDef>`, core definitions, `RegistryPalette`, and EditMode
validation tests are in place, with `SandboxTileIds` deliberately left as a compatibility shim so
that change set stayed behavior-preserving. This ticket performs the behavioral swap: sandbox
callers bind to registry runtime indices and `TileDefinition` properties, saves migrate through the
legacy table and persist the palette, and the item/entity registries and mod load order are
completed.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** [synthet/project-twelve#86](https://github.com/synthet/project-twelve/issues/86)
- **Backlink requirement:** Satisfied — the issue body links to this markdown ticket.

## User story

As a systems developer on the P2 milestone, I want the sandbox runtime to consume registry
definitions instead of legacy constants so that content changes, saves, and future mods flow
through one data-driven identity system.

## Requirements

### Functional requirements

1. Replace `SandboxTileIds` consumption in `SandboxWorld`, `SandboxTerrainGenerator`,
   `SandboxChunkRenderer`, `SandboxPlayerController`, `SandboxTileVisualCatalog` (including its
   tile-ID `switch`), and `GameplayMcpTools` with registry runtime indices and `TileDefinition`
   lookups (`Solid`, `AtlasSprite`).
2. Load version-1 saves by mapping legacy numeric IDs through
   `SandboxCoreContent.LegacyTileIdToStringId`; write the `RegistryPalette` into new saves and
   resolve it on load (header field coordinated with P2-SAVE-001).
3. Delete `SandboxTileIds` once no caller references it, or keep it only as `static readonly`
   values resolved from the registry if editor tooling still needs names.
4. Complete `ItemDefinition`/`EntityDefinition` fields as P2-INV-001 and P2-AI-001 firm up their
   contracts; keep definitions pure data.
5. Specify mod load order and override/priority rules in `docs/wiki/12-modding.md` (pre-work for
   P4-MOD-001).

### Non-functional requirements

1. Hot paths remain array-indexed by runtime int — no per-tile string lookups per frame.
2. The pinned-seed prototype scene renders identically before and after the swap.
3. Minimal diffs per subsystem; the swap may land as a short stack of reviewable changes.

## Acceptance criteria

- No runtime code reads `SandboxTileIds` constants for content decisions.
- A version-1 save loads correctly after the swap; a new save survives a registry reordering
  (palette round-trip in-engine, not just registry-level).
- EditMode tests cover the save migration path; existing sandbox tests still pass.
- Pinned-seed regression spot-check recorded per the P1 vertical-slice runbook positions.
- The GitHub issue and this markdown ticket link to each other.

## Exit evidence checklist

- [ ] GitHub issue URL is recorded in this ticket.
- [ ] GitHub issue links back to this markdown ticket.
- [ ] Caller migration landed with EditMode coverage.
- [ ] Save palette persisted and legacy migration tested.
- [ ] Mod load order specified in `docs/wiki/12-modding.md`.
- [ ] Pinned-seed regression check recorded.
