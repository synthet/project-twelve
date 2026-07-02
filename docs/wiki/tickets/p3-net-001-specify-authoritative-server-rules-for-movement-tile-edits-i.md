---
type: Task
id: P3-NET-001
title: "[P3-NET-001] Specify authoritative server rules for movement, tile edits, inventory, and chunk subscription."
description: Server-authoritative validation rules for edits, movement reconciliation, inventory RPCs, and chunk interest management behind an engine-agnostic seam.
status: open
phase: "Phase P3 — Networking alpha"
github_project: "https://github.com/users/synthet/projects/2"
github_issue: "https://github.com/synthet/project-twelve/issues/40"
github_issue_status: created
resource: wiki/tickets/p3-net-001-specify-authoritative-server-rules-for-movement-tile-edits-i.md
tags: [docs, wiki, ticket, networking, security, p3]
timestamp: 2026-07-01T00:00:00Z
okf_version: 0.1
spec_references:
  - "docs/wiki/spec-driven-development-tasks.md"
  - "docs/wiki/10-multiplayer.md"
  - "docs/wiki/multiplayer-and-modding.md"
---

# [P3-NET-001] Specify authoritative server rules for movement, tile edits, inventory, and chunk subscription.

## Open knowledge summary

This ticket specifies the server-authoritative rules from `docs/wiki/10-multiplayer.md`: the
server owns canonical world state; clients send **requests**, never state. It defines the
validation contract for tile edits (the security boundary), client prediction + reconciliation
for movement, server-owned inventory mutation, and chunk subscription (interest management) —
all behind a project-owned interface so no rule depends on Mirror/NGO/any specific package.
Single-player runs as one local client + embedded server so multiplayer is not a retrofit.

## GitHub project linkage

- **Project:** [synthet project 2](https://github.com/users/synthet/projects/2)
- **Issue:** [synthet/project-twelve#40](https://github.com/synthet/project-twelve/issues/40)
- **Backlink requirement:** The GitHub issue body must link back to this markdown ticket.

## User story

As a networking developer starting the P3 alpha, I want authoritative rules specified before any
transport code lands so that cheating is structurally impossible (clients cannot self-authorize
world changes) and the validation logic is the same code that P2-INV-001 runs locally.

## Requirements

### Functional requirements

1. **Authority:** the server owns the canonical world, inventories, and entity state. Clients
   never apply persistent world changes without server validation; local visual prediction is
   allowed but must reconcile.
2. **Tile-edit flow** (normative, per `10-multiplayer.md`): client request `(x, y, action, item)`
   → server validates → server applies via the canonical `SetTile` choke point → server
   broadcasts a sequence-numbered delta → clients apply locally (remesh + relight). Rejected
   requests get an explicit denial so the client can roll back its prediction.
3. **Validation checks** (server-side, reusing the P2-INV-001 boundary logic): reach/`editRange`,
   inventory holds the consumed item, action cooldown/rate limit per player, target-cell rules
   (placeable/breakable), and world bounds/loaded state. Validation constants are shared with
   single-player — one implementation, two call sites.
4. **Movement:** local player uses client prediction; the server validates positions against
   speed/teleport bounds and reconciles (correction snapshots). Remote players interpolate.
5. **Inventory:** server-owned; mutations only through validated RPCs tied to edits/pickups;
   clients render replicated state.
6. **Chunk subscription:** clients subscribe to chunks within their interest radius; the server
   sends snapshots on subscribe and deltas while subscribed (formats owned by P3-NET-002);
   unsubscribed chunks receive nothing (bandwidth bound).
7. **Seam:** all of the above is expressed through project-owned interfaces (requests, deltas,
   subscription events); a transport adapter (Mirror or NGO, chosen in P3-NET-003) implements
   them. No gameplay file references the networking package directly.

### Non-functional requirements

1. Validation is deterministic and pure over world + player state (EditMode-testable without a
   transport).
2. The threat model is written down: spoofed reach, item duplication via request replay,
   rapid-fire edit flooding, subscription abuse (requesting the whole map), and movement
   teleport/speed hacks each have a documented mitigation.
3. Single-player performance is unchanged: the embedded-server path adds no per-frame allocation
   or measurable overhead versus P2 behavior.

## Acceptance criteria

- Clients cannot apply persistent world changes without server validation (design review against
  the flow + code inspection at implementation time).
- EditMode validation tests: out-of-reach edit rejected; edit without the required item rejected;
  over-rate requests rejected; valid request applies exactly once.
- EditMode replay test: re-sending an already-applied request (same request id/sequence) does not
  double-apply or double-consume.
- Movement bounds test: a position update exceeding max speed is corrected, not accepted.
- Threat-model review recorded in the spec page with mitigations per threat.
- The GitHub issue and this markdown ticket link to each other.
- Exit evidence records the commit, verification commands, and reviewer findings.

## Detailed technical specifications

### Scope

- Rule specification, request/denial message shapes, validation implementation shared with
  single-player, interest-management policy, and the transport-agnostic interface definitions.
- Out of scope: wire formats (P3-NET-002), transport/library integration and LAN bring-up
  (P3-NET-003), reconnect semantics (P3-NET-004), anti-cheat beyond authority (no client
  attestation).

### Inputs and dependencies

- `Assets/Scripts/Sandbox/SandboxWorld.cs` — `SetTile` choke point the server drives.
- P2-INV-001 — the validation boundary this ticket promotes to server-side.
- `docs/wiki/10-multiplayer.md` — flow, pitfalls, library guidance.
- `Assets/Scripts/Integration/` — home for the seam interfaces (pattern:
  `ISandboxPlayerLocomotion`).

### Verification plan

- EditMode validation/replay/movement-bounds tests (pure logic, no transport).
- Threat-model review session recorded in the spec page (attack → mitigation table).
- Design review: no gameplay assembly references the networking package.

## Documentation impact

- `docs/wiki/multiplayer-and-modding.md` — authoritative rules, threat model, seam interfaces.
- `docs/wiki/10-multiplayer.md` — mark decisions adopted.
- P2-INV-001 ticket — shared-validation cross-reference.
- Update `docs/wiki/spec-driven-development-tasks.md` if task scope or sequencing changes.

## Exit evidence checklist

- [ ] GitHub issue URL is recorded in this ticket.
- [ ] GitHub issue links back to this markdown ticket.
- [ ] Authoritative rules + threat model documented before transport work.
- [ ] Validation, replay, and movement-bounds EditMode tests pass.
- [ ] Seam interfaces reviewed (no package coupling in gameplay code).
- [ ] Follow-up tasks created for permissions/ownership zones and admin tooling.
