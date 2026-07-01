# Roadmap and Tasks

This page keeps near-term prototype work in an open knowledge format: milestones describe outcomes, tasks define scoped implementation work, and validation notes explain how contributors can confirm completion.

## Milestone 1: Barebone Sandbox

**Outcome:** The repository remains focused on the playable 2D sandbox prototype.

- Keep only sandbox-relevant scripts and documentation.
- Maintain a minimal scene without unrelated demo objects.
- Verify chunk generation, rendering, collision, movement, and editing compile in Unity.

**Validation notes**

- Open `Assets/Scene.unity` in Unity Editor 6.0.5.1f1.
- Run batch-mode project validation when Unity is available.
- Confirm no unrelated demo assets or generated scene artifacts are introduced.

## Milestone 2: Prototype Hardening

**Outcome:** Core world operations are testable and stable under common edit scenarios.

| Task | Scope | Acceptance criteria |
|------|-------|---------------------|
| Coordinate conversion tests | Cover world-to-chunk and world-to-local conversion, including negative coordinates. | Tests demonstrate expected chunk/local coordinates for positive, zero, and negative positions. |
| Chunk dirty-state tests | Verify tile edits set render and collider dirty flags independently. | Dirty flags are set by edits and cleared by the relevant rebuild path. |
| Generation determinism tests | Verify identical seeds/settings produce identical chunk data. | Repeated generation of the same chunk produces equivalent tile IDs and state. |
| Border edit propagation | Dirty neighboring chunks when edits happen on chunk borders. | Editing a border tile causes affected adjacent chunk render/collider state to rebuild. |
| Collider strategy review | Compare many `BoxCollider2D` instances, merged chunk colliders, and manual collision. | A documented decision identifies the next collision approach and profiling target. |
| Visual EditMode tests | Cover `CharacterSheetLayout` clip keys and autotile resolver invariants. | P1-VISUAL-002; tests run without licensed art fixtures. |

## Milestone 3: Content Foundation

**Outcome:** Tiles, items, and player interactions can move from hard-coded prototype IDs toward data-driven content.

| Task | Scope | Acceptance criteria |
|------|-------|---------------------|
| Tile registry | Introduce stable tile definitions keyed by string IDs. | Runtime tile IDs resolve through a registry without breaking existing chunk storage. |
| Item registry | Define item definitions for pickups, inventory entries, and placement behavior. | Items can reference placeable tiles and display metadata from definitions. |
| Texture atlas rendering | Replace vertex-color-only tiles with atlas-backed tile visuals. | Chunk rendering can select tile UVs from registered tile definitions. |
| Visual catalog pipeline | Document submodule catalog generation and `LocalImportConfig` contract. | P2-VISUAL-001; regen workflow documented and reproducible. |
| Player avatar integration | Composed avatar in play mode with locomotion driven from controller. | P1-VISUAL-001; QA checklist includes autotiles and avatar. |
| Extended character presentation | VFX, firearms, Walk vs Run, combat triggers. | P2-VISUAL-002 spec closes gaps vs vendor reference behavior. |
| Inventory-backed edits | Gate placement/removal through inventory state. | Player placement consumes inventory where appropriate; removal creates pickup or inventory output. |

## Knowledge-page maintenance

When a task changes architecture or subsystem responsibilities, update the relevant wiki page in the same change. Prefer durable task descriptions and acceptance criteria over prompt templates so the roadmap stays useful to any contributor or tool.
