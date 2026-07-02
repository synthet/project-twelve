---
type: Task
id: P3-NET-004
title: "[P3-NET-004] Specify disconnect/reconnect and save consistency behavior."
description: Reconnect convergence via snapshots and sequence numbers, server-owned authoritative saves, and no-duplication guarantees for items and edits.
status: open
phase: "Phase P3 — Networking alpha"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/43"
github_issue_status: created
resource: wiki/tickets/p3-net-004-specify-disconnect-reconnect-and-save-consistency-behavior.md
tags: [docs, wiki, ticket, networking, persistence, p3]
timestamp: 2026-07-01T00:00:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/spec-driven-development-tasks.md"
  - "docs/wiki/10-multiplayer.md"
  - "docs/wiki/11-saving-loading.md"
  - "docs/wiki/tickets/p3-net-002-specify-chunk-snapshot-and-delta-formats.md"
  - "docs/wiki/tickets/p2-save-001-specify-save-load-format-using-seed-plus-dirty-chunk-diffs.md"
---

# [P3-NET-004] Specify disconnect/reconnect and save consistency behavior.

## Open knowledge summary

This ticket specifies what happens when the connection breaks: how a reconnecting client
converges to server state (fresh snapshots — client-side stale state is discarded, never merged),
how in-flight requests are prevented from double-applying (request ids + sequence numbers from
P3-NET-002), where the player's body and items go while offline, and the rule that the
**server-side save (P2-SAVE-001 format) is the only authoritative persistence** in multiplayer —
clients never write world saves. The convergence invariant: after reconnect, client chunk state,
player inventory, and entity views equal the server's, with no duplicated edits or items.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** [synthet/project-twelve#43](https://github.com/synthet/project-twelve/issues/43)
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a networking developer hardening the P3 alpha, I want disconnect/reconnect behavior specified
so that flaky Wi-Fi produces at worst a brief interruption — never duplicated items, ghost edits,
lost progress, or a corrupted server save.

## Requirements

### Functional requirements

1. **Disconnect detection:** heartbeat/timeout thresholds (named constants) on both sides. On
   client loss the server despawns or suspends the player avatar per a documented rule, retains
   the player's inventory/position in server state, and continues simulating the world.
2. **Reconnect flow:** reconnecting client authenticates as the same player identity → receives
   current player state (position, inventory) → re-subscribes to chunks → receives fresh
   snapshots per P3-NET-002. All client-cached chunk state from the previous session is
   invalidated; convergence comes from snapshots, never from merging stale client state.
3. **No duplication:** requests carry client-generated request ids; the server's dedup window
   spans disconnects, so a request applied just before the drop is not re-applied on retry after
   reconnect (idempotent replay). Item pickups and edit consumption follow the same rule.
4. **Save consistency:** the server autosaves on its normal cadence plus on host shutdown and on
   last-player-leave; a client disconnect never triggers partial or concurrent writes. In
   single-player (embedded server), quit-save is the same code path.
5. **Host loss:** if the host/server dies, clients surface a terminal "server lost" state; world
   state recovers from the server's last save on restart (bounded, documented loss window =
   autosave interval).
6. **Rejoin identity:** player identity is stable across sessions (name/GUID for LAN alpha —
   mechanism documented) so inventory/position restore to the right player.

### Non-functional requirements

1. Reconnect to a converged, playable state completes within a documented time budget on LAN
   (target: seconds, dominated by snapshot transfer of subscribed chunks).
2. The dedup window's memory cost is bounded (documented size/eviction rule).
3. All convergence logic is simulation-testable without a real network (harness from P3-NET-002).

## Acceptance criteria

- Reconnected clients converge to server state without duplicating edits or items (the invariant
  holds in all scenario tests below).
- Scenario test — mid-edit drop: client sends an edit, connection drops before the ack, client
  retries after reconnect → the edit exists exactly once, the item was consumed exactly once.
- Scenario test — offline world change: server world changes while the client is gone → after
  reconnect the client renders the changed chunks (stale cache discarded).
- Scenario test — pickup race: item picked up immediately before the drop is not re-collectable
  after reconnect.
- Scenario test — host shutdown: restart from save restores the world including all applied
  edits up to the last autosave; loss window matches the documented bound.
- Manual LAN test: pull the network cable / kill the client mid-play, reconnect, verify
  convergence and inventory (extends the P3-NET-003 checklist).
- The GitHub issue and this markdown ticket link to each other.
- Exit evidence records the commit, verification commands, and reviewer findings.

## Detailed technical specifications

### Scope

- Timeout/heartbeat constants, reconnect handshake, dedup-across-disconnect rule, offline-player
  policy, autosave/shutdown triggers, host-loss behavior, and the scenario-test suite.
- Out of scope: host migration, mid-session world transfer between machines, account systems
  (LAN identity only), and client-side world caching optimizations beyond invalidation.

### Inputs and dependencies

- P3-NET-002 — snapshots/sequence numbers (convergence mechanism); its simulation harness hosts
  the scenario tests.
- P3-NET-001 — request ids and validation flow the dedup rule extends.
- P2-SAVE-001 — the save format and atomic-write behavior the server relies on.
- P3-NET-003 — the LAN setup used for the manual disconnect test.

### Verification plan

- EditMode/simulation scenario tests: mid-edit drop, offline change, pickup race, host shutdown.
- Manual LAN disconnect/reconnect checklist run with captures.
- Review: no client-side world-save write path exists in multiplayer builds.

## Documentation impact

- `docs/wiki/multiplayer-and-modding.md` — reconnect flow, dedup rule, save-authority section.
- `docs/wiki/11-saving-loading.md` — server-authoritative save note (cloud/hosted section).
- P3-NET-003 ticket — extend the playtest checklist with the disconnect scenario.
- Update `docs/wiki/spec-driven-development-tasks.md` if task scope or sequencing changes.

## Exit evidence checklist

- [ ] GitHub issue URL is recorded in this ticket.
- [ ] GitHub issue links back to this markdown ticket.
- [ ] Reconnect/dedup/save-authority rules documented before implementation.
- [ ] All four scenario tests pass in the simulation harness.
- [ ] Manual LAN disconnect test recorded.
- [ ] Follow-up tasks created for host migration and account identity.
