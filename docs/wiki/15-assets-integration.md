# 15 — Asset Integration Requirements

> **Status:** Planning.
> **Decisions:** Treat sprites, animation clips, and rotation metadata as data-driven content referenced by stable IDs; import first-party pixel art with consistent pixels-per-unit and atlas settings; keep rendering adapters replaceable between Unity Tilemap/SpriteRenderer and the future engine-facing atlas renderer.
> **Invariants:** Asset definitions never become authoritative world state; tile/entity state stores IDs, variants, frame/rotation metadata, and animation state, while render backends resolve those values to Unity assets or engine atlas regions.

This page translates current Unity 2D asset practices into ProjectTwelve requirements for a Terraria-like sandbox. It complements [Rendering](04-rendering.md) and [Modding & Content](12-modding.md): rendering decides how chunks draw, while content definitions decide which sprites, animations, pivots, and rotations a tile/entity can use.

## Research baseline

ProjectTwelve targets Unity 6.0.2+ for the prototype. The external baseline below is from Unity documentation and should be rechecked when upgrading the Unity editor or 2D packages.

| Topic | Unity baseline | ProjectTwelve requirement |
|-------|----------------|----------------------------|
| Sprite import | Unity sprite texture import settings define sprite mode, pixels-per-unit, wrapping, filtering, and related texture behavior; Point filtering preserves pixel-art edges, while Bilinear/Trilinear blur when scaled.[^unity-sprite-import] | First-party pixel art should use a single project PPU convention, Point filter, no mipmaps unless intentionally used for scaled backgrounds, and no lossy compression for sprites that will be atlas-packed or used as tile art. |
| Sprite atlases | Unity Sprite Atlas combines textures into one texture so Unity can issue fewer draw calls and exposes runtime loading control.[^unity-sprite-atlas] | Tiles, walls, liquids, items, and entity sheets should be packed by content domain and render layer, with padding/extrusion to avoid seams. Definitions reference logical sprite IDs, not raw texture paths. |
| Sprite rendering | SpriteRenderer displays a selected sprite, supports draw modes, sorting/layer settings, and flip settings that mirror the texture without moving the GameObject.[^unity-sprite-renderer] | Dynamic entities may use SpriteRenderer/Animator in Unity, but chunk terrain should stay behind `IChunkRenderView` so Tilemap and custom mesh implementations can share the same content definitions. |
| Sprite animations | Unity supports flipbook-style 2D sprite animation clips from sprite sheets and property-keyed animation through the Animation window.[^unity-sprite-animation] | Author animation clips as named states with deterministic frame timing. Runtime simulation stores animation state IDs/timers; Unity animation assets are presentation outputs. |
| Sprite Library / Resolver | Unity 2D Animation's Sprite Resolver selects sprites by category/label from a Sprite Library on the same hierarchy for runtime swaps.[^unity-sprite-resolver] | Use resolver-style category/label concepts for modular equipment, skins, and variants, but keep the project-level identity as registry IDs such as `core:player/torso/iron`. |
| Rigidbody2D rotation | Rigidbody2D owns Transform position/rotation for attached 2D colliders, and colliders on the same Rigidbody2D move/rotate as one compound body.[^unity-rigidbody2d] | Physics-driven actors should rotate through Rigidbody2D APIs/constraints; visual-only rotation and sprite flipping should be isolated to child render objects when collision geometry must remain stable. |

## Asset domains

| Domain | Examples | Unity representation | Engine-facing representation | Notes |
|--------|----------|----------------------|------------------------------|-------|
| Foreground tiles | dirt, stone, ore, crafted blocks | Tile assets or atlas sprites consumed by per-chunk Tilemaps/custom meshes | `TileDef` with `spriteSet`, `solid`, `opaque`, `rotationPolicy`, `variantPolicy` | Tile world state stores runtime tile ID plus compact metadata. |
| Background walls | cave wall, placed wall | Separate Tilemap/mesh layer | Wall definition or tile layer flag | Must not share collision semantics with foreground tiles. |
| Liquids/overlays | water, lava, foam | Animated sprites or shader material per layer | Liquid material/sprite definition with fill and frame rules | Simulation fill level remains data; waves/foam are visual. |
| Items | dropped item, inventory icon | SpriteRenderer/UI Sprite | `ItemDef.iconSprite`, `dropSprite`, optional animation | UI icons may use a different atlas size/PPU than world drops. |
| Entities | player, NPCs, enemies, projectiles | SpriteRenderer + Animator, or 2D Animation rig for complex actors | `EntityVisualDef` with states, clips, pivots, sockets, hitbox references | Gameplay hitboxes must be explicit data, not inferred from opaque pixels. |
| Effects | particles, impact flashes, dust | ParticleSystem, sprite sheet animation, VFX prefab | Effect definition referenced by event ID | Effects are client-side presentation unless gameplay explicitly depends on them. |
| UI | buttons, slots, fonts, icons | UI Sprite, TextMeshPro assets | UI asset IDs | Keep UI assets outside chunk/render atlases when different compression/filtering is needed. |

## Sprite requirements

### Import conventions

- **Pixels per unit:** choose one base PPU for tile-scale art and document it in the first asset-import ticket. Recommended starting point: one tile equals one Unity unit, so a 16×16 tile uses 16 PPU or a 32×32 tile uses 32 PPU. Characters can be larger sheets but should keep the same world PPU so collision and placement math stay intuitive.
- **Filter mode:** pixel-art gameplay sprites should use Point filtering. Background paintings can opt into Bilinear/Trilinear only when authored for scaled, non-tile use.
- **Compression:** avoid lossy compression for source sprites used in atlases. If platform compression is required later, apply it at atlas/platform override level and validate seams, color shifts, and alpha edges.
- **Wrap mode:** tile and entity source sprites should generally clamp. Repeating/tiled behavior should be explicit through Tilemap, mesh UVs, or materials.
- **Pivot policy:** tiles use centered or lower-left pivots consistently per renderer; entities use authored pivots at feet/center of mass, with sockets for hands, tools, muzzle points, and equipment anchors.
- **Naming:** source art should use stable, registry-aligned names (`core.tile.dirt.0`, `core.player.idle.0`) so automated importers can generate definitions and catch missing references.

### Atlas requirements

- Keep atlases split by update/loading needs: `terrain`, `walls`, `liquids`, `items`, `entities`, `ui`, and optional biome/mod atlases.
- Add padding/extrusion around sprites to prevent atlas bleeding, especially for chunk meshes and camera scaling.
- Store atlas references as logical sprite IDs. Unity may resolve them to `Sprite` assets, while a custom engine path resolves them to atlas rects/UVs.
- Validate maximum texture size per target platform before accepting large sheets. Prefer multiple atlases over a single global atlas that forces all content to load together.
- For mods, load Addressables/AssetBundles or validated external textures after definitions, then build or bind runtime atlases before registries freeze.

## Animation requirements

ProjectTwelve needs two animation tracks: a Unity-authoring track for fast iteration and an engine-facing declarative track for portability.

| Requirement | Details |
|-------------|---------|
| Stable state IDs | Use names such as `idle`, `walk`, `jump`, `fall`, `mine`, `swing`, `hurt`, and `die`; content can map these to Unity clips or data clips. |
| Deterministic timing | Define frame duration, loop mode, and transition policy in data so networking/replay can reproduce state changes when needed. |
| Separation from gameplay | Animation events may request effects or sounds, but gameplay-critical tile edits, damage, and item use must be driven by simulation state and validated commands. |
| Direction handling | Prefer separate left/right/up/down frames only when silhouettes differ. Otherwise use flip/rotation policies to reduce art volume. |
| Modular swaps | Equipment and skins should use category/label or socket concepts, resolving through registry IDs instead of directly editing animation clips per item. |
| Bounds and hitboxes | Store hitboxes/hurtboxes/tool reach as explicit data keyed by animation state/frame when needed. Never derive combat or collision from SpriteRenderer bounds alone. |

### Minimum entity visual schema

```text
EntityVisualDef
  id: core:player.visual.default
  basePpu: 32
  defaultFacing: right
  states:
    idle: { clip: core:player.idle, loop: true }
    walk: { clip: core:player.walk, loop: true }
    swing: { clip: core:player.swing.pickaxe, loop: false, eventTrack: core:player.swing.pickaxe.events }
  sockets:
    hand_r: { x: 10, y: 18 }
    hand_l: { x: 6, y: 18 }
  hitboxes:
    standing: { x: -6, y: 0, w: 12, h: 30 }
```

## Rotation and flipping requirements

Rotations affect tiles, entities, projectiles, tools, lighting, and collision differently. They must be modeled explicitly instead of hidden in sprite transforms.

| Use case | Allowed representation | Requirements |
|----------|------------------------|--------------|
| Tile variants | 2-bit or 3-bit metadata for 0/90/180/270 degrees and optional mirror flags | Only for definitions with `rotationPolicy`; update render, collision, and lighting consumers together. |
| Slopes/platforms | Shape ID plus rotation | Collision/pathfinding must consume the same rotated shape as rendering. |
| Entity facing | SpriteRenderer `flipX` or child visual scale/rotation | Keep root physics collider stable unless the body is intentionally rotated. |
| Projectiles/tools | Transform/Rigidbody2D angle plus sprite pivot | Simulation owns angle and velocity; renderer interpolates. |
| Decorative props | Transform rotation or tile metadata | If non-solid, visual-only rotation is acceptable; if solid, collision shape must rotate too. |

Rotation metadata should live beside the tile/entity state rather than inside the asset path. For example, `core:torch` plus metadata `wallMount=east` is preferable to separate authoritative tile IDs for every orientation, unless those orientations have different gameplay rules.

## Unity integration path

1. **Define import presets:** create Unity presets or asset postprocessors for pixel-art sprites once the first real art drop lands.
2. **Add first-party definitions:** introduce `TileDef`, `ItemDef`, and `EntityVisualDef` ScriptableObjects or JSON files with stable IDs and sprite references.
3. **Build core atlases:** pack terrain/wall/liquid/item/entity sprites into Sprite Atlases for the Tilemap/SpriteRenderer prototype.
4. **Bridge to chunk rendering:** update `SandboxChunkRenderer` or the next renderer adapter to resolve tile IDs through a registry instead of hard-coded debug colors.
5. **Add animation adapter:** map entity visual definitions to Unity Animator/SpriteRenderer for the prototype, while preserving declarative animation data for non-Unity logic.
6. **Validate rotation policies:** add tests for tile metadata encoding and renderer/collider agreement before enabling rotatable solid tiles.
7. **Prepare mod content:** document external asset bundle/address rules, namespace validation, atlas limits, and missing-reference behavior.

## Acceptance checklist for first asset drop

- [ ] A documented base tile size and PPU exist.
- [ ] Sprite import preset or importer automation applies Point filtering and expected compression settings.
- [ ] At least one terrain atlas has padding/extrusion and stable logical sprite IDs.
- [ ] `TileDef` includes sprite reference, solidity, opacity, light emission, variant policy, and rotation policy.
- [ ] Entity visuals define states, frame timing, pivots/sockets, and explicit hitboxes.
- [ ] Rotation metadata is specified for any rotatable solid tile, including collision and lighting behavior.
- [ ] Markdown docs and canonical sources are updated when workflow or architecture changes.

## See also

- [Rendering](04-rendering.md) — chunk-local Tilemap/custom-mesh rendering decisions.
- [Data Models](02-data-models.md) — compact tile metadata and runtime IDs.
- [Modding & Content](12-modding.md) — registries, stable string IDs, and external assets.
- [Collision & Physics](05-collision-physics.md) — collider rebuild implications for rotated solids.
- [Multiplayer & Modding](multiplayer-and-modding.md) — content-pack and registry boundaries.

[^unity-sprite-import]: Unity Manual, Sprite texture import settings: <https://docs.unity3d.com/6000.4/Documentation/Manual/texture-type-sprite.html>
[^unity-sprite-atlas]: Unity Manual, Sprite Atlas: <https://docs.unity3d.com/2020.1/Documentation/Manual/class-SpriteAtlas.html>
[^unity-sprite-renderer]: Unity Manual, Sprite Renderer component reference: <https://docs.unity3d.com/6000.0/Documentation/Manual/sprite/renderer/sprite-renderer-reference.html>
[^unity-sprite-animation]: Unity Learn, Introduction to Sprite Animations: <https://learn.unity.com/tutorial/introduction-to-sprite-animations>
[^unity-sprite-resolver]: Unity 2D Animation package manual, Sprite Resolver component: <https://docs.unity3d.com/Packages/com.unity.2d.animation%403.0/manual/SRComponent.html>
[^unity-rigidbody2d]: Unity Manual, Introduction to Rigidbody 2D: <https://docs.unity3d.com/6000.5/Documentation/Manual/2d-physics/rigidbody/introduction-to-rigidbody-2d.html>
