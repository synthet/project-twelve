---
description: Unity project conventions for ProjectTwelve agents
alwaysApply: true
---

# Unity project rules (ProjectTwelve)

- Preserve Unity `.meta` files when adding, moving, or deleting **project-owned** assets.
- Licensed art lives in the `Assets/_Licensed/` submodule — see [`docs/PAID_ASSETS.md`](../../docs/PAID_ASSETS.md).
- Run `python3 scripts/check_paid_assets.py --staged` before commits.
- Batch validation: `Unity -batchmode -quit -projectPath . -logFile Logs/unity-validate.log`
- EditMode tests: see [`AGENTS.md`](../../AGENTS.md) test vocabulary table.
- Keep world data, rendering, input, and persistence concerns separated under `Assets/Scripts/`.
- **Autotile changes:** run targeted EditMode tests (`AutotileVisualTests`, fixture export tests) and `cd tools/tile-viz && npm test`.
- **Rule tables:** exported JSON under `tools/tile-viz/data/` comes from C# (`AutotileRulesFixtureExportTests`, `AutotileFixtureExportTests`) — regenerate, do not hand-edit.
- **Visual contract:** follow [`docs/VISUAL_BEHAVIOR_SPEC.md`](../../docs/VISUAL_BEHAVIOR_SPEC.md); anchor sprites by **bounds**, not pivots (see history-regression-guard).
- **Play Mode debug:** Runtime MCP autotile tools (`tile_autotile`, `tiles_autotile_area`) require Play Mode; see [AGENTS.md](../../AGENTS.md) § In-game runtime MCP.
- Supplementary C# style: [`.cursorrules`](../../.cursorrules).
