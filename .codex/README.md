# Codex project setup

Codex loads the root [`AGENTS.md`](../AGENTS.md) as project guidance and, after the repository is
trusted, loads [`config.toml`](config.toml). Repository skills are generated at
`.agents/skills/` from the canonical `.claude/skills/` tree.

## First run

```powershell
codex doctor --summary
python scripts/sync_assistant_trees.py --check
codex
```

The committed config keeps command execution in the workspace-write sandbox, asks before escalation,
keeps sandboxed network access off, and enables the portable in-game and FFF MCP definitions. Install
`fff-mcp` and put it on `PATH` as described in [`AGENTS.md`](../AGENTS.md). The in-game endpoint is
available only while the game is in Play Mode or a desktop build is running.

## PixelLab MCP

This project's [`config.toml`](config.toml) declares PixelLab as a **stdio** `mcp-remote` bridge
(not a bare `url`). ChatGPT Codex rejects a project `url` entry when the same server already
exists as stdio in `~/.codex/config.toml` (`url is not supported for stdio in mcp_servers.pixellab`),
which blocks thread resume.

Set `PIXELLAB_API_KEY` in the gitignored project `.env` file or in the parent environment (never
commit the key). The `scripts/start-pixellab-mcp.js` launcher loads `.env`, passes the credential to
`mcp-remote` through a process-only environment variable, and keeps the token out of the committed
TOML and command-line arguments. Restart Codex after adding or changing the key. If you also define
`[mcp_servers.pixellab]` in the user config, keep it stdio-only — do not add `url` / `headers` there.

Token: [pixellab.ai/mcp](https://www.pixellab.ai/mcp). Skill: [`.claude/skills/pixellab-mcp/SKILL.md`](../.claude/skills/pixellab-mcp/SKILL.md).

## Graphify MCP

Portable stdio entry `[mcp_servers.project-twelve-graphify-mcp]` runs
`node scripts/start-graphify-mcp.js`. Build a local graph first (`uv tool install "graphifyy[mcp]"` then
`graphify extract . --code-only`). The launcher exits with a clear error if `graphify-out/graph.json`
is missing. `graphify-out/` is gitignored. Skill: [`.claude/skills/graphify/SKILL.md`](../.claude/skills/graphify/SKILL.md).
Do not run `graphify codex install` — edit `.claude/` and sync instead.

## Unity Editor MCP

The Unity relay path and absolute project path are machine-specific, so they belong in the user-level
`~/.codex/config.toml`, not this committed file. Configure them once:

```powershell
codex mcp add project-twelve-unity-mcp --env UNITY_PROJECT_PATH="D:\Projects\project-twelve" -- "$HOME\.unity\relay\relay_win.exe" --mcp
```

Adjust the project and relay paths for the machine. Keep secrets in environment variables; never add
tokens or machine-specific credentials to the project config.

## Ownership

- Author skills under `.claude/skills/`.
- Run `python scripts/sync_assistant_trees.py` to regenerate `.cursor/` and `.agents/skills/`.
- Do not hand-edit `.agents/skills/`.
- Codex does not consume the repo's Claude/Cursor custom slash-command files. Use the matching skill
  or the workflow playbooks under `.agent/workflows/` in Codex tasks.
