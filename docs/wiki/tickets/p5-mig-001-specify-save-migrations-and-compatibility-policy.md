---
type: Task
id: P5-MIG-001
title: "[P5-MIG-001] Specify save migrations and compatibility policy."
description: Version-support window, migration chain over archived save fixtures, and fail-safe messaging for unsupported or damaged saves.
status: open
phase: "Phase P5 — Release candidate and launch"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/51"
github_issue_status: created
resource: wiki/tickets/p5-mig-001-specify-save-migrations-and-compatibility-policy.md
tags: [docs, wiki, ticket, save, migration, p5]
timestamp: 2026-07-01T00:00:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/spec-driven-development-tasks.md"
  - "docs/wiki/generation-and-saving.md"
  - "docs/wiki/11-saving-loading.md"
  - "docs/wiki/tickets/p2-save-001-specify-save-load-format-using-seed-plus-dirty-chunk-diffs.md"
---

# [P5-MIG-001] Specify save migrations and compatibility policy.

## Open knowledge summary

This ticket turns the migration *mechanism* from P2-SAVE-001 (version header, forward-migration
steps) into a launch *policy*: which save versions the shipped game supports, how migrations are
maintained and tested against an **archived fixture corpus**, what the player sees when a save
can't load (fail safely with a message — never silently regenerate or corrupt), and how
generation-settings changes interact with old seeds (a world generated under old settings must
keep its persisted chunks; clean-chunk regeneration differences are documented policy). Player
worlds are the most valuable data the game touches; this policy is what protects them across
updates.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** [synthet/project-twelve#51](https://github.com/synthet/project-twelve/issues/51)
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a player updating the game, I want my worlds to keep working — or to be told clearly why they
can't and what my options are — so that an update never costs me my progress; as the maintainer,
I want a fixture-backed migration suite so I can change the format without fear.

## Requirements

### Functional requirements

1. **Support window:** the shipped game loads saves from all public releases since 1.0 (policy
   default — decision recorded); older pre-release/beta formats have an explicit documented
   cutoff. Version N+1 always loads version N.
2. **Migration chain:** one forward migration step per format bump (N→N+1), composed for older
   saves; each step is pure (bytes in → bytes/objects out), individually tested, and kept until
   its source version leaves the support window.
3. **Fixture corpus:** every released format version contributes archived fixture saves
   (small/typical/edge: heavy edits, mods per P4-MOD-001, mid-progression inventory) stored in
   the repo/test assets; the migration suite loads every fixture through the full chain and
   round-trips it.
4. **Fail-safe behavior:** unsupported-version, failed-migration, and corrupted saves produce a
   user-facing message naming the problem and options (update the game, restore `.bak`, keep the
   file untouched); the original file is **never modified** by a failed load, and a successful
   migration writes a new file while preserving the pre-migration original (one-version rollback).
5. **Seed/settings compatibility:** generation-settings changes bump the settings version in the
   header; persisted (dirty) chunks always load; the policy for clean chunks under changed
   generation (regenerate-different, with terrain seams accepted and documented) is stated
   player-facing (feeds P5-DOC-001 known issues).
6. **Mod-set interaction:** missing-mod refusal follows P4-MOD-001; migration never drops
   mod-owned tiles silently.

### Non-functional requirements

1. Migration of a large fixture (documented size) completes within a bounded time with progress
   indication.
2. The suite runs in EditMode/CI as part of quality gates on any save-code change.
3. Adding a format bump has a documented developer procedure (checklist: write step, add
   fixtures, extend suite) — enforced by review.

## Acceptance criteria

- Saves from supported versions migrate or fail safely with user-facing messaging (all paths
  demonstrated in tests).
- Migration suite passes over the full archived fixture corpus (every version × every fixture
  class).
- Corruption/unsupported tests: truncated, garbage, future-version, and missing-mod fixtures each
  produce the specified message and leave the original file byte-identical.
- Rollback test: post-migration, the preserved original still loads in the previous game version
  (or the preservation guarantee is verified byte-level).
- The developer format-bump checklist is documented and validated by executing it once (real or
  rehearsed bump).
- The GitHub issue and this markdown ticket link to each other.
- Exit evidence records the suite output and reviewer findings.

## Detailed technical specifications

### Scope

- Policy document, fixture corpus + archiving procedure, migration test suite, fail-safe UX
  messages, and the format-bump developer checklist.
- Out of scope: cloud-save sync conflicts, cross-platform save transfer (P5-PLAT-001 notes if
  relevant), automatic save repair tools (recorded follow-up).

### Inputs and dependencies

- P2-SAVE-001 — version header, atomic writes, `.bak`, migration scaffold (blocking).
- P4-MOD-001 — mod-set header interaction.
- P4-UX-001 — message presentation surface.

### Verification plan

- EditMode migration suite over the fixture corpus (green required).
- Manual: trigger each failure message in a build and screenshot for evidence.
- Review: format-bump checklist walkthrough.

## Documentation impact

- `docs/wiki/generation-and-saving.md` — compatibility policy section (support window, clean-chunk
  regeneration policy, rollback guarantee).
- Player-facing messaging text feeds P5-DOC-001 (troubleshooting section).
- Update `docs/wiki/spec-driven-development-tasks.md` if task scope or sequencing changes.

## Exit evidence checklist

- [ ] GitHub issue URL is recorded in this ticket.
- [ ] GitHub issue links back to this markdown ticket.
- [ ] Compatibility policy merged into `generation-and-saving.md`.
- [ ] Fixture corpus archived with the documented procedure.
- [ ] Migration + failure-path suite green; message screenshots attached.
- [ ] Format-bump checklist validated.
