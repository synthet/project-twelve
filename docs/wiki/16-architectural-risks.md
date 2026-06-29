---
type: Risk Analysis
title: Architectural Risks & Spike Candidates
description: Identified architectural risks with mitigations, owners, and decision deadlines for ProjectTwelve.
resource: wiki/16-architectural-risks.md
tags: [docs, wiki, architecture, risks, spikes]
timestamp: 2026-06-29T00:00:00Z
okf_version: 0.1
---

# 16 — Architectural Risks & Spike Candidates

> **Status:** Active analysis (P0-SPEC-005).
> **Scope:** High-impact technical risks that could block vertical slice (P1) or core systems (P2) delivery.
> **Goal:** Identify, mitigate, and track resolution timeline for each risk.

## Risk summary

This document consolidates six identified architectural risks from [00-overview.md](00-overview.md) with detailed mitigations, owners, and decision deadlines. Each risk is scoped to the phase in which it must be resolved.

---

## Risk 1: Large destructible tilemaps exceed memory / performance budget

**Severity:** HIGH  
**Phase:** P1 (must mitigate before vertical slice completion)  
**Owner:** Vladimir Bragin  
**Decision Deadline:** End of P1 (2026-07-31)

### Description

A naive world representation—e.g., one monolithic tile array or a single global Tilemap2D—cannot scale to Terraria-scale worlds (gigabytes of tile data, thousands of edits per session). The challenge compounds when applying physics (global CompositeCollider2D rebuild), lighting (full-world BFS), and networking (broadcasting all edits).

### Mitigation

**Primary:** Chunk-local cost through tile data chunking (see [03-chunking.md](03-chunking.md)).
- Break the world into fixed-size chunks (e.g., 16×16 tiles per chunk).
- Load/unload chunks around the player; keep only ~3×3 chunk region in memory.
- Each chunk owns its tile array, collider, light layer, and fluid state.

**Secondary:** Deterministic on-demand generation to avoid storing every chunk.
- Regenerate clean chunks from seed + coordinate (no IO for unmodified chunks).
- Store only dirty-chunk diffs relative to seed.

**Tertiary:** Chunk-local rebuild scheduling.
- Don't rebuild on every edit; mark chunk dirty and rebuild on next opportunity (render frame or fixed update).

### Verification

- **Unit tests:** Coordinate conversion, chunk lookup, chunk lifecycle (load/unload).
- **Play-mode tests:** Player traversal across multiple chunk boundaries; memory stable after chunk churn.
- **Profiler targets:** Memory usage bounded by loaded chunk count; no full-world array allocations.

### Related specs

- [03-chunking.md](03-chunking.md) — chunk data model and lifecycle.
- [01-architecture.md](01-architecture.md) — World and Chunk ownership rules.
- [14-roadmap.md](14-roadmap.md) — P1 chunk load/unload implementation.

### Spike / PoC tasks

- **P1-WORLD-002:** Implement chunk load/visibility/unload lifecycle; verify memory stable under traversal.

---

## Risk 2: Expensive collider rebuilds degrade frame rate on tile edits

**Severity:** HIGH  
**Phase:** P1 (must validate before vertical slice completion)  
**Owner:** Vladimir Bragin  
**Decision Deadline:** End of P1 (2026-07-31)

### Description

Unity's CompositeCollider2D with auto-rebuild is a known performance cliff: rebuilding a 1000-tile area can spike to 10–50 ms per frame. Even with chunk-local architecture, per-frame edits can accumulate. A tile break in one location + a tile place elsewhere + fluid erosion = multiple collider rebuilds in one frame.

### Mitigation

**Primary:** Per-chunk colliders instead of global CompositeCollider2D (see [05-collision-physics.md](05-collision-physics.md)).
- Each chunk owns its own PolygonCollider2D or BoxCollider2D array for solid tiles.
- Editing a tile rebuilds only that chunk's collider.
- Neighboring chunks' colliders remain untouched.

**Secondary:** Collider rebuild batching / budget-aware scheduling.
- Don't rebuild immediately; mark chunk dirty and rebuild on next low-cost frame.
- Cap rebuild time per frame (e.g., 2 chunks/frame max).

**Tertiary:** Manual tile-based collision instead of mesh colliders for high-churn areas.
- In fluid-heavy zones, use raycast collision checks instead of active colliders.
- Accept slight latency (player queried once per frame) in exchange for no rebuild cost.

### Verification

- **Play-mode tests:** Rapid tile place/break in one area; measure frame time spike; confirm < 2 ms per edit.
- **Profiler targets:** Collider rebuild < 2 ms per chunk; no global CompositeCollider2D rebuilds detected.
- **Manual QA:** Place and break 50 tiles in sequence; confirm smooth 60 FPS (desktop) or 30 FPS (mobile target).

### Related specs

- [05-collision-physics.md](05-collision-physics.md) — per-chunk collider design and manual collision fallback.
- [01-architecture.md](01-architecture.md) — tile edit data flow and dirty-flag contract.

### Spike / PoC tasks

- **P1-COLL-001:** Specify prototype collision rules; validate per-chunk collider performance.

---

## Risk 3: Terraria-style lighting breaks performance on large lit areas

**Severity:** MEDIUM  
**Phase:** P2 (core systems), decision must be locked by end of P1  
**Owner:** Vladimir Bragin  
**Decision Deadline:** End of P1 (2026-07-31)

### Description

Terraria uses a tile lightmap with BFS propagation. Naive BFS over a large lit area (e.g., all tiles within 15 units of a light source) can be O(n²) in the radius. In a 100×100 chunk world with 50 light sources, O(n) becomes prohibitive.

### Mitigation

**Primary:** Chunk-bounded BFS with neighbor propagation (see [06-lighting.md](06-lighting.md)).
- Each chunk stores light values (0–15 per tile).
- Light updates are confined to dirty regions around edits or light source changes.
- Propagate light only to neighboring chunks if the edge is brighter than threshold.

**Secondary:** Lazy propagation.
- Don't update lighting every frame; batch updates and propagate on next "lighting tick" (e.g., 10 Hz).
- Use fallback ambient light in unlit chunks.

**Tertiary:** Fallback to simple directional + ambient if chunk-bounded BFS proves too costly.
- Detect when light propagation spike exceeds budget.
- Fall back to flat lighting (sun direction + ambient) for that frame; queue full update for next cycle.

### Verification

- **Unit tests:** Light propagation falloff (inverse square or similar); dirty-region relight matches full relight.
- **Play-mode tests:** Place 10 light sources; move between lit and unlit areas; confirm light updates within 100 ms.
- **Profiler targets:** Light propagation < 5 ms per update tick; no frame spikes above 16 ms (60 FPS) or 33 ms (30 FPS).

### Related specs

- [06-lighting.md](06-lighting.md) — lighting propagation rules and chunk-local constraints.
- [02-data-models.md](02-data-models.md) — tile light value encoding.

### Spike / PoC tasks

- **P2-LIGHT-001:** Specify lighting data layout and propagation rules; validate chunk-bounded BFS scalability.

---

## Risk 4: Fluid simulation cost outpaces budget on large water features

**Severity:** MEDIUM  
**Phase:** P2 (core systems), decision must be locked by end of P1  
**Owner:** Vladimir Bragin  
**Decision Deadline:** End of P1 (2026-07-31)

### Description

Terraria-style flowing liquids require a cellular automaton (CA) update for every active cell each frame. A large 100×100 body of water = 10,000 cells; even at O(1) per cell, that is 10,000 operations per frame. In a world with multiple water bodies and lava, the budget explodes.

### Mitigation

**Primary:** Active-cell set with wake/sleep scheduling (see [08-liquids.md](08-liquids.md)).
- Maintain an "active set" of fluid cells that flow or react to nearby changes.
- Only update cells in the active set each frame; sleep cells are unchanged.
- Add cells to active set when neighbors change (edge source, gravity, pressure).
- Remove cells when they stabilize.

**Secondary:** Chunk-local pausing.
- Pause fluid simulation in chunks far from the player (e.g., > 2 chunk distance).
- Resume when player approaches or when a surface change (lava eruption) wakes nearby chunks.

**Tertiary:** Temporal batching.
- Update different chunks' fluid on different frames (chunk 0 on frame 0, chunk 1 on frame 1, etc.).
- Spread the O(n) cost across time instead of one spike.

### Verification

- **Deterministic fixtures:** 100×100 water body; verify it settles without infinite oscillation and matches golden output.
- **Play-mode tests:** Create large water feature; measure frame time spike; confirm < 3 ms per update.
- **Profiler targets:** Fluid iteration < 3 ms per frame; active-set size stable when no edits occur nearby.

### Related specs

- [08-liquids.md](08-liquids.md) — liquid simulation rules, active-set management, and save format.
- [03-chunking.md](03-chunking.md) — chunk dirty flag coordination with fluid system.

### Spike / PoC tasks

- **P2-FLUID-001:** Specify simple liquid simulation constraints; validate active-cell CA scalability.

---

## Risk 5: Save file bloats to gigabytes; load time exceeds acceptable threshold

**Severity:** MEDIUM  
**Phase:** P2 (save/load), decision must be locked by end of P1  
**Owner:** Vladimir Bragin  
**Decision Deadline:** End of P1 (2026-07-31)

### Description

A naive "dump all chunks to disk" save grows linearly with world size. A 1000×1000 chunk world (16 million tiles) ≈ 50 MB per tile byte (assuming 8 bytes per tile). Add compression: still 5–10 MB per world. Load-time must stay under 2–3 seconds (user expectation) or players abandon saves.

### Mitigation

**Primary:** Store only dirty-chunk diffs relative to seed (see [11-saving-loading.md](11-saving-loading.md)).
- At save time, only serialize chunks that differ from seed-generated state.
- Store: world seed + list of (chunk_coord, tile_delta[]) for modified chunks.
- Typical playthrough edits < 5% of chunks; save size shrinks 10–20×.

**Secondary:** Compression and versioning.
- Use DEFLATE or LZ4 compression on chunk diffs.
- Include schema version for save migrations (P5 task).

**Tertiary:** Streaming load.
- Load world seed; generate visible chunks on-demand.
- Load dirty chunks from save in background while player spawns.
- Ensure player is playable before all chunks are loaded; finish loading in background.

### Verification

- **Round-trip tests:** Save and load a chunk; verify data identity (deterministic fixtures).
- **Scale tests:** Create a 50-chunk world with edits; measure save size and load time; confirm save < 1 MB and load < 500 ms.
- **Play-mode test:** Edit ~20% of chunks; save; close; load; verify all edits persist and load time < 2 seconds.

### Related specs

- [11-saving-loading.md](11-saving-loading.md) — save format, versioning, and chunk diff encoding.
- [07-procedural-generation.md](07-procedural-generation.md) — deterministic seed-based regeneration.

### Spike / PoC tasks

- **P2-SAVE-001:** Specify save/load format using seed plus dirty chunk diffs; validate save size and load performance.

---

## Risk 6: Multiplayer cheating / desync: clients modify tiles without server validation

**Severity:** HIGH  
**Phase:** P3 (networking), decision must be locked by end of P2  
**Owner:** Vladimir Bragin  
**Decision Deadline:** End of P2 (2026-08-31)

### Description

In a peer-to-peer or weakly-validated architecture, a malicious (or buggy) client can send a tile edit and apply it locally without server checks. This breaks game balance (infinite resources, clipping through terrain) and breaks consistency with other players.

### Mitigation

**Primary:** Server-authoritative tile deltas (see [10-multiplayer.md](10-multiplayer.md)).
- Client sends intent: "I want to break tile at (100, 50)".
- Server validates: player is close enough, has the tool, tile is breakable.
- Server applies edit to its world state.
- Server broadcasts new tile value to all clients.
- Client applies the server's decision, not its own intent.

**Secondary:** Lag compensation without client-side prediction of tile state.
- Client predicts player movement with extrapolation.
- **Never** predict tile state client-side; always apply server's tile decision.
- Client can show "pending" UI (spinning cursor) while waiting for server.

**Tertiary:** Anti-cheat measures.
- Log all tile edits (player ID, location, timestamp) for post-game review.
- Detect and kick clients that report impossible edits (e.g., breaking a tile > 10 units away).
- Implement rate limiting (max edits per second) per player.

### Verification

- **Threat-model review:** Document all client→server messages and validate each against attack scenarios (infinite resources, teleport, build outside reachable area).
- **Network tests:** Simulate packet loss and reorder; verify client and server eventually agree on world state.
- **Replay tests:** Record a multiplayer session; replay server log; verify all clients converge to server state.

### Related specs

- [10-multiplayer.md](10-multiplayer.md) — server-authoritative tile delta protocol and lag compensation.
- [01-architecture.md](01-architecture.md) — tile edit choke point (`SetTile`).

### Spike / PoC tasks

- **P3-NET-001:** Specify authoritative server rules for movement, tile edits, inventory, and chunk subscription; validate threat model.

---

## Summary table

| Risk | Phase | Owner | Deadline | Primary Mitigation | PoC Task |
|------|-------|-------|----------|---------------------|----------|
| 1. Large tilemaps | P1 | VB | 2026-07-31 | Chunked data + on-demand gen | P1-WORLD-002 |
| 2. Collider cost | P1 | VB | 2026-07-31 | Per-chunk colliders | P1-COLL-001 |
| 3. Lighting perf | P2 | VB | 2026-07-31 | Chunk-bounded BFS | P2-LIGHT-001 |
| 4. Fluid cost | P2 | VB | 2026-07-31 | Active-cell CA + wake/sleep | P2-FLUID-001 |
| 5. Save bloat | P2 | VB | 2026-07-31 | Seed + dirty diffs | P2-SAVE-001 |
| 6. Multiplayer cheat | P3 | VB | 2026-08-31 | Server-authoritative deltas | P3-NET-001 |

---

## Spike candidates

The following spikes are scheduled to validate mitigations:

1. **P1-WORLD-001/002:** Chunk coordinate conversion and lifecycle (load/visibility/unload).
2. **P1-COLL-001:** Per-chunk collision rules and performance validation.
3. **P1-RENDER-001:** Chunk-local render rebuild and profiler targets.
4. **P1-GEN-001:** Deterministic terrain generation from seed.
5. **P2-LIGHT-001:** Chunk-bounded BFS lighting propagation.
6. **P2-FLUID-001:** Active-cell CA fluid simulation.
7. **P2-SAVE-001:** Chunk-diff save format and round-trip verification.
8. **P3-NET-001:** Server-authoritative tile delta protocol and threat model.

All spikes are tracked in the backlog and linked to their corresponding risks in this document.

---

## Decision log

### 2026-06-29 — Initial risk identification (P0-SPEC-005)

Six high-impact risks identified from architecture review. Mitigations and PoC tasks documented. All risks tracked to P1, P2, or P3 completion with explicit deadlines.

---

## See also

- [00-overview.md](00-overview.md) — Top technical risks (summary).
- [01-architecture.md](01-architecture.md) — System decomposition and ownership rules.
- [Quality Gates](quality-gates.md) — Verification requirements for each risk mitigation.
- [14-roadmap.md](14-roadmap.md) — Phase sequencing and timelines.
