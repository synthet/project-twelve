---
description: Core SDLC conventions — backlog discipline, minimal diffs, canonical sources.
alwaysApply: true
---

# SDLC core (always on)

- **Work from the board.** Pick/claim from `Stage = Ready` via `/task-claim <N>`; don't invent work or
  skip Stage transitions. See [`docs/project/00-backlog-workflow.md`](../../docs/project/00-backlog-workflow.md).
- **Minimal diffs.** Targeted edits over rewrites; no drive-by refactors; touch one module per task.
- **Tests for behavior changes.** Run the narrowest scope from [`AGENTS.md`](../../AGENTS.md) test vocabulary — including offline `npm test` in `tools/tile-viz` / `tools/world-viz` when touching autotile or terrain generation.
- **Quality gates before push.** Run all checks locally: markdown links, OKF frontmatter (if docs change), paid assets, tree sync (if .claude/ changes). See [`CLAUDE.md`](../../CLAUDE.md) § Commands.
- **Don't invent contracts.** Check [`docs/CANONICAL_SOURCES.md`](../../docs/CANONICAL_SOURCES.md)
  before using an API path, config key, schema name, or status value.
- **PRs reference issues** with `Closes #<N>`; keep the written contract and code in agreement.
- **Loop:** `/spec → /clarify → /plan → /tasks → /analyze → /implement → /test-and-fix → /pr-ready`.
