# MCP Code Intelligence

## Purpose
Choose lightweight MCP/code-intelligence layers that improve agent navigation without replacing direct CLI inspection.

## When to Use
Use when an agent needs reusable codebase memory, symbol/graph navigation, indexed search, or semantic retrieval beyond raw shell commands.

## Required Tools
Minimal: rg, fd, read_file, git diff, patch_file. Better: ast-grep, git tools, project task runner. Advanced: Serena, codebase-memory-mcp, Zoekt, optional embedding search such as claude-context.

## Install
### Windows PowerShell
```powershell
winget install BurntSushi.ripgrep.MSVC sharkdp.fd Git.Git
npm i -g @ast-grep/cli
uvx --from git+https://github.com/oraios/serena serena --help
uv tool install codebase-memory-mcp
```

### WSL2 Ubuntu
```bash
sudo apt update
sudo apt install -y git ripgrep fd-find curl sqlite3
npm i -g @ast-grep/cli
uvx --from git+https://github.com/oraios/serena serena --help
uv tool install codebase-memory-mcp
go install github.com/sourcegraph/zoekt/cmd/zoekt-index@latest
go install github.com/sourcegraph/zoekt/cmd/zoekt@latest
```

### macOS
```bash
brew install ripgrep fd ast-grep go uv
uvx --from git+https://github.com/oraios/serena serena --help
uv tool install codebase-memory-mcp
go install github.com/sourcegraph/zoekt/cmd/zoekt-index@latest
go install github.com/sourcegraph/zoekt/cmd/zoekt@latest
```

## Common Commands
```bash
rg "SomeSymbol" . --glob '!node_modules'
fd "Controller|Service|Repository" .
sg run -p 'function $F($$$ARGS) { $$$BODY }' -l js src/
git diff --stat
zoekt-index .
zoekt 'SomeSymbol'
codebase-memory-mcp --help
```

## Agent-Safe Patterns
- Minimal MCP setup: `rg` + `fd` + `read_file` + `git diff` + `patch_file`.
- Better setup: `rg` + `fd` + `ast-grep` + git tools + project task runner.
- Advanced setup: Serena or codebase-memory-mcp for symbol/graph workflows, Zoekt for indexed trigram search, optional embeddings for fuzzy conceptual retrieval.
- Distinguish layers: CLI wrappers search text/files; ast-grep searches syntax; ctags/Serena provide symbol views; codebase-memory-mcp stores graph/code memory; Zoekt indexes text and symbols; embeddings find semantically similar chunks.
- Use embeddings second, not first: embedding indexes are often heavier, can miss exact strings, and may require extra services/storage.

## Commands Requiring Confirmation
Enabling MCP servers from untrusted repos, granting write tools, long-running background indexers, embedding/index uploads to cloud services, broad automated refactors through MCP write tools.

## Troubleshooting
- MCP stdio servers execute local processes; inspect config and pin commands/paths.
- Prefer loopback-only HTTP servers and avoid exposing MCP ports publicly.
- If an index is stale, re-run bounded CLI checks (`rg`, `fd`, `git diff`) before editing.
- For Windows path issues, run MCP servers inside WSL2 when repos live in WSL.

## Verification Checklist
```bash
rg --version
fd --version || fdfind --version
sg --version
codebase-memory-mcp --help || true
zoekt --help || true
```

Official references checked: project docs/repositories for ripgrep, fd, fzf, tree/eza/zoxide/bat/delta, ast-grep, Semgrep, tree-sitter, universal-ctags, Serena, codebase-memory-mcp, Zoekt, claude-context, git, GitHub CLI, git-filter-repo, Gitleaks, jq, yq, dasel, SQLite, curl, HTTPie, just, mise, direnv, uv, Ruff, Pyright, Node.js/Corepack/pnpm, Docker, ShellCheck, shfmt, Prettier, ESLint, Hadolint, Trivy, hyperfine, entr, watchexec, and Watchman.
