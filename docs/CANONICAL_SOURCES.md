---
type: Technical Reference
title: Canonical Sources
description: Authority map for project, agent, tooling, documentation, and asset configuration sources.
resource: CANONICAL_SOURCES.md
tags: [docs, governance, agents]
timestamp: 2026-07-12T06:10:00Z
okf_version: 0.1
---

# Canonical Sources



| Subject | Canonical source | Notes |

|---------|------------------|-------|

| Project overview and setup | `README.md` | User-facing entry point. |

| Agent commands and contracts | `AGENTS.md` | Source of truth for automation and agent behavior. |

| Assistant orientation | `CLAUDE.md` | High-level project context and workflow reminders. |

| Claude commands/skills/agents | `.claude/` | Canonical authoring source for slash commands, skills, subagents, rules. |

| Cursor mirror | `.cursor/` | Generated from `.claude/` via `scripts/sync_assistant_trees.py`. |
| Codex project config | `.codex/config.toml` | Trusted-project sandbox, agent, and portable MCP defaults. |
| Codex skill mirror | `.agents/skills/` | Generated from `.claude/skills/` via `scripts/sync_assistant_trees.py`. |

| Agent governance | `.agent/` | Safety, inventory, subagent role matrix, workflow playbooks. |

| Agent task prompts | `docs/prompts/` | Focused implementation and review prompts for agents (e.g. HUD polish). |

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
| HUD/UI framework | `docs/wiki/flexible-hud-framework.md` | Reusable uGUI HUD/UI framework: design tokens, scaling, layers, focus/input, inventory view-model boundary. |
| Ground autotile 32 rules | `docs/wiki/ground-autotile-32-rules.md` | PixelFantasy mask table, mirrored scenarios, acceptance checks. |

| Visual machine setup | `docs/VISUAL_SETUP.md` | Local catalog import and inspector wiring. |
| Visual catalog import pipeline | `docs/wiki/visual-catalog-import-pipeline.md` | Config precedence, generated catalog contracts, code-only behavior, and Unity/Python parity. |

| Safety rules | `.agent/SAFETY.md` | Secret handling, git hygiene, and destructive-operation rules. |

| Paid/licensed assets | `docs/PAID_ASSETS.md` | Private submodule `Assets/_Licensed/` → `project-twelve-assets`; guard via `scripts/check_paid_assets.py`. |
| Licensed asset inventory (public index) | `docs/wiki/licensed-assets-reference.md` | Points to public contracts vs private `Assets/_Licensed/docs/` submodule inventory. |
| Licensed asset inventory (private) | `Assets/_Licensed/docs/` (submodule) | Full vendor script API and asset catalogs; requires project-twelve-assets access. |

| MCP server config | `.mcp.json` | Project-level MCP definitions with no inline secrets. |

| Unity AI Assistant | `Packages/manifest.json` → `com.unity.ai.assistant` | Official Unity MCP bridge and Assistant tooling; see `AGENTS.md`. |

| Unity MCP (Cursor) | `.cursor/mcp.json` (local) | Relay bridge to Unity Editor; template in `.cursor/mcp.example.json`. |
| Unity MCP (Codex) | `~/.codex/config.toml` (local) | Machine-specific relay bridge; setup in `.codex/README.md`. |

| FFF file search MCP | [dmtrKovalenko/fff](https://github.com/dmtrKovalenko/fff) | Optional agent file search; wired via `project-twelve-fff-mcp` in `.cursor/mcp.example.json`, `.mcp.json`, and `.codex/config.toml`. |

| PixelLab MCP (Vibe Coding) | [pixellab.ai/mcp](https://www.pixellab.ai/mcp) | Optional pixel art generation; skill [`.claude/skills/pixellab-mcp/SKILL.md`](../.claude/skills/pixellab-mcp/SKILL.md); template in `.cursor/mcp.example.json` `_optionalServers.pixellab`. |
| PixelLab REST API v2 | [api.pixellab.ai/v2/llms.txt](https://api.pixellab.ai/v2/llms.txt) | HTTP/SDK endpoint index; local skill copy [`.claude/skills/pixellab-mcp/references/api-v2-llms.md`](../.claude/skills/pixellab-mcp/references/api-v2-llms.md). Prefer MCP when connected. |

| Unity C# coding rules (supplementary) | `.cursorrules` | Long-form Cursor rules for Unity C# style and patterns. |



When sources disagree, update the stale document in the same change or call out the follow-up explicitly.
