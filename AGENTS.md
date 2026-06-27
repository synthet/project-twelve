# AI Agents Configuration — ProjectTwelve

## Overview
ProjectTwelve is a Unity 2D sandbox prototype. This repo adopts the reusable agent-operation practices from `synthet-code-framework`: explicit build/test commands, safety rules, MCP configuration hygiene, canonical-source docs, minimal diffs, and PR-ready validation.

## Commands
```bash
# Open/build in Unity Editor 6.0.2+; Unity regenerates solution files.
# Batch-mode edit validation (requires Unity installed in the environment):
Unity -batchmode -quit -projectPath . -logFile Logs/unity-validate.log

# Run Unity edit-mode tests (requires Unity installed in the environment):
Unity -batchmode -quit -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile Logs/unity-editmode-tests.log

# Documentation/link hygiene:
python3 scripts/check_markdown_links.py
```

## MCP servers
- Define project MCP servers in [`.mcp.json`](.mcp.json); keep secrets in environment variables only.
- Prefer a compact `search` + `dispatch` surface over exposing many raw tools.
- Use project-scoped names such as `project-twelve-*`.
- Local Cursor MCP config should be untracked at `.cursor/mcp.json`.

## Common workflow
Use a spec-first flow for non-trivial changes: clarify scope, plan, implement, test, then prepare a PR summary. Keep changes focused and cite the relevant docs or code in handoff notes.

## Git configuration — do not modify
Never modify `.git/config` or add non-standard git extensions. If a worktree is needed, use a temporary one and clean it up immediately.

## Coding-agent contract
- **Unity scope:** preserve Unity `.meta` files when adding, moving, or deleting assets.
- **Code style:** match the surrounding C# style; prefer explicit access modifiers and serialized private fields for Inspector settings.
- **Architecture:** keep world data, rendering, player input, and persistence concerns separated unless an explicit contract change is documented.
- **Security:** secrets belong in `secrets.json`, `.env`, or environment variables; never commit tokens, service keys, or machine-specific paths.
- **Change control:** make minimal diffs, include tests or validation notes for behavior changes, and avoid drive-by reformatting.
- **Documentation:** update `README.md`, `docs/wiki/`, or `docs/CANONICAL_SOURCES.md` when changing architecture, workflows, or public conventions.

## Test vocabulary
| You say | Canonical name | Where | How to run |
|---------|----------------|-------|------------|
| Unity validation | Batch-mode project load | Unity project root | `Unity -batchmode -quit -projectPath . -logFile Logs/unity-validate.log` |
| EditMode tests | Unity EditMode test suite | Unity Test Framework | `Unity -batchmode -quit -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile Logs/unity-editmode-tests.log` |
| Markdown links | Docs link checker | `docs/`, `README.md`, agent docs | `python3 scripts/check_markdown_links.py` |

## AI workspace assets
| Asset | Location |
|-------|----------|
| Agent contract | `AGENTS.md` |
| Assistant orientation | `CLAUDE.md` |
| Safety rules | `.agent/SAFETY.md` |
| Agent inventory | `.agent/AGENT_INFRA_INVENTORY.md` |
| Workflow index | `docs/ai-workflow/README.md` |
| Canonical sources | `docs/CANONICAL_SOURCES.md` |
