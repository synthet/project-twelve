---
type: Reference
title: Liquids
description: Compressible-water cellular-automaton contract — active-set flow, mass conservation, wake-on-edit, chunk pause, per-tick budget, and save persistence.
resource: wiki/08-liquids.md
tags: [docs, wiki, fluids, simulation, p2]
timestamp: 2026-07-04T00:00:00Z
okf_version: 0.1
---

# 08 — Liquids

> **Status:** Planning.
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

## P2-FLUID-001 specification (water)

This section is the authoritative contract for the first liquid pass
([P2-FLUID-001 ticket](tickets/p2-fluid-001-specify-simple-liquid-simulation-constraints.md)).
Water is the only liquid in scope; lava and fluid-type interactions are recorded follow-ups.
The **pressure-amount model** above is adopted; a discrete CA is not used.

### Data source of truth

- Per-cell fill amount is `SandboxTile.fluid ∈ [0.0, ~1.02]`. It reads `0` for air with no water and
  may exceed `maxFill` for pressurized (compressed) cells — that overfill is what lets water rise to
  its source level in U-shaped channels.
- A cell is **solid** when `SandboxTile.IsSolid`; solid cells hold no fluid and are impassable to flow.
- A cell is **renderable fluid** when `fluid > minVisibleFill`. Rendering never changes simulation state.
- Simulation, collision, and edits all read the same tile grid — there is no parallel fluid mask.

### Model and constants

Flow is a compressible-water cellular automaton over **active cells only** (see
`SandboxFluidSimulator`). Values are named constants (`SandboxFluidConstants`), never inline literals;
a test guard pins the code to this table.

| Constant | Value | Meaning |
|----------|-------|---------|
| `maxFill` | `1.0` | Nominal full cell; equilibrium target for an uncompressed cell |
| `maxCompress` | `0.02` | Extra mass a cell holds per stacked cell of head above it (drives pressure / U-tube rise) |
| `minFlow` | `0.01` | Below this a transfer skips the 0.5 damping factor, so thin films still drain instead of creeping |
| `maxTransferPerTick` | `1.0` | Cap on a single down/up transfer per cell per tick (flow rate) |
| `settleEpsilon` | `1e-4` | A cell whose net movement this tick is below this **sleeps** (leaves the active set) |
| `minVisibleFill` | `0.02` | Render-only threshold for drawing a partial-fill cell; unused by sim math |
| `iterationsPerFrame` | `2` | Simulation ticks advanced per rendered frame for smoother motion |
| `maxActiveCellsPerTick` | `4096` | Per-tick active-cell budget; excess cells stay awake and are processed next tick |

**Equilibrium (`StableStateBelow(total)`)** — the mass the lower of two vertically stacked cells
holds at rest given their combined mass `total`:

```text
total ≤ maxFill                       → maxFill
total < 2·maxFill + maxCompress       → (maxFill² + total·maxCompress) / (maxFill + maxCompress)
otherwise                             → (total + maxCompress) / 2
```

### Tick procedure (normative)

One tick reads a **pre-tick snapshot** of the active cells and accumulates every transfer into a delta
buffer that is applied at the end. Because both sideways transfers are computed from the same snapshot
and every transfer subtracts from the source and adds the identical amount to the destination, the tick
is **order-independent and exactly mass-conserving** — there is no directional (L↔R) drift and no need
for randomized scan order. Active cells are processed bottom-up (row ascending, then column) only so the
per-tick **budget** truncates deterministically.

```text
for each active cell c (bottom-up, budget-capped; deferred cells stay awake):
    if solid(c) or fluid(c) ≤ 0: continue
    remaining = fluid(c)
    # 1. down (gravity)
    if !solid(below):  flow = StableStateBelow(remaining + fluid(below)) − fluid(below)
                       damp; transfer down (cap maxTransferPerTick)
    # 2. sideways (symmetric split toward equal fill)
    if !solid(left):   flow = (fluid(c) − fluid(left))  / 4; damp; transfer left
    if !solid(right):  flow = (fluid(c) − fluid(right)) / 4; damp; transfer right
    # 3. up (pressure)
    if !solid(above):  flow = remaining − StableStateBelow(remaining + fluid(above))
                       damp; transfer up (cap maxTransferPerTick)
    # damp: if flow > minFlow, flow *= 0.5
    # every transfer ≥ settleEpsilon re-wakes source + destination for next tick
    keep c awake next tick iff it moved ≥ settleEpsilon, else sleep
```

### Active set, wake, and determinism

- The simulator owns an **active set**; a settled lake has an empty active set and costs ~zero per tick.
- A cell (and its 4-neighbourhood) is woken by: an explicit source/sink (`AddFluid`), a tile edit at or
  adjacent to it, and any transfer ≥ `settleEpsilon` it participates in.
- **Determinism:** identical initial state ⇒ identical field after N ticks (order-independent math). The
  budget truncation order is fixed, so determinism holds under a budget cap as well.

### Tile-edit flow (wake wiring)

Wake is wired into the single `SandboxWorld.SetTile` choke point (P1-EDIT-001):

1. Placing a **solid** tile into a fluid cell removes that cell's fluid — an explicit **sink** (blocks
   displace water in this pass; volume-preserving displacement is a follow-up).
2. Digging a solid tile to **air** opens an empty cell.
3. Either way the edited cell and its neighbours are woken so surrounding fluid re-flows on the next tick.
   Edits are the only sanctioned way total mass changes; between edits mass is conserved within
   `~1e-9` (see conservation test).

### Chunk lifecycle

- Fluids simulate only in **loaded** chunks (renderer-backed window); active cells in unloaded chunks are
  dropped from the set, so distant water is free.
- On chunk **load/resume**, border cells of the newly loaded chunk are re-woken so flow continues across
  the seam consistently.
- `fluid` amounts **persist** for dirty chunks (unlike light, fluid is not cheaply recomputable). The
  field rides in the existing per-tile save record; coordinate versioning with
  [P2-SAVE-001](tickets/p2-save-001-specify-save-load-format-using-seed-plus-dirty-chunk-diffs.md).

### Rendering contract (minimal)

Fill height is proportional to `fluid` (clamped to `[0,1]` for display) on a liquids overlay layer
above terrain, drawn only for cells with `fluid > minVisibleFill`. Transparency/type tint and the
"just-received-downflow renders full" polish are **out of scope**; the overlay renderer itself is built
in the Unity play-mode pass.

### Performance target

The active-set step stays under the `docs/wiki/quality-gates.md` budget (< 3 ms) on representative
content; `maxActiveCellsPerTick` defers overflow to the next tick rather than blowing the frame, and a
settled lake performs no work.

### Out of scope (recorded follow-ups)

- Lava, viscosity, and fluid-type interactions (lava + water → obsidian/stone).
- Fluid gameplay (drowning, buoyancy, swimming hooks) and polished fluid rendering.
- Volume-preserving displacement when a solid is placed into water.

### EditMode verification fixtures (required before close)

Pure-data tests on handcrafted grids via an in-memory `ISandboxFluidGrid` (no scene):

- **Conservation** — random terrain + random drops, N ticks, total mass constant within epsilon.
- **Settling** — a poured column in a closed basin reaches a flat surface and the active set empties.
- **U-tube** — filling one shaft of a connected pair raises the other via the pressure rule.
- **Determinism** — identical initial state ⇒ identical field after N ticks.
- **Wake** — opening a tile under a settled pool wakes exactly the affected cells and flow resumes.
- **Budget** — a small `maxActiveCellsPerTick` still conserves mass (deferred cells keep their fluid).
- **Constants guard** — `SandboxFluidConstants` values match this table.

Play-mode (exit evidence): digging a channel drains a generated lake convincingly with no frame hitches.

## See also

- [Data Models](02-data-models.md) — the `fluid` field.
- [Chunking](03-chunking.md) — pausing/LOD for distant fluids.
- [Procedural Generation](07-procedural-generation.md) — initial lakes/lava placement.
- [Pathfinding](09-pathfinding.md) — sibling P2 active-set/dirty-version pattern (`SandboxChunk.NavVersion`).
