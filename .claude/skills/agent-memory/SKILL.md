---
name: agent-memory
description: Log agent sessions, consolidate project memory (dream), promote reviewed memory, and load context for new chats. Use when ending a session, improving cross-session recall, or when the user mentions agent memory, dream, log-session, or memory.md.
---

# Agent memory (compiled harness)

Local consolidation for **ProjectTwelve** — external artifacts only (no model training).
Scripts under [`scripts/agent-memory/`](../../../scripts/agent-memory/).

## When to use

- **Session start (required):** `python scripts/agent-memory/context.py` (or read
  `.agent-memory/memory.md`). Prefer repo evidence on conflict. Rule:
  `.claude/rules/agent-memory.md`.
- **Session end / milestone:** `/log-session`.
- **Periodic:** `/dream-memory` → review changelog → `/promote-memory` after human approval.

## Commands (repo root)

```powershell
python scripts/agent-memory/context.py
python scripts/agent-memory/log_session.py --summary "..." --outcome "..." --candidate "text|working_rule|high"
python scripts/agent-memory/dream.py
python scripts/agent-memory/promote_dream.py --dream .agent-memory/dreams/<timestamp>.md
```

Candidate shape: `text|category|confidence` with categories
`stable_fact | user_preference | working_rule | recurring_issue | successful_pattern | open_question | deprecated`
and confidence `low | medium | high`.

## LLM / human judgment

- Dream: review `dreams/*-changelog.md` **Uncertain**, redact secrets, promote only when accurate.
- Do not edit `memory.md` directly during implementation.
- Committed `.agent-memory/memory.md` wins over Claude Code native personal memory on conflict.

## Safety

Scripts block common secret patterns. Never log `.env`, tokens, or personal library paths.
`raw-sessions/` and `dreams/` are gitignored; shared files are `memory.md`, `schema.md`,
`config.json`, `CURSOR_USAGE.md`.

## Reference

- [`.agent-memory/CURSOR_USAGE.md`](../../../.agent-memory/CURSOR_USAGE.md)
- [`.agent-memory/schema.md`](../../../.agent-memory/schema.md)
- Compilation policy: [`.agent/SKILL_COMPILATION.md`](../../../.agent/SKILL_COMPILATION.md)
