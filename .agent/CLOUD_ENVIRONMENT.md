# Claude Code cloud environment — project-twelve

Agent work **without Unity**: C#/docs edits, Python lint scripts, and offline `tile-viz` / `world-viz` tests.

Unity EditMode, Unity MCP, Play Mode MCP, and licensed art under `Assets/_Licensed/` remain on your local workstation.

## Create the environment (claude.ai/code)

Open **Add environment** and use:

| Field | Value |
|-------|--------|
| **Name** | `project-twelve` |
| **Network access** | **Trusted** |
| **Environment variables** | Empty, or `PYTHONUNBUFFERED=1` only (no secrets, no Unity paths) |
| **Setup script** | Contents of [`scripts/cloud-environment-setup.sh`](../scripts/cloud-environment-setup.sh) |

Connect GitHub via the Claude GitHub App or `/web-setup` in the CLI before your first session.

### Environment variables — do not add

- `UNITY_EDITOR`, `UNITY_LICENSE`, `UNITY_SERIAL` — Unity is not available in cloud VMs
- Tokens, API keys, or `secrets.json` contents — visible to anyone who can edit the environment
- Windows paths (e.g. `D:\Soft\Unity\...`)

Optional: `GH_TOKEN` if you need the `gh` CLI beyond built-in GitHub tools (session shell only; not visible to the setup script).

## What works in cloud

| Task | Command |
|------|---------|
| Mirror sync check | `python scripts/sync_assistant_trees.py --check` |
| Markdown links | `python3 scripts/check_markdown_links.py` |
| Paid-assets guard | `python3 scripts/check_paid_assets.py --push` |
| Autotile / terrain parity | `cd tools/tile-viz && npm test`, `cd tools/world-viz && npm test` |
| Docs / agent assets | Edit under `.claude/`, `docs/` |

## What stays local

- Unity batch validation and EditMode tests — [`.claude/skills/unity-tests/SKILL.md`](../.claude/skills/unity-tests/SKILL.md)
- Unity MCP and runtime Play Mode MCP
- `Assets/_Licensed/` submodule and visual catalog regen

## Validate after creating the environment

Start a cloud session with:

> Run `python scripts/sync_assistant_trees.py --check`, then `cd tools/tile-viz && npm test` and `cd tools/world-viz && npm test`. Report results.

Resumed sessions also run [`scripts/cloud-session-check.sh`](../scripts/cloud-session-check.sh) via the SessionStart hook in [`.claude/settings.json`](../.claude/settings.json).

## Related

- [`.agent/INFRA_QUICKSTART.md`](INFRA_QUICKSTART.md) — local workstation setup
- [`AGENTS.md`](../AGENTS.md) — commands and test vocabulary
- [Claude Code on the web](https://code.claude.com/docs/en/claude-code-on-the-web)
