---
capability: "pick-next-task agent asset workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: medium
---

> **Claude Code:** Same intent as Cursor `/pick-next-task`. When customizing, keep in sync with `.cursor/commands/pick-next-task.md`.

# /pick-next-task — recommend the next backlog ticket to work on

Use when you want to start work but don't yet have a specific ticket. Scans open
wiki tickets, ranks them by phase and priority, and recommends the top candidate.
This is the read-only "what's next" step **before** `/task-claim <N>`.

**Usage:**
```
/pick-next-task [--phase P1] [--tag rendering]
```

Optional `$ARGUMENTS`:
- `--phase <Pn>` — restrict to a single phase (e.g. `--phase P1`).
- `--tag <tag>` — restrict to tickets whose frontmatter `tags` include `<tag>`.

See [`docs/project/00-backlog-workflow.md`](../../docs/project/00-backlog-workflow.md)
and the [`backlog-queue`](../skills/backlog-queue/SKILL.md) skill for the full contract.

## Action

Run in order. Do **not** claim, assign, or edit anything — this command only recommends.

### 1. Sync first (recommended)

If the working tree may be stale, refresh the backlog before ranking:

```bash
python scripts/fetch_remotes.py --fetch-only
```

### 2. Read the ticket index

Read [`docs/wiki/tickets/README.md`](../../docs/wiki/tickets/README.md) and the ticket
files under `docs/wiki/tickets/`. Each ticket's status lives in YAML frontmatter
(`status: open | claimed | in_progress | blocked | done`).

### 3. Filter to workable tickets

Keep only tickets whose `status` is `open`. Exclude `claimed`, `in_progress`,
`blocked`, `implemented`, and `done`. Apply any `--phase` / `--tag` filters from
`$ARGUMENTS`.

### 4. Rank

Sort ascending by phase (`P0 → P1 → P2 → P3 → P4 → P5`), then by ticket ID within
the phase (lexical, e.g. `P2-AI-001` before `P2-DATA-001`). The first entry is the
recommendation; keep the next 2–3 as alternates.

### 5. Recommend

Report, without mutating state:
- **Recommended ticket:** ID, title, `status`, phase, and linked GitHub issue (`#N`).
- **Why:** lowest open phase/priority (and any filters applied).
- **Alternates:** the next 2–3 candidates.
- **Next step:** `Run /task-claim <N> to claim it.`

If no ticket qualifies, stop and say so — **do not invent work.** Suggest the
maintainer file a ticket (see the `backlog-queue` skill), or relax `--phase` / `--tag`.

## Notes

- Read-only: no `gh` edits, no frontmatter changes, no board Stage moves (this fork
  does not use GitHub Project Stage fields).
- Claiming is a separate, explicit step: `/task-claim <N>`.
- A ticket with no `github_issue` link is not claimable — flag it and suggest
  `python3 scripts/sync_wiki_tickets_to_github.py`.

## Checklist

- [ ] Only `status: open` tickets considered
- [ ] Ranked by phase then ID
- [ ] Nothing claimed or edited
- [ ] Clear next step (`/task-claim <N>`) given
