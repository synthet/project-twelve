# Canonical Sources



| Subject | Canonical source | Notes |

|---------|------------------|-------|

| Project overview and setup | `README.md` | User-facing entry point. |

| Agent commands and contracts | `AGENTS.md` | Source of truth for automation and agent behavior. |

| Assistant orientation | `CLAUDE.md` | High-level project context and workflow reminders. |

| Claude commands/skills/agents | `.claude/` | Canonical authoring source for slash commands, skills, subagents, rules. |

| Cursor mirror | `.cursor/` | Generated from `.claude/` via `scripts/sync_assistant_trees.py`. |

| Agent governance | `.agent/` | Safety, inventory, subagent role matrix, workflow playbooks. |

| Project memory | `.agent-memory/` | log → dream → promote workflow; see `CURSOR_USAGE.md`. |

| Backlog workflow | `docs/project/00-backlog-workflow.md` | Wiki-ticket + GitHub issue contract. |

| Backlog tickets | `docs/wiki/tickets/` | One markdown ticket per spec-driven task; links to GitHub issues. |

| Runtime implementation | `Assets/Scripts/` | Unity C# source for prototype behavior. |

| Unity configuration | `ProjectSettings/` and `Packages/manifest.json` | Unity-owned project configuration. |

| Architecture docs | `docs/wiki/` | Implementation-facing design details. |

| OKF adoption | `docs/OKF_ADOPTION.md` | Incremental frontmatter and docs bundle policy. |

| Wiki schema | `docs/WIKI_SCHEMA.md` | Taxonomy and maintenance rules for `docs/`. |

| Security | `docs/security.md` | Secret handling and agent security expectations. |

| External CLI reviews | `docs/EXTERNAL_CLI_REVIEWS.md` | subagent-orchestrator MCP usage. |

| Unity offline docs index | `docs/unity-reference/README.md` | Wiki-style TOC for local Unity 6.5 manual and ScriptReference (`Editor/Data/Documentation/en`). |

| Asset integration requirements | `docs/wiki/15-assets-integration.md` | Sprites, atlases, animations, rotations, and Unity/engine asset seams. |

| Visual behavior | `docs/VISUAL_BEHAVIOR_SPEC.md` | Autotile, character sheet, animation API contracts. |

| Visual machine setup | `docs/VISUAL_SETUP.md` | Local catalog import and inspector wiring. |

| Safety rules | `.agent/SAFETY.md` | Secret handling, git hygiene, and destructive-operation rules. |

| Paid/licensed assets | `docs/PAID_ASSETS.md` | Private submodule `Assets/_Licensed/` → `project-twelve-assets`; guard via `scripts/check_paid_assets.py`. |

| MCP server config | `.mcp.json` | Project-level MCP definitions with no inline secrets. |

| Unity AI Assistant | `Packages/manifest.json` → `com.unity.ai.assistant` | Official Unity MCP bridge and Assistant tooling; see `AGENTS.md`. |

| Unity MCP (Cursor) | `.cursor/mcp.json` (local) | Relay bridge to Unity Editor; template in `.cursor/mcp.example.json`. |

| Unity C# coding rules (supplementary) | `.cursorrules` | Long-form Cursor rules for Unity C# style and patterns. |



When sources disagree, update the stale document in the same change or call out the follow-up explicitly.
