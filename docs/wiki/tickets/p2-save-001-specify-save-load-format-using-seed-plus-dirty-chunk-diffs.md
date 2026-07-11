---
type: Task
id: P2-SAVE-001
title: "[P2-SAVE-001] Specify save/load format using seed plus dirty chunk diffs."
description: Versioned save format — header with seed/settings/palette, dirty-chunk diffs, atomic writes, corruption fail-safe, and migration hooks.
status: in_progress
phase: "Phase P2 — Core systems alpha"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/38"
github_issue_status: created
resource: wiki/tickets/p2-save-001-specify-save-load-format-using-seed-plus-dirty-chunk-diffs.md
tags: [docs, wiki, ticket, save, persistence, p2]
timestamp: 2026-07-01T00:00:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/spec-driven-development-tasks.md"
  - "docs/wiki/11-saving-loading.md"
  - "docs/wiki/generation-and-saving.md"
  - "docs/wiki/12-modding.md"
---

# [P2-SAVE-001] Specify save/load format using seed plus dirty chunk diffs.

## Open knowledge summary

This ticket hardens the prototype save path (`Assets/Scripts/Sandbox/SandboxSaveData.cs`:
`version=1`, seed, player position, per-chunk tile-edit lists) into the production contract from
`docs/wiki/11-saving-loading.md`: clean chunks regenerate from the seed; **edited chunks persist
as diffs**; every save opens with a version header that drives forward migration; writes are
atomic with a rolling backup; corrupted saves fail safely instead of destroying worlds. It also
reserves the registry **palette** field (string ID → runtime index, P2-DATA-001) so tiles survive
content reordering. Presentation-only visual override metadata is not part of the production
simulation save contract: it is persisted exclusively in a sidecar file next to the normal world
save, and missing sidecars load as an empty override map.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** [synthet/project-twelve#38](https://github.com/synthet/project-twelve/issues/38)
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a persistence developer on the P2 milestone, I want the save format specified with explicit
versioning, diff semantics, and failure behavior so that player worlds survive crashes, game
updates, and content changes — the three ways sandbox games classically destroy saves.

## Requirements

### Functional requirements

1. Save contents (per `11-saving-loading.md`): header — **format version** (first bytes read),
   world seed, generation settings (P2-GEN-001 settings object), registry palette (P2-DATA-001);
   body — dirty-chunk records, player state (position, inventory per P2-INV-001), global flags
   (time-of-day placeholder).
2. Diff semantics: a chunk is persisted only once it diverges from generation
   (`IsDirtyForSave`); clean chunks are absent from the file and regenerate from the seed.
   Per-chunk storage is the edit-list form (current `SandboxChunkSaveData`) with a documented
   threshold to switch a chunk to full-array form when edits exceed ~25% of `Size × Size` cells.
3. Derived state: `light` is never persisted (recomputed on load, P2-LIGHT-001); `fluid` amounts
   **are** persisted for dirty chunks (P2-FLUID-001).
4. Encoding: versioned binary with GZip-compressed chunk payloads is the production target; the
   current JSON form may remain as a debug export. The format is decoupled from any networking
   wire format (P3-NET-002 reuses concepts, not bytes).
5. Write safety: save writes go to a temp file, fsync, then atomic rename; the previous save is
   kept as a rolling `.bak`. Autosave cadence and save-on-quit are specified.
6. Load safety: version newer than supported → refuse with a clear message (no partial load);
   corrupted file (truncation, bad checksum/parse) → fall back to `.bak` and surface a
   user-facing warning; never silently regenerate over an existing world.
7. Migration: loads of version N < current run forward-migration steps N→N+1→…; migrating the
   current prototype `version=1` JSON save is the first migration (or an explicit documented
   break while pre-alpha — decision recorded in the spec, default: migrate).
8. Visual override persistence: `SandboxSaveData` remains unchanged unless a separate production
   save migration is explicitly approved. Presentation metadata uses a separate serializable
   sidecar model such as `SandboxVisualOverrideSaveData`, written beside the normal save by
   deriving names like `sandbox-world.visual-overrides.json` from `sandbox-world.json`.

### Non-functional requirements

1. Save/load of a typical session (≤ a few hundred dirty chunks) causes no visible frame stall;
   writes may spread across frames.
2. Chunk payloads compress via GZip; target file sizes documented for representative worlds.
3. Save code is pure C# over data objects (EditMode-testable round-trips without scenes or disk
   where possible; disk-path tests use temp files).
4. Machine-independent: no absolute paths; saves live under `Application.persistentDataPath`.

## Acceptance criteria

- Clean chunks derive from seed; edited chunks persist and reload exactly (round-trip equality on
  tile arrays including `fluid`, excluding `light`).
- EditMode round-trip test: random edit sequence → save → load → world equality at all edited and
  spot-checked clean positions.
- EditMode migration test: a stored version-1 fixture save loads through the migration path with
  data intact.
- EditMode corruption tests: truncated file and garbage bytes both fall back to `.bak` without
  exception escape; unsupported future version refuses cleanly.
- EditMode palette test: reordered registry + palette still resolves tiles to the same string IDs
  (shared with P2-DATA-001).
- Atomicity check: kill/interrupt during save (simulated) leaves either the old or the new save
  valid — never a corrupt primary with no backup.
- The GitHub issue and this markdown ticket link to each other.
- Exit evidence records the commit, verification commands, and reviewer findings.

## Detailed technical specifications

### Scope

- Format specification, versioned binary encode/decode, atomic write + backup, corruption
  handling, migration scaffold with the v1 migration, autosave hooks.
- Out of scope: region-file packing and per-chunk streaming files (recorded as a scaling option),
  cloud sync, server-side authoritative saves (P3-NET-004 consumes this format).

### Inputs and dependencies

- `Assets/Scripts/Sandbox/SandboxSaveData.cs` — current v1 data shapes (baseline + migration source).
- `Assets/Scripts/Sandbox/SandboxWorld.cs` — save/load orchestration and `IsDirtyForSave` flags.
- `Assets/Scripts/Sandbox/Persistence/SandboxVisualOverrideSaveData.cs` — sidecar-only visual
  override metadata; loaded after `SandboxWorld.LoadFromPath()` succeeds and never allowed to
  mutate tile IDs, palette remapping, chunk edits, or player position.
- P2-DATA-001 (palette), P2-GEN-001 (settings object), P2-INV-001 (inventory state),
  P2-FLUID-001 (fluid persistence) — coordinate field-level contracts with each.

### Format sketch (normative layout in the spec page)

```text
[header]  magic, formatVersion, worldSeed, genSettings, palette{stringId → runtimeIndex}
[player]  position, inventory slots
[globals] timeOfDay, flags
[chunks]  count, then per dirty chunk:
          coord, encoding(editList | fullArray), gzip payload, checksum

[sidecar: <save-name>.visual-overrides.<ext>]
          version, presentation override map only; optional for backward compatibility
```

### Verification plan

- EditMode: round-trip, migration, corruption, palette, sidecar path/missing-sidecar compatibility,
  and diff-threshold tests over temp files.
- Manual: autosave + quit/reload session; simulated interrupt (debug hook) proving atomicity.
- `Unity -batchmode … -testPlatform EditMode …` per `docs/wiki/quality-gates.md`.

## Documentation impact

- `docs/wiki/generation-and-saving.md` — save format layout, thresholds, failure behavior.
- `docs/wiki/11-saving-loading.md` — mark decisions adopted (hybrid diff form, GZip, atomic writes).
- P2-DATA-001 / P2-FLUID-001 / P2-INV-001 tickets — field-contract cross-references.
- Update `docs/wiki/spec-driven-development-tasks.md` if task scope or sequencing changes.

## Exit evidence checklist

- [ ] GitHub issue URL is recorded in this ticket.
- [ ] GitHub issue links back to this markdown ticket.
- [ ] Format layout documented in `generation-and-saving.md` before implementation.
- [ ] Sidecar visual override contract documented as the only required persistence path for
      presentation metadata; missing sidecars verified as empty override maps.
- [ ] Round-trip, migration, corruption, and palette EditMode tests pass.
- [ ] Atomic-write interruption evidence attached.
- [ ] Follow-up tasks created for region files, streaming writes, and cloud sync.
