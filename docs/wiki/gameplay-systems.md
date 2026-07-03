# Gameplay Systems

## Player

The player controller owns movement input and translates tile edit inputs into world requests. Validation such as edit range belongs near the player/controller boundary, while authoritative world mutation belongs in `SandboxWorld` or a future server layer.

### Player presentation

Simulation and visuals are intentionally separated:

| Layer | Component | Responsibility |
|-------|-----------|----------------|
| Simulation | `SandboxPlayerController` | Physics, grounded state, velocity, tile edit requests |
| Presentation | `SandboxPlayerAvatarVisual` | Spawns composed avatar via `PlayerAvatarFactory` |
| Animation bridge | `SandboxPlayerAvatarAnimation` | Maps controller state to `ISandboxPlayerLocomotion` |
| Locomotion API | `CharacterLocomotionDriver` | Drives prefab `Animator` bools/triggers |

Gameplay code must not depend on `SpriteRenderer` bounds or vendor animation scripts. Equipment appearance is data on `CharacterComposer` (layer strings) resolved at compose time — see [Visual integration](visual-integration.md).

## Inventory and Items

Future inventory should be data-driven and registry-backed. Tile placement should consume item stacks after validation. Tool strength, range, cooldown, and tile damage should be item/tool properties rather than hard-coded player logic.

## Enemies and Pathfinding

Enemy behavior for the P2 alpha is specified in [09 — Pathfinding](09-pathfinding.md) §
"P2-AI-001 specification (walker archetype)" and implemented against the shared solidity grid
(`SandboxTile.IsSolid`).

| Layer | Component | Responsibility |
|-------|-----------|----------------|
| Simulation | Walker enemy controller (TBD) | Path follow, local steering fallback, despawn rules |
| Navigation | `GridPathfinder` (TBD) | A* over walk/jump/fall edges with expansion budgets |
| Spawn | Area spawn controller (TBD) | Candidate selection per distance/light/population rules |
| Presentation | `MonsterSpawnHelper` / `MonsterLocomotionDriver` | Visual spawn and animation (P2-VISUAL-003) |

**Nav-dirty:** `SandboxWorld.SetTile` marks affected chunks nav-dirty; agents recompute lazily on
the next step when their path crosses changed cells.

**Spawn constraints:** candidates must be air-with-support inside loaded chunks, outside the camera
view, within `[minSpawnDistance, maxSpawnDistance]` of the player, and respect underground light
thresholds. Never spawn in unloaded chunks.

**Out of scope for P2-AI-001:** combat/damage, flying/swimming archetypes, waypoint-graph scaling.
See the pathfinding page for constants and EditMode fixture requirements.
