---
description: Regression guardrails learned from recent fix commits; always review before coding or PR handoff.
alwaysApply: true
---

# History regression guard (always on)

Recent fix commits show repeated failures in **documentation metadata**, **repo safety guards**, and
**Unity geometry assumptions**. Treat these as repo-specific hazards and check them before committing.

## Lessons from recent fixes

- **OKF frontmatter drift is a recurring CI failure.** Commits `afe34a1`, `b509ed3`, and `f65d87a`
  repaired docs/tickets that were missing valid OKF metadata. Any change under `docs/` or
  `docs/wiki/tickets/` must preserve or add frontmatter and run the OKF lint gate.
- **Paid-assets guard logic must fail safe.** Commits `5bded97`, `a6554a2`, and `89b54e7` fixed
  base-ref/upstream edge cases in `scripts/check_paid_assets.py`. When editing Git/CI guard scripts,
  assume missing upstreams, detached HEAD, shallow clones, empty staged sets, and absent base refs.
- **Generated assistant trees drift unless synced.** `.cursor/` is generated from `.claude/`; any
  `.claude/commands`, `.claude/rules`, `.claude/skills`, or `.claude/agents` edit requires
  `python scripts/sync_assistant_trees.py` and committing both trees.
- **Unity visual math must not depend on fragile asset import settings.** Commit `b3e3dc2` fixed
  autotile cell anchoring by using sprite bounds instead of pivots. Rendering/collision changes must
  account for pivots, bounds, tile size, and add EditMode coverage when practical.
- **Markdown examples can break docs checks.** Commit `6112af7`/nearby docs fixes show that fenced
  examples, links, and frontmatter need validation, not just visual inspection.
- **Autotile Unity/JS drift:** C# resolver changes without `tile-viz` tests or stale
  `data/autotile-rules.*.json` break offline parity.

## Required pre-commit decision table

| If you touch... | You must do before commit |
|-----------------|---------------------------|
| `.claude/**` | Run `python scripts/sync_assistant_trees.py`, then `python scripts/sync_assistant_trees.py --check`. |
| `.cursor/**` generated mirrors | Stop and edit `.claude/**` instead, then sync. |
| `docs/**` or `README.md` | Run `python3 scripts/check_markdown_links.py`; run OKF lint for docs/wiki changes. |
| `scripts/check_paid_assets.py`, hooks, or CI guard logic | Test staged and push/no-upstream paths where possible; never make asset checks pass open on ambiguity. |
| Unity C# under `Assets/Scripts/**` | Run targeted EditMode tests if Unity is available; otherwise document Unity availability as the blocker. |
| `Assets/Scripts/Visual/Tiles/**` | Unity EditMode autotile tests; `cd tools/tile-viz && npm test`; regen exported JSON if rule tables changed |
| `tools/tile-viz/**` or `tools/world-viz/**` | `npm test` in that package; if resolver logic changed, verify matching Unity EditMode coverage |
| `Assets/Scripts/RuntimeMcp/**` | EditMode MCP dispatcher tests if tool contracts change; document Play Mode requirement for manual smoke |
| Assets or submodules | Run `python3 scripts/check_paid_assets.py --staged` and inspect `git diff --cached --name-only`. |

## Codex-specific enforcement

- Do **not** rely on memory. Start by reading `AGENTS.md` and this rule, then inspect recent fix
  history with `git log --oneline --grep='fix' -n 20` when asked to change rules, CI, docs, or agent
  scaffolding.
- Before final response, run `git status --short` and verify the staged/committed file set matches
  the intended scope.
- If a required tool is unavailable (for example Unity in a headless container), record the exact
  command and the environment limitation instead of silently skipping it.
