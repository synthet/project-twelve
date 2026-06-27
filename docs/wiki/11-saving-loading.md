# 11 — Saving & Loading

> **Status:** Planning.
> **Decisions:** **Chunk-based** saves of **diffs from generated baseline**; versioned **binary**;
> GZip compression.
> **Invariants:** A save always carries a version header; loads migrate old versions forward.

## Strategy: seed + diffs

Because generation is deterministic (see [Procedural Generation](07-procedural-generation.md)),
the world is reproducible from `seed` + generation settings. So the save needs to store only what
the player *changed*, not the whole world:

- **Diff/command form:** store seed + an ordered log of edits (break/place). Replay on load.
  Smallest on disk when edits are sparse; replay cost grows with edit count.
- **Modified-chunk form:** store full tile arrays only for chunks that were touched; untouched
  chunks regenerate from seed. Bounded load cost; larger than a sparse command log.

A practical hybrid: per-chunk, keep a "dirty" bit; persist the chunk's tile array (compressed) once
it diverges from generation. This is the recommended production target.

## What a save contains

- **Format version** (header) — first thing read; drives migration.
- **World seed** and generation settings.
- **Dirty chunk tile data** (or an edit-command log).
- **Entity states** (positions, health, AI state).
- **Inventories** and player progression.
- **Time of day** and global events/flags.
- Optionally **fluid amounts** for chunks where exact resume matters (see [Liquids](08-liquids.md));
  light is cheap to recompute on load (see [Lighting](06-lighting.md)).

## Serialization format

- **Binary** (custom structs, or MessagePack/protobuf) for production: compact and fast.
- **JSON** for early debugging only — readable but far too verbose for full worlds.
- **GZip** (or similar) compress chunk payloads; tile arrays compress extremely well.

Keep the save format decoupled from any networking library's wire format, even though both reuse
the same chunk-diff concept (see [Multiplayer](10-multiplayer.md)).

## Streaming & cadence

- Save **per chunk**, only dirty chunks (`IsDirtyForSave`, see [Chunking](03-chunking.md)). On
  unload, flush if dirty.
- Stream chunks near the player in/out of memory; one file per chunk or a region-file packing many
  chunks (region files reduce file-count overhead at the cost of more complex writes).
- Periodic autosave + save-on-quit; avoid long synchronous stalls by spreading writes.

## Versioning & migration

- Every save starts with a version number. On load, if older, run forward-migration steps to the
  current schema. Keep migration code until you're sure no old saves remain.
- Registry IDs are **strings** at authoring time but may map to compact runtime IDs; persist the
  string→runtime mapping (or the strings) so saved tiles survive registry reordering — see
  [Modding & Content](12-modding.md).

## Cloud / hosted saves

For single-player or small co-op, saves can sync to Steam Cloud / platform cloud / your server. In
multiplayer the **server** owns the canonical world; clients keep only a view, so the authoritative
save lives server-side.

## Pitfalls

- **No version header** → can't evolve the format without breaking saves. Non-negotiable.
- **Persisting the whole world** every save → unbounded files and stalls. Diff/dirty-only.
- **Saving compact runtime IDs without the mapping** → tiles corrupt when the registry changes.
- **Synchronous full-world writes** on the main thread → frame hitches. Spread/stream writes.

## See also

- [Chunking](03-chunking.md) — `IsDirtyForSave` and per-chunk persistence.
- [Procedural Generation](07-procedural-generation.md) — determinism that enables diff-only saves.
- [Modding & Content](12-modding.md) — stable IDs and registry mapping.
