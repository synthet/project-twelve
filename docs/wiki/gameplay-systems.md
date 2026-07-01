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

Enemy movement should use chunk-aware navigation. Basic enemies can query solid tiles directly; advanced platformer pathfinding needs jump height, fall distance, doors, ladders, and terrain edits that invalidate paths.
