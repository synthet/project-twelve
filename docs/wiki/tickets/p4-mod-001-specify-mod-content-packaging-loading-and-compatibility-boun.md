---
type: Task
id: P4-MOD-001
title: "[P4-MOD-001] Specify mod content packaging, loading, and compatibility boundaries."
description: Mod package format, deterministic load order, supported extension surface, and the save/network compatibility rules mods must not break.
status: open
phase: "Phase P4 — Feature complete and beta"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/45"
github_issue_status: created
resource: wiki/tickets/p4-mod-001-specify-mod-content-packaging-loading-and-compatibility-boun.md
tags: [docs, wiki, ticket, modding, compatibility, p4]
timestamp: 2026-07-01T00:00:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/spec-driven-development-tasks.md"
  - "docs/wiki/12-modding.md"
  - "docs/wiki/multiplayer-and-modding.md"
  - "docs/wiki/tickets/p2-data-001-specify-tile-item-and-entity-registry-contracts.md"
---

# [P4-MOD-001] Specify mod content packaging, loading, and compatibility boundaries.

## Open knowledge summary

This ticket turns the registry foundation (P2-DATA-001) into a real mod surface per
`docs/wiki/12-modding.md`: a mod package format (manifest + JSON definitions + assets), a
deterministic load order with dependency declarations, an explicit list of what mods **can**
extend (data-only in this milestone: tiles, items, recipes, loot tables), and the compatibility
boundaries mods must not break — saves referencing missing mods, network sessions with mismatched
mod sets, and registry collisions. Arbitrary mod **code** is explicitly out of scope; the page's
security warning about untrusted code becomes a hard boundary.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** [synthet/project-twelve#45](https://github.com/synthet/project-twelve/issues/45)
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a systems developer in P4, I want mod packaging and compatibility rules specified so that
data mods can add content without ever corrupting a save or desyncing a multiplayer session —
the two failure modes that make modding support a liability instead of a feature.

## Requirements

### Functional requirements

1. **Package format:** a mod is a folder/archive with `mod.json` (id in `namespace` form, name,
   version, dependencies with version ranges, supported game version range) plus definition files
   (JSON per registry kind) and referenced assets (sprites via Addressables/AssetBundle or loose
   files — decision recorded here per `12-modding.md`).
2. **Load order** (per `12-modding.md`): core → mods in dependency-resolved, then
   deterministic-tiebreak (id-sorted) order → registries freeze → runtime indices assigned →
   atlases built. Override rule documented (last-loader-wins or explicit priority — decision
   recorded).
3. **Extension surface (this milestone):** new tiles, items, recipes, loot-table entries, and
   overrides of existing definitions' data fields. No mod C# code loading; behavior hooks beyond
   data are recorded follow-ups.
4. **Validation:** after load, the full reference graph must resolve (recipes reference existing
   items, tiles reference existing sprites); failures list every offender and abort to a clear
   error state — no partial content.
5. **Save compatibility:** saves record the active mod set (ids + versions) alongside the
   P2-SAVE-001 palette. Loading with a missing mod fails safely with a message naming the mod;
   tiles from removed mods have a documented policy (refuse load in this milestone — placeholder
   substitution is a recorded follow-up).
6. **Network compatibility:** the P3-NET-002 handshake compares mod sets + versions; mismatches
   refuse the connection with a clear message. Palette exchange already covers index agreement.

### Non-functional requirements

1. Mod discovery/load happens once at startup; load time scales linearly and is reported per mod.
2. A malformed mod (bad JSON, missing manifest, cyclic dependency) can never crash the game past
   the error screen or corrupt registries (fail-safe before freeze).
3. The mod format is documented well enough for an external author (feeds P5-DOC-001).

## Acceptance criteria

- Mods can add supported data without breaking save or network contracts (the boundary tests
  below all pass).
- EditMode fixture tests: a sample data mod adds a tile + item + recipe; content resolves and
  runtime indices are deterministic across two loads.
- EditMode: dependency cycle, missing dependency, duplicate ID collision, and unresolved
  reference each fail with the documented error, leaving registries untouched.
- EditMode: save created with mod X refuses to load without X, with a message naming X; loading
  with X present round-trips exactly (palette + mod-set check).
- Simulation test: P3-NET-002 handshake refuses mismatched mod sets.
- Compatibility matrix documented: {mod added, mod removed, mod updated} × {existing save, new
  save, multiplayer join} with the specified behavior per cell.
- The GitHub issue and this markdown ticket link to each other.
- Exit evidence records the commit, verification commands, and reviewer findings.

## Detailed technical specifications

### Scope

- Manifest schema, load-order resolution, JSON definition loading for the existing registry
  kinds, validation, save/network compatibility checks, and fixture mods for tests.
- Out of scope: mod code execution/scripting, workshop/distribution tooling, in-game mod browser
  UI, placeholder substitution for removed mods, mod-added biomes/structures (needs P2-GEN-001
  extension points — recorded follow-up).

### Inputs and dependencies

- P2-DATA-001 — registry freeze lifecycle and palette (blocking).
- P2-SAVE-001 — save header extension for the mod set.
- P3-NET-002 — handshake extension.
- `docs/wiki/12-modding.md` — format guidance, load order, pitfalls.

### Verification plan

- EditMode: fixture-mod load tests, failure-mode tests, save-refusal round-trips (fixture mods
  live in test assets, no licensed content).
- Simulation: handshake mismatch test.
- Doc review: an author-facing packaging guide draft exists (feeds P5-DOC-001).

## Documentation impact

- `docs/wiki/multiplayer-and-modding.md` — package format, load order, compatibility matrix.
- `docs/wiki/12-modding.md` — mark decisions adopted (definition format, override rule).
- P2-SAVE-001 / P3-NET-002 tickets — header/handshake cross-references.
- Update `docs/wiki/spec-driven-development-tasks.md` if task scope or sequencing changes.

## Exit evidence checklist

- [ ] GitHub issue URL is recorded in this ticket.
- [ ] GitHub issue links back to this markdown ticket.
- [ ] Package format and compatibility matrix documented before implementation.
- [ ] Fixture-mod load, failure-mode, and save/network boundary tests pass.
- [ ] Author-facing packaging guide drafted.
- [ ] Follow-up tasks created for scripting hooks, placeholder substitution, and distribution.
