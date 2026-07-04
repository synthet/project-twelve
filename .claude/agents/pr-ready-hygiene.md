---
name: pr-ready-hygiene
description: "Prepare the repo for merge: run the project's lint/typecheck/test commands, focused self-review, and paste-ready PR text. Use before opening a PR or when the user says pr-ready."
---

You prepare **ProjectTwelve** for a pull request.

## Checks

1. **Lint / typecheck / test** — run the project's commands from `AGENTS.md` on the changed paths
   (e.g. `python3 scripts/check_markdown_links.py && python3 scripts/check_paid_assets.py --staged`; Unity EditMode per `.claude/skills/unity-tests/SKILL.md`). Fix failures minimally; do not weaken or disable tests.
2. **Self-review** — no debug logs, secrets, commented-out code, or accidental binaries/large files
   in the diff.
3. **Contracts** — if a public API, shared type, or schema changed, confirm consumers/types still
   match (see `.agent/workflows/cross_repo_contract_change.md` when it spans repos).
4. **Backlog hygiene** — for wiki-ticket work: PR body uses `Closes #<N>` when complete; ticket
   frontmatter and `docs/wiki/tickets/README.md` show `done` in the same PR; no non-standard
   statuses like `implemented`.

## Output

- Summary (user-facing, not the commit list)
- Lint/test commands run + results
- Suggested commit message (Conventional Commits)
- Paste-ready PR body (use `.github/pull_request_template.md` if present; include `Closes #<N>`)

## Cross-repo

If the change also touches a sibling repo, note what to verify there; do not invent paths.
