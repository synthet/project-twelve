---
capability: "implement agent asset workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: medium
---

> **Claude Code:** Same intent as Cursor `/implement`. When customizing, keep in sync with `.cursor/commands/implement.md`.

# /implement — Execute an approved plan

Use when the user has approved a plan or given a small, explicit task.

## Inputs

- Approved plan and, for non-trivial work, approved `/tasks` task list with `T-n` IDs mapped to `AC-n` criteria.
- **AGENTS.md** for lint/test/build commands.

## Steps

1. Select the next `T-n` task (when applicable) and confirm its linked `AC-n` criteria and verification command.
2. Write the failing test stubs from the plan/tasks **before** implementation; confirm they fail.
3. Implement in **minimal diffs** until the stubs pass; match existing style.
4. Run **lint** and **tests** from AGENTS.md; fix failures.
5. Update task status/evidence so `/pr-ready` can trace changes back to `AC-n` criteria.
6. Summarize what changed and where.

## Done when

- All agreed items are implemented.
- Tests written-and-failing before code, now passing (or failures explained with next steps).

## Checklist

- [ ] Each implemented `T-n` maps to one or more `AC-n` criteria (when tasks exist)
- [ ] Test stubs written and **failing** before implementation began
- [ ] Tests pass after implementation
- [ ] No unrelated refactors
- [ ] No secrets or licensed art committed (`python3 scripts/check_paid_assets.py --staged`)
- [ ] AGENTS.md commands run (or documented why not)
