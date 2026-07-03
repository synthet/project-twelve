---
type: Specification
title: Save/Load Format Specification (P2-SAVE-001)
description: Versioned save format — seed+settings+palette header, dirty-chunk diffs, atomic writes with rolling backup, corruption fail-safe, and forward migration.
resource: wiki/save-load-format.md
tags: [docs, wiki, spec, save, persistence, p2]
timestamp: 2026-07-03T00:00:00Z
okf_version: 0.1
---

# Save/Load Format Specification (P2-SAVE-001)

> **Status:** Draft for review (spec phase of `/spec → /plan → /implement`).
> **Ticket:** [P2-SAVE-001](tickets/p2-save-001-specify-save-load-format-using-seed-plus-dirty-chunk-diffs.md) · **Issue:** [#38](https://github.com/synthet/project-twelve/issues/38)
> **Reconciles:** [11 — Saving & Loading](11-saving-loading.md) (decisions), [Generation and Saving](generation-and-saving.md) (prototype code).
> On acceptance, the normative format layout moves into [Generation and Saving](generation-and-saving.md); this page remains the reviewable requirements record.

## 1. Summary

ProjectTwelve worlds are reproducible from a `seed` plus generation settings, so a save must persist
only what the player *changed*, not the whole world. This spec defines the production save format:
a versioned file that opens with a header (format version, seed, generation settings, registry
palette), followed by player state, global flags, and one record per **dirty chunk** stored as a
diff from the generated baseline. Writes are **atomic** (temp file → fsync → rename) with a rolling
`.bak`; loads are **fail-safe** (version-gated, checksum-verified, `.bak` fallback, never a silent
regenerate-over-world). The format supersedes the current prototype
(`SandboxSaveData` `version = 1`, unversioned-on-load JSON, non-atomic `File.WriteAllText`, whole-chunk
"edit lists") and carries a forward-migration path from that v1 save.

## 2. Users / stakeholders

- **Players** — worlds survive crashes, game updates, and content/registry changes without loss.
- **Persistence developers** — a single documented contract for `SandboxWorld` save/load, testable in EditMode.
- **Content/registry developers (P2-DATA-001)** — the palette guarantees saved tiles survive registry reordering.
- **Fluid/lighting developers (P2-FLUID-001 / P2-LIGHT-001)** — clear rules on which derived state persists.
- **Networking developers (P3-NET-002/004)** — reuse the chunk-diff *concept*; the on-disk bytes stay decoupled from the wire format.

## 3. Non-goals

- **Region-file packing** and one-file-per-chunk streaming (recorded as a scaling follow-up, not built here).
- **Asynchronous / multi-frame streaming writes** as a hard requirement — spreading writes is allowed, not mandated for the prototype scale.
- **Cloud / platform-cloud sync** (Steam Cloud, etc.).
- **Server-authoritative saves** — P3-NET-004 consumes this format; it is not defined here.
- **Entity/AI-state persistence beyond player** — enemies and world entities are out of scope until their systems land.
- **A stable cross-version *binary* schema for external tools** — the format is internal; migration, not third-party compatibility, is the compatibility contract.

## 4. User stories

- As a **player**, I want an interrupted save (power loss, crash) to leave either my old or my new
  world intact, so a bad moment never destroys my world.
- As a **player**, I want a save from a previous game version to load after an update, so patches
  don't cost me my world.
- As a **player**, I want a save made after the developers reordered blocks to still show the right
  tiles, so content updates don't corrupt what I built.
- As a **persistence developer**, I want save/load to be pure C# over data objects, so I can prove
  round-trip, migration, and corruption behavior in EditMode without opening a scene.
- As a **fluid developer**, I want fluid amounts persisted for dirty chunks while lighting is
  recomputed on load, so resumed worlds look identical without bloating the file.

## 5. Acceptance criteria

Testable statements. "AC-n" ids are referenced by the verification plan and the ticket exit checklist.

### Format & diff semantics
- **AC-1 (clean chunks derive from seed).** Given a chunk never edited, When the world saves, Then
  the chunk is absent from the file; When the world loads, Then that chunk regenerates from the seed
  and equals the pre-save chunk at every cell.
- **AC-2 (edited chunks round-trip).** Given a random sequence of tile edits, When save → load runs,
  Then every edited cell and a sample of clean cells are equal to pre-save state, comparing tile id
  and `fluid`, and **excluding** `light`.
- **AC-3 (diff encoding threshold).** Given a chunk with ≤ 25% of `Size × Size` cells diverged from
  generation, When it is persisted, Then it uses the sparse edit-list encoding; When > 25% diverged,
  Then it uses the full-array encoding. Both encodings load to identical chunk state (the threshold
  is a size optimization, never a correctness difference).
- **AC-4 (derived-state rule).** Given a loaded save, Then `light` was not read from the file and is
  recomputed; and `fluid` for dirty chunks equals its pre-save value.

### Write / load safety
- **AC-5 (atomic write).** Given a simulated interruption at any point during save (debug hook),
  Then either the previous primary save or the fully-written new save is valid on next load — never a
  truncated primary with no usable backup.
- **AC-6 (rolling backup).** Given a successful save, Then the immediately-previous primary is
  retained as `<name>.bak` (single generation).
- **AC-7 (corruption fallback).** Given a primary save that is truncated or contains garbage bytes,
  When loading, Then the loader detects it (parse failure or checksum mismatch), falls back to `.bak`,
  surfaces a user-facing warning, throws no exception out of the load call, and **never** silently
  regenerates over the existing world.
- **AC-8 (future-version refusal).** Given a save whose `formatVersion` exceeds the supported max,
  When loading, Then the loader refuses with a clear message and performs no partial load.

### Migration & palette
- **AC-9 (v1 migration).** Given a stored prototype `version = 1` JSON fixture, When loading, Then it
  passes through the migration path (v1 → current) and the resulting world matches the fixture's
  intended tiles.
- **AC-10 (palette stability).** Given a save written with registry ordering A and loaded with
  reordered ordering B, When resolving tiles through the persisted palette (`stringId → runtimeIndex`),
  Then every tile resolves to the same string id it was saved as. (Shared with P2-DATA-001.)

### Portability & non-functional
- **AC-11 (machine independence).** The save contains no absolute paths and lives under
  `Application.persistentDataPath`.
- **AC-12 (compression).** Chunk payloads are GZip-compressed; the spec records target file sizes for
  a representative world (e.g., N dirty chunks).
- **AC-13 (traceability).** The GitHub issue and the wiki ticket link to each other; exit evidence
  records commit, verification commands, and reviewer findings.

## 6. Normative format sketch

Byte-order and exact field widths are fixed during `/implement`; this is the required logical layout.

```text
[header]
  magic            "P12S" (4 bytes, guards against loading foreign files)
  formatVersion    uint16, FIRST field parsed after magic; gates load (AC-8) & migration (AC-9)
  worldSeed        int64
  genSettings      P2-GEN-001 settings object (opaque, versioned with the format)
  palette          count, then [stringId (utf8), runtimeIndex] pairs   (P2-DATA-001, AC-10)
[player]
  hasPosition, position(x,y)
  inventory        P2-INV-001 slot list (reserved; empty until INV lands)
[globals]
  timeOfDay        placeholder scalar
  flags            reserved key/value block
[chunks]
  count, then per dirty chunk:
    coord(x,y)
    encoding         enum { editList = 0, fullArray = 1 }              (AC-3)
    payload          GZip( tiles, including fluid; excluding light )    (AC-4, AC-12)
    checksum         CRC32/xxhash over the uncompressed payload         (AC-7)
[trailer]
  fileChecksum      over header+body, verified before any world mutation (AC-7)
```

**Load order of operations (fail-safe):** read+verify `magic` → read `formatVersion`, refuse if
unsupported (AC-8) → verify `fileChecksum`; on any failure before this point, abandon the primary and
retry the whole load against `.bak` (AC-7). Only after full verification does the loader mutate world
state, so a rejected primary never half-applies.

**Encoding equivalence:** `editList` stores `(localX, localY, tile)` for diverged cells only;
`fullArray` stores the whole `Size × Size` tile grid. The current prototype always emits the
whole grid *as* an edit list (every cell), which AC-3 replaces with a true diverged-cell diff plus
the full-array fast path.

## 7. Open questions

Decisions needed from a human before `/implement`; each carries the spec's default.

1. **v1 migration vs. clean break.** The prototype is pre-alpha. Migrate the `version = 1` JSON save
   (AC-9), or declare a documented one-time break and drop it? **Default: migrate** (exercises the
   migration path with a real fixture).
2. **Checksum algorithm.** CRC32 (tiny, in-repo) vs. xxHash/SHA-family (stronger, may need a dep).
   **Default: CRC32** for payloads + file, revisited only if collision risk matters.
3. **Backup depth.** Single `.bak` (AC-6) vs. a small ring (e.g., 3 generations)? **Default: single**
   `.bak`; deeper history is a follow-up.
4. **`genSettings` embedding.** Embed the full settings object in the header vs. store only a settings
   hash and refuse on mismatch. **Default: embed** so a save is self-describing without the current build's defaults.
5. **Autosave cadence.** Concrete interval + save-on-quit values (e.g., every N seconds and on
   `OnApplicationQuit`). **Default: proposal recorded in `/plan`**, not fixed here.
6. **Dependency sequencing.** P2-DATA-001 (palette), P2-GEN-001 (settings), P2-INV-001 (inventory),
   P2-FLUID-001 (fluid) are in flight. Reserve their fields now (empty/opaque) and fill as each lands,
   or block implementation until they're done? **Default: reserve fields** so save work proceeds in
   parallel behind stable field contracts.

## 8. Verification plan

Per [Quality Gates](quality-gates.md); all EditMode over temp files / in-memory data objects.

| Test | Covers |
|------|--------|
| Round-trip (random edit sequence → save → load → equality) | AC-1, AC-2, AC-4 |
| Diff-threshold (below/above 25% → both encodings load equal) | AC-3 |
| Migration (stored v1 JSON fixture → load) | AC-9 |
| Corruption (truncated file; garbage bytes) → `.bak` fallback, no throw | AC-7 |
| Future-version refusal | AC-8 |
| Palette (reordered registry resolves same string ids) | AC-10 |
| Atomic-write interruption (debug hook mid-write) | AC-5, AC-6 |
| Path/portability assertion (no absolute paths; under `persistentDataPath`) | AC-11 |

Manual: autosave + quit/reload session; simulated interrupt proving atomicity.
Command: `Unity -batchmode -quit -projectPath . -runTests -testPlatform EditMode …` per Quality Gates.

## 9. Impacted code & docs (for `/plan`)

- `Assets/Scripts/Sandbox/SandboxSaveData.cs` — v1 shapes become the migration source; new versioned records.
- `Assets/Scripts/Sandbox/SandboxWorld.cs` — `SaveToPath`/`LoadFromPath` gain atomic write, `.bak`,
  version gate, checksum verify, migration dispatch; `HasEdits`/`IsDirtyForSave` drive the diff set.
- Docs: fold the §6 layout into [Generation and Saving](generation-and-saving.md); mark adopted
  decisions in [11 — Saving & Loading](11-saving-loading.md); cross-reference P2-DATA/FLUID/INV tickets.

## See also

- [11 — Saving & Loading](11-saving-loading.md) — strategy, pitfalls, versioning rationale.
- [Generation and Saving](generation-and-saving.md) — prototype save code this spec hardens.
- [Procedural Generation](07-procedural-generation.md) — determinism that makes diff-only saves possible.
- [Modding & Content](12-modding.md) — stable string IDs and the registry→runtime mapping.
- [Quality Gates](quality-gates.md) — required verification before merge.
