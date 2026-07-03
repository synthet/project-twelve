# Lint Format Security

## Purpose
Run shell, code, Dockerfile, dependency, and container checks without making unsafe automatic changes.

## When to Use
Use before commits, after modifying scripts/configs, and when reviewing CI or security-sensitive changes.

## Required Tools
shellcheck, shfmt, prettier, eslint, hadolint, trivy, semgrep, gitleaks, ruff, pyright.

## Install
### Windows PowerShell
```powershell
winget install koalaman.shellcheck mvdan.shfmt Gitleaks.Gitleaks AquaSecurity.Trivy
npm i -g prettier eslint
uv tool install ruff
uv tool install pyright
uv tool install semgrep
```

### WSL2 Ubuntu
```bash
sudo apt update
sudo apt install -y shellcheck
curl -sS https://webi.sh/shfmt | sh
npm i -g prettier eslint
uv tool install ruff
uv tool install pyright
uv tool install semgrep
curl -sSfL https://raw.githubusercontent.com/gitleaks/gitleaks/master/scripts/install.sh | sh -s -- -b ~/.local/bin
```

### macOS
```bash
brew install shellcheck shfmt hadolint trivy gitleaks semgrep ruff pyright
npm i -g prettier eslint
```

## Common Commands
```bash
shellcheck scripts/*.sh
shfmt -d scripts
prettier --check .
eslint . --max-warnings=0
hadolint Dockerfile
trivy fs --scanners vuln,secret,misconfig --severity HIGH,CRITICAL .
gitleaks detect --source . --no-git --redact
ruff check .
pyright
```

## Agent-Safe Patterns
- Run check/diff modes first: `shfmt -d`, `prettier --check`, `ruff check`.
- Scope scans to changed paths where possible for speed, then run broader project gates if needed.
- Treat secret findings as sensitive; do not paste raw secrets into chat or logs.

## Commands Requiring Confirmation
`prettier --write .`, `eslint --fix`, `ruff check --fix`, `shfmt -w`, `trivy image` pulling large images, uploading scan results, changing CI security policy.

## Troubleshooting
- Prefer project-local linters via `pnpm exec`, `uv run`, `npm run lint`, or `just lint`.
- Hadolint may not be available through all Windows package channels; use WSL2 or Docker if needed.
- Trivy vulnerability DB downloads need network and cache space.

## Verification Checklist
```bash
shellcheck --version
shfmt --version || true
prettier --version || true
eslint --version || true
hadolint --version || true
trivy --version || true
gitleaks version || true
```

Official references checked: project docs/repositories for ripgrep, fd, fzf, tree/eza/zoxide/bat/delta, ast-grep, Semgrep, tree-sitter, universal-ctags, Serena, codebase-memory-mcp, Zoekt, claude-context, git, GitHub CLI, git-filter-repo, Gitleaks, jq, yq, dasel, SQLite, curl, HTTPie, just, mise, direnv, uv, Ruff, Pyright, Node.js/Corepack/pnpm, Docker, ShellCheck, shfmt, Prettier, ESLint, Hadolint, Trivy, hyperfine, entr, watchexec, and Watchman.
