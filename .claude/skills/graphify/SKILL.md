---
name: graphify
description: Query ProjectTwelve's local Graphify knowledge graph via project-twelve-graphify-mcp (architecture, how X connects to Y, god nodes, communities). Use when the user asks for graphify, knowledge-graph navigation, or cross-module connection tracing; prefer fff/rg for literal file or string search.
capability: "graphify MCP-first knowledge-graph query and local graph refresh for ProjectTwelve"
side_effect_level: local_write
approval_required: false
requires_tools: "project-twelve-graphify-mcp (preferred); graphify CLI via uv tool install graphifyy[mcp]; scripts/start-graphify-mcp.js; graphify-out/graph.json"
output_schema: "MCP tool results (path/node/neighbors/stats) or CLI build/query status against graphify-out/graph.json"
risk_class: low
---

# Graphify knowledge graph (MCP-first)

Local AST knowledge graph for ProjectTwelve. Prefer **MCP tools** on `project-twelve-graphify-mcp` when the server is connected. Fall back to the `graphify` CLI only for builds/refreshes or when MCP is unavailable.

Upstream: [Graphify-Labs/graphify](https://github.com/Graphify-Labs/graphify). PyPI package is `graphifyy`; CLI command is `graphify`.

## When to Use

- Architecture / subsystem maps (“god nodes”, communities, graph stats)
- Relationship questions (“how does X connect to Y?”, “what neighbors does SandboxWorld have?”)
- Refreshing `graphify-out/graph.json` after large code moves
- Preferring structured graph traversal over broad greps for *connections*

**Do not use** for literal filename/content search — use [search-tool-selection](../search-tool-selection/SKILL.md) (`rg` / fff MCP).

## Start with live MCP

1. Confirm `project-twelve-graphify-mcp` is available (tools may be bare or prefixed, e.g. `mcp_…_shortest_path`).
2. Read the live schema for each tool before calling it (`GetMcpTools` / tool descriptor).
3. If the server is missing: tell the user to reload MCP after enabling the entry in `.cursor/mcp.json` (see Enable MCP below). Do not invent REST endpoints.
4. If tools fail with a missing-graph error: run `graphify extract . --code-only` then retry MCP.

## Route the question

| Ask | Preferred MCP tool |
|-----|--------------------|
| Plain-language “what connects …?” | `query_graph` |
| Explain one symbol/type | `get_node` |
| What touches this node? | `get_neighbors` |
| Path from A to B | `shortest_path` |
| Community / subsystem for a node | `get_community` |
| Most-connected hubs | `god_nodes` |
| Node/edge/community counts | `graph_stats` |
| PR impact / triage (optional) | `list_prs`, `get_pr_impact`, `triage_prs` |

Always read schemas for argument names (`source`/`target`, `node`, `question`, etc.) before `CallMcpTool`.

Verified smoke (stdio): `shortest_path` SandboxWorld → SandboxChunkRenderer returns a 1-hop `references [EXTRACTED]` edge.

See [references/mcp-tools.md](references/mcp-tools.md) for the tool list and enable steps.

## Enable MCP (Cursor, once)

Local config is gitignored [`.cursor/mcp.json`](../../../.cursor/mcp.json). Add:

```json
"project-twelve-graphify-mcp": {
  "command": "node",
  "args": ["scripts/start-graphify-mcp.js"]
}
```

Requires `graphify-out/graph.json` (build below) and `uv tool install "graphifyy[mcp]"`. Template: [`.cursor/mcp.example.json`](../../../.cursor/mcp.example.json) `_optionalServers`. Launcher: [`scripts/start-graphify-mcp.js`](../../../scripts/start-graphify-mcp.js).

**Reload the MCP client** after editing. Codex/Antigravity: see [`.codex/README.md`](../../../.codex/README.md) / [`.agents/README.md`](../../../.agents/README.md).

## Build / refresh graph (CLI only)

```bash
uv tool install "graphifyy[mcp]"   # once per machine
graphify extract . --code-only     # first build or full rebuild
graphify extract . --code-only --update
graphify cluster-only .            # refresh GRAPH_REPORT.md / graph.html
```

Windows PowerShell: use `graphify .` — not `/graphify .`.

`graphify-out/` is **gitignored**. Respect [`.graphifyignore`](../../../.graphifyignore).

## CLI fallback (when MCP is down)

```bash
graphify query "what connects SandboxWorld to chunk rendering?"
graphify path "SandboxWorld" "SandboxChunkRenderer"
graphify explain "SandboxPlayerController"
```

## ProjectTwelve hard rules

- Never run `graphify cursor install`, `graphify antigravity install`, or `graphify codex install` — edit `.claude/` + `python scripts/sync_assistant_trees.py`.
- Do not commit `graphify-out/` or paste secrets into queries.
- Prefer MCP for queries; use CLI for extract/cluster only.

## Agent workflow

1. Prefer MCP tools when connected; read schemas first.
2. If graph/MCP missing → build with `--code-only`, confirm launcher, ask user to reload MCP.
3. Answer from graph edges (note EXTRACTED vs INFERRED); open source files only for the few hot nodes.
4. Fall back to `rg` / fff for literal string/path search.
