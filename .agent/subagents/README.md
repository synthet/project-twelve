# Subagents and logical roles — ProjectTwelve

Logical roles map to subagent definitions under `.claude/agents/` (mirrored in `.cursor/agents/`).
Codex uses its native collaboration agents and the repo guidance in `AGENTS.md`; the Claude/Cursor
agent definition format is not copied into `.agents/skills/`.

| Role | Subagent | Allowed | Forbidden |
|------|----------|---------|-----------|
| PR hygiene | `pr-ready-hygiene` | Run validation, fix lint/test failures minimally, summarize PR | Weaken tests, broad refactors |
| Critical commit audit | `critical-commit-audit` | Read-only bug hunt on recent commits | Write fixes without user request |
| External Codex review | `external-codex-review` | Review-only via subagent-orchestrator MCP | Writes, secrets in context |
| External Gemini review | `external-gemini-review` | Review-only via subagent-orchestrator MCP | Writes, secrets in context |
| External CLI panel | `external-cli-reviewer` | Coordinate Codex + Gemini reviews | Writes, secrets in context |

External review agents require the sibling `subagent-orchestrator` MCP server (project key
`project-twelve-subagent-orchestrator`). See [`docs/EXTERNAL_CLI_REVIEWS.md`](../../docs/EXTERNAL_CLI_REVIEWS.md).

After editing subagent files, run `python scripts/sync_assistant_trees.py` and commit the canonical
and generated Cursor trees.
