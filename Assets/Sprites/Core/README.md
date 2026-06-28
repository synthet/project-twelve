# Core Prototype Sprites

This folder contains first-party pixel-art sprites for the ProjectTwelve sandbox prototype.

## Import settings

- Sprite type: `Sprite (2D and UI)`.
- Pixels per unit: `16`, so each 16×16 tile occupies one Unity unit.
- Filter mode: Point, preserving pixel-art edges.
- Mipmaps: disabled for gameplay sprites.
- Compression: uncompressed by default to avoid atlas seams and palette artifacts.
- Packing tag: `core`, matching the first-party asset namespace.

## Included sprites

| Sprite | Intended use | Notes |
|--------|--------------|-------|
| `core_tile_dirt_00.png` | Foreground dirt tile | 16×16 terrain tile with subtle texture variation. |
| `core_tile_grass_00.png` | Foreground grass tile | 16×16 terrain tile with grassy top pixels and dirt body. |
| `core_tile_stone_00.png` | Foreground stone tile | 16×16 terrain tile with cooler rock palette. |
| `core_tile_ore_copper_00.png` | Foreground ore tile | 16×16 terrain tile using copper-colored ore clusters. |
| `core_tiles_atlas.png` | Chunk mesh terrain atlas | 64×16 atlas wired to `TileMaterial` and `SandboxChunkRenderer` UVs in dirt, grass, stone, copper ore order. |
| `core_player_idle_00.png` | Player idle prototype | 16×24 transparent character frame for entity rendering experiments. |
| `core_item_pickaxe_copper_icon.png` | Copper pickaxe/item placeholder | 16×16 transparent inventory/world icon placeholder. |
| `core_ui_heart_full.png` | Health UI placeholder | 16×16 transparent heart icon placeholder. |

These sprites are intentionally small, lossless prototype assets. They provide stable filenames and Unity `.meta` files for future importer, atlas, and registry work without changing the current mesh-color terrain renderer.
