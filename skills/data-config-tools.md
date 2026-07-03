# Data Config Tools

## Purpose
Inspect and transform JSON, YAML, TOML-like data, SQLite databases, and HTTP APIs with bounded output.

## When to Use
Use for package manifests, Docker Compose files, CI configs, API responses, local SQLite state, and machine-readable task output.

## Required Tools
jq, yq, dasel, sqlite3, curl, httpie, rg/fd.

## Install
### Windows PowerShell
```powershell
winget install jqlang.jq MikeFarah.yq TomWright.dasel SQLite.SQLite cURL.cURL HTTPie.HTTPie
```

### WSL2 Ubuntu
```bash
sudo apt update
sudo apt install -y jq sqlite3 curl
sudo snap install yq || go install github.com/mikefarah/yq/v4@latest
go install github.com/tomwright/dasel/v2/cmd/dasel@latest
uv tool install httpie
```

### macOS
```bash
brew install jq yq dasel sqlite curl httpie
```

## Common Commands
```bash
jq '.scripts' package.json
yq '.services' docker-compose.yml
dasel -f config.toml '.tool' | sed -n '1,120p'
sqlite3 app.db '.tables'
sqlite3 -header -column app.db 'select name from sqlite_master limit 20;'
curl -fsSL https://example.com/health | jq .
http --check-status --timeout=10 GET https://example.com/health
```
PowerShell:
```powershell
Get-Content .\package.json -TotalCount 160
jq '.scripts' package.json
```

## Agent-Safe Patterns
- Use read-only queries first; add `limit` to SQL.
- Use `curl -f --max-time 20 --retry 2` for bounded API checks.
- Write transformed data to a temp file, inspect diff, then move into place.

## Commands Requiring Confirmation
SQL `update/delete/drop`, writing secrets to logs, authenticated production API calls, `curl | sh`, in-place config rewrites across many files.

## Troubleshooting
- There are multiple `yq` implementations; verify with `yq --version` and prefer Mike Farah yq v4 syntax.
- Quote jq/yq expressions differently in PowerShell if single quotes conflict with nested quoting.
- Use `sqlite3 file '.schema table'` before editing database content.

## Verification Checklist
```bash
jq --version
yq --version
dasel --version || true
sqlite3 --version
curl --version
http --version || true
```

Official references checked: project docs/repositories for ripgrep, fd, fzf, tree/eza/zoxide/bat/delta, ast-grep, Semgrep, tree-sitter, universal-ctags, Serena, codebase-memory-mcp, Zoekt, claude-context, git, GitHub CLI, git-filter-repo, Gitleaks, jq, yq, dasel, SQLite, curl, HTTPie, just, mise, direnv, uv, Ruff, Pyright, Node.js/Corepack/pnpm, Docker, ShellCheck, shfmt, Prettier, ESLint, Hadolint, Trivy, hyperfine, entr, watchexec, and Watchman.
