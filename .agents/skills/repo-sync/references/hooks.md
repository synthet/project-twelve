# Git hooks (repo-sync)

Humans opt in locally (`python scripts/install_githooks.py`). Agents do **not** install hooks
automatically.

| Hook | Repo | Behavior |
|------|------|----------|
| `pre-commit` | parent | Paid-asset guard + `fetch_remotes.py --verify` |
| `pre-push` | parent | Paid-asset guard + `--verify` |
| `post-merge` | parent | `--local-sync` after pull/merge |
| `post-checkout` | parent | `--local-sync` on branch switch |
| `submodule/pre-push` | assets | Warn if parent gitlink ≠ submodule `HEAD` |

Hooks enforce **local alignment** only; they do not fetch remotes on every commit. Use
`/fetch-remotes` or `python scripts/fetch_remotes.py` for network sync.
