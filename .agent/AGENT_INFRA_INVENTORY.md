# Agent Infrastructure Inventory

| Area | Path | Purpose |
|------|------|---------|
| Agent contract | `AGENTS.md` | Build/test commands, safety contract, MCP conventions. |
| Assistant orientation | `CLAUDE.md` | Project architecture, commands, and development guidelines. |
| MCP config | `.mcp.json` | Project-scoped Claude Code MCP server definitions. |
| Unity AI Assistant | `Packages/manifest.json` (`com.unity.ai.assistant`) | Official Unity MCP bridge; Edit → Project Settings → AI → Unity MCP. |
| Cursor MCP example | `.cursor/mcp.example.json` | Template for untracked local Cursor MCP config (relay + `UNITY_PROJECT_PATH`). |
| Cursor MCP (local) | `.cursor/mcp.json` | Gitignored; machine-local relay path for this repo. |
| Safety rules | `.agent/SAFETY.md` | Secret handling, git hygiene, and Unity asset safety. |
| Workflow docs | `docs/ai-workflow/README.md` | Spec-plan-implement-test-PR loop for agents. |
| Canonical sources | `docs/CANONICAL_SOURCES.md` | Authority map for code and documentation changes. |
| Markdown link check | `scripts/check_markdown_links.py` | Lightweight local validation for Markdown links. |
