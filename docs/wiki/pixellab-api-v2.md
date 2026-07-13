---
type: Technical Reference
title: PixelLab API v2 Agent Reference
description: ProjectTwelve routing, security, polling, and asset-integration guidance derived from PixelLab's official LLM API index.
resource: wiki/pixellab-api-v2.md
tags: [pixellab, api, pixel-art, hud, assets, agents]
timestamp: 2026-07-12T00:00:00Z
okf_version: 0.1
---

# PixelLab API v2 agent reference

This is a compact ProjectTwelve-oriented interpretation of PixelLab's living [API v2 LLM index](https://api.pixellab.ai/v2/llms.txt) and [interactive API documentation](https://api.pixellab.ai/v2/docs), retrieved 2026-07-12. The upstream API documentation remains authoritative; check it before relying on fields or endpoints that may have changed.

Do not vendor the full `llms.txt` response into this repository. Keep this document concise, link to the live source, and update only durable workflow rules.

## Authentication and security

- API root: `https://api.pixellab.ai/v2`.
- Send the PixelLab token with the Bearer authentication scheme.
- Store `PIXELLAB_API_KEY` in the gitignored project `.env` or parent process environment.
- Never put the token in prompts, committed JSON/TOML, screenshots, command output, generation metadata, or download URLs.
- Treat returned UUID download URLs as bearer capabilities. Download promptly, but do not commit or quote those URLs.
- A `401` means authentication is absent or invalid; `402` means insufficient credits/generations; `422` means request validation failed; `429` means rate or concurrent-job limits were reached.

## Asynchronous job contract

Most generation endpoints return immediately with a `background_job_id`, asset/object ID, `status`, and usage data.

1. Submit one narrowly scoped generation request.
2. Store the returned job and asset IDs without storing capability URLs.
3. Poll the corresponding resource endpoint or `GET /background-jobs/{background_job_id}` at a bounded interval. Five to ten seconds is the upstream baseline; respect longer server ETAs.
4. Stop polling on `completed`, `failed`, or review state.
5. Inspect the actual bitmap before integration.
6. Download completed assets promptly and validate dimensions, mode, transparency, and content.

Do not duplicate a slow job merely because its ETA fluctuates. A `429` should release capacity before retrying, not trigger a parallel retry loop.

## Capability routing

| Need | API v2 route/capability | ProjectTwelve guidance |
|---|---|---|
| Exact-size pixel image | `POST /generate-image-v2` | Prefer for isolated icons or UI elements when exact width/height matters. |
| Style-guided image | `generate-image-v2` with `style_image` and `style_options` | Use an approved HUD element as the concept/style reference; explicitly choose palette, outline, detail, and shading transfer. |
| UI panel generation | `POST /create-ui-asset` | Use for one panel/frame per call, transparent background, and a narrow description. Never request an entire HUD sheet. |
| One-direction prop/icon | `POST /create-1-direction-object` | Use for player portrait, hearts, tile icons, and cursor when candidate review is useful. |
| Candidate promotion | `POST /objects/{object_id}/select-frames` | Show previews and obtain explicit user approval for selected indices before promotion. |
| Reject a review pack | `POST /objects/{object_id}/dismiss-review` | Destructive: require explicit approval. |
| Arbitrary image to pixel art | `POST /image-to-pixelart` | Use when converting an approved mockup; supply input and desired output size. |
| Automatic native-scale cleanup | Pro image-to-pixel-art workflow | Use when source pixel scale is unknown; validate the detected scale afterward. |
| Prompt improvement | `POST /enhance-pixen-prompt` | Optional; do not let enhancement add objects, text, or scenes outside the specification. |

The live API also covers characters, animations, maps, rotations, inpainting, editing, tiles, fonts, and asset-management operations. Load the relevant upstream endpoint schema only when the task needs it.

## Image-generation request fields

Common durable fields include:

- `description`: precise subject and exclusion list.
- `image_size`: explicit width and height where supported.
- `no_background`: request alpha-ready output.
- `seed`: deterministic rerolls and style-family continuity.
- `style_image`: optional base64 reference image with its size and usage description.
- `style_options`: booleans controlling whether palette, outline, detail, and shading are copied.

Large style images may be downscaled, and non-square references may be padded to square. Inspect the resulting subject bounds instead of assuming the original reference geometry survives unchanged.

## HUD generation rules

For the ProjectTwelve HUD specification in [`../specs/hud-assets.json`](../specs/hud-assets.json):

1. Generate one asset at a time. Do not ask the model for a sheet containing panels, slots, icons, labels, and menus together.
2. Include a strict exclusion list: no baked text, no menu, no character, no environment, no extra components, no gradients, no glow, no watermark.
3. Use transparent backgrounds for isolated assets.
4. Use shared palette and style reference inputs after the first element is approved.
5. Prefer the smallest supported native generation size that still preserves the intended detail.
6. Normalize only after approval: crop transparent margins, resize with nearest-neighbor sampling, and preserve aspect ratio.
7. Reject assets that require inventing missing content or non-uniform stretching to fit the specification.
8. Keep PixelLab drafts in `docs/images/hud-mockups/` until approved. Production art belongs under `Assets/Sprites/UI/Generated/` with Unity `.meta` identity and import verification.

## MCP versus API v2

PixelLab MCP is preferred when its live tool schema covers the task. The currently exposed MCP `create_ui_asset` is narrower than the website's Create UI Elements (Pro) workflow: it does not expose concept-image guidance, exact small output dimensions, or UI candidate grids.

When MCP lacks a required API v2 capability:

- do not invent an MCP tool or REST endpoint;
- consult the live API v2 schema;
- use direct API integration only when the user explicitly requests or authorizes it;
- otherwise use the closest MCP workflow and document the limitation.

## Verification checklist

- [ ] Completed or review status confirmed from PixelLab.
- [ ] User approved review-candidate indices before promotion.
- [ ] Bitmap dimensions match the intended source size or have a documented normalization step.
- [ ] RGBA/alpha and transparent subject bounds verified.
- [ ] No baked text, scene, watermark, or extra component.
- [ ] Point filtering, no mipmaps, no compression, and correct PPU planned for Unity.
- [ ] Capability URLs and authentication values absent from tracked files and logs.
- [ ] Paid-assets guard passes; generated public art is outside `Assets/_Licensed/`.

## Related project references

- [`hud-assets-manifest.md`](hud-assets-manifest.md)
- [`../specs/hud-assets.json`](../specs/hud-assets.json)
- [`../../.claude/skills/pixellab-mcp/SKILL.md`](../../.claude/skills/pixellab-mcp/SKILL.md)
- [`../PAID_ASSETS.md`](../PAID_ASSETS.md)
