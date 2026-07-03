# Windows Agent Tooling

## Purpose
Set up and use native Windows tools safely from PowerShell while knowing when to move work into WSL2.

## When to Use
Use for Windows-native repos, PowerShell scripts, GitHub CLI operations, simple search/editing, Python via uv, Node via pnpm, and Docker Desktop orchestration.

## Required Tools
git, gh, rg, fd, jq, delta, bat, zoxide, Node.js LTS, corepack/pnpm, uv, PowerShell 7, optional Docker Desktop and Scoop.

## Install
### Windows PowerShell
```powershell
winget install Git.Git
winget install GitHub.cli
winget install BurntSushi.ripgrep.MSVC
winget install sharkdp.fd
winget install jqlang.jq
winget install dandavison.delta
winget install sharkdp.bat
winget install ajeetdsouza.zoxide
winget install OpenJS.NodeJS.LTS
corepack enable
corepack prepare pnpm@latest --activate
npm i -g @ast-grep/cli
powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"
uv tool install ruff
uv tool install pyright
```
Scoop alternatives can be better for CLI-only machines: `scoop install git gh ripgrep fd jq delta bat zoxide nodejs-lts uv`.

### WSL2 Ubuntu
Use this only from WSL; see `wsl2-agent-tooling.md`.

### macOS
Not applicable; see overview or per-tool skills.

## Common Commands
```powershell
git status --short
rg "SomeSymbol" .
fd "Controller|Service|Repository"
Get-Content .\path\to\file.java -TotalCount 160
git diff --stat
git diff -- path\to\file | delta
jq '.scripts' package.json
```

## Agent-Safe Patterns
- Prefer Windows host for VS Code/Cursor/Claude Desktop/ChatGPT, Docker Desktop, local LLM apps, GitHub CLI, and simple editing.
- Quote paths with spaces and prefer forward slashes when tools accept them.
- Keep repos under WSL (`\\wsl$` or Linux filesystem) for Linux-heavy projects; avoid mixed line endings.

## Commands Requiring Confirmation
`Remove-Item -Recurse -Force`, `git reset --hard`, `git clean -fdx`, registry edits, service restarts, Docker volume deletion, credential changes, `winget upgrade --all` on a user machine.

## Troubleshooting
- Restart PowerShell after installs so PATH updates are visible.
- Use `where.exe rg` and `$env:Path -split ';'` to debug command resolution.
- If npm global binaries are missing, inspect `npm prefix -g`.

## Verification Checklist
```powershell
git --version; gh --version; rg --version; fd --version; jq --version
node --version; corepack --version; pnpm --version
uv --version; ruff --version; pyright --version
git status --short
```

Official references checked: project docs/repositories for ripgrep, fd, fzf, tree/eza/zoxide/bat/delta, ast-grep, Semgrep, tree-sitter, universal-ctags, Serena, codebase-memory-mcp, Zoekt, claude-context, git, GitHub CLI, git-filter-repo, Gitleaks, jq, yq, dasel, SQLite, curl, HTTPie, just, mise, direnv, uv, Ruff, Pyright, Node.js/Corepack/pnpm, Docker, ShellCheck, shfmt, Prettier, ESLint, Hadolint, Trivy, hyperfine, entr, watchexec, and Watchman.
