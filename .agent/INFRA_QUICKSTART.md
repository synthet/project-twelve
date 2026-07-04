# Infra quickstart — ProjectTwelve

One-page checklist for agents and contributors setting up or validating agent infrastructure.

## 1. Read first

- [`AGENTS.md`](../AGENTS.md) — commands, MCP, contract
- [`.agent/SAFETY.md`](SAFETY.md) — hard safety rules
- [`docs/ai-workflow/README.md`](../docs/ai-workflow/README.md) — SDLC loop

## 2. Local setup

```bash
pip install pyyaml
python scripts/install_githooks.py    # optional: paid-asset + submodule sync hooks
python scripts/fetch_remotes.py       # sync main repo + Assets/_Licensed submodule
cp .cursor/mcp.example.json .cursor/mcp.json   # then set RELAY_PATH + UNITY_PROJECT_PATH
# Optional: install FFF file search MCP — see AGENTS.md § FFF file search MCP
```

Open Unity 6.0.5.1f1; confirm Unity MCP bridge is **Running**. Reload Cursor after MCP config changes.

## 3. Before a PR

```bash
python scripts/sync_assistant_trees.py --check
python3 scripts/check_markdown_links.py
python3 scripts/check_paid_assets.py --staged
Unity -batchmode -quit -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile Logs/unity-editmode-tests.log
```

## 4. Pick work

- Browse open tickets: [`docs/wiki/tickets/`](../docs/wiki/tickets/)
- Claim: `/task-claim <issue-number>`
- Contract: [`docs/project/00-backlog-workflow.md`](../docs/project/00-backlog-workflow.md)

## 5. Edit agent assets

1. Change files under `.claude/` or `.agent/`.
2. Run `python scripts/sync_assistant_trees.py`.
3. Commit `.claude/` and `.cursor/` in the same PR.

See [`.agent/SKILL_CHANGE_AST10_REVIEW.md`](SKILL_CHANGE_AST10_REVIEW.md).
