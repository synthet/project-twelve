# Install Checklist

## Purpose
Provide ready-to-run installation blocks for common coding-agent CLI tools on Windows PowerShell, WSL2 Ubuntu, and macOS.

## When to Use
Use when provisioning a new agent workstation, container, WSL distro, or CI-like development environment.

## Required Tools
Package managers: winget or Scoop, apt, Homebrew, npm/Corepack, uv, cargo/go when needed.

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
winget install Docker.DockerDesktop
winget install Casey.Just
winget install jdx.mise
winget install Gitleaks.Gitleaks
winget install AquaSecurity.Trivy
corepack enable
corepack prepare pnpm@latest --activate
npm i -g @ast-grep/cli tree-sitter-cli prettier eslint
powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"
uv tool install ruff
uv tool install pyright
uv tool install semgrep
uv tool install git-filter-repo
```

### WSL2 Ubuntu
```bash
sudo apt update
sudo apt install -y git curl jq ripgrep fd-find shellcheck sqlite3 direnv tree ca-certificates gnupg
curl -LsSf https://astral.sh/uv/install.sh | sh
curl https://mise.run | sh
curl -fsSL https://deb.nodesource.com/setup_lts.x | sudo -E bash -
sudo apt install -y nodejs just git-filter-repo universal-ctags
corepack enable
corepack prepare pnpm@latest --activate
npm i -g @ast-grep/cli tree-sitter-cli prettier eslint
uv tool install ruff
uv tool install pyright
uv tool install semgrep
uv tool install httpie
cargo install bat eza zoxide shfmt hyperfine watchexec --locked
curl -sSfL https://raw.githubusercontent.com/gitleaks/gitleaks/master/scripts/install.sh | sh -s -- -b ~/.local/bin
go install github.com/tomwright/dasel/v2/cmd/dasel@latest
go install github.com/sourcegraph/zoekt/cmd/zoekt-index@latest
go install github.com/sourcegraph/zoekt/cmd/zoekt@latest
```

### macOS
```bash
brew install git gh ripgrep fd jq yq dasel sqlite curl httpie git-delta bat zoxide fzf tree eza
brew install ast-grep semgrep tree-sitter universal-ctags just mise direnv uv node pnpm docker docker-compose
brew install shellcheck shfmt prettier eslint hadolint trivy gitleaks hyperfine entr watchexec watchman
uv tool install ruff
uv tool install pyright
uv tool install git-filter-repo
```

## Common Commands
```bash
git --version
rg --version
fd --version || fdfind --version
jq --version
node --version
pnpm --version
uv --version
sg --version
```

## Agent-Safe Patterns
- Install only what the task needs in ephemeral containers; full checklist is for reusable workstations.
- Prefer user-local installs (`uv tool`, npm global under user prefix) over system mutation where possible.
- Record versions in issue/PR notes when debugging environment-specific failures.

## Commands Requiring Confirmation
`winget upgrade --all`, `sudo apt full-upgrade`, `brew upgrade`, `curl | sh` from non-official sources, changing shell startup files on a human workstation, installing Docker Desktop on managed devices.

## Troubleshooting
- Restart shells after PATH changes.
- Ubuntu apt versions can lag; use official install scripts or language package managers for newer CLI tools.
- Corporate Windows machines may block winget; use Scoop or approved internal package sources.

## Verification Checklist
```bash
git --version
rg --version
fd --version || fdfind --version
jq --version
yq --version || true
sg --version || true
semgrep --version || true
gitleaks version || true
trivy --version || true
```

Official references checked: project docs/repositories for ripgrep, fd, fzf, tree/eza/zoxide/bat/delta, ast-grep, Semgrep, tree-sitter, universal-ctags, Serena, codebase-memory-mcp, Zoekt, claude-context, git, GitHub CLI, git-filter-repo, Gitleaks, jq, yq, dasel, SQLite, curl, HTTPie, just, mise, direnv, uv, Ruff, Pyright, Node.js/Corepack/pnpm, Docker, ShellCheck, shfmt, Prettier, ESLint, Hadolint, Trivy, hyperfine, entr, watchexec, and Watchman.
