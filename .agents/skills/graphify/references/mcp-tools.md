# Graphify MCP tools

Server key: `project-twelve-graphify-mcp`  
Launcher: `node scripts/start-graphify-mcp.js`  
Graph file: `graphify-out/graph.json` (gitignored; build with `graphify extract . --code-only`)

## Tools (verified)

| Tool | Purpose |
|------|---------|
| `query_graph` | Scoped subgraph for a plain-language question |
| `get_node` | Detail for one named concept |
| `get_neighbors` | Neighbor edges (EXTRACTED / INFERRED) |
| `get_community` | Community / subsystem for a node |
| `god_nodes` | Highest-degree hubs |
| `graph_stats` | Node / edge / community counts |
| `shortest_path` | Shortest path between two concepts |
| `list_prs` | PR dashboard helpers (optional) |
| `get_pr_impact` | Graph impact for a PR (optional) |
| `triage_prs` | Triage ranking (optional; may need a backend) |

Always fetch live schemas before calling — argument names can change with `graphifyy` upgrades.

## Enable (Cursor)

In gitignored `.cursor/mcp.json` under `mcpServers`:

```json
"project-twelve-graphify-mcp": {
  "command": "node",
  "args": ["scripts/start-graphify-mcp.js"]
}
```

Then reload MCP. Codex uses the same launcher via [`.codex/config.toml`](../../../../.codex/config.toml).

## Smoke test (stdio)

From repo root, a successful `tools/list` includes the names above. Example call:

- `shortest_path` with source `SandboxWorld` and target `SandboxChunkRenderer` → 1-hop `references [EXTRACTED]`.

## Forbidden vendor installs

```bash
graphify cursor install
graphify antigravity install
graphify codex install
```

Edit `.claude/skills/graphify/` and run `python scripts/sync_assistant_trees.py` instead.
