---
type: Documentation Hub
title: AI Workflow & Asset Map
description: Where every agent asset lives (rules, commands, skills, agents, memory, workflows) and the SDLC loop they support.
resource: ai-workflow/README.md
tags: [docs, agents, workflow]
timestamp: 2026-06-28T00:00:00Z
okf_version: 0.1
---

# AI workflow & asset map

ProjectTwelve follows a spec-first loop adapted from `synthet-code-framework`.

## Where agent assets live

| Asset | Location | Notes |
|-------|----------|-------|
| Claude commands | `.claude/commands/*.md` | **Canonical** authoring source |
| Claude skills | `.claude/skills/*/SKILL.md` | **Canonical** authoring source |
| Claude subagents | `.claude/agents/*.md` | **Canonical** authoring source |
| Claude rules | `.claude/rules/*.md` | Always-on guidance |
| Cursor mirror | `.cursor/{rules,commands,skills,agents}` | **Generated** from `.claude/` — do not edit by hand |
| MCP template | `.cursor/mcp.example.json`, `.mcp.json` | Copy to gitignored `.cursor/mcp.json` to attach servers |
| Agent governance | `.agent/` | Safety, inventory, subagent role matrix, workflow playbooks |
| Project memory | `.agent-memory/` | log → dream → promote (see `CURSOR_USAGE.md`) |
| Workflow playbooks | `.agent/workflows/*.md` | spec / plan / implement / pr-ready / test-and-fix / … |
| Unity C# rules (supplementary) | `.cursorrules` | Long-form Unity coding spec |

**Single source of truth:** edit assets under `.claude/` + `.agent/`, then run
`python scripts/sync_assistant_trees.py` to regenerate the `.cursor/` mirror.

## The SDLC loop

```
/spec → /plan → /implement → /test-and-fix → /pr-ready → (optional) /subagent-review → /release-notes
```

1. **Spec** — identify the gameplay, tooling, or documentation outcome and affected canonical sources.
2. **Plan** — list the files to change, validation commands, and Unity asset/meta implications.
3. **Implement** — make focused diffs; avoid unrelated formatting or broad rewrites.
4. **Test and fix** — run the relevant commands from `AGENTS.md`; document environment limitations when Unity is unavailable.
5. **PR-ready** — review `git status --short`, run `python3 scripts/check_paid_assets.py --staged`, summarize changes, cite tests, and note follow-up work.

### Backlog

- Pick work from open tickets in [`docs/wiki/tickets/`](../wiki/tickets/) sorted by phase/priority.
- Claim via `/task-claim <issue-number>` (assigns GitHub issue; updates ticket frontmatter).
- PR descriptions **must** include `Closes #<N>`.
- Full contract: [`docs/project/00-backlog-workflow.md`](../project/00-backlog-workflow.md).

### Review and docs

- **Review:** `/critical-commit-audit` for high-severity bug hunts; `/check-subagents` +
  `/run-codex-review` / `/run-gemini-review` for external second opinions (requires subagent-orchestrator MCP).
- **Docs:** `/wiki-ingest`, `/wiki-lint`, `/wiki-query` keep `docs/` healthy (see [WIKI_SCHEMA](../WIKI_SCHEMA.md)).
- **Memory:** `/log-session` → `/dream-memory` → `/promote-memory` → `/memory-context`.

## Unity MCP

When the Unity Editor is open, agents can call Editor tools (scenes, assets, console) through [Unity MCP](https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.9/manual/integration/unity-mcp-overview.html). Setup: `AGENTS.md` → **Unity MCP (Editor bridge)**. Package: `com.unity.ai.assistant` in `Packages/manifest.json`.

## Safety

All of the above operate under [`.agent/SAFETY.md`](../../.agent/SAFETY.md),
[`docs/PAID_ASSETS.md`](../PAID_ASSETS.md), and [`docs/security.md`](../security.md).

See `.agent/AGENT_INFRA_INVENTORY.md` for the full adopted agent infrastructure catalog.
