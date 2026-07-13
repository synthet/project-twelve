# PixelLab REST API v2 — agent reference

Captured from [https://api.pixellab.ai/v2/llms.txt](https://api.pixellab.ai/v2/llms.txt) (LLM-oriented index). Prefer live MCP tools when connected. Use this reference only when the user explicitly wants HTTP/SDK integration, or when a capability exists on REST but not on the connected MCP server.

## Contract

| Item | Value |
|------|--------|
| Base URL | `https://api.pixellab.ai/v2` |
| Auth | `Authorization: Bearer <token>` (token from [pixellab.ai/account](https://pixellab.ai/account); never commit or log) |
| Async pattern | Most generation endpoints return a job/asset id; poll until ready |
| Machine schema | [OpenAPI](https://api.pixellab.ai/v2/openapi.json) |
| Interactive docs | [api.pixellab.ai/v2/docs](https://api.pixellab.ai/v2/docs) |
| ReDoc ops | [api.pixellab.ai/v2/redoc](https://api.pixellab.ai/v2/redoc) |
| This index | [`GET /llms.txt`](https://api.pixellab.ai/v2/llms.txt) |

Official SDKs: [Python](https://github.com/pixellab-code/pixellab-python) (`pip install pixellab`), [JavaScript](https://github.com/pixellab-code/pixellab-js), [MCP](https://github.com/pixellab-code/pixellab-mcp).

## When to use REST vs MCP

| Situation | Choice |
|-----------|--------|
| Cursor/Claude connected to PixelLab MCP | Use MCP tools (`create_*` / `get_*`); do not guess REST paths |
| HTTP script, CI, or custom batch client | REST v2 + OpenAPI / SDK |
| Capability missing from MCP (e.g. pixflux/pixen image create, inpaint, resize, remove-bg, skeleton animate) | REST v2 with explicit user approval |
| MCP temporarily disconnected | Report outage; do **not** silently switch to curl unless the user asks for HTTP |

Background job status (generic): `GET /background-jobs/{job_id}`.

## MCP ↔ REST map (common)

Same product surface; names differ. Prefer MCP tool names in agent sessions.

| MCP-oriented workflow | REST endpoints (v2) |
|-----------------------|---------------------|
| Balance | `GET /balance` |
| Characters create | `POST /create-character-with-4-directions`, `/create-character-with-8-directions`, `/create-character-pro`, `/create-character-v3` |
| Character state | `POST /create-character-state` |
| Character animate | `POST /animate-character`, `POST /characters/animations` |
| Character CRUD / zip / tags | `GET|DELETE /characters/{id}`, `GET /characters/{id}/zip`, `PATCH /characters/{id}/tags`, `GET /characters` |
| Top-down tileset | `POST /tilesets`, `POST /create-tileset`, `GET /tilesets`, `GET /tilesets/{id}` |
| Sidescroller tileset | `POST /tilesets-sidescroller`, `POST /create-tileset-sidescroller` |
| Isometric tile | `POST /create-isometric-tile`, `GET /isometric-tiles`, `GET /isometric-tiles/{id}` |
| Tiles pro | `POST /create-tiles-pro`, `GET /tiles-pro/{id}` |
| Map object | `POST /map-objects` |
| 1-/8-direction objects | `POST /create-1-direction-object`, `POST /create-8-direction-object` |
| Object animate / state / review | `POST /objects/{id}/animations`, `/states`, `/select-frames`, `/dismiss-review` |
| Object CRUD / tags | `GET|DELETE /objects/{id}`, `PATCH /objects/{id}/tags`, `GET /objects` |
| UI panel | `POST /create-ui-asset`, `GET /ui-assets`, `GET|DELETE /ui-assets/{id}` |
| Portrait ↔ character (Pro) | `POST /portrait-character-pro` |
| Pixel font (Pro) | `POST /generate-font-pro` |

## REST-heavy / often MCP-absent categories

Use these via OpenAPI when the user needs them and MCP has no matching tool. Confirm cost/subscription implications before Pro routes.

### Create image

- `POST /generate-image-v2` — Generate image (Pro)
- `POST /generate-with-style-v2` — Generate with style (Pro)
- `POST /generate-ui-v2` — Generate UI (Pro)
- `POST /create-image-pixflux` — Create image (pixflux)
- `POST /create-image-pixflux-background` — Create image (pixflux, background)
- `POST /create-image-pixen` — Create image (pixen)
- `POST /create-image-bitforge` — Create image (bitforge)

### Edit / inpaint / image ops

- `POST /edit-images-v2` — Edit images (Pro)
- `POST /edit-image` — Edit image
- `POST /inpaint-v3` — Inpaint (Pro)
- `POST /inpaint` — Inpaint
- `POST /image-to-pixelart` / `POST /image-to-pixelart-pro`
- `POST /resize` — Resize pixel art
- `POST /remove-background`

### Animate (low-level / Pro)

- `POST /edit-animation-v2` — Edit animation (Pro)
- `POST /interpolation-v2` — Interpolate (Pro)
- `POST /transfer-outfit-v2` — Transfer outfit (Pro)
- `POST /animate-with-skeleton`
- `POST /animate-with-text` / `-v2` / `-v3`
- `POST /estimate-skeleton`

### Rotate

- `POST /generate-8-rotations-v2` / `-v3`
- `POST /rotate`

### Enhance prompt

- `POST /enhance-pixen-prompt`
- `POST /enhance-character-v3-prompt`
- `POST /enhance-animation-v3-prompt`

## Full endpoint index (from llms.txt)

Grouped as published. Each operation has a ReDoc anchor under `https://api.pixellab.ai/v2/redoc#operation/<id>`.

### Account

- `GET /balance` — Get balance

### Animate

- `POST /edit-animation-v2` — Edit animation (Pro)
- `POST /interpolation-v2` — Interpolate (Pro)
- `POST /transfer-outfit-v2` — Transfer outfit (Pro)
- `POST /animate-with-skeleton` — Animate with skeleton
- `POST /animate-with-text` — Animate with text
- `POST /animate-with-text-v2` — Animate with text (pro)
- `POST /animate-with-text-v3` — Animate with text v3
- `POST /estimate-skeleton` — Estimate skeleton

### Background Jobs

- `GET /background-jobs/{job_id}` — Get background job status

### Character Management

- `GET /characters` — List user's characters
- `GET /characters/{character_id}` — Get character details
- `DELETE /characters/{character_id}` — Delete a character and all associated data
- `GET /characters/{character_id}/zip` — Export character as ZIP
- `PATCH /characters/{character_id}/tags` — Update character tags

### Character from template

- `POST /create-character-with-4-directions` — Create character with 4 directions
- `POST /create-character-with-8-directions` — Create character with 8 directions
- `POST /create-character-pro` — Create character with Pro mode (8 directions)
- `POST /create-character-v3` — Create character with v3 model (8 rotations)
- `POST /characters/animations` — Create Character Animation
- `POST /animate-character` — Animate character

### Characters

- `POST /create-character-state` — Create a state of an existing character

### Create Image

- `POST /generate-image-v2` — Generate image (Pro)
- `POST /generate-with-style-v2` — Generate with style (Pro)
- `POST /generate-ui-v2` — Generate UI (Pro)
- `POST /create-image-pixflux` — Create image (pixflux)
- `POST /create-image-pixflux-background` — Create image (pixflux, background)
- `POST /create-image-pixen` — Create image (pixen)
- `POST /create-image-bitforge` — Create image (bitforge)

### Create map

- `POST /tilesets` — Create a tileset asynchronously
- `GET /tilesets` — List user's tilesets
- `POST /create-tileset` — Create top-down tileset (async processing)
- `GET /tilesets/{tileset_id}` — Get generated tileset by ID
- `POST /tilesets-sidescroller` — Create a sidescroller tileset asynchronously
- `POST /create-tileset-sidescroller` — Create sidescroller tileset (async processing)
- `POST /create-isometric-tile` — Create isometric tile (async processing)
- `GET /isometric-tiles/{tile_id}` — Get generated isometric tile by ID
- `GET /isometric-tiles` — List user's isometric tiles
- `POST /create-tiles-pro` — Create tiles pro (async processing)
- `GET /tiles-pro/{tile_id}` — Get generated tiles pro by ID

### Documentation

- `GET /llms.txt` — Get LLM-friendly API documentation

### Edit

- `POST /edit-images-v2` — Edit images (Pro)
- `POST /edit-image` — Edit image

### Enhance Prompt

- `POST /enhance-pixen-prompt` — Enhance pixen prompt
- `POST /enhance-character-v3-prompt` — Enhance character v3 prompt
- `POST /enhance-animation-v3-prompt` — Enhance animation v3 prompt

### Generate

- `POST /portrait-character-pro` — Portrait ↔ character (Pro)
- `POST /generate-font-pro` — Generate pixel font (Pro)

### Image Operations

- `POST /image-to-pixelart` — Convert image to pixel art
- `POST /image-to-pixelart-pro` — Convert image to pixel art (Pro)
- `POST /resize` — Resize pixel art image
- `POST /remove-background` — Remove background

### Inpaint

- `POST /inpaint-v3` — Inpaint image (Pro)
- `POST /inpaint` — Inpaint image

### Map Objects

- `POST /map-objects` — Create map object

### Object Management

- `GET /objects` — List user's objects
- `GET /objects/{object_id}` — Get object details
- `DELETE /objects/{object_id}` — Delete an object and all associated data
- `PATCH /objects/{object_id}/tags` — Update object tags

### Objects

- `POST /create-1-direction-object` — Create a 1-direction object
- `POST /create-8-direction-object` — Create an 8-direction object
- `POST /objects/{object_id}/animations` — Add an animation to an existing object
- `POST /objects/{object_id}/states` — Create a state of an existing object
- `POST /objects/{object_id}/select-frames` — Promote selected frames of a review object to completed objects
- `POST /objects/{object_id}/dismiss-review` — Dismiss a review object without saving any frames

### Rotate

- `POST /generate-8-rotations-v2` — Generate 8 rotations (Pro)
- `POST /generate-8-rotations-v3` — Generate 8 rotations v3
- `POST /rotate` — Rotate character or object

### UI Creator

- `POST /create-ui-asset` — Create UI panel (Pro)
- `GET /ui-assets` — List UI assets
- `GET /ui-assets/{ui_asset_id}` — Get UI asset
- `DELETE /ui-assets/{ui_asset_id}` — Delete UI asset

## Guides (human docs)

- [Getting started](https://www.pixellab.ai/docs/getting-started)
- [Rotating a character](https://www.pixellab.ai/docs/guides/rotating-a-character)
- [Map tiles](https://www.pixellab.ai/docs/guides/map-tiles)

## Refresh policy

`llms.txt` is an index only — request/response shapes and enums live in OpenAPI. When regenerating this file:

1. Fetch `https://api.pixellab.ai/v2/llms.txt`.
2. Diff endpoint groups against this reference.
3. Keep the MCP-first policy and secret-handling notes above.
4. Re-run `python scripts/sync_assistant_trees.py` after editing under `.claude/skills/`.
