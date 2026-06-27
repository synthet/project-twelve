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

## See also

- [Data Models](02-data-models.md) — the `fluid` field.
- [Chunking](03-chunking.md) — pausing/LOD for distant fluids.
- [Procedural Generation](07-procedural-generation.md) — initial lakes/lava placement.
