# Canonical Sources

| Subject | Canonical source | Notes |
|---------|------------------|-------|
| Project overview and setup | `README.md` | User-facing entry point. |
| Agent commands and contracts | `AGENTS.md` | Source of truth for automation and agent behavior. |
| Assistant orientation | `CLAUDE.md` | High-level project context and workflow reminders. |
| Runtime implementation | `Assets/Scripts/` | Unity C# source for prototype behavior. |
| Unity configuration | `ProjectSettings/` and `Packages/manifest.json` | Unity-owned project configuration. |
| Architecture docs | `docs/wiki/` | Implementation-facing design details. |
| Asset integration requirements | `docs/wiki/15-assets-integration.md` | Sprites, atlases, animations, rotations, and Unity/engine asset seams. |
| Safety rules | `.agent/SAFETY.md` | Secret handling, git hygiene, and destructive-operation rules. |
| MCP server config | `.mcp.json` | Project-level MCP definitions with no inline secrets. |

When sources disagree, update the stale document in the same change or call out the follow-up explicitly.
