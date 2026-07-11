# ProjectTwelve — Unity 2D Sandbox Prototype

ProjectTwelve is a Unity C# prototype for a Terraria-like 2D sandbox with chunked world data, procedural terrain generation, chunk-local rendering, collision rebuilds, and basic tile editing.

This repository adopts the reusable agent practices from `synthet-code-framework`: slash commands, skills, subagents, safety rules, project memory, OKF docs tooling, explicit build/test commands, canonical sources, minimal diffs, and PR-ready validation.

## Backlog

The canonical task queue is **wiki tickets** under [`docs/wiki/tickets/`](docs/wiki/tickets/), each linked to a GitHub issue. See [`docs/project/00-backlog-workflow.md`](docs/project/00-backlog-workflow.md) for the pick/claim/PR contract.

## Architecture

| Module / component | Role |
|--------------------|------|
| `Assets/Scripts/Sandbox/SandboxTile.cs` | Tile IDs and tile state. |
| `Assets/Scripts/Sandbox/SandboxChunk.cs` | Fixed-size chunk data and dirty flags. |
| `Assets/Scripts/Sandbox/SandboxWorld.cs` | Chunk loading, procedural generation, tile edits, and save/load. |
| `Assets/Scripts/Sandbox/SandboxChunkRenderer.cs` | Chunk mesh/collider rebuilds. |
| `Assets/Scripts/Sandbox/SandboxPlayerController.cs` | Prototype player movement and mouse tile editing. |
| `Assets/Scripts/Visual/` | Autotile, character, creature, monster, and effect presentation systems. |
| `Assets/Scripts/Visual/Tiles/` | Autotile masks, rule tables, resolver, chunk mesh sprite selection (`AutotileMaskBuilder`, `AutotileResolver`, `AutotileRuleTables`). |
| `Assets/Scripts/RuntimeMcp/` | Play Mode JSON-RPC MCP (`tile_autotile`, `tiles_area`, tile edits); loopback only. |
| `Assets/_Licensed/` | Git submodule → private licensed art and visual catalogs (`project-twelve-assets`). |
| `Assets/Scripts/Integration/PlayerAvatarFactory.cs` | Spawns player avatars without vendor script references. |
| `tools/world-viz/` | Engine-free terrain generation / ASCII debug; parity vs Unity golden fixture. |
| `tools/tile-viz/` | Engine-free autotile resolver + PNG compositor; parity with C# via exported JSON fixtures. |
| `docs/wiki/` | Open architecture and implementation reference. |
| `com.unity.ai.assistant` | [Unity MCP](https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.9/manual/integration/unity-mcp-overview.html) editor bridge; configure via Edit → Project Settings → AI → Unity MCP. |
| [FFF MCP](https://github.com/dmtrKovalenko/fff) | Optional fast file search for agents (`fffind`, `ffgrep`, `fff-multi-grep`); see `AGENTS.md` § FFF file search MCP. |

Full agent contract, MCP setup, and test vocabulary: [`AGENTS.md`](AGENTS.md).

### Visual & debug tooling

- [`docs/VISUAL_BEHAVIOR_SPEC.md`](docs/VISUAL_BEHAVIOR_SPEC.md) — autotile contract
- [`tools/tile-viz/README.md`](tools/tile-viz/README.md) — offline resolver CLI and fixture formats
- [`tools/world-viz/README.md`](tools/world-viz/README.md) — offline terrain viz

## Commands

```bash
# Requires Unity Editor 6.0.5.1f1 in PATH.
Unity -batchmode -quit -projectPath . -logFile Logs/unity-validate.log
Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile Logs/unity-editmode-tests.log

# Documentation/link hygiene and OKF validation (run before pushing any docs/ changes).
python3 scripts/check_markdown_links.py
python scripts/ci/okf_lint_changed.py --base origin/main --head HEAD --profile project --fail-on error
python3 scripts/check_paid_assets.py --staged
python scripts/sync_assistant_trees.py --check

# Offline tooling (no Unity)
cd tools/world-viz && npm test
cd tools/tile-viz && npm test
```

**When to run offline tests:**

- Autotile / visual tile logic changes → Unity EditMode tests **and** `tile-viz` npm test
- Terrain generation changes → Unity EditMode tests **and** `world-viz` npm test
- Rule-table changes → regenerate `tools/tile-viz/data/*.json` (Unity export tests or `tools/tile-viz/scripts/generate-rules-json.mjs`); do not hand-edit exported JSON

## SDLC loop

```
/spec → /plan → /implement → /test-and-fix → /pr-ready → (optional) /subagent-review → /release-notes
```

Slash commands live under `.claude/commands/` (canonical); Cursor mirrors them under
`.cursor/commands/`. Codex uses the root `AGENTS.md`, `.codex/config.toml`, and generated
`.agents/skills/` rather than these custom slash-command files.
See [`docs/ai-workflow/README.md`](docs/ai-workflow/README.md) for the full asset map.

## Development Guidelines

- Preserve Unity `.meta` files with every asset change.
- Keep runtime code in `Assets/Scripts/` focused by responsibility: data, world simulation, rendering, input, and future persistence should remain separable.
- Do not hardcode machine-specific paths; prefer project-relative paths or Unity APIs.
- Keep public interfaces stable unless docs and callers are updated together.
- Use minimal diffs; avoid unrelated refactors and broad formatting changes.
- Commit no secrets. Use `.env`, `.env.*`, `secrets.json`, or local editor settings for credentials and machine-local configuration.
- Never commit licensed Asset Store content into the **public** repo. Licensed art lives in the `Assets/_Licensed` submodule (`docs/PAID_ASSETS.md`). Run `python3 scripts/check_paid_assets.py --staged` before commits.
- Never modify `.git/config` or introduce non-standard git extensions.
- After editing `.claude/` assets, run `python scripts/sync_assistant_trees.py` and commit the
  `.cursor/` and `.agents/skills/` mirrors together.
- When changing autotile behavior, keep Unity C#, EditMode fixtures, and `tools/tile-viz` JS resolver in sync (see parity table in [`tools/tile-viz/README.md`](tools/tile-viz/README.md)).

## Documentation

Start with `docs/CANONICAL_SOURCES.md` for authority mapping, then `docs/wiki/README.md` for system design. Agent workflow and safety references live in `docs/ai-workflow/README.md`, `.agent/SAFETY.md`, and `.agent/AGENT_INFRA_INVENTORY.md`.
