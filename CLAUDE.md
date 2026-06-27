# ProjectTwelve — Unity 2D Sandbox Prototype

ProjectTwelve is a Unity C# prototype for a Terraria-like 2D sandbox with chunked world data, procedural terrain generation, chunk-local rendering, collision rebuilds, and basic tile editing.

This repository adopts the reusable agent practices from `synthet-code-framework`: keep build/test commands explicit, document canonical sources, prefer minimal diffs, preserve safety rules, and keep PR handoffs test-focused.

## Architecture
| Module / component | Role |
|--------------------|------|
| `Assets/Scripts/SandboxTile.cs` | Tile IDs and tile state. |
| `Assets/Scripts/SandboxChunk.cs` | Fixed-size chunk data and dirty flags. |
| `Assets/Scripts/SandboxWorld.cs` | Chunk loading, procedural generation, and tile edits. |
| `Assets/Scripts/SandboxChunkRenderer.cs` | Chunk mesh/collider rebuilds. |
| `Assets/Scripts/SandboxPlayerController.cs` | Prototype player movement and mouse tile editing. |
| `docs/wiki/` | Open architecture and implementation reference. |

## Commands
```bash
# Requires Unity Editor 6.0.2+ in PATH.
Unity -batchmode -quit -projectPath . -logFile Logs/unity-validate.log
Unity -batchmode -quit -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile Logs/unity-editmode-tests.log

# Documentation/link hygiene.
python3 scripts/check_markdown_links.py
```

## Development Guidelines
- Preserve Unity `.meta` files with every asset change.
- Keep runtime code in `Assets/Scripts/` focused by responsibility: data, world simulation, rendering, input, and future persistence should remain separable.
- Do not hardcode machine-specific paths; prefer project-relative paths or Unity APIs.
- Keep public interfaces stable unless docs and callers are updated together.
- Use minimal diffs; avoid unrelated refactors and broad formatting changes.
- Commit no secrets. Use `.env`, `.env.*`, `secrets.json`, or local editor settings for credentials and machine-local configuration.
- Never modify `.git/config` or introduce non-standard git extensions.

## Documentation
Start with `docs/CANONICAL_SOURCES.md` for authority mapping, then `docs/wiki/README.md` for system design. Agent workflow and safety references live in `docs/ai-workflow/README.md`, `.agent/SAFETY.md`, and `.agent/AGENT_INFRA_INVENTORY.md`.
