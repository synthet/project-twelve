# AI Agents Configuration — ProjectTwelve

## Overview

ProjectTwelve is a Unity 2D sandbox prototype. This repo ships agent scaffolding for Claude Code and
Cursor: slash commands, skills, subagents, safety rules, an `.agent/` governance hub, a project-memory
subsystem, and OKF docs tooling. This file is the **source of truth** for how agents build/test/run and
which tools they may use.

## Authoring & skill source of truth

- **Canonical** assets are authored under `.claude/` (+ `.agent/`).
- The **`.cursor/`** tree (rules/commands/skills/agents) is **generated** by
  `python scripts/sync_assistant_trees.py` — do not hand-edit it; edit `.claude/` and re-run sync.
- When you change a skill/command/agent, run the sync and commit both trees in the **same PR**
  (see [`.agent/SKILL_CHANGE_AST10_REVIEW.md`](.agent/SKILL_CHANGE_AST10_REVIEW.md)).

## Commands

```bash
# Open/build in Unity Editor 6.0.5.1f1; Unity regenerates solution files.
# Batch-mode edit validation (requires Unity installed in the environment):
Unity -batchmode -quit -projectPath . -logFile Logs/unity-validate.log

# Run Unity edit-mode tests (requires Unity installed in the environment):
Unity -batchmode -quit -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile Logs/unity-editmode-tests.log

# Documentation/link hygiene and paid-asset guard:
python3 scripts/check_markdown_links.py
python3 scripts/check_paid_assets.py --staged

# OKF docs health:
python3 scripts/okf_lint.py --profile project --exclude-prefix archive/ docs
python3 scripts/wiki_lint.py --exclude-prefix archive/

# Agent assets:
python scripts/sync_assistant_trees.py    # regenerate .cursor/ from .claude/

# Project memory:
python scripts/agent-memory/log_session.py --summary "..." --outcome "..." --candidate "text|working_rule|high"
python scripts/agent-memory/dream.py
python scripts/agent-memory/promote_dream.py --dream .agent-memory/dreams/<timestamp>.md
python scripts/agent-memory/context.py
```

**Python dependency:** `pip install pyyaml` (required for OKF lint and memory scripts).

## MCP servers

- Define project servers in [`.mcp.json`](.mcp.json) (Claude Code) and copy
  [`.cursor/mcp.example.json`](.cursor/mcp.example.json) → `.cursor/mcp.json` (gitignored) for Cursor.
- **Naming:** `<scope>-<role>-*` (e.g. `project-twelve-unity-mcp`, `project-twelve-github`).
- **Secrets via env only**, never CLI args. Reload the MCP client after changing keys.
- User-level `~/.cursor/mcp.json` holds cross-repo tools; project keys live in this repo.

### Unity MCP (Editor bridge)

Connect Cursor and Claude Code to the running Unity Editor via [Unity MCP](https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.9/manual/integration/unity-mcp-overview.html). This project declares `com.unity.ai.assistant` in `Packages/manifest.json`.

**One-time setup (per machine):**
1. Open the project in Unity 6.0.5.1f1 and let Package Manager resolve `com.unity.ai.assistant`.
2. Go to **Edit → Project Settings → AI → Unity MCP** and confirm **Unity Bridge** is **Running** (green). Unity installs the relay binary to `%USERPROFILE%\.unity\relay\relay_win.exe` on first Editor start.
3. Under **Integrations**, select **Cursor** (or Claude Code) and click **Configure** (recommended). Or copy [`.cursor/mcp.example.json`](.cursor/mcp.example.json) to `.cursor/mcp.json`, set `RELAY_PATH` and `UNITY_PROJECT_PATH`, then restart Cursor.
4. On first connect, approve the pending client under **Pending Connections** in Unity MCP settings.

**Before each session:** Unity Editor open on this project with the MCP bridge **Running**.

**Smoke test:** ask the agent to read Unity console messages (e.g. via `Unity_ReadConsole`).

**Multi-instance:** set `UNITY_PROJECT_PATH` to this repo root when several Editors are open. See [Get started with Unity MCP](https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.9/manual/integration/unity-mcp-get-started.html).

**Requirements:** Unity 6+, Unity AI trial or subscription for Assistant features.

### In-game runtime MCP (Play Mode / desktop builds)

While the game is running, `ProjectTwelve.RuntimeMcp` hosts a JSON-RPC MCP endpoint at
`http://127.0.0.1:8765/mcp` (loopback only). Cursor connects via the `project-twelve-ingame-mcp`
http entry in [`.cursor/mcp.example.json`](.cursor/mcp.example.json).

**Before each session:** enter Play Mode (or launch a desktop build) so the runtime server is listening.

**Disable or rebind:** set env `PROJECTTWELVE_MCP_ENABLED=false` or `PROJECTTWELVE_MCP_PORT=<port>`.

**Smoke test:** call `player_state` or `world_info` while Play Mode is active.

**Gateway Timeout:** the game must be in **Play Mode** (console log: `Runtime MCP listening on …`).
`initialize` / `tools/list` respond on the HTTP thread; `tools/call` needs the main thread — unpause
the Editor if Play Mode is paused.

### External CLI reviews

Optional second opinions via the sibling `subagent-orchestrator` MCP server. See
[`docs/EXTERNAL_CLI_REVIEWS.md`](docs/EXTERNAL_CLI_REVIEWS.md). Review-only; never enable writes.

## Available tools

<!-- BEGIN MCP TOOL INVENTORY -->
<!-- Auto-generated; do not edit by hand. Regenerate when your MCP tools change. -->

### project-twelve-ingame-mcp (runtime, Play Mode / desktop build)

| Tool | Kind | Description |
|------|------|-------------|
| `player_move` | write | Set horizontal movement (`direction`: left/right/none; optional `durationSeconds`). |
| `player_jump` | write | Request a jump when grounded. |
| `player_teleport` | write | Teleport player to world `{ x, y }`. |
| `world_set_tile` | write | Place/remove tile at `{ x, y, tileId }` (0 = Air). |
| `player_state` | read | Player position, velocity, grounded, tile coord. |
| `world_info` | read | Seed, tile size, player position/chunk, loaded chunk count. |
| `tile_at` | read | Tile id/solid/light/fluid at `{ x, y }`. |
| `tiles_area` | read | Rectangular tile dump with ASCII grid; bounds via `xMin`…`yMax` or `centerX`/`centerY` + `radius`, optional `aroundPlayer`. |
| `tile_autotile` | read | Autotile masks, neighbor connectivity, matching rule ids, and resolved ground/cover sprites at `{ x, y }`. |
| `tiles_autotile_area` | read | Same autotile debug as `tile_autotile` for each solid tile in a region (`maxCells` default 400). |
| `perf` | read | Smoothed FPS and frame time (ms). |

Endpoint: `http://127.0.0.1:8765/mcp` (override port with `PROJECTTWELVE_MCP_PORT`).

<!-- END MCP TOOL INVENTORY -->

## Common workflows

`/spec → /plan → /implement → /test-and-fix → /pr-ready`. See
[`docs/ai-workflow/README.md`](docs/ai-workflow/README.md) for the full asset map and loop, and
[`.agent/workflows/`](.agent/workflows/) for playbooks.

Pick work from [`docs/wiki/tickets/`](docs/wiki/tickets/) and linked GitHub issues (see
[`docs/project/00-backlog-workflow.md`](docs/project/00-backlog-workflow.md)).

## Git configuration — do not modify

Never modify `.git/config` or add non-standard git extensions (do not set
`extensions.worktreeConfig` or change `core.repositoryformatversion`). Embedded git libraries in
third-party tooling choke on non-standard extensions and break workspace resolution. If a worktree is
needed, use a temporary one and clean it up immediately.

**Optional git hooks:** run `python scripts/install_githooks.py` (human opt-in; agents do not run
this automatically) to enable `.githooks/`: paid-asset guard, submodule verify on commit/push, and
submodule local sync after pull/checkout. Network sync: `python scripts/fetch_remotes.py` or
`/fetch-remotes`.

## Test vocabulary

| You say | Canonical name | Where | How to run |
|---------|----------------|-------|------------|
| Unity validation | Batch-mode project load | Unity project root | `Unity -batchmode -quit -projectPath . -logFile Logs/unity-validate.log` |
| EditMode tests | Unity EditMode test suite | Unity Test Framework | `Unity -batchmode -quit -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile Logs/unity-editmode-tests.log` |
| Markdown links | Docs link checker | `docs/`, `README.md`, agent docs | `python3 scripts/check_markdown_links.py` |
| Paid assets guard | Block licensed paths in public repo | `config/paid-assets.local-only.example.txt` | `python3 scripts/check_paid_assets.py --staged` (commit) or `--push` |
| Visual catalog regen | Submodule catalog generator | `scripts/generate_visual_catalogs.py` | `python3 scripts/generate_visual_catalogs.py` |
| OKF lint | Docs frontmatter checker | `docs/` | `python3 scripts/okf_lint.py --profile project --exclude-prefix archive/ docs` |
| Cursor sync check | `.cursor/` drift gate | `scripts/sync_assistant_trees.py` | `python scripts/sync_assistant_trees.py --check` |

## Codex regression guardrails

Recent fix history shows recurring agent mistakes that must be checked before committing:

- **Docs metadata:** fixes repeatedly repaired missing OKF frontmatter in `docs/**` and
  `docs/wiki/tickets/**`. When editing docs, add/maintain frontmatter and run the docs lint gates.
- **Generated mirrors:** `.cursor/**` is generated from `.claude/**`. Do not hand-edit generated
  Cursor rules/commands/skills/agents; edit `.claude/**`, run `python scripts/sync_assistant_trees.py`,
  and commit both trees.
- **Safety scripts fail safe:** paid-asset and CI guard scripts must handle missing upstreams, detached
  HEAD, shallow clones, no staged files, and absent base refs without allowing licensed assets through.
- **Unity visual assumptions:** rendering/collision fixes must account for sprite bounds, pivots, tile
  size, and import settings; add EditMode coverage when feasible.
- **Required history check for rule/tooling changes:** when asked to update agent rules, Codex rules,
  skills, commands, CI guards, or docs conventions, inspect recent fixes with
  `git log --oneline --grep='fix' -n 20` and encode any repeated failure mode into the canonical
  `.claude/` source plus generated `.cursor/` mirror.

## Coding-agent contract

- **Unity scope:** preserve Unity `.meta` files when adding, moving, or deleting assets.
- **Code style:** match the surrounding C# style; prefer explicit access modifiers and serialized private fields for Inspector settings.
- **Architecture:** keep world data, rendering, player input, and persistence concerns separated unless an explicit contract change is documented.
- **Security (hard rules):** secrets via env/`secrets.json` only; never commit tokens, service keys, or machine-specific paths. See [`docs/security.md`](docs/security.md) and [`.agent/SAFETY.md`](.agent/SAFETY.md).
- **Paid/licensed assets:** licensed content lives in the private `project-twelve-assets` repo as git submodule `Assets/_Licensed/`. Never commit licensed blobs into the public repo. Policy in `docs/PAID_ASSETS.md`. Run `python3 scripts/check_paid_assets.py --staged` or `--push` before commit/push.
- **Change control:** make minimal diffs, include tests or validation notes for behavior changes, and avoid drive-by reformatting.
- **Documentation:** update `README.md`, `docs/wiki/`, or `docs/CANONICAL_SOURCES.md` when changing architecture, workflows, or public conventions.
- **Prohibited:** committing secrets, disabling/weakening tests to go green, drive-by reformatting, inventing API paths / config keys / schema names (check [`docs/CANONICAL_SOURCES.md`](docs/CANONICAL_SOURCES.md)).

## AI workspace assets

| Asset | Location |
|-------|----------|
| Agent contract | `AGENTS.md` |
| Assistant orientation | `CLAUDE.md` |
| Claude commands/skills/agents | `.claude/` (canonical) |
| Cursor mirror | `.cursor/` (generated) |
| Safety rules | `.agent/SAFETY.md` |
| Agent inventory | `.agent/AGENT_INFRA_INVENTORY.md` |
| Workflow playbooks | `.agent/workflows/` |
| Project memory | `.agent-memory/` |
| Workflow index | `docs/ai-workflow/README.md` |
| Backlog workflow | `docs/project/00-backlog-workflow.md` |
| Canonical sources | `docs/CANONICAL_SOURCES.md` |
| Paid asset policy | `docs/PAID_ASSETS.md` |
| Unity C# rules (supplementary) | `.cursorrules` |
