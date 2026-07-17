# AI Agents Configuration — ProjectTwelve

## Overview

ProjectTwelve is a Unity 2D sandbox prototype. This repo ships agent scaffolding for Claude Code,
Cursor, Codex, and Antigravity/Gemini: commands, skills, subagents, safety rules, an `.agent/` governance hub, a
project-memory subsystem, and OKF docs tooling. This file is the **source of truth** for how agents
build/test/run and which tools they may use.

## Authoring & skill source of truth

- **Canonical** assets are authored under `.claude/` (+ `.agent/`).
- The **`.cursor/`** tree (rules/commands/skills/agents) and Codex/Antigravity-native **`.agents/`** tree
  are **generated** by `python scripts/sync_assistant_trees.py` — do not hand-edit them; edit
  `.claude/` and re-run sync.
- When you change a skill/command/agent, run the sync and commit the canonical and generated trees in the **same PR**
  (see [`.agent/SKILL_CHANGE_AST10_REVIEW.md`](.agent/SKILL_CHANGE_AST10_REVIEW.md)).

## Session start (all agents)

**Every new chat or resumed session** — before modifying code or claiming work:

1. **Read** [`.agent-memory/memory.md`](.agent-memory/memory.md) (compact: `python scripts/agent-memory/context.py`).
2. **Prefer repo evidence** (files, tests, docs) over memory when they conflict.
3. **At session end**, log durable learnings via `/log-session` — do not hand-edit `memory.md` during implementation.

Always-on rule: `.claude/rules/agent-memory.md` (mirrored to `.cursor/rules/agent-memory.mdc`). Skill: [`.claude/skills/agent-memory/SKILL.md`](.claude/skills/agent-memory/SKILL.md).

## Commands

```bash
# Open/build in Unity Editor 6.0.5.1f1; Unity regenerates solution files.
# Batch-mode edit validation (Unity 6000.5.1f1 — see .claude/skills/unity-tests/SKILL.md):
Unity -batchmode -quit -projectPath . -logFile Logs/unity-validate.log

# EditMode tests (on Windows: omit -quit with -runTests — see unity-tests skill):
Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile Logs/unity-editmode-tests.log

# Documentation/link hygiene and paid-asset guard:
python3 scripts/check_markdown_links.py
python3 scripts/check_paid_assets.py --staged

# OKF docs health:
python3 scripts/okf_lint.py --profile project --exclude-prefix archive/ docs
python3 scripts/wiki_lint.py --exclude-prefix archive/

# Agent assets:
python scripts/sync_assistant_trees.py    # regenerate .cursor/ + .agents/skills/ from .claude/

# Offline tooling (no Unity):
cd tools/world-viz && npm test             # terrain parity vs Unity golden fixture
cd tools/tile-viz && npm test              # autotile resolver + snippet fixtures

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
- Codex loads portable project servers from [`.codex/config.toml`](.codex/config.toml); keep
  machine-specific relay paths in user-level `~/.codex/config.toml` (setup: [`.codex/README.md`](.codex/README.md)).
- Antigravity/Gemini loads project servers from `.agents/mcp_config.json` (gitignored);
  copy [`.agents/mcp_config.example.json`](.agents/mcp_config.example.json) and set machine paths.
- **Naming:** `<scope>-<role>-*` (e.g. `project-twelve-unity-mcp`, `project-twelve-github`).
- **Secrets via env only**, never CLI args. Reload the MCP client after changing keys.
- User-level `~/.cursor/mcp.json` holds cross-repo tools; project keys live in this repo.

### Unity MCP (Editor bridge)

Connect Cursor, Claude Code, Codex, and Antigravity to the running Unity Editor via [Unity MCP](https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.9/manual/integration/unity-mcp-overview.html). This project declares `com.unity.ai.assistant` in `Packages/manifest.json`.

**One-time setup (per machine):**
1. Open the project in Unity 6.0.5.1f1 and let Package Manager resolve `com.unity.ai.assistant`.
2. Go to **Edit → Project Settings → AI → Unity MCP** and confirm **Unity Bridge** is **Running** (green). Unity installs the relay binary to `%USERPROFILE%\.unity\relay\relay_win.exe` on first Editor start.
3. Under **Integrations**, select **Cursor** (or Claude Code) and click **Configure** (recommended).
   For Codex, follow [`.codex/README.md`](.codex/README.md). Or copy
   [`.cursor/mcp.example.json`](.cursor/mcp.example.json) to `.cursor/mcp.json`, set `RELAY_PATH` and
   `UNITY_PROJECT_PATH`, then restart Cursor. For Antigravity, edit `.agents/mcp_config.json`.
4. On first connect, approve the pending client under **Pending Connections** in Unity MCP settings.

**Before each session:** Unity Editor open on this project with the MCP bridge **Running**.

**Smoke test:** ask the agent to read Unity console messages (e.g. via `Unity_ReadConsole`).

**Multi-instance:** set `UNITY_PROJECT_PATH` to this repo root when several Editors are open. See [Get started with Unity MCP](https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.9/manual/integration/unity-mcp-get-started.html).

**Requirements:** Unity 6+, Unity AI trial or subscription for Assistant features.

### In-game runtime MCP (Play Mode / desktop builds)

While the game is running, `ProjectTwelve.RuntimeMcp` hosts a JSON-RPC MCP endpoint at
`http://127.0.0.1:8765/mcp` (loopback only). Cursor connects via the `project-twelve-ingame-mcp`
entry in [`.cursor/mcp.example.json`](.cursor/mcp.example.json); Codex uses the committed
[`.codex/config.toml`](.codex/config.toml) entry; Antigravity uses `.agents/mcp_config.json`.

**Before each session:** enter Play Mode (or launch a desktop build) so the runtime server is listening.

**Disable or rebind:** set env `PROJECTTWELVE_MCP_ENABLED=false` or `PROJECTTWELVE_MCP_PORT=<port>`.

**Smoke test:** call `player_state` or `world_info` while Play Mode is active.

**Gateway Timeout:** the game must be in **Play Mode** (console log: `Runtime MCP listening on …`).
`initialize` / `tools/list` respond on the HTTP thread; `tools/call` needs the main thread — unpause
the Editor if Play Mode is paused.

### External CLI reviews

Optional second opinions via the sibling `subagent-orchestrator` MCP server. See
[`docs/EXTERNAL_CLI_REVIEWS.md`](docs/EXTERNAL_CLI_REVIEWS.md). Review-only; never enable writes.

### FFF file search MCP

[FFF](https://github.com/dmtrKovalenko/fff) is a fast, frecency-ranked file search MCP for agents.
Use its tools for repo-wide path and content search instead of repeated grep roundtrips when the
server is connected.

**One-time setup (per machine):**

- **Windows:** `irm https://raw.githubusercontent.com/dmtrKovalenko/fff/main/install-mcp.ps1 | iex`
- **Linux / macOS:** `curl -L https://dmtrkovalenko.dev/install-fff-mcp.sh | bash`
- **Homebrew:** `brew install dmtrKovalenko/fff/fff-mcp`

Then add `project-twelve-fff-mcp` from [`.cursor/mcp.example.json`](.cursor/mcp.example.json) to
gitignored `.cursor/mcp.json` (or enable in [`.mcp.json`](.mcp.json) for Claude Code) and reload the
MCP client. Codex already has the portable server definition in [`.codex/config.toml`](.codex/config.toml).
Antigravity uses the local `.agents/mcp_config.json`.

**Agent guidance:** for file search or grep in the current git-indexed directory, prefer FFF MCP tools
(`fffind`, `ffgrep`, `fff-multi-grep`) over built-in search when available.

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
| `chunk_info` | read | Chunk dirty flags, renderer state, save-diff status, nav version at `{ chunkX, chunkY }` (or world tile `{ x, y }`); never generates the chunk. |
| `light_at` | read | Light value at `{ x, y }`; never generates chunks. |
| `fluid_at` | read | Fluid amount + active-set membership at `{ x, y }`; never generates chunks. |
| `tiles_area` | read | Rectangular tile dump with ASCII grid; bounds via `xMin`…`yMax` or `centerX`/`centerY` + `radius`, optional `aroundPlayer`. |
| `tile_autotile` | read | Autotile masks, neighbor connectivity, matching rule ids, and resolved ground/cover sprites at `{ x, y }`. |
| `tiles_autotile_area` | read | Same autotile debug as `tile_autotile` for each solid tile in a region (`maxCells` default 400). |
| `world_export_tile_space` | read | Export a world region as `project-twelve/tile-space/v1` JSON (legacy tile ids); optional `writeFile`. |
| `autotile_diff_baseline` | read | Compare live autotile vs committed baseline; returns mismatches only (`maxDiffs` default 100). |
| `perf` | read | Smoothed FPS and frame time (ms). |

Endpoint: `http://127.0.0.1:8765/mcp` (override port with `PROJECTTWELVE_MCP_PORT`).

### project-twelve-fff-mcp (file search)

| Tool | Kind | Description |
|------|------|-------------|
| `fffind` | read | Frecency-ranked path and filename search over the indexed repo. |
| `ffgrep` | read | Content search (plain, regex, fuzzy fallback) with context and pagination. |
| `fff-multi-grep` | read | Multi-pattern OR content search in one call. |

Upstream: [dmtrKovalenko/fff](https://github.com/dmtrKovalenko/fff). Binary: `fff-mcp`.

### pixellab (pixel art generation, remote HTTP)

| Tool category | Kind | Description |
|------|------|-------------|
| Characters | write | `create_character`, `animate_character`, `create_character_state` — async jobs (~2–5 min) |
| Tilesets | write | `create_sidescroller_tileset`, `create_topdown_tileset` — chain via base tile IDs |
| UI / objects | write | `create_ui_asset`, `create_map_object`, `create_isometric_tile` |
| Status / download | read | `get_*`, `list_*` — poll until completed; download from response URLs |
| Help | read | `agent_help`, `agent_feedback`, `get_balance` |

Endpoint: `https://api.pixellab.ai/mcp` (Bearer token via `PIXELLAB_API_KEY` env or gitignored `.cursor/mcp.json`). Skill: [`.claude/skills/pixellab-mcp/SKILL.md`](.claude/skills/pixellab-mcp/SKILL.md). Setup: [pixellab.ai/mcp](https://www.pixellab.ai/mcp).

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
| EditMode tests | Unity EditMode test suite | Unity Test Framework | See [`.claude/skills/unity-tests/SKILL.md`](.claude/skills/unity-tests/SKILL.md) (`.env` paths, Windows `-quit` quirk, result parsing) |
| Markdown links | Docs link checker | `docs/`, `README.md`, agent docs | `python3 scripts/check_markdown_links.py` |
| Paid assets guard | Block licensed paths in public repo | `config/paid-assets.local-only.example.txt` | `python3 scripts/check_paid_assets.py --staged` (commit) or `--push` |
| Visual catalog regen | Submodule catalog generator | `scripts/generate_visual_catalogs.py` | `python3 scripts/generate_visual_catalogs.py` |
| OKF lint | Docs frontmatter checker | `docs/` | `python3 scripts/okf_lint.py --profile project --exclude-prefix archive/ docs` |
| Assistant sync check | `.cursor/` + `.agents/skills/` drift gate | `scripts/sync_assistant_trees.py` | `python scripts/sync_assistant_trees.py --check` |
| world-viz tests | Engine-free terrain JS port | `tools/world-viz/` | `cd tools/world-viz && npm test` |
| tile-viz tests | Engine-free autotile resolver/render | `tools/tile-viz/` | `cd tools/tile-viz && npm test` |

## Codex regression guardrails

Recent fix history shows recurring agent mistakes that must be checked before committing:

- **Docs metadata:** fixes repeatedly repaired missing OKF frontmatter in `docs/**` and
  `docs/wiki/tickets/**`. When editing docs, add/maintain frontmatter and run the docs lint gates.
- **Generated mirrors:** `.cursor/**` and `.agents/skills/**` are generated from `.claude/**`. Do not
  hand-edit generated Cursor assets or Codex skills; edit `.claude/**`, run
  `python scripts/sync_assistant_trees.py`, and commit all generated trees.
- **Safety scripts fail safe:** paid-asset and CI guard scripts must handle missing upstreams, detached
  HEAD, shallow clones, no staged files, and absent base refs without allowing licensed assets through.
- **Unity visual assumptions:** rendering/collision fixes must account for sprite bounds, pivots, tile
  size, and import settings; add EditMode coverage when feasible.
- **Required history check for rule/tooling changes:** when asked to update agent rules, Codex rules,
  skills, commands, CI guards, or docs conventions, inspect recent fixes with
  `git log --oneline --grep='fix' -n 20` and encode any repeated failure mode into the canonical
  `.claude/` source plus generated `.cursor/` and `.agents/skills/` mirrors.

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
| Codex project config | `.codex/config.toml` + `.codex/README.md` |
| Antigravity project config | `.agents/mcp_config.example.json` + `.agents/README.md` |
| Codex & Antigravity skill mirror | `.agents/skills/` (generated) |
| Antigravity project rules | `.agents/AGENTS.md` (generated) |
| Safety rules | `.agent/SAFETY.md` |
| Agent inventory | `.agent/AGENT_INFRA_INVENTORY.md` |
| Workflow playbooks | `.agent/workflows/` |
| Project memory | `.agent-memory/` |
| Workflow index | `docs/ai-workflow/README.md` |
| Backlog workflow | `docs/project/00-backlog-workflow.md` |
| Canonical sources | `docs/CANONICAL_SOURCES.md` |
| Paid asset policy | `docs/PAID_ASSETS.md` |
| Unity C# rules (supplementary) | `.cursorrules` |
