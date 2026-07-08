# /publish-assets-submodule — Commit assets repo and bump gitlink

Commit and push changes in **project-twelve-assets** (`Assets/_Licensed/`), then record the new submodule pointer in **project-twelve**.

## When to use

- After editing licensed docs, catalogs, or art under `Assets/_Licensed/`.
- User asks to publish submodule changes or bump `Assets/_Licensed` gitlink.

## Steps

1. Read skill: `.claude/skills/assets-submodule-publish/SKILL.md`.
2. Inspect alignment:

   ```bash
   python scripts/publish_assets_submodule.py --status
   ```

3. Publish (adjust `-m` to match the change):

   ```bash
   python scripts/publish_assets_submodule.py \
     -m "docs: <subject>" \
     --submodule-checkout main \
     --pull-submodule
   ```

   If the submodule is **already pushed** and only the parent gitlink is stale:

   ```bash
   python scripts/publish_assets_submodule.py \
     --submodule-checkout main \
     --pull-submodule \
     --skip-submodule-push \
     --parent-message "chore(assets): bump Assets/_Licensed submodule pointer"
   ```

4. Report submodule SHA, parent commit (if any), and whether push is still needed.

## Safety

- Licensed blobs stay in **project-twelve-assets**; parent commit should only change the gitlink (plus any public `docs/` edits).
- Parent bump runs `check_paid_assets.py --staged`.
- Do not push parent unless the user asks.

## See also

- `/fetch-remotes` — pull main repo and sync submodule to existing gitlink
- Skill: `assets-submodule-publish`, `repo-sync`
- `docs/PAID_ASSETS.md`, `docs/wiki/licensed-assets-reference.md`
