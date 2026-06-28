# Agent Safety Rules

- Never commit secrets, tokens, local service URLs with credentials, or private machine paths.
- Keep secrets in environment variables, `.env`, `.env.*`, or `secrets.json`; these files are ignored by git.
- **Never commit licensed Asset Store content into the public repo.** Licensed art lives in the `Assets/_Licensed` git submodule (`project-twelve-assets`). Run `python3 scripts/check_paid_assets.py --staged` before commits and `--push` before push.
- Do not modify `.git/config` or add non-standard git extensions.
- Treat destructive file operations, asset migrations, and generated Unity metadata changes as high-risk; verify `git status` before and after.
- Preserve Unity `.meta` files when adding, moving, or deleting **project-owned** assets (not paid/local-only packs).
- Prefer minimal, reviewable diffs with validation commands documented in PRs.
