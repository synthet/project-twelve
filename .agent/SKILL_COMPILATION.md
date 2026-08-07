# Skill compilation — ProjectTwelve

Policy for turning crystallized agent skills into thin bootloaders + deterministic harnesses.
Inspired by [Vivek Haldar — Compiling an AI agent skill](https://vivekhaldar.com/articles/compiling-an-ai-agent-skill/)
and the [Token Shrinker](https://tokenshrinker.com/) profile → extract → partition → compile → replay loop.
Aligned with `synthet-code-framework` (last cherry-pick: ee11310 / 2026-07-19).

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
| **Code** | Paths, adapters, filters, ranks, command flags, XML/JSON parsing, terminal states | harness CLIs below |
| **LLM** | Interpreting failures, fuzzy selection, drafting prose, AC verdicts | Reading Unity log tails after harness failure |
| **Human** | Push, publish, paid-asset writes, promote memory | `assets-submodule-publish`, `/promote-memory` |

## Dual layout (both valid)

### A. Root harness (ProjectTwelve-native)

Prefer when the CLI is reused across skills or is project-wide tooling:

```text
scripts/<harness>.py              # deterministic CLI
.claude/skills/<name>/SKILL.md    # thin bootloader
.claude/skills/<name>/references/ # optional long tables
```

Examples: `scripts/run_unity_tests.py`, `scripts/pick_next_task.py`, `scripts/fetch_remotes.py`,
`scripts/agent-memory/*.py`.

### B. Per-skill harness (framework pattern)

Prefer when the procedure is skill-local and shares helpers under `scripts/skill_harness/`:

```text
.claude/skills/<name>/
├── SKILL.md           # thin bootloader: when-to-use, harness invoke, LLM slots
└── scripts/
    └── harness.py     # deterministic CLI; prefer --json for agents
scripts/skill_harness/ # shared parsers (acceptance, changelog, version, verify, search)
```

Examples: `commit-and-push`, `release-bump`, `validate-implementation`,
`verification-before-completion`, `search-tool-selection`.

Author under `.claude/`, then `python scripts/sync_assistant_trees.py` so `.cursor/skills/` and
`.agents/skills/` mirrors include `scripts/`. Do not hand-edit mirrors.

## Compiled in this repo (2026-07-22)

| Skill | Harness | LLM still used for |
|-------|---------|-------------------|
| `unity-tests` | `scripts/run_unity_tests.py` | Diagnosing failures from summary + logs |
| `pick-next-task` | `scripts/pick_next_task.py` | Presenting recommendation (ranking is code) |
| `repo-sync` | `scripts/fetch_remotes.py` | Interpreting sync errors / access issues |
| `agent-memory` | `scripts/agent-memory/*.py` | Dream review quality; promote approval is human |
| `commit-and-push` | `.claude/skills/commit-and-push/scripts/harness.py` | Commit message; paid-assets + licensed path gates are code |
| `release-bump` | `.claude/skills/release-bump/scripts/harness.py` | Choose major\|minor\|patch |
| `validate-implementation` | `.claude/skills/validate-implementation/scripts/harness.py` | Verdict + evidence per AC |
| `verification-before-completion` | `.claude/skills/verification-before-completion/scripts/harness.py` | Name claims; interpret outputs |
| `search-tool-selection` | `.claude/skills/search-tool-selection/scripts/harness.py` | Map ask → task type |

`backlog-queue` step 1 delegates pick to `pick_next_task.py`; claim/PR mutation stays procedural prose.

## Recipe (five moves)

1. **Profile** — rank skill reads / repeated workflows from session traces.
2. **Extract** — write down inputs, tools, outputs, and gates from the existing `SKILL.md`.
3. **Partition** — mark each step code / LLM / human.
4. **Compile** — implement the harness; shrink `SKILL.md` to trigger + command + judgment notes.
5. **Replay** — unit-test pure parsing/ranking (`tests/test_skill_harnesses.py`); smoke the CLI; sync + frontmatter checks.

## Measuring success

Without per-skill token telemetry in Cursor transcripts, prefer qualitative metrics:

- Bootloader `SKILL.md` line count vs prior SOP
- Agent tool turns before a correct Unity/ticket/ship command (should be ~1 harness call)
- Unit tests covering parsers so regressions do not require re-discovering flags

## Related

- [SKILL_INVENTORY.md](./SKILL_INVENTORY.md)
- [skill-authoring](../.claude/skills/skill-authoring/SKILL.md)
- [SKILL_CHANGE_AST10_REVIEW.md](./SKILL_CHANGE_AST10_REVIEW.md)
