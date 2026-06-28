# Project guide — how agents navigate ProjectTwelve

## Start here

| Need | Go to |
|------|-------|
| Build/test commands | [`AGENTS.md`](../AGENTS.md) |
| Architecture overview | [`CLAUDE.md`](../CLAUDE.md) |
| Implementation wiki | [`docs/wiki/README.md`](../docs/wiki/README.md) |
| Authority map | [`docs/CANONICAL_SOURCES.md`](../docs/CANONICAL_SOURCES.md) |
| Backlog tickets | [`docs/wiki/tickets/`](../docs/wiki/tickets/) |
| Safety rules | [`.agent/SAFETY.md`](SAFETY.md) |
| Paid assets | [`docs/PAID_ASSETS.md`](../docs/PAID_ASSETS.md) |

## `.agent/` layout

| Path | Purpose |
|------|---------|
| `SAFETY.md` | Hard safety rules |
| `COMMANDS.md` | Verified command cheat sheet |
| `AGENT_INFRA_INVENTORY.md` | Full asset catalog |
| `workflows/` | SDLC playbooks |
| `subagents/` | Subagent role matrix |

## Unity-specific

- Preserve `.meta` files with every asset change.
- Licensed art: submodule at `Assets/_Licensed/` — never commit blobs to the public repo.
- Unity MCP: see `AGENTS.md` → Unity MCP section.
- Supplementary C# rules: [`.cursorrules`](../.cursorrules).

## SDLC

```
/spec → /plan → /implement → /test-and-fix → /pr-ready
```

Slash commands: `.claude/commands/` (canonical). Cursor mirror: `.cursor/commands/` (generated).
