---
type: Task
id: P2-VISUAL-001
title: "[P2-VISUAL-001] Specify visual catalog import pipeline contract."
description: Define the visual catalog import pipeline for autotile, character, and monster catalogs via submodule and scripts.
status: open
phase: "Phase P2 — Core systems alpha"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/76"
github_issue_status: created
resource: wiki/tickets/p2-visual-001-specify-visual-catalog-import-pipeline-contract.md
tags: [docs, wiki, ticket, visual, assets, p2]
timestamp: 2026-06-30T12:00:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/15-assets-integration.md"
  - "docs/VISUAL_SETUP.md"
  - "docs/PAID_ASSETS.md"
  - "docs/wiki/visual-integration.md"
---

# [P2-VISUAL-001] Specify visual catalog import pipeline contract.

## Open knowledge summary

This ticket defines the single specification for visual catalog generation: submodule layout, `LocalImportConfig`, Unity editor import menus, and `scripts/generate_visual_catalogs.py`. It closes the gap referenced in `15-assets-integration.md` as the "first asset-import ticket."

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** Pending creation via `scripts/sync_wiki_tickets_to_github.py`.
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a developer working on the P2 milestone, I want a documented visual catalog import pipeline so that contributors can regenerate autotile, character layer, and monster catalogs reproducibly without breaking paid-asset policy.

## Requirements

### Functional requirements

1. Document inputs and outputs for autotile, character layer, and monster catalog generation.
2. Define `LocalImportConfig` resolution order (submodule path vs local override).
3. Document failure modes for code-only checkout (warnings vs hard fail per subsystem).
4. Document CI/contributor workflow: `check_paid_assets.py` + catalog regen steps.

### Non-functional requirements

1. No licensed art blobs in the public repo; catalogs live in submodule or generated locally.
2. Python regen path (`generate_visual_catalogs.py`) and Unity menu paths produce equivalent catalog content.

## Acceptance criteria

- A single wiki or doc section describes the full catalog pipeline for all three catalog types.
- Code-only checkout behavior is explicit for each visual subsystem.
- Regeneration workflow is reproducible on a machine with `Assets/_Licensed` initialized.
- The GitHub issue and this markdown ticket link to each other.

## Detailed technical specifications

### Scope

- Specification and documentation only; implementation fixes to importers are in scope only if required to match the spec.
- Cross-reference `docs/PAID_ASSETS.md` for commit guards.

### Inputs and dependencies

- `scripts/generate_visual_catalogs.py`
- `Assets/Scripts/Editor/Visual/` importers
- `Assets/Scripts/Integration/LocalImportConfig.cs`
- `Assets/_Licensed/config/visual-import.txt`

### Verification plan

- Regenerate catalogs on a machine with submodule; confirm expected asset metadata updates only.
- Code-only clone logs warnings and falls back to vertex-color terrain per `VISUAL_SETUP.md`.

## Documentation impact

- `docs/VISUAL_SETUP.md` — pipeline cross-links.
- `docs/wiki/15-assets-integration.md` — link to P2-VISUAL-001 as the asset-import ticket.
- `docs/wiki/visual-integration.md` — catalog pipeline summary.
- Future `PropCatalog` importer ([P2-VISUAL-005](p2-visual-005-specify-tile-engine-props-and-parallax-backgrounds.md)) should follow the same pipeline contract defined here.

## Exit evidence checklist

- [ ] GitHub issue URL is recorded in this ticket.
- [ ] GitHub issue links back to this markdown ticket.
- [ ] Catalog regen verified on submodule-enabled machine.
- [ ] Paid-asset guard documented in pipeline section.
