# Infra quickstart — ProjectTwelve

One-page checklist for agents and contributors setting up or validating agent infrastructure.

## 1. Read first

- [`AGENTS.md`](../AGENTS.md) — commands, MCP, contract
- [`.agent/SAFETY.md`](SAFETY.md) — hard safety rules
- [`docs/ai-workflow/README.md`](../docs/ai-workflow/README.md) — SDLC loop

## 2. Safe commands (read-only first)

```bash
git status && git log --oneline -n 20
python scripts/okf_lint.py --profile project --exclude-prefix archive/ docs
python scripts/agent-memory/context.py
python scripts/sync_assistant_trees.py --check
```

## 3. Local setup

```bash
pip install pyyaml
python scripts/install_githooks.py    # optional: paid-asset + submodule sync hooks
python scripts/fetch_remotes.py       # sync main repo + Assets/_Licensed submodule
cp .cursor/mcp.example.json .cursor/mcp.json   # then set RELAY_PATH + UNITY_PROJECT_PATH
# Optional: install FFF file search MCP — see AGENTS.md § FFF file search MCP
```

Open Unity 6.0.5.1f1; confirm Unity MCP bridge is **Running**. Reload Cursor after MCP config changes.

CLI tools on PATH: see [`.claude/skills/cli-tools-overview/references/agent-environment.md`](../.claude/skills/cli-tools-overview/references/agent-environment.md).

**Claude Code cloud (no Unity):** see [`.agent/CLOUD_ENVIRONMENT.md`](CLOUD_ENVIRONMENT.md) — paste `scripts/cloud-environment-setup.sh` into the cloud environment setup script field.

## 4. Before a PR

```bash
python scripts/sync_assistant_trees.py --check
python scripts/validate_cli_skills.py
python scripts/ci/check_agent_frontmatter.py
python3 scripts/check_markdown_links.py
python3 scripts/check_paid_assets.py --staged
# Unity EditMode — see .claude/skills/unity-tests/SKILL.md
```

## 5. Pick work

- Browse open tickets: [`docs/wiki/tickets/`](../docs/wiki/tickets/)
- Claim: `/task-claim <issue-number>`
- Contract: [`docs/project/00-backlog-workflow.md`](../docs/project/00-backlog-workflow.md)

## 6. Edit agent assets

1. Change files under `.claude/` or `.agent/`.
2. Run `python scripts/sync_assistant_trees.py`.
3. Run `python scripts/validate_cli_skills.py` when CLI skills changed.
4. Commit `.claude/` and `.cursor/` in the same PR.

See [`.agent/SKILL_CHANGE_AST10_REVIEW.md`](SKILL_CHANGE_AST10_REVIEW.md).

## Known pitfalls

- Edit assets under `.claude/` (canonical), not `.cursor/` (generated).
- Memory: never hand-edit `.agent-memory/memory.md`; use log → dream → promote.
- `.agent/scratch/`, `.agent-memory/raw-sessions/`, `.agent-memory/dreams/` are gitignored.
