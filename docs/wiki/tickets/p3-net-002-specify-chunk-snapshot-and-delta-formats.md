---
type: Task
id: P3-NET-002
title: "[P3-NET-002] Specify chunk snapshot and delta formats."
description: Versioned, sequence-numbered chunk snapshot and tile-delta wire formats that are ordered, deduplicatable, and replayable under loss and reorder.
status: open
phase: "Phase P3 — Networking alpha"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/41"
github_issue_status: created
resource: wiki/tickets/p3-net-002-specify-chunk-snapshot-and-delta-formats.md
tags: [docs, wiki, ticket, networking, serialization, p3]
timestamp: 2026-07-01T00:00:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/spec-driven-development-tasks.md"
  - "docs/wiki/10-multiplayer.md"
  - "docs/wiki/multiplayer-and-modding.md"
  - "docs/wiki/11-saving-loading.md"
---

# [P3-NET-002] Specify chunk snapshot and delta formats.

## Open knowledge summary

This ticket specifies the two wire formats that keep clients consistent with the server world:
**chunk snapshots** (state of a chunk as a diff from its seed-generated baseline, sent on
subscribe/join) and **tile deltas** (individual or batched edits, broadcast live). Both are
versioned, sequence-numbered per chunk, and replayable, so clients can order, deduplicate, and
detect gaps under packet loss and reorder. The formats reuse the *concepts* of the save system
(seed + diffs, P2-SAVE-001) but are deliberately decoupled bytes — per `10-multiplayer.md`,
coupling save and wire formats is a recorded pitfall.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** [synthet/project-twelve#41](https://github.com/synthet/project-twelve/issues/41)
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a networking developer on the P3 alpha, I want snapshot and delta formats specified with
explicit ordering and versioning semantics so that a joining client, a lagging client, and the
server all converge on identical chunk state without full-world transfers.

## Requirements

### Functional requirements

1. **Delta message:** protocol version, chunk coord, per-chunk sequence number, and one or more
   `(localX, localY, tileId, metadata)` edits. Bulk changes (explosions, generation rewrites)
   batch into one message per affected chunk rather than per-tile spam.
2. **Snapshot message:** protocol version, chunk coord, the chunk's current sequence number, and
   the diff from the seed baseline (edit list or full array, same threshold rule as P2-SAVE-001).
   A clean chunk's snapshot is an explicit "baseline, seq N" marker — clients regenerate from
   seed locally.
3. **Ordering contract:** per-chunk sequence numbers are monotonically increasing; clients apply
   deltas in order, drop duplicates (seq ≤ applied), and on a gap request a fresh snapshot for
   that chunk rather than applying out of order.
4. **Join/subscribe flow:** subscribe → snapshot(seq N) → subsequent deltas (seq > N) apply on
   top; deltas arriving before the snapshot are buffered or discarded per a documented rule.
5. **Replayability:** applying snapshot(N) + deltas(N+1..M) yields exactly the server's chunk
   state at M — the core convergence invariant (also consumed by P3-NET-004 reconnect).
6. **Versioning:** every message carries the protocol version; mismatched versions refuse the
   connection cleanly at handshake (no silent misparse).
7. Tile identity on the wire uses runtime indices only alongside a session-pinned registry
   palette exchanged at handshake (P2-DATA-001 determinism makes indices stable per content set).

### Non-functional requirements

1. Encode/decode is pure C# with no networking-package or Unity-scene dependency
   (EditMode-testable; the transport ships opaque byte arrays).
2. Size budgets documented: single-edit delta stays within tens of bytes; a worst-case full-array
   snapshot for a 32×32 chunk is bounded and compressed (GZip or bit-packing — decision recorded
   in the spec).
3. No allocation-per-message in steady state (reusable buffers) — broadcast happens on every
   edit, so this is a hot path.

## Acceptance criteria

- Initial sync and incremental edits are versioned, ordered, and replayable per the contract
  above, documented in the spec page before implementation.
- EditMode round-trip tests: delta and snapshot messages encode → decode to equality, including
  batched deltas and the clean-chunk baseline marker.
- EditMode simulation tests (no real network): random loss, duplication, and reorder of a delta
  stream — client converges to server state via the gap→snapshot rule; duplicates never
  double-apply.
- EditMode convergence test: snapshot(N) + deltas(N+1..M) equals server chunk state at M for
  randomized edit sequences.
- Version-mismatch handshake test: incompatible protocol versions refuse cleanly.
- The GitHub issue and this markdown ticket link to each other.
- Exit evidence records the commit, verification commands, and reviewer findings.

## Detailed technical specifications

### Scope

- Message layouts, sequencing/gap rules, palette handshake, encode/decode implementation, and the
  loss/reorder simulation harness.
- Out of scope: transport integration and bandwidth measurement on a real LAN (P3-NET-003),
  entity/inventory replication messages (P3-NET-001 owns their semantics; formats can follow the
  pattern in a follow-up), encryption/compression negotiation.

### Inputs and dependencies

- P3-NET-001 — request/validation flow that produces deltas; seam interfaces these messages ride.
- P2-SAVE-001 — shared diff-from-baseline concept and threshold rule (bytes intentionally
  independent).
- P2-DATA-001 — deterministic runtime indices + palette for the handshake.
- `docs/wiki/quality-gates.md` — network serialization test vocabulary (round-trip, sequence
  ordering).

### Verification plan

- EditMode: round-trip, loss/reorder simulation, convergence, and handshake tests (pure data).
- Byte-size report for representative messages attached as exit evidence.

## Documentation impact

- `docs/wiki/multiplayer-and-modding.md` — message layouts, sequencing rules, handshake.
- P3-NET-003 / P3-NET-004 tickets — consume these formats; confirm cross-links.
- Update `docs/wiki/spec-driven-development-tasks.md` if task scope or sequencing changes.

## Exit evidence checklist

- [ ] GitHub issue URL is recorded in this ticket.
- [ ] GitHub issue links back to this markdown ticket.
- [ ] Wire formats documented before implementation.
- [ ] Round-trip and loss/reorder simulation tests pass.
- [ ] Convergence invariant test passes for randomized sequences.
- [ ] Follow-up tasks created for entity/inventory message formats and compression choice.
