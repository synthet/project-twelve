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
- Supplementary C# style: [`.cursorrules`](../../.cursorrules).
