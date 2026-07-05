---
type: Reference
title: Liquids
description: Grid cellular-automaton liquid contract — pressure flow rules, active-set sleep/wake, per-tick budget, and adopted P2-FLUID-001 constants.
resource: wiki/08-liquids.md
tags: [docs, wiki, fluids, simulation, p2]
timestamp: 2026-07-04T00:00:00Z
okf_version: 0.1
---

# 08 — Liquids

> **Status:** Specified and implemented for water (P2-FLUID-001) under
> `Assets/Scripts/Sandbox/Fluid/`, with EditMode fixtures; profiler capture and the fluid render
> overlay are pending.
> **Decisions:** Grid **cellular automaton** with a per-cell fluid **amount (0.0–1.0)**; simulate
> only **active cells**; pause distant chunks.
> **Invariants:** Fluid is mass-conserving within tolerance; only awake cells consume CPU.

## Model

Each tile stores `fluid ∈ [0.0, 1.0]` = how full it is (see [Data Models](02-data-models.md)).
An **active set** tracks cells that changed last tick (and their neighbors); the simulation only
visits those, so still water costs nothing.

Liquid types (water, lava, …) differ by parameters: flow rate, viscosity, and interactions (lava +
water → obsidian/stone; lava ignites; etc.). The flow rules below are shared.

## Rule order (pressure model)

Apply per tick, typically bottom-up, several iterations per frame for smoother motion:

1. **Flow down.** If the cell below can hold more, push fluid down (up to what gravity/capacity
   allows, e.g. `transfer = (cur + below - maxFill) / 2` style balancing). Liquid falls until blocked.
2. **Flow sideways.** With remaining fluid, if below is full/blocked, equalize into left/right
   neighbors that aren't full; split when both are open.
3. **Flow up (pressure).** If a cell still holds more than max after 1–2, it's pressurized and
   pushes the excess into the cell above. This is what lets water rise to its source level in
   U-shaped channels.

```text
for each active cell c (bottom-up):
    if solid(c): continue
    // 1. down
    flow = min(c.fluid, capacity(below) )
    move flow c -> below; wake(below)
    // 2. sideways
    balance c with left/right toward equal fill; wake neighbors that changed
    // 3. up (pressure)
    if c.fluid > maxFill: move excess c -> above; wake(above)
    if c changed this tick: keep c awake next tick, else sleep it
```

## Activity management

- A cell sleeps when its fluid is stable (no transfer above an epsilon for a tick).
- Tile edits, neighbor changes, and new fluid sources **wake** cells (part of the tile-edit flow,
  see [Architecture](01-architecture.md)).
- Use a small epsilon to settle near-equal levels, or shallow puddles jitter forever.
- **Pause fluids in distant/unloaded chunks** (LOD, see [Chunking](03-chunking.md)); on resume,
  re-wake border cells so flow continues consistently. Persist `fluid` for paused chunks if exact
  resume state matters (see [Saving & Loading](11-saving-loading.md)).

## Rendering

- Draw fill height proportional to `fluid` (partial tile), with transparency by type.
- A common polish: render a cell that just received downward flow as visually full to hide the
  one-frame gap as a column falls.
- Liquids are their own overlay layer above terrain (see [Rendering](04-rendering.md)).

## Alternatives

- **Discrete "sand-like" CA:** cells are full or empty; fall if below empty, else spread sideways.
  Simpler, but pools look blocky and pressure/equalization is lost. Fine for a prototype.
- **Unit-exchange CA:** capacity-1 cells exchanging discrete units between neighbors.

The pressure-amount model is the recommended target; a discrete CA is an acceptable first cut.

## Pitfalls

- **Simulating all cells** instead of the active set — the main performance trap.
- **Mass leaks/gains** from asymmetric transfer math — test conservation (see [Testing](13-tooling-testing.md)).
- **Order bias** (always L-to-R) causing directional drift — randomize or alternate scan order.
- **Inconsistent resume** of paused chunks — re-wake borders and persist amounts when needed.

## P2-FLUID-001 specification (adopted)

This section is the normative contract implemented by `Assets/Scripts/Sandbox/Fluid/`. Water is the
only liquid in scope; lava and fluid interactions are recorded follow-ups.

### Adopted model — finite-water pressure cells

The pressure model above is realised with the **finite-water-cells** transfer function, a
well-known formulation of the same down → sideways → pressure-up rules that is mass-conserving by
construction (every transfer subtracts from the source exactly what it adds to the destination).
A cell may transiently hold **more than `MaxFill`**; the excess is what a stacked column pushes
upward, and it is what makes water rise to its source level in a U-tube.

For any vertical pair, the **lower** cell's stable amount is:

```text
StableLower(total):                      // total = lower.fluid + upper.fluid
    if total <= MaxFill:                  return total                                   // all fits below
    if total <  2*MaxFill + MaxCompress:  return (MaxFill² + total*MaxCompress)          // partly compressed
                                                 / (MaxFill + MaxCompress)
    else:                                 return (total + MaxCompress) / 2                // both compressed
```

Per active cell, bottom-up:

1. **Down** — move `StableLower(fluid[c] + fluid[below]) − fluid[below]` from `c` into the cell
   below, clamped to `[0, min(fluid[c], MaxTransferPerTick)]`.
2. **Sideways** — with the remaining fluid, equalize toward each open horizontal neighbour by
   `(fluid[c] − fluid[n]) / 4` (dampened to avoid oscillation), clamped the same way; both
   neighbours are visited so an open pair splits.
3. **Pressure-up** — if `c` still holds more than its stable share of the pair with the cell above
   (`fluid[c] − StableLower(fluid[c] + fluid[above])`), push that excess up.

### Scan order and determinism

Cells are processed **bottom-up by row** (ascending `y`). The **left↔right** direction of each row
is chosen from a seeded PRNG (`hash(seed, tick, y)`), so a run is fully reproducible (same world +
seed + tick count ⇒ identical field) while directional bias is averaged out across rows. No
mass-dropping is performed inside the tick — sub-epsilon amounts are left in place rather than
discarded, so mass conservation is exact within float tolerance. Setting a solid tile over a fluid
cell is the only implicit sink and is treated as an explicit edit.

### Active set, wake/sleep, and budget

- A cell **sleeps** when its net transfer for the tick is below `SettleEpsilon`; every transfer at
  or above `SettleEpsilon` re-wakes both endpoints for the next tick, so flow propagates and a
  settled lake performs no work (empty active set).
- Tile edits wake the edited cell and its four neighbours through the `SetTile` flow
  (`SandboxWorld.TileFluidWakeRequested`), so carving a channel under a pool resumes flow.
- Distant/unloaded chunks are impassable: fluid never flows into an unloaded cell, so simulation
  pauses at the loaded-set border and re-wakes there on reload.
- Each tick processes at most `MaxActiveCellsPerTick` cells (deterministic sorted order); the
  overflow is carried to the next tick rather than blowing the frame budget.

### Constants (`SandboxFluidConstants`)

| Constant | Value | Meaning |
|----------|-------|---------|
| `MaxFill` | `1.0` | Nominal full cell (uncompressed). |
| `MaxCompression` | `0.02` | Extra fill a cell accepts per pressure unit; drives U-tube rise. |
| `MaxTransferPerTick` | `1.0` | Max fluid moved across one edge per tick (flow rate). |
| `SettleEpsilon` | `0.0001` | Net change below this sleeps the cell. |
| `MinVisibleFill` | `0.05` | Below this a cell renders as empty (render-only threshold). |
| `MaxActiveCellsPerTick` | `4096` | Per-tick active-cell budget; overflow defers to next tick. |

### Saves

`fluid` amounts are not cheaply recomputable, so they must persist for dirty chunks. Persistence is
owned by [P2-SAVE-001](tickets/p2-save-001-specify-save-load-format-using-seed-plus-dirty-chunk-diffs.md);
until it lands, `SandboxWorld.SetTileFluid` deliberately does **not** flag chunks dirty (it would
balloon saves), and fluid state is transient across save/load. This is the one recorded follow-up
coupling.

## See also

- [Data Models](02-data-models.md) — the `fluid` field.
- [Chunking](03-chunking.md) — pausing/LOD for distant fluids.
- [Procedural Generation](07-procedural-generation.md) — initial lakes/lava placement.
