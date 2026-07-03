# Structural Code Search

## Purpose
Use syntax-aware search, static rules, parsers, and tags to find or rewrite code more safely than text-only search.

## When to Use
Use when matching function calls, imports, control flow, API usage, language constructs, or safe mechanical rewrites.

## Required Tools
ast-grep (`sg`), semgrep, tree-sitter CLI, universal-ctags, rg/fd as fallbacks.

## Install
### Windows PowerShell
```powershell
npm i -g @ast-grep/cli tree-sitter-cli
uv tool install semgrep
winget install universal-ctags.ctags
```

### WSL2 Ubuntu
```bash
npm i -g @ast-grep/cli tree-sitter-cli
uv tool install semgrep
sudo apt install -y universal-ctags
```

### macOS
```bash
brew install ast-grep semgrep tree-sitter universal-ctags
```

## Common Commands
```bash
sg run -p 'console.log($A)' -l ts .
sg run -p 'if ($COND) { $BODY }' -l js src/
semgrep scan --config=auto --max-target-bytes 1000000
semgrep scan --config=p/ci --error --timeout 60
ctags -R --fields=+n --extras=+q --exclude=node_modules --exclude=.git .
rg "functionName" . --glob '!node_modules'
```

## Agent-Safe Patterns
- Start with read-only searches (`sg run`, `semgrep scan`) before using rewrites.
- For ast-grep rewrites, preview and inspect `git diff --stat` and focused diffs.
- Limit Semgrep scans by path/config and use timeouts in large repos.
- Use ctags for symbol lookup, not as a source of truth for semantic refactors.

## Commands Requiring Confirmation
`sg run -p ... -r ... --update-all`, `semgrep --autofix`, generated `tags` committed to repo, broad codemods, rule packs that send code to remote services.

## Troubleshooting
- ast-grep language detection may need `-l ts`, `-l py`, etc.
- Semgrep Windows support is beta; prefer WSL2 or Docker if native install is unreliable.
- tree-sitter CLI requires grammars/config for custom parsing tasks.

## Verification Checklist
```bash
sg --version
semgrep --version
tree-sitter --version
ctags --version
sg run -p '$A' -l js . | sed -n '1,40p'
```

Official references checked: project docs/repositories for ripgrep, fd, fzf, tree/eza/zoxide/bat/delta, ast-grep, Semgrep, tree-sitter, universal-ctags, Serena, codebase-memory-mcp, Zoekt, claude-context, git, GitHub CLI, git-filter-repo, Gitleaks, jq, yq, dasel, SQLite, curl, HTTPie, just, mise, direnv, uv, Ruff, Pyright, Node.js/Corepack/pnpm, Docker, ShellCheck, shfmt, Prettier, ESLint, Hadolint, Trivy, hyperfine, entr, watchexec, and Watchman.
