---
name: eval
description: Capture task quality signals and log them to agent memory to build a feedback loop.
  Use at the end of each implemented task or merged PR, or whenever a task required more than one
  agent iteration.
---

# eval

Eval design — building feedback loops with verifiable signals — is a core agentic engineering
skill. Without it, the project improves only by accident. This skill closes that loop by logging
structured quality signals after each task so patterns surface in project memory.

## When to use

- After any `/implement` + `/pr-ready` cycle completes (success or not)
- When a task required more than one agent round to complete
- When tests were missing and had to be written after the fact
- When a regression was introduced and caught (or missed) during the task

## Three signals to capture

For each completed task, measure:

| Signal | Question | Values |
|--------|----------|--------|
| `test_pass_rate` | Did all tests pass on the **first** agent attempt? | `yes` / `partial` / `no` |
| `first_try_success` | Was the implementation accepted without revision? | `yes` / `no` |
| `iteration_count` | How many agent rounds before done? | integer |

## Step-by-step

### 1. Assess the outcome

After the task completes, answer the three signal questions above.

### 2. Map to a memory candidate

| Outcome | Category | Confidence | Example text |
|---------|----------|------------|--------------|
| First-try success, all tests green | `successful_pattern` | `high` | "EditMode autotile tests + tile-viz npm test: zero retries" |
| Required 2–3 iterations | `recurring_issue` | `medium` | "Unity sprite bounds changes need tile-viz parity check" |
| Required >3 iterations | `recurring_issue` | `high` | "Autotile rule-table edits need C# export regen before npm test" |
| Tests were missing (written after code) | `working_rule` | `high` | "Always run tile-viz npm test when touching AutotileResolver" |
| Regression caught in review | `working_rule` | `high` | "Run EditMode tests before merging Visual/Tiles changes" |
| Regression shipped (caught later) | `recurring_issue` | `high` | "world-viz parity drift — add fixture when terrain gen changes" |

### 3. Log the session

```bash
python scripts/agent-memory/log_session.py \
  --summary "Implemented <feature> (issue #<N>)" \
  --outcome "<first_try_success|partial|multi-iteration>" \
  --test-results "<pass|partial|fail>" \
  --candidate "<memory text>|<category>|<confidence>"
```

Add a second `--candidate` flag for each additional insight from the task.

### 4. Periodic review

After 5–10 tasks, run `/dream-memory` and check the `## Successful Patterns` and
`## Recurring Issues` sections. If the same module or workflow appears in recurring issues
more than twice, that is a signal to improve the spec template, add a pre-flight checklist,
or improve test coverage for that area.

## Done when

- At least one memory candidate is logged per task outcome.
- Any pattern appearing ≥ 3 times in recurring issues has a proposed fix filed as a backlog issue.

## Cross-references

- `/log-session` — CLI wrapper for `log_session.py`
- `/dream-memory` — Consolidate logged sessions into a proposed memory update
- `agent-memory` skill — Full memory pipeline (log → dream → promote → context)
- `backlog-queue` skill — File a follow-up issue for systemic problems found via eval
