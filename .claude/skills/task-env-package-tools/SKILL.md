---
name: task-env-package-tools
description: Run project tasks, manage language versions, and install developer tools reproducibly. Use after inspecting project entry-point files.
---

# Task Env Package Tools

## Purpose
Run project tasks, manage language versions, isolate environments, and install developer tools reproducibly.

## When to Use
Use after inspecting project files to discover canonical build/test/lint commands and avoid inventing ad hoc workflows.

## Required Tools
just, mise, direnv, uv, ruff, pyright, node, corepack, pnpm, docker, docker compose.

## Install
### Windows PowerShell
```powershell
winget install OpenJS.NodeJS.LTS Docker.DockerDesktop Casey.Just jdx.mise
corepack enable
corepack prepare pnpm@latest --activate
powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"
uv tool install ruff
uv tool install pyright
```

### WSL2 Ubuntu
```bash
sudo apt update
sudo apt install -y curl direnv ca-certificates gnupg
curl https://mise.run | sh
curl -LsSf https://astral.sh/uv/install.sh | sh
curl -fsSL https://deb.nodesource.com/setup_lts.x | sudo -E bash -
sudo apt install -y nodejs just
corepack enable
corepack prepare pnpm@latest --activate
```

### macOS
```bash
brew install just mise direnv uv node pnpm docker docker-compose
uv tool install ruff
uv tool install pyright
```

## Common Commands
```bash
just --list
just test
mise tasks
mise run test
direnv status
uv run pytest -q
uv tool run ruff check .
uv tool run pyright
pnpm run
pnpm test -- --runInBand
docker compose ps
docker compose config --quiet
```

## Agent-Safe Patterns
- Inspect `package.json`, `pyproject.toml`, `justfile`, `mise.toml`, `Makefile`, and CI files before choosing commands.
- Prefer lockfile-respecting installs: `pnpm install --frozen-lockfile`, `uv sync --locked` when the project expects it.
- Use Docker Compose read-only checks (`config`, `ps`, logs with `--tail`).

## Commands Requiring Confirmation
`direnv allow` for untrusted repos, dependency upgrades, global package installs on user machines, `docker compose down -v`, image/volume prune, publishing packages.

## Troubleshooting
- Corepack may need a new shell after Node install.
- `direnv allow` executes repo-controlled shell; review `.envrc` first.
- If Docker is unavailable in WSL, verify Docker Desktop WSL integration and `docker context ls`.


## ProjectTwelve validation

After discovering project entry points, prefer these canonical gates from [`AGENTS.md`](../../../AGENTS.md):

```bash
# Unity batch validation (requires Unity in PATH)
Unity -batchmode -quit -projectPath . -logFile Logs/unity-validate.log
Unity -batchmode -quit -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile Logs/unity-editmode-tests.log

# Repo guards
python3 scripts/check_paid_assets.py --staged
python scripts/sync_assistant_trees.py --check
python3 scripts/check_markdown_links.py   # when docs/ changed
python scripts/validate_cli_skills.py
```

## Windows Notes
- Use PowerShell 7+; prefer `winget` or Scoop for CLI installs.
- Native binaries use `fd`, `bat`, and `delta` (not Ubuntu `fdfind`/`batcat` names).
- Quote paths with spaces; forward slashes often work in modern Windows CLIs.
- Set `NO_COLOR=1` or `--color=never` when color escapes pollute agent logs.

## WSL2 Notes
- Clone repos inside the WSL filesystem (`~/projects/`), not `/mnt/c/`, for speed and file watchers.
- `fd` is often installed as `fdfind`; `bat` as `batcat`.
- Prefer WSL for Bash-heavy tooling, Docker Compose, and MCP servers expecting Unix paths.

## Verification Checklist
```bash
just --version || true
mise --version || true
direnv version || true
uv --version
ruff --version
pyright --version
node --version
pnpm --version
docker --version || true
docker compose version || true
```

Official references checked: project docs/repositories for ripgrep, fd, fzf, tree/eza/zoxide/bat/delta, ast-grep, Semgrep, tree-sitter, universal-ctags, Serena, codebase-memory-mcp, Zoekt, claude-context, git, GitHub CLI, git-filter-repo, Gitleaks, jq, yq, dasel, SQLite, curl, HTTPie, just, mise, direnv, uv, Ruff, Pyright, Node.js/Corepack/pnpm, Docker, ShellCheck, shfmt, Prettier, ESLint, Hadolint, Trivy, hyperfine, entr, watchexec, and Watchman.