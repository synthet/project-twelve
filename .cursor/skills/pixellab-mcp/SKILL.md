---
name: pixellab-mcp
description: Generate and manage pixel-art game assets through PixelLab's MCP server, including characters, animations, sidescroller and top-down tilesets, isometric tiles, map objects, UI panels, and fonts. Use when the user asks for PixelLab/Vibe Coding, pixel-art sprites or animations, game tilesets, HUD art, or other PixelLab-generated assets; also use to poll, download, review, list, or delete PixelLab jobs and assets.
capability: "pixellab-mcp agent asset workflow"
side_effect_level: read_only
approval_required: false
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: low
---

# PixelLab MCP

Use PixelLab's MCP tools for asset generation. Do not guess REST endpoints or replace an available MCP tool with `curl`.

Official sources:

- [Vibe Coding setup](https://www.pixellab.ai/mcp)
- [Live MCP tool guide](https://api.pixellab.ai/mcp/docs)
- [REST API v2 llms.txt](https://api.pixellab.ai/v2/llms.txt) — local copy: [references/api-v2-llms.md](references/api-v2-llms.md)

## Start with live capabilities

1. Confirm that a `pixellab` MCP server and its tools are available in the current client. Tool names may be bare or prefixed, such as `mcp__pixellab__create_character`.
2. Read the live schema for every tool before calling it. The server guide is generated from current tool definitions and may change after this skill is authored.
3. If the tools are absent, report that PixelLab MCP must be configured or reloaded. Do not invent a REST fallback. Use API v2 only when the user explicitly requests an HTTP integration or MCP cannot satisfy the task — then follow [references/api-v2-llms.md](references/api-v2-llms.md) and the [OpenAPI spec](https://api.pixellab.ai/v2/openapi.json).
4. Use `agent_help` for a focused PixelLab workflow/schema question when the live schema and [tools overview](references/tools-overview.md) are insufficient.

## Route the asset request

| Requested output | Preferred workflow |
|---|---|
| Player or NPC | `create_character` → optional `animate_character` → `get_character` |
| Consistent character variant | `create_character_state` → `get_character` |
| ProjectTwelve terrain/platform art | `create_sidescroller_tileset` → `get_sidescroller_tileset` |
| Top-down corner/Wang terrain | `create_topdown_tileset` → `get_topdown_tileset` |
| Isometric block or tile | `create_isometric_tile` → `get_isometric_tile` |
| One transparent prop | `create_map_object` → `get_map_object` |
| Reviewable or multi-direction prop | `create_1_direction_object` or `create_8_direction_object` → `get_object` |
| Pixel-art HUD panel/frame | `create_ui_asset` → `get_ui_asset` |
| Pixel font or advanced tile | `create_font` / `create_tiles_pro` → matching `get_*` |

Read [references/tools-overview.md](references/tools-overview.md) for parameters, chaining, review-state tools, and advanced capability boundaries.

## Generate asynchronously

Most creation tools return an ID immediately and continue in the background.

1. Translate the request into explicit art constraints: subject, view, canvas/tile dimensions, palette, outline, shading, detail, transparency, and required directions or frames.
2. Call the narrowest matching `create_*` tool. Reuse a seed, base tile ID, source asset ID, or style/reference image when consistency matters.
3. Queue dependent work immediately only when the returned ID is documented as usable before completion; character animations and chained tilesets support this pattern.
4. Poll the matching `get_*` tool until it reports completed or failed. Respect any ETA; do not busy-loop.
5. On review status, show candidate previews and ask the user which frames to promote before calling `select_object_frames`. Call `dismiss_review` only with explicit approval.
6. Download from the completed response URL. Treat UUID download URLs as bearer-capability links: do not commit, publish, or paste them into durable logs even though no authorization header is required.
7. Inspect the downloaded artifact before integrating it. Confirm dimensions, transparency, frame/direction ordering, and style continuity.

Map objects expire after eight hours; download them promptly after completion.

## Consent and external-side-effect boundaries

- An explicit request to generate an asset authorizes the corresponding scoped `create_*` call. Do not silently expand one requested asset into batches, animations, variants, or additional generations.
- If a tool requires `confirm_cost=true`, present the quoted cost and obtain approval unless the user already approved that exact priced operation.
- Obtain explicit approval before `delete_*`, `delete_animation`, `dismiss_review`, or replacing committed production art.
- Use `chat_*`, `sandbox_*`, `agent_talk`, or deployment/sync capabilities only when the user explicitly requests the broader game-building, remote-execution, or agent-debugging workflow. Pixel-art generation alone does not authorize them.
- Treat `agent_inspect` traces and grown memory as potentially sensitive. Retrieve only what the user requested and do not copy secrets into the repo or response.
- Ask before sending `agent_feedback`; it communicates externally with PixelLab.
- Never expose the PixelLab API token in prompts, logs, commits, patches, or download commands.

## Integrate with ProjectTwelve

After download, follow [references/projecttwelve-import.md](references/projecttwelve-import.md).

- Put public generated HUD art under `Assets/Sprites/UI/Generated/`; never put it in the licensed-assets submodule.
- Treat generated art as a draft until the user approves it.
- Preserve Unity `.meta` files when replacing assets and verify Point filtering, no mipmaps, dimensions, slicing, and PPU against the relevant manifest/spec.
- For terrain art, verify the tileset's topology against ProjectTwelve's resolver before wiring it in. Do not assume PixelLab's 16-tile sidescroller or Wang layout matches the existing 32-rule autotile contract.
- Run targeted EditMode tests for changed HUD/layout behavior and the Unity plus `tools/tile-viz` parity gates for autotile behavior changes.

## Troubleshoot

| Symptom | Response |
|---|---|
| No PixelLab tools | Verify the server is enabled and reload/restart the MCP client. |
| Unauthorized | Check that the configured authorization value is `Bearer …`; never print the value. |
| Processing | Poll the matching `get_*` after the reported ETA. |
| Failed | Report the server error and retry only if the request or error indicates retry is appropriate. |
| Wrong size/style | Tighten dimensions and art constraints; reuse seed/base/style references when supported. |
| Tool behavior unclear | Call `agent_help` with a narrow question or reread the live guide. |

## Verify completion

- Confirm the requested asset reached completed status.
- Inspect the actual image or spritesheet, not only the success response.
- Save it to the agreed path without exposing capability URLs or credentials.
- Verify engine import settings and preserve `.meta` identity where applicable.
- Report generation IDs only when useful for later polling or iteration.
- State any unverified visual, animation, topology, or Unity-import assumptions.
