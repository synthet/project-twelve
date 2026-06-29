---
type: Technical Reference
title: "Multiplayer"
description: "ProjectTwelve Multiplayer reference — design notes, contracts, and decisions for the multiplayer area of the sandbox prototype."
resource: wiki/10-multiplayer.md
tags: [wiki, multiplayer]
timestamp: 2026-06-28T00:00:00Z
okf_version: 0.1
---

# 10 — Multiplayer

> **Status:** Planning (build last; see [Roadmap](14-roadmap.md)).
> **Decisions:** **Server-authoritative**. Clients request edits; server validates and broadcasts deltas.
> **Invariants:** Server owns canonical world state; clients never self-authorize tile edits.

## Why server-authoritative

The server holding truth prevents cheating (clients can't fabricate edits), keeps all clients
consistent, and gives one place to run validation and persistence. Design single-player as
"one local client + embedded server" so multiplayer isn't a retrofit.

## Tile-edit flow

```
1. Client A → Server     break/place request (x, y, tool)
2. Server validates      range · tool/inventory · cooldown · ownership/permissions
3. Server applies         SetTile on canonical world (the same SetTile from 01-architecture)
4. Server → all clients   tile delta broadcast with a sequence number
5. Each client applies     SetTile locally → remesh chunk → relight dirty region
```

- Validation is the security boundary; never trust client-claimed reach/tools/inventory.
- Sequence numbers let clients order/deduplicate deltas and detect gaps.

## Sync strategies

- **Tiles:** broadcast individual deltas for sparse edits; send **chunk diffs** for bulk changes
  (explosions, generation). On a client joining or entering a region, send the chunk snapshot/diff
  so it matches the server's state (untouched chunks regenerate from seed — see
  [Procedural Generation](07-procedural-generation.md)).
- **Entities:** sync transforms at intervals; use **client prediction + server reconciliation** for
  the local player (move immediately, correct on server feedback) for responsive control.
- **Inventory/stats:** server-owned; mutate via validated RPCs / synced vars on change.

## Library options

| Library | Model | Notes |
|---------|-------|-------|
| **Mirror** | Self-hosted client-server (UNET lineage) | Free, mature, lots of docs; good default |
| **Netcode for GameObjects (NGO)** | Self-hosted client-server | Official Unity, integrated, still evolving |
| **Photon (PUN/Fusion)** | Hosted relay | Easy setup/global relays; free CCU cap; harder to enforce authority |
| Fish-Net / DarkRift / others | Various | Evaluate per needs |

For an indie team, **Mirror or NGO** are the usual picks. Photon's relay model relays messages
through their servers (not true P2P) and makes strict server authority harder.

## Keep the game logic engine-agnostic

Wrap networking behind your own interface. The tile-diff format, chunk subscription/streaming,
validation rules, and save logic should **not** depend on a specific package — so you can swap
Mirror↔NGO without rewriting gameplay. This is the "no engine lock-in at the seams" invariant
(see [Architecture](01-architecture.md)).

## Pitfalls

- **Trusting the client** on reach/tool/inventory → cheating. Validate server-side.
- **Sending every tile individually** during bulk edits → bandwidth spikes. Batch into chunk diffs.
- **No reconciliation** for the local player → rubber-banding or input lag. Predict + reconcile.
- **Coupling save format to network format** to one library → painful migration later.

## See also

- [Architecture](01-architecture.md) — the `SetTile` path the server drives.
- [Saving & Loading](11-saving-loading.md) — chunk diffs reused for both net and disk.
- [Chunking](03-chunking.md) — chunk subscription/streaming to clients.
