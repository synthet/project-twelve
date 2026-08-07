---
capability: "task-claim agent asset workflow"
side_effect_level: remote_write
approval_required: true
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: medium
---

> **Claude Code:** Same intent as Cursor `/task-claim`. ProjectTwelve fork — issue-only claim (no GitHub Project Stage moves).

# /task-claim — claim a backlog issue (wiki-ticket workflow)

Use when starting work on a backlog item. `$ARGUMENTS` is the issue number (and optional repo).

**Usage:**
```
/task-claim <issue-number> [--repo <owner/repo>]
```

If `--repo` is omitted, default to `synthet/project-twelve`.

See [`docs/project/00-backlog-workflow.md`](../../docs/project/00-backlog-workflow.md) for the full contract.

## Action

Run the steps in order. Stop and report on any failure — do not proceed to the next step.

### 1. Resolve repo + verify the issue is claimable

```bash
OWNER="synthet"
REPO="project-twelve"           # override via --repo owner/repo
N="<issue-number>"

gh issue view "$N" --repo "$OWNER/$REPO" --json number,state,assignees,title
```

If `state == "CLOSED"`, abort: report "issue is closed".
If `assignees` is non-empty and you are not in it, abort: report who has it (require explicit override).

### 2. Assign yourself

```bash
gh issue edit "$N" --repo "$OWNER/$REPO" --add-assignee @me
```

### 3. Find the matching wiki ticket

Search `docs/wiki/tickets/` for frontmatter containing `github_issue` linking to issue `#N`.
If found, update frontmatter `status: claimed`.

### 4. Confirm to the user

Report:
- Issue title and number
- Link to wiki ticket (if found)
- Reminder: update ticket `status: in_progress` on first commit; PR must include `Closes #N`

## Notes

- This ProjectTwelve fork does **not** move GitHub Project board Stage fields.
- If no wiki ticket exists for the issue, warn the user and suggest creating one under `docs/wiki/tickets/`.
