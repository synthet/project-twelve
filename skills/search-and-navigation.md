# Search and Navigation

## Purpose
Find files, symbols, text, and nearby context quickly without loading entire repositories into context.

## When to Use
Use at the start of any code task, before editing, and whenever a symbol, route, config key, or test needs locating.

## Required Tools
ripgrep (`rg`), fd (`fd`/`fdfind`), fzf, tree, eza, zoxide, bat, git.

## Install
### Windows PowerShell
```powershell
winget install BurntSushi.ripgrep.MSVC sharkdp.fd sharkdp.bat ajeetdsouza.zoxide eza-community.eza junegunn.fzf
```

### WSL2 Ubuntu
```bash
sudo apt update
sudo apt install -y ripgrep fd-find fzf tree
cargo install eza zoxide --locked
```

### macOS
```bash
brew install ripgrep fd fzf tree eza zoxide bat
```

## Common Commands
```bash
rg "SomeSymbol" . --glob '!node_modules' --glob '!target' --glob '!build'
rg --files . | sed -n '1,200p'
fd "Controller|Service|Repository" .
tree -L 3 -I 'node_modules|target|build|dist|.git'
eza --tree --level=3 --git-ignore
bat --line-range 1:160 path/to/file
```
PowerShell:
```powershell
rg "SomeSymbol" .
fd "Controller|Service|Repository"
Get-Content .\path\to\file.java -TotalCount 160
```

## Agent-Safe Patterns
- Bound results: `rg -n --max-count 20 "pattern" path/` and `fd pattern path -d 4`.
- Exclude generated/heavy paths: `--glob '!node_modules' --glob '!dist' --glob '!target'`.
- Use `rg --files` before opening files; use `bat --line-range` or `sed -n` for slices.

## Commands Requiring Confirmation
Do not pipe search results into deletion or rewriting commands automatically, e.g. `fd ... -x rm`, `rg ... | xargs sed -i`, or bulk rename commands.

## Troubleshooting
- `rg` respects `.gitignore`; add `-uuu` only after confirming generated/vendor content is needed.
- `fd` regex is default; use `-g` for glob matching.
- If output is too colorful for logs, add `--color=never`.

## Verification Checklist
```bash
rg --version
fd --version || fdfind --version
fzf --version
tree --version || eza --version
bat --version || batcat --version
```

Official references checked: project docs/repositories for ripgrep, fd, fzf, tree/eza/zoxide/bat/delta, ast-grep, Semgrep, tree-sitter, universal-ctags, Serena, codebase-memory-mcp, Zoekt, claude-context, git, GitHub CLI, git-filter-repo, Gitleaks, jq, yq, dasel, SQLite, curl, HTTPie, just, mise, direnv, uv, Ruff, Pyright, Node.js/Corepack/pnpm, Docker, ShellCheck, shfmt, Prettier, ESLint, Hadolint, Trivy, hyperfine, entr, watchexec, and Watchman.
