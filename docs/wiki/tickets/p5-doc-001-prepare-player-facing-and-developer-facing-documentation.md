---
type: Task
id: P5-DOC-001
title: "[P5-DOC-001] Prepare player-facing and developer-facing documentation."
description: Player guide (setup, controls, troubleshooting, known issues) and developer/modder docs, verified by a documentation QA pass.
status: open
phase: "Phase P5 — Release candidate and launch"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/49"
github_issue_status: created
resource: wiki/tickets/p5-doc-001-prepare-player-facing-and-developer-facing-documentation.md
tags: [docs, wiki, ticket, documentation, p5]
timestamp: 2026-07-01T00:00:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/spec-driven-development-tasks.md"
  - "docs/wiki/README.md"
  - "docs/CANONICAL_SOURCES.md"
---

# [P5-DOC-001] Prepare player-facing and developer-facing documentation.

## Open knowledge summary

This ticket produces launch documentation for two audiences. **Players:** getting started
(install, system requirements from P5-PLAT-001, first world), controls reference (final
P4-UX-001 bindings), gameplay basics (the P4-CONTENT-001 loop), multiplayer setup (host/join per
P3-NET-003), troubleshooting (save messages from P5-MIG-001, common failures), and known issues
(S2/S3 list per P5-REL-001). **Developers/modders:** contributor onboarding built on the existing
wiki (`docs/CANONICAL_SOURCES.md` as the map), plus the mod-author packaging guide drafted in
P4-MOD-001, promoted to a complete external-facing document. A documentation QA pass —
following each doc literally on a clean setup — is the verification gate.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** [synthet/project-twelve#49](https://github.com/synthet/project-twelve/issues/49)
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a new player or mod author at launch, I want documentation that answers setup, controls,
multiplayer, and troubleshooting questions without asking the developer, so that the first-hour
experience and the first mod don't require support contact.

## Requirements

### Functional requirements

1. **Player documentation set** (format decided here: in-repo markdown as source of truth,
   optionally exported for the distribution channel): getting started, controls reference,
   gameplay basics, multiplayer setup (including firewall/port notes for LAN), troubleshooting
   (every P5-MIG-001 user-facing message appears with its remedy), known issues.
2. **Developer documentation:** contributor onboarding page verifying the path README →
   CANONICAL_SOURCES → wiki still holds for the launch codebase; stale wiki pages found during
   the pass are fixed or ticketed.
3. **Modder documentation:** the P4-MOD-001 packaging guide completed to external-author
   standard: manifest schema reference, worked example mod, validation-error catalog,
   compatibility rules (save/network), and support boundaries.
4. **Accuracy binding:** controls tables generate from or are checked against the shipped
   bindings; system requirements copy from P5-PLAT-001 certification output; known issues copy
   from the P5-REL-001 ship-with list — no hand-maintained duplicates without a named source.
5. All new docs carry OKF frontmatter and pass the repo link/lint gates.

### Non-functional requirements

1. Player docs assume no technical background; developer docs assume Unity familiarity only.
2. Every player doc is skimmable: numbered steps, one task per section, screenshots where a step
   is visual.
3. Docs live under `docs/` and are covered by `check_markdown_links.py` and OKF lint (CI-guarded
   like all other docs).

## Acceptance criteria

- Setup, troubleshooting, controls, modding, and known issues are documented for launch.
- Documentation QA pass completed: a tester follows getting-started + multiplayer setup literally
  on a clean machine and succeeds without outside help; the worked example mod builds and loads
  by following only the modding guide.
- Every P5-MIG-001 failure message has a troubleshooting entry; every S2/S3 known issue from the
  P5-REL-001 dry-run/RC list appears in known issues.
- Controls reference matches shipped bindings (spot-audit).
- All docs pass link + OKF gates.
- The GitHub issue and this markdown ticket link to each other.
- Exit evidence records the QA-pass notes and reviewer findings.

## Detailed technical specifications

### Scope

- Authoring the two documentation sets, the accuracy bindings, and the documentation QA pass.
- Out of scope: marketing/storefront copy (P5-LAUNCH-001), localized documentation, video
  tutorials, API reference generation for engine code.

### Inputs and dependencies

- P4-UX-001 (bindings/screens), P4-CONTENT-001 (loop), P3-NET-003 (multiplayer setup),
  P5-MIG-001 (messages), P5-PLAT-001 (system requirements), P5-REL-001 (known-issues list),
  P4-MOD-001 (guide draft) — content sources; most must be final before this ticket closes.
- Existing wiki + `docs/CANONICAL_SOURCES.md` — developer-doc backbone.

### Verification plan

- Clean-machine documentation QA pass (player path) with notes.
- Example-mod build-and-load following only the guide.
- `python3 scripts/check_markdown_links.py` + OKF lint on all new pages.

## Documentation impact

- New pages under `docs/` (player set, e.g. `docs/player/`; modding guide location coordinated
  with P4-MOD-001), all with OKF frontmatter.
- `docs/wiki/README.md` / `docs/CANONICAL_SOURCES.md` — index the new sets.
- Update `docs/wiki/spec-driven-development-tasks.md` if task scope or sequencing changes.

## Exit evidence checklist

- [ ] GitHub issue URL is recorded in this ticket.
- [ ] GitHub issue links back to this markdown ticket.
- [ ] Player, developer, and modder doc sets merged with OKF frontmatter.
- [ ] Clean-machine QA pass and example-mod pass recorded.
- [ ] Accuracy spot-audits (controls, requirements, known issues) recorded.
- [ ] Follow-up tasks filed for localization and post-launch doc updates.
