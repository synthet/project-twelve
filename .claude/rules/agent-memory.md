---
description: Session-start project memory — read memory.md before coding; log learnings at session end.
alwaysApply: true
---

# Project memory (always on)

Cross-session learnings live in [`.agent-memory/memory.md`](../../.agent-memory/memory.md).

## Session start

Before modifying code or picking up a ticket:

1. **Read** `.agent-memory/memory.md` (or run `python scripts/agent-memory/context.py` for a compact dump).
2. Treat memory as **helpful but not infallible** — prefer current repo evidence (files, tests, docs) when they conflict.
3. Use the [`agent-memory` skill](../../.claude/skills/agent-memory/SKILL.md) for log → dream → promote workflow.

## Session end / milestones

When you discover durable facts, preferences, recurring issues, or successful patterns:

- Run `/log-session` or `python scripts/agent-memory/log_session.py` with `--candidate` entries.
- Do **not** hand-edit `memory.md` during implementation; promote via `/dream-memory` → human review → `/promote-memory`.

## Quick reference

| Action | Command |
|--------|---------|
| Load memory | `python scripts/agent-memory/context.py` |
| Log session | `/log-session` |
| Consolidate proposal | `/dream-memory` |
| Approve into memory | `/promote-memory` |

Full usage: [`.agent-memory/CURSOR_USAGE.md`](../../.agent-memory/CURSOR_USAGE.md).
