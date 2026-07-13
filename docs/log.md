# Wiki activity log

Append-only log of documentation ingest, lint, and structural changes.

## [2026-06-28] adopt | synthet-code-framework agent infrastructure

Adopted full agent scaffolding from synthet-code-framework: `.claude/`, `.cursor/` mirror, `.agent-memory/`, OKF lint scripts, and CI agent checks. Backlog adapted to wiki-tickets + GitHub issues workflow.

## [2026-07-12] reorganized | HUD and PixelLab pages consolidated into wiki

Moved `hud-conversation-summary.md`, `hud-assets-manifest.md`, `specs/hud-redesign-pixellab.md`, and `skills/pixellab-api-v2.md` into `wiki/` (LLM wiki consolidation); removed the empty `skills/` folder. Fixed relative links and `resource` frontmatter, updated the inbound link in `.claude/skills/pixellab-mcp/references/projecttwelve-import.md`, and added a HUD & PixelLab group to `wiki/README.md` and `INDEX.md`. Ingested `wiki/hud-redesign-pixellab.md` (new PixelLab capability research and v3 asset contract plan).

## [2026-07-12] updated | HUD contract v3 generated via PixelLab pieces workflow

Bumped `specs/hud-assets.json` to v3 (9-slice-first panels, 16px hearts, 48px slots, 40px portrait). Generated the panel family from one `pieces`-templated `create_ui_asset` hero sheet and small art from `create_1_direction_object` candidate packs; deterministic crop/repair/normalize steps live in `scripts/normalize_pixellab_hud_assets.py` (`--v3-sheet`, `--v3-objects`). Updated `wiki/hud-assets-manifest.md` and `wiki/hud-redesign-pixellab.md` with the v3 contract, generation record, and operational lessons (64-candidate packs time out; style_images dictate output size). Production sprites replaced under `Assets/Sprites/UI/Generated/` with `.meta` GUIDs preserved; `SandboxHudController`, `SandboxHudPrefabBuilder`, and `SandboxHudTests` updated to v3 metrics. Unity in-editor verification pending.
