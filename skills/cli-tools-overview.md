# CLI Tools Overview

## Purpose
Provide a compact map of low-memory CLI tools that help coding agents inspect, edit, verify, and summarize repositories safely.

## When to Use
Use this before choosing repo commands, especially in unknown projects or when working across Windows, WSL2, macOS, and Linux.

## Required Tools
Core: git, rg, fd, bat, delta, jq, yq, curl. Helpful: fzf, tree or eza, zoxide, ast-grep, semgrep, gh, just, mise, uv, node, pnpm, docker, shellcheck, shfmt, prettier, eslint, trivy, hyperfine, watchexec.

## Install
### Windows PowerShell
```powershell
winget install Git.Git GitHub.cli BurntSushi.ripgrep.MSVC sharkdp.fd sharkdp.bat dandavison.delta jqlang.jq ajeetdsouza.zoxide
winget install OpenJS.NodeJS.LTS Docker.DockerDesktop
corepack enable
corepack prepare pnpm@latest --activate
```

### WSL2 Ubuntu
```bash
sudo apt update
sudo apt install -y git curl jq ripgrep fd-find shellcheck sqlite3 direnv tree
curl -LsSf https://astral.sh/uv/install.sh | sh
```

### macOS
```bash
brew install git gh ripgrep fd bat git-delta jq yq zoxide fzf tree eza ast-grep semgrep just mise direnv uv shellcheck shfmt hyperfine watchexec
```

## Common Commands
```bash
git status --short
fd "Controller|Service|Repository" .
rg "SomeSymbol" . --glob '!node_modules' --glob '!target' --glob '!build'
tree -L 3 -I 'node_modules|target|build|dist|.git'
bat --line-range 1:160 path/to/file
jq '.scripts' package.json
yq '.services' docker-compose.yml
git diff --stat
git diff -- path/to/file | delta
```

## Agent-Safe Patterns
- Start with `git status --short` and inspect project files before assuming language or tooling.
- Prefer project entry points: `just --list`, `mise tasks`, `npm run`, `pnpm run`, `make help`.
- Bound output with `--max-count`, `--line-number`, `--files`, `--line-range`, `-L`, or path filters.
- Use `git diff --stat` plus focused `git diff -- path` before finalizing.

## Commands Requiring Confirmation
`rm -rf`, `git reset --hard`, `git clean -fdx`, `git filter-repo`, `git push --force`, package publish commands, production deploy commands, destructive database migrations, Docker volume pruning.

## Troubleshooting
- On Ubuntu, `fd` may be installed as `fdfind`; add an alias or call `fdfind`.
- If color escapes pollute logs, add `--color=never` or set `NO_COLOR=1`.
- If a tool is missing, fall back to `git ls-files`, `find`, `sed -n`, or PowerShell `Get-Content -TotalCount`.

## Verification Checklist
```bash
git --version
rg --version
fd --version || fdfind --version
jq --version
git status --short
```

Official references checked: project docs/repositories for ripgrep, fd, fzf, tree/eza/zoxide/bat/delta, ast-grep, Semgrep, tree-sitter, universal-ctags, Serena, codebase-memory-mcp, Zoekt, claude-context, git, GitHub CLI, git-filter-repo, Gitleaks, jq, yq, dasel, SQLite, curl, HTTPie, just, mise, direnv, uv, Ruff, Pyright, Node.js/Corepack/pnpm, Docker, ShellCheck, shfmt, Prettier, ESLint, Hadolint, Trivy, hyperfine, entr, watchexec, and Watchman.
