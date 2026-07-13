# PixelLab MCP — tools overview

Condensed from PixelLab's [live MCP tool guide](https://api.pixellab.ai/mcp/docs), generated from the server definitions. Treat live tool schemas as authoritative when they differ from this reference. Use MCP tools when connected; REST v2 is secondary and requires an explicit HTTP-integration request.

## Non-blocking job pattern

```python
# 1. Create (returns immediately)
result = create_character(description="brave knight", n_directions=8, size=48)
character_id = result.character_id

# 2. Queue animations without waiting
animate_character(character_id, template_animation_id="walking")

# 3. Poll until complete
status = get_character(character_id)
# → download URLs when completed
```

Do not busy-loop. Poll after the tool's ETA. Treat returned UUID download URLs as bearer-capability links and keep them out of commits, tickets, and durable logs.

## Character and animation

| Tool | Purpose |
|------|---------|
| `create_character` | Queue character; returns `character_id` |
| `create_character_state` | Variant of existing character (auto-waits up to 30s for source) |
| `animate_character` | Queue animation (~2–4 min); mode auto-detected from `template_animation_id` |
| `get_character` | Status, rotation URLs, animations, download link |
| `list_characters` | Compact list; use `get_character` for details |
| `delete_character` | Permanent; requires `confirm=true` |
| `create_portrait_character` | Portrait ↔ sprite conversion (pro) |
| `get_portrait_character` | Portrait job status |

**Key params — `create_character`:**

- `mode`: `standard` (default, 1 generation) · `pro` (20–40 generations, always 8 directions, ignores style/proportions) · `v3` (2–9 generations, always 8 directions, highest quality). **Mode drives cost — do not use `pro`/`v3` without the user asking for higher quality.**
- `body_type`: `humanoid` (default) or `quadruped` (requires `template`: bear, cat, dog, horse, lion)
- `n_directions`: `4` (cardinal) or `8` (full rotation); ignored in `pro`/`v3` (always 8)
- `size`: **character** size in pixels, 16–128 (default 48). Canvas comes back ~40% larger to leave room for animation (48px character → ~68px canvas), so do not pass the canvas size you want.
- `view`: `low top-down` (default, 3/4 RPG) · `high top-down` · `side` (eye-level; use for ProjectTwelve sidescroller work) · `oblique` (beta)
- `proportions`: preset JSON — default, chibi, cartoon, stylized, realistic_male, realistic_female, heroic (humanoid only; ignored in `pro`/`v3`)

**Key params — `animate_character`:**

- `template_animation_id`: preset skeleton (`walk`, `walking`, `running-8-frames`, `jumping-1`, `crouching`, `fireball`, …). Call `get_character` to list what a given character supports. 1 generation/direction; frame count fixed by the template.
- `action_description`: custom motion (`"walking stealthily"`). Describe **movement only** — no locations or props.
- `mode`: auto-detected — `template` when `template_animation_id` is set, else `v3`. Override only deliberately.
- `frame_count`: 4–16, must be even (`v3` only).
- `directions`: template mode animates **all** character directions; `v3` defaults to **south only**. Pass `directions` explicitly when you need more.

**Cost escalation** — start cheap, only climb if the result is poor (`delete_animation` first, then retry):

1. `template` — 1 gen/direction, standard walk/run/idle.
2. `v3` — 1 gen/direction, custom `action_description`, cheap to re-roll.
3. `pro` — **20–40 gen/direction.** Call once *without* `confirm_cost` to get the quoted price, show it to the user, and only re-call with `confirm_cost=true` after explicit approval. Never set `confirm_cost=true` on a first call.

## Sidescroller tilesets (2D platformer)

Designed for side-view games with transparent backgrounds. **Preferred for ProjectTwelve terrain experiments.**

| Tool | Purpose |
|------|---------|
| `create_sidescroller_tileset` | 16-tile platform set |
| `get_sidescroller_tileset` | Status, download links, tile count |
| `list_sidescroller_tilesets` | List owned tilesets |
| `delete_sidescroller_tileset` | Permanently delete by ID |

**Chaining example:**

```python
stone = create_sidescroller_tileset(
    lower_description="stone brick",
    transition_description="moss and vines"
)
wood = create_sidescroller_tileset(
    lower_description="wooden planks",
    transition_description="grass",
    base_tile_id=stone.base_tile_id
)
```

**Key params:**

- `lower_description` (**required**): platform material (stone, wood, metal, ice)
- `transition_description` (**required**): top decoration (grass, snow, moss) — pass `transition_size: 0` if you want no surface layer
- `transition_size`: 0 = none, 0.25 = light, 0.5 = heavy
- `tile_size`: default `{"width": 16, "height": 16}` (16 or 32) — 16×16 matches ProjectTwelve's tile grid
- `base_tile_id`: from previous tileset for visual consistency

## Top-down Wang tilesets

16 tiles (25 at `transition_size=1.0`) for corner-based autotiling.

| Tool | Purpose |
|------|---------|
| `create_topdown_tileset` | Wang tileset between two terrains |
| `get_topdown_tileset` | Status, download, base tile IDs for chaining |
| `list_topdown_tilesets` | List owned tilesets |
| `delete_topdown_tileset` | Permanently delete by ID |

**Chaining example:**

```python
t1 = create_topdown_tileset("ocean water", "sandy beach")
t2 = create_topdown_tileset("sandy beach", "green grass",
                           lower_base_tile_id=t1.beach_base_id)
```

**Key params:**

- `transition_size`: 0 = sharp, 0.25 = medium, 0.5 = wide, 1.0 = full-tile cliff (yields 25 tiles instead of 16)
- `view`: `high top-down` (RTS, default) or `low top-down` (RPG)
- `mode`: `standard` (16 or 32 px tiles) or `pro` (adds 64 px plus `spread_x` / `slope_size` / `raggedness` shape controls; experimental)
- `lower_base_tile_id` / `upper_base_tile_id`: chain tilesets

## Isometric tiles

| Tool | Purpose |
|------|---------|
| `create_isometric_tile` | Single 3D-looking tile (~10–20s) |
| `get_isometric_tile` | Status and image |
| `list_isometric_tiles` | List owned tiles |
| `delete_isometric_tile` | Permanently delete by ID |

**Key params:** `size` (16–64; >24 gives noticeably better results), `tile_shape` — exact enum strings `"thin tile"` (~10% canvas height) / `"thick tile"` (~25%) / `"block"` (~50%, default) — plus `outline`, `detail`, `seed` (reuse a seed across a tile set for consistency)

## Map objects and multi-direction objects

| Tool | Purpose |
|------|---------|
| `create_map_object` | Single object, transparent BG (~15–30s); **expires in 8h** |
| `get_map_object` | Status and image |
| `create_1_direction_object` | Single-view object batch (~30–90s) |
| `create_8_direction_object` | 8-angle object (~2–4 min) |
| `get_object` | Full object details (processing / review / completed / failed) |
| `list_objects` | List with status filter |
| `animate_object` | Add animation to object |
| `create_object_state` | Variant of existing object |
| `select_object_frames` | Promote review candidates |
| `dismiss_review` | Discard review object |
| `delete_object` | Permanent; requires confirm |

**Key params — `create_map_object`:**

- **Basic mode** (no `background_image`): `width` and `height` are **required**, 32–400 px each.
- **Style-matching mode**: pass `background_image` to blend with existing map/HUD art; `width`/`height` are then auto-detected from it, and the canvas is capped at **192×192**.
- `background_image` must be JSON `{"type": "base64", "base64": "..."}`. **File paths are not supported** — the server cannot read local files, so base64-encode the PNG yourself.
- `inpainting`: `{"type": "oval", "fraction": 0.3}` · `{"type": "rectangle", "fraction": 0.5}` · `{"type": "mask", "mask_image": "base64..."}`. Defaults to oval 0.6 when `background_image` is given. Mask convention: **white = AI generates, black = preserved**.

Map objects expire after eight hours; download completed results promptly.

## UI assets

| Tool | Purpose |
|------|---------|
| `create_ui_asset` | Pixel UI panel (~30–90s); returns `ui_asset_id` |
| `get_ui_asset` | Status, image URL, download |
| `list_ui_assets` | List owned panels |
| `delete_ui_asset` | Permanent delete; requires `confirm=true` |

**Key params:** `width`, `height`, `description`, `no_background` (default true), `color_palette`, `pieces`

## Utility and meta

| Tool | Purpose |
|------|---------|
| `agent_help` | Ask PixelLab docs agent |
| `agent_feedback` | Report tool issues externally; ask first |
| `get_balance` | Account credits / subscription |
| `delete_animation` | Remove character/object animation |

## Pro / advanced

| Tool | Purpose |
|------|---------|
| `create_tiles_pro` / `get_tiles_pro` | Pro tile generation |
| `create_font` / `get_font` | Pixel font generation |
| `chat_send_message` | PixelLab game-building agent (blocks up to 10 min) |
| `chat_list_conversations` / `chat_get_messages` | Chat history |
| `sandbox_*` | Remote Node.js/TypeScript environment with shell, file writes, sync, and deployment capability |
| `agent_list` / `agent_inspect` / `agent_talk` | Deployed-agent debugging; inspection may expose live traces and memory |

Use `chat_*`, `sandbox_*`, and deployed-agent tools only after the user explicitly requests those broader workflows. Ordinary asset generation does not authorize remote shell commands, deployment/sync, messaging an external game-building agent, or inspecting agent traces.

## MCP documentation resources

Fetch via MCP resources when available:

| URI | Topic |
|-----|-------|
| `pixellab://docs/overview` | Platform overview |
| `pixellab://docs/python/wang-tilesets` | Wang tilesets (Python) |
| `pixellab://docs/python/sidescroller-tilesets` | Sidescroller tilesets (Python) |
| `pixellab://docs/unity/isometric-tilemaps-2d` | Unity 2D isometric tilemaps |
| `pixellab://docs/godot/wang-tilesets` | Godot Wang tilesets |
| `pixellab://docs/godot/sidescroller-tilesets` | Godot sidescroller tilesets |
| `pixellab://docs/godot/isometric-tiles` | Godot isometric tiles |

## REST v2 fallback

Local agent catalog (from PixelLab `llms.txt`): [api-v2-llms.md](api-v2-llms.md).

Upstream: [https://api.pixellab.ai/v2/llms.txt](https://api.pixellab.ai/v2/llms.txt) · [OpenAPI](https://api.pixellab.ai/v2/openapi.json) · [Interactive docs](https://api.pixellab.ai/v2/docs).

Use REST for code-level batch/custom integrations or endpoints not exposed as MCP tools (image create/edit, inpaint, resize, remove-bg, skeleton animate, etc.). Do not improvise REST calls merely because the MCP server is temporarily disconnected.

## Support

- Setup: [pixellab.ai/mcp](https://www.pixellab.ai/mcp)
- Discord: [discord.gg/pBeyTBF8T7](https://discord.gg/pBeyTBF8T7)
