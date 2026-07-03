# Git and Diff Workflows

## Purpose
Inspect repository state, create minimal patches, review diffs, and interact with GitHub safely.

## When to Use
Use before editing, before committing, for PR review, history inspection, secret cleanup planning, and GitHub issue/PR workflows.

## Required Tools
git, gh, delta, git-filter-repo, gitleaks, rg/fd.

## Install
### Windows PowerShell
```powershell
winget install Git.Git GitHub.cli dandavison.delta Gitleaks.Gitleaks
uv tool install git-filter-repo
```

### WSL2 Ubuntu
```bash
sudo apt update
sudo apt install -y git git-filter-repo
curl -sSfL https://raw.githubusercontent.com/gitleaks/gitleaks/master/scripts/install.sh | sh -s -- -b ~/.local/bin
```

### macOS
```bash
brew install git gh git-delta git-filter-repo gitleaks
```

## Common Commands
```bash
git status --short
git branch --show-current
git log --oneline -n 20
git diff --stat
git diff -- path/to/file | delta
git diff --check
gh pr status
gitleaks detect --source . --no-git --redact --verbose
```

## Agent-Safe Patterns
- Always run `git status --short` before editing and before commit.
- Use path-scoped diffs for review: `git diff -- src/file`.
- Use `git add path1 path2` instead of `git add .` when unrelated user changes exist.
- Prefer `gh pr view --json title,body,files` over scraping web pages.

## Commands Requiring Confirmation
`git reset --hard`, `git clean -fdx`, `git push --force`, `git rebase -i`, `git filter-repo`, `git gc --prune=now`, deleting branches, `gh pr merge`, release publishing.

## Troubleshooting
- If `delta` is unavailable, use plain `git diff -- path`.
- If hooks fail, read hook output; do not bypass with `--no-verify` unless a human explicitly confirms.
- For secret history cleanup, coordinate with maintainers before `git-filter-repo` and force pushes.

## Verification Checklist
```bash
git --version
gh --version || true
delta --version || true
gitleaks version || true
git status --short
git diff --stat
```

Official references checked: project docs/repositories for ripgrep, fd, fzf, tree/eza/zoxide/bat/delta, ast-grep, Semgrep, tree-sitter, universal-ctags, Serena, codebase-memory-mcp, Zoekt, claude-context, git, GitHub CLI, git-filter-repo, Gitleaks, jq, yq, dasel, SQLite, curl, HTTPie, just, mise, direnv, uv, Ruff, Pyright, Node.js/Corepack/pnpm, Docker, ShellCheck, shfmt, Prettier, ESLint, Hadolint, Trivy, hyperfine, entr, watchexec, and Watchman.
