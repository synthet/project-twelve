# Skill compilation — ProjectTwelve

Policy for turning crystallized agent skills into thin bootloaders + deterministic harnesses.
Inspired by [Vivek Haldar — Compiling an AI agent skill](https://vivekhaldar.com/articles/compiling-an-ai-agent-skill/)
and the [Token Shrinker](https://tokenshrinker.com/) profile → extract → partition → compile → replay loop.

## When to compile

Compile a skill when **all** of these hold:

1. It has been run enough times that the tool order and state shape are stable.
2. Most steps are inventory, filter, invoke, parse, or validate — not semantic judgment.
3. A wrong automated step is cheap to detect (exit codes, schemas, XML, frontmatter).

Leave natlang when the value is judgment, exploration, or changing APIs (e.g. pixellab-mcp,
systematic-debugging, security-review, CLI reference maps).

## Partition

| Owner | Owns | Examples |
|-------|------|----------|
| **Code** | Paths, adapters, filters, ranks, command flags, XML/JSON parsing, terminal states | `scripts/run_unity_tests.py`, `scripts/pick_next_task.py`, `scripts/fetch_remotes.py` |
| **LLM** | Interpreting failures, fuzzy selection, drafting prose | Reading Unity log tails after harness failure |
| **Human** | Push, publish, paid-asset writes, promote memory | `assets-submodule-publish`, `/promote-memory` |

## Layout

```text
scripts/<harness>.py              # deterministic CLI (preferred for cross-skill reuse)
.claude/skills/<name>/SKILL.md    # thin bootloader: when to use → run harness → judgment gates
.claude/skills/<name>/references/ # optional long tables moved out of the bootloader
```

Author under `.claude/`, then `python scripts/sync_assistant_trees.py`. Do not hand-edit
`.cursor/skills/` or `.agents/skills/`.

## Recipe (five moves)

1. **Profile** — rank skill reads / repeated workflows from session traces.
2. **Extract** — write down inputs, tools, outputs, and gates from the existing `SKILL.md`.
3. **Partition** — mark each step code / LLM / human.
4. **Compile** — implement the harness; shrink `SKILL.md` to trigger + command + judgment notes.
5. **Replay** — unit-test pure parsing/ranking; smoke the CLI; sync + frontmatter checks.

## Compiled in this repo (2026-07-19)

| Skill | Harness | LLM still used for |
|-------|---------|-------------------|
| `unity-tests` | `scripts/run_unity_tests.py` | Diagnosing failures from summary + logs |
| `pick-next-task` | `scripts/pick_next_task.py` | Presenting recommendation (ranking is code) |
| `repo-sync` | `scripts/fetch_remotes.py` | Interpreting sync errors / access issues |
| `agent-memory` | `scripts/agent-memory/*.py` | Dream review quality; promote approval is human |

`backlog-queue` step 1 delegates pick to `pick_next_task.py`; claim/PR mutation stays procedural prose.

## Measuring success

Without per-skill token telemetry in Cursor transcripts, prefer qualitative metrics:

- Bootloader `SKILL.md` line count vs prior SOP
- Agent tool turns before a correct Unity/ticket command (should be ~1 harness call)
- Unit tests covering parsers so regressions do not require re-discovering flags

## Related

- [SKILL_INVENTORY.md](./SKILL_INVENTORY.md)
- [skill-authoring](../.claude/skills/skill-authoring/SKILL.md)
- [SKILL_CHANGE_AST10_REVIEW.md](./SKILL_CHANGE_AST10_REVIEW.md)
