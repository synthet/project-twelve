---
name: pick-next-task
description: Recommend the next open wiki ticket to work on, ranked by phase then priority, without claiming or mutating anything. Use when the user asks "what's next", to pick the next task, or to start work without a specific ticket in hand.
---

# Pick next task (compiled harness)

> Wiki tickets at [`docs/wiki/tickets/`](../../../docs/wiki/tickets/) are the queue.
> This skill **recommends** only. Claiming is `/task-claim <N>`.
> Runner: [`scripts/pick_next_task.py`](../../../scripts/pick_next_task.py).

## When to use

- User asks "what's next", "pick the next task", or starts work without a ticket.
- Need a ranked shortlist of open backlog items.

Do **not** claim or edit tickets here — that is `/task-claim` and [`backlog-queue`](../backlog-queue/SKILL.md).

## Run (repo root)

```bash
python scripts/pick_next_task.py
python scripts/pick_next_task.py --phase P2
python scripts/pick_next_task.py --tag ux
python scripts/pick_next_task.py --json
python scripts/pick_next_task.py --fetch-only   # refresh remotes first, then rank
```

The harness filters `status: open`, ranks by phase (`P0`→`P5`) then ticket id, prints the
recommendation + alternates, and flags tickets missing `github_issue` as unclaimable.

## After the script

Present the script output to the user. **Next step:** `/task-claim <N>` when claimable.
If nothing qualifies, stop — do not invent work. Suggest filing via backlog-queue or
`python scripts/sync_wiki_tickets_to_github.py` when the issue link is missing.

## Related

- Slash command: [`/pick-next-task`](../../../.claude/commands/pick-next-task.md)
- Claim: [`/task-claim`](../../../.claude/commands/task-claim.md)
- Contract: [`docs/project/00-backlog-workflow.md`](../../../docs/project/00-backlog-workflow.md)
- Compilation policy: [`.agent/SKILL_COMPILATION.md`](../../../.agent/SKILL_COMPILATION.md)
