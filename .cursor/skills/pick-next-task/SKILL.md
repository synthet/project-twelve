---
name: pick-next-task
description: Recommend the next open wiki ticket to work on, ranked by phase then priority, without claiming or mutating anything. Use when the user asks "what's next", to pick the next task, or to start work without a specific ticket in hand.
---

# Pick next task (ProjectTwelve fork)

> **ProjectTwelve adaptation:** The canonical task queue is **wiki tickets** at
> [`docs/wiki/tickets/README.md`](../../../docs/wiki/tickets/README.md), each with frontmatter linking to a GitHub issue.
> This skill only **recommends** the next ticket. Claiming is the separate, explicit `/task-claim <N>` step.
> This fork does not use GitHub Project Stage fields.

## When to use

- The user asks "what's next", "pick the next task", or "what should I work on".
- Work is starting but no specific ticket/issue has been chosen yet.
- You need a ranked shortlist of workable backlog items.

Do **not** use this to claim, assign, or edit tickets — that is `/task-claim` and the
[`backlog-queue`](../backlog-queue/SKILL.md) contract.

## Contract (read-only)

### 1. Sync (recommended)

Refresh the backlog before ranking if the tree may be stale:

```bash
python scripts/fetch_remotes.py --fetch-only
```

### 2. Read the ticket index and files

Read [`docs/wiki/tickets/README.md`](../../../docs/wiki/tickets/README.md) and the ticket
markdown under `docs/wiki/tickets/`. Status is in YAML frontmatter:
`status: open | claimed | in_progress | blocked | implemented | done`.

### 3. Filter to workable tickets

Keep only `status: open`. Exclude every other status. Honor optional filters:
- `--phase <Pn>` — a single phase (e.g. `P1`).
- `--tag <tag>` — tickets whose frontmatter `tags` include `<tag>`.

### 4. Rank

Sort ascending by phase (`P0 → P5`), then by ticket ID within the phase (lexical).
The first entry is the recommendation; keep 2–3 alternates.

### 5. Recommend (no mutation)

Report:
- **Recommended:** ID, title, `status`, phase, linked issue `#N`.
- **Why:** lowest open phase/priority (plus any filters).
- **Alternates:** next 2–3 candidates.
- **Next step:** `Run /task-claim <N>.`

If nothing qualifies, stop and say so. **Do not invent work.** Suggest filing a ticket
(see [`backlog-queue`](../backlog-queue/SKILL.md)) or relaxing filters. A ticket lacking a
`github_issue` link is not claimable — flag it and suggest
`python3 scripts/sync_wiki_tickets_to_github.py`.

## Guardrails

- **Read-only.** No `gh` edits, no frontmatter writes, no board Stage moves.
- **Never invent work** not represented by an open ticket.
- Claiming and status transitions belong to `/task-claim` and `backlog-queue`.

## Related

- Slash command: [`/pick-next-task`](../../commands/pick-next-task.md).
- Claim step: [`/task-claim`](../../commands/task-claim.md).
- Full contract: [`docs/project/00-backlog-workflow.md`](../../../docs/project/00-backlog-workflow.md).
