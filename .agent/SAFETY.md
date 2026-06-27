# Agent Safety Rules

- Never commit secrets, tokens, local service URLs with credentials, or private machine paths.
- Keep secrets in environment variables, `.env`, `.env.*`, or `secrets.json`; these files are ignored by git.
- Do not modify `.git/config` or add non-standard git extensions.
- Treat destructive file operations, asset migrations, and generated Unity metadata changes as high-risk; verify `git status` before and after.
- Preserve Unity `.meta` files when adding, moving, or deleting assets.
- Prefer minimal, reviewable diffs with validation commands documented in PRs.
