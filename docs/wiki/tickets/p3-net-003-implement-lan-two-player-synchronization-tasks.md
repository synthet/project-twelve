---
type: Task
id: P3-NET-003
title: "[P3-NET-003] Implement LAN two-player synchronization tasks."
description: Bring up two-player LAN sync on a chosen transport adapter, validating consistent movement, edits, and chunk state under expected latency.
status: open
phase: "Phase P3 — Networking alpha"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/42"
github_issue_status: created
resource: wiki/tickets/p3-net-003-implement-lan-two-player-synchronization-tasks.md
tags: [docs, wiki, ticket, networking, integration, p3]
timestamp: 2026-07-01T00:00:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/spec-driven-development-tasks.md"
  - "docs/wiki/10-multiplayer.md"
  - "docs/wiki/multiplayer-and-modding.md"
  - "docs/wiki/tickets/p3-net-001-specify-authoritative-server-rules-for-movement-tile-edits-i.md"
  - "docs/wiki/tickets/p3-net-002-specify-chunk-snapshot-and-delta-formats.md"
---

# [P3-NET-003] Implement LAN two-player synchronization tasks.

## Open knowledge summary

This ticket is the P3 integration milestone: two players on a LAN share one world with consistent
movement, tile edits, and chunk state. It selects the transport library (Mirror or Netcode for
GameObjects per the `docs/wiki/10-multiplayer.md` comparison — decision recorded here), implements
the transport **adapter** for the P3-NET-001 seam interfaces carrying P3-NET-002 messages, and
validates behavior under real latency with a playtest checklist and profiler capture. Gameplay
code must remain package-free: swapping the library later touches only the adapter.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** [synthet/project-twelve#42](https://github.com/synthet/project-twelve/issues/42)
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a networking developer completing the P3 alpha, I want two-player LAN synchronization working
on top of the specified rules and formats so that the authoritative architecture is proven with
real packets, real latency, and a second human — not just simulations.

## Requirements

### Functional requirements

1. **Library decision:** evaluate Mirror vs Netcode for GameObjects against the criteria in
   `10-multiplayer.md` (authority enforcement, maturity, docs); record the choice and rationale
   in `multiplayer-and-modding.md`. Hosted-relay options (Photon) are out per the seam/authority
   requirements.
2. **Topology:** player 1 runs host (embedded server + local client); player 2 joins over LAN by
   address. Host and client run identical builds.
3. **Transport adapter:** implements the P3-NET-001 seam (requests up, deltas/snapshots down,
   subscription events) carrying P3-NET-002 bytes as opaque payloads. No gameplay assembly
   references the library.
4. **Synchronized in this milestone:** player movement (prediction + reconciliation for local,
   interpolation for remote), tile edits end-to-end (request → validate → broadcast → remesh on
   both machines), chunk subscribe/snapshot on join and on entering new regions, and join/leave
   notifications. Inventory sync follows P3-NET-001 rules for the edit-consumed items.
5. **Late join:** a client joining after the host has edited the world receives snapshots that
   reproduce the edited state (P3-NET-002 convergence in practice).
6. **Failure surfacing:** connection loss shows a clear state (disconnected UI/log), not a
   frozen world; full reconnect semantics are P3-NET-004.

### Non-functional requirements

1. On a typical LAN (≤ 5 ms RTT): local edit feedback is immediate (prediction), remote edits
   appear within 100 ms, and remote movement shows no visible rubber-banding during normal play.
2. Steady-state bandwidth for two idle-to-lightly-active players stays within a documented budget
   (measure and record; no per-frame chunk retransmission).
3. The synchronization loop allocates nothing per frame in steady state (profiler-verified).

## Acceptance criteria

- Two players see consistent movement, edits, and chunk state under expected latency (LAN
  playtest checklist below passes end-to-end).
- Playtest checklist (scripted, both machines captured): (a) both players visible and moving
  smoothly; (b) P1 edits appear on P2's screen and vice versa, including on a chunk border;
  (c) simultaneous edits to nearby cells converge identically on both machines; (d) late join
  reproduces the edited world; (e) walking into an unexplored region streams identical terrain
  on both machines (seed agreement).
- Rejected-edit path verified: an out-of-range edit attempt on the client is denied by the host
  and rolls back visually.
- Network profiler capture recorded (bandwidth, message counts) against the documented budget.
- No gameplay assembly references the transport package (review check).
- The GitHub issue and this markdown ticket link to each other.
- Exit evidence records the commit, verification commands, playtest notes, and reviewer findings.

## Detailed technical specifications

### Scope

- Library selection + adapter, host/join flow, movement/edit/chunk sync, LAN validation.
- Out of scope: internet play, NAT traversal, dedicated headless server builds, reconnect
  convergence (P3-NET-004), voice/chat, more than two players (architecture must not preclude
  more; testing is two).

### Inputs and dependencies

- P3-NET-001 (rules + seam) and P3-NET-002 (formats) — blocking dependencies.
- `Assets/Scripts/Integration/` — adapter home, following existing seam patterns.
- Second test machine or a second local build instance for LAN validation.

### Verification plan

- Scripted two-player LAN playtest checklist with captures from both machines.
- Network profiler capture + bandwidth report.
- EditMode tests for the adapter's message framing (transport mocked).

## Documentation impact

- `docs/wiki/multiplayer-and-modding.md` — library decision record, topology, adapter notes.
- `docs/wiki/10-multiplayer.md` — mark the library decision as taken.
- Update `docs/wiki/spec-driven-development-tasks.md` if task scope or sequencing changes.

## Exit evidence checklist

- [ ] GitHub issue URL is recorded in this ticket.
- [ ] GitHub issue links back to this markdown ticket.
- [ ] Library decision recorded with rationale.
- [ ] LAN playtest checklist executed with captures from both machines.
- [ ] Bandwidth/profiler report attached.
- [ ] Follow-up tasks created for internet play, >2 players, and dedicated server builds.
