# PR-ready Workflow

1. Confirm the diff is scoped to the requested change.
2. Run the relevant validation commands from `AGENTS.md`.
3. Run `python3 scripts/check_paid_assets.py --staged` before committing.
4. Review `git status --short` for unintended files, generated artifacts, or missing Unity `.meta` files.
5. Summarize behavior changes, docs changes, and test results in the PR body.
