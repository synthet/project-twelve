---
type: Runbook
title: Backlog Workflow
description: The canonical backlog contract for ProjectTwelve — wiki tickets linked to GitHub issues.
resource: project/00-backlog-workflow.md
tags: [docs, project, backlog, workflow]
timestamp: 2026-06-28T00:00:00Z
okf_version: 0.1
---

# Backlog workflow

The canonical task queue for ProjectTwelve is **wiki tickets** under [`docs/wiki/tickets/`](../wiki/tickets/),
each linked to a GitHub issue. This replaces the upstream framework's GitHub Project Stage workflow.

Agent-facing rules live in the [`backlog-queue`](../../.claude/skills/backlog-queue/SKILL.md) skill.

## The five-step contract

1. **Pick** from open tickets in [`docs/wiki/tickets/`](../wiki/tickets/), sorted by phase (P0 → P5) and priority. If nothing is ready, ask the maintainer — do not invent work.
2. **Claim** the linked issue (`/task-claim <N>`): assigns you via `gh issue edit` and updates ticket frontmatter `status: claimed`.
3. **In progress** — set ticket `status: in_progress` on your first commit.
4. **Blocked** → comment on the GitHub issue with reason + unblock condition; set ticket `status: blocked`.
5. **Reference** the issue in the PR (`Closes #<N>`); set ticket `status: done` on merge.

## Ticket format

The canonical task ID format, area-code registry, and full ticket schema live in
[`docs/wiki/task-schema.md`](../wiki/task-schema.md). In short, each ticket is a markdown file
with YAML frontmatter:

```yaml
---
id: P1-EDIT-001
title: "[P1-EDIT-001] Specify tile edit flow..."
status: open          # open | claimed | in_progress | blocked | done
phase: "Phase P1 — ..."
github_issue: "https://github.com/synthet/project-twelve/issues/N"
spec_references:
  - "docs/wiki/spec-driven-development-tasks.md"
---
```

See existing tickets for examples.

## GitHub issue sync

Create or update GitHub issues from wiki tickets:

```bash
python3 scripts/sync_wiki_tickets_to_github.py
```

Issue bodies should link back to the wiki ticket path.

## Labels

Prefer `spec-driven` plus phase labels (`p0`, `p1`, …) on all backlog issues.

## Related docs

- Ticket index: [`docs/wiki/tickets/README.md`](../wiki/tickets/README.md)
- Spec-driven task list: [`docs/wiki/spec-driven-development-tasks.md`](../wiki/spec-driven-development-tasks.md)
- Task schema & ID conventions: [`docs/wiki/task-schema.md`](../wiki/task-schema.md)
- Agent skill: [`.claude/skills/backlog-queue/SKILL.md`](../../.claude/skills/backlog-queue/SKILL.md)
