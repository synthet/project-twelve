# Graphify command cheatsheet

Official package: `graphifyy`. CLI: `graphify`. Prefer MCP for queries — see [mcp-tools.md](mcp-tools.md).

## Build

| Command | Purpose |
|---------|---------|
| `graphify extract . --code-only` | Local AST graph (no LLM / API key) |
| `graphify extract . --code-only --update` | Re-extract changed files only |
| `graphify extract . --code-only --force` | Overwrite even if node count shrinks |
| `graphify cluster-only .` | Recluster + GRAPH_REPORT.md / graph.html |

## Query (CLI fallback)

| Command | Purpose |
|---------|---------|
| `graphify query "…"` | Scoped subgraph for a question |
| `graphify path "A" "B"` | Shortest path between two nodes |
| `graphify explain "Name"` | Node detail + neighbors |

## MCP launcher

```bash
node scripts/start-graphify-mcp.js
```

Fails clearly if `graphify-out/graph.json` is missing.
