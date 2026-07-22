---
name: backlog-queue
description: Wiki tickets under docs/wiki/tickets/ are the canonical task queue, each linked to a GitHub issue. Use when picking work, claiming an issue, or preparing PR references.
capability: "backlog-queue agent asset workflow"
side_effect_level: remote_write
approval_required: true
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: medium
---

# Backlog queue (ProjectTwelve fork)

> **ProjectTwelve adaptation:** The canonical task queue is **wiki tickets** at
> [`docs/wiki/tickets/README.md`](../../../docs/wiki/tickets/README.md), each with frontmatter linking to a GitHub issue.
> This fork replaces the upstream GitHub Project Stage workflow. Do not use board Stage field IDs.

## When to use

- The user asks to pick the next task, start work, or "what's next".
- The user asks to file a new backlog item.
- A PR is being prepared and needs a `Closes #N` reference.
- A task has hit a blocker or is ready for review.
- An agent picks up a task without a corresponding issue — stop and file one first (or run
  `python3 scripts/sync_wiki_tickets_to_github.py` if the ticket exists but issue is missing).

## The five-step contract

Every contributor (human or AI) follows the same five steps.

### 1. Pick from open wiki tickets

Run the compiled recommender (or use [`pick-next-task`](../pick-next-task/SKILL.md)):

```bash
python scripts/pick_next_task.py
```

It ranks open tickets by phase (P0 → P5) then ticket id. If nothing is ready, stop and ask the
maintainer. **Do not invent new work.**

### 2. Claim the linked GitHub issue

Use `/task-claim <issue-number>` (preferred) or:

```bash
gh issue edit <N> --repo synthet/project-twelve --add-assignee @me
```

Update the ticket markdown frontmatter `status` to `claimed` (or `in_progress` on first commit).

### 3. Mark in progress on first commit

Update ticket frontmatter `status: in_progress` when you push the first commit for the task.

### 4. If blocked, comment on the issue

```bash
gh issue comment <N> --repo synthet/project-twelve --body "Blocked: <reason + what would unblock>."
```

Update ticket frontmatter `status: blocked`.

### 5. PR references the issue

Your PR description **must** contain `Closes #<N>` when the PR **completes** the ticket. Use
`Refs #<N>` only for partial work that leaves the issue open.

**In the same PR** (or an immediate follow-up before calling the task done):

1. Set ticket frontmatter `status: done` (canonical values: `open | claimed | in_progress | blocked | done`).
2. Update the matching row in [`docs/wiki/tickets/README.md`](../../../docs/wiki/tickets/README.md).
3. Check exit-evidence boxes in the ticket body when the acceptance criteria are met.

After merge, confirm GitHub auto-closed the issue. If the PR used `Refs` by mistake, close the
issue manually with a comment citing the merge commit.

## Filing a new task

1. Add a ticket markdown file under `docs/wiki/tickets/` following existing frontmatter conventions.
2. Update [`docs/wiki/tickets/README.md`](../../../docs/wiki/tickets/README.md) index.
3. Create the GitHub issue (manually or via `scripts/sync_wiki_tickets_to_github.py`).
4. Link the issue URL in the ticket frontmatter (`github_issue`).

## Label conventions

When creating issues, prefer:

- `spec-driven` — all backlog items
- Phase labels matching ticket prefix (e.g. `p0`, `p1`)
- `type:spec`, `type:bug`, `type:chore` as appropriate

## Human reference

Full contract: [`docs/project/00-backlog-workflow.md`](../../../docs/project/00-backlog-workflow.md).
