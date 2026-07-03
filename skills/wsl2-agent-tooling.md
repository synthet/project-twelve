# WSL2 Agent Tooling

## Purpose
Use WSL2 Ubuntu as the default coding-agent execution environment for Unix-like repos and CI-like validation.

## When to Use
Use for Bash-heavy repos, Docker Compose workflows, Java/Node/Python monorepos, MCP servers expecting Unix paths, tools with weaker native Windows support, and local reproduction of Linux CI.

## Required Tools
git, curl, jq, yq, rg, fd/fdfind, bat/batcat, delta, shellcheck, sqlite3, direnv, uv, Node.js/Corepack/pnpm, ast-grep, just, mise, docker CLI.

## Install
### Windows PowerShell
```powershell
wsl --install -d Ubuntu
wsl --status
```

### WSL2 Ubuntu
```bash
sudo apt update
sudo apt install -y git curl jq ripgrep fd-find shellcheck sqlite3 direnv tree ca-certificates gnupg
curl -LsSf https://astral.sh/uv/install.sh | sh
curl https://mise.run | sh
curl -fsSL https://deb.nodesource.com/setup_lts.x | sudo -E bash -
sudo apt install -y nodejs
corepack enable
corepack prepare pnpm@latest --activate
npm i -g @ast-grep/cli
```

### macOS
Not applicable; use Homebrew equivalents from other skills.

## Common Commands
```bash
git status --short
fdfind "Controller|Service|Repository" .
rg "SomeSymbol" . --glob '!node_modules' --glob '!target' --glob '!build'
sed -n '1,160p' path/to/file
batcat --line-range 1:160 path/to/file
docker compose ps
```

## Agent-Safe Patterns
- Store repositories inside the WSL Linux filesystem, not `/mnt/c`, for speed and file-watcher reliability.
- Use Docker Desktop WSL integration or a native Docker engine; inspect `docker context ls` before assuming.
- Prefer project task runners over ad hoc build commands.

## Commands Requiring Confirmation
`sudo rm -rf`, `sudo apt full-upgrade`, `git reset --hard`, `git clean -fdx`, Docker prune/volume removal, changing `/etc/wsl.conf`, chmod/chown over large trees.

## Troubleshooting
- Ubuntu names: `fd` is often `fdfind`, `bat` is often `batcat`.
- If WSL networking or DNS fails, verify from Windows and WSL with `curl -I https://github.com`.
- If file watching fails, keep repo outside `/mnt/c` and consider `watchman` or polling.

## Verification Checklist
```bash
uname -a
git --version
rg --version
fdfind --version
jq --version
node --version
pnpm --version
uv --version
git status --short
```

Official references checked: project docs/repositories for ripgrep, fd, fzf, tree/eza/zoxide/bat/delta, ast-grep, Semgrep, tree-sitter, universal-ctags, Serena, codebase-memory-mcp, Zoekt, claude-context, git, GitHub CLI, git-filter-repo, Gitleaks, jq, yq, dasel, SQLite, curl, HTTPie, just, mise, direnv, uv, Ruff, Pyright, Node.js/Corepack/pnpm, Docker, ShellCheck, shfmt, Prettier, ESLint, Hadolint, Trivy, hyperfine, entr, watchexec, and Watchman.
