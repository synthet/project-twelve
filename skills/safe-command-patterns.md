# Safe Command Patterns

## Purpose
Give agents reusable command rules that minimize accidental data loss, context overflow, and unreviewed changes.

## When to Use
Use throughout every coding task: before editing, while searching, after modifying files, and before final response or commit.

## Required Tools
git, rg, fd, bat or sed/Get-Content, delta, project task runner, language-specific test/lint tools.

## Install
### Windows PowerShell
```powershell
winget install Git.Git BurntSushi.ripgrep.MSVC sharkdp.fd sharkdp.bat dandavison.delta
```

### WSL2 Ubuntu
```bash
sudo apt update
sudo apt install -y git ripgrep fd-find
```

### macOS
```bash
brew install git ripgrep fd bat git-delta
```

## Common Commands
```bash
git status --short
rg "SomeSymbol" . --glob '!node_modules' --glob '!target' --glob '!build'
fd "Controller|Service|Repository" .
sed -n '1,160p' path/to/file
bat --line-range 1:160 path/to/file
git diff --stat
git diff -- path/to/file | delta
```
PowerShell:
```powershell
git status --short
Get-Content .\path\to\file.java -TotalCount 160
git diff --stat
```

## Agent-Safe Patterns
- Inspect before action: status, tree/files, relevant configs, then edit.
- Keep output bounded and path-scoped; never dump generated/minified/vendor files.
- Prefer additive patches and focused changes; avoid drive-by formatting.
- Verify with the smallest relevant test first, then broader project checks.
- Preserve unrelated user changes; do not stage or revert files you did not change.

## Commands Requiring Confirmation
Deletion, history rewrite, force push, package publishing, production deploys, database writes, credential changes, installing global services, `curl | sh` on unreviewed URLs.

## Troubleshooting
- If output is too large, rerun with path filters, `--max-count`, `head`, `sed -n`, or `--line-range`.
- If tests fail from missing dependencies or services, report as an environment limitation with exact output.
- If unexpected modified files exist, stop and avoid overwriting them.

## Verification Checklist
```bash
git status --short
git diff --stat
git diff --check
```

Official references checked: project docs/repositories for ripgrep, fd, fzf, tree/eza/zoxide/bat/delta, ast-grep, Semgrep, tree-sitter, universal-ctags, Serena, codebase-memory-mcp, Zoekt, claude-context, git, GitHub CLI, git-filter-repo, Gitleaks, jq, yq, dasel, SQLite, curl, HTTPie, just, mise, direnv, uv, Ruff, Pyright, Node.js/Corepack/pnpm, Docker, ShellCheck, shfmt, Prettier, ESLint, Hadolint, Trivy, hyperfine, entr, watchexec, and Watchman.
