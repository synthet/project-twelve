---
name: repo-sync
description: Sync project-twelve with the Assets/_Licensed submodule via fetch_remotes.py, git hooks, and /fetch-remotes. Use at session start, before picking backlog work, or when the user asks to sync repos or get latest.
capability: "repo-sync agent asset workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "See asset body for tool requirements."
output_schema: "Markdown report or documented command output."
risk_class: medium
---

# Repo sync (compiled harness)

> Main repo [project-twelve](https://github.com/synthet/project-twelve); licensed art submodule
> [project-twelve-assets](https://github.com/synthet/project-twelve-assets) at `Assets/_Licensed/`.
> Runner: [`scripts/fetch_remotes.py`](../../../scripts/fetch_remotes.py).

## When to use

- Session start or before picking backlog work (`/fetch-remotes`).
- User says “sync repo”, “get latest”, or “update submodule”.
- Submodule checkout looks wrong after pull/branch switch (`--local-sync`).
- Before committing parent changes that touch the submodule gitlink.

## Run

```bash
python scripts/fetch_remotes.py              # fetch + fast-forward + checkout gitlink SHA
python scripts/fetch_remotes.py --fetch-only  # network fetch only
python scripts/fetch_remotes.py --local-sync  # align checkout to parent gitlink
python scripts/fetch_remotes.py --verify      # fail if misaligned
```

Default sync checks out the submodule at the **parent gitlink**, not the submodule `main` tip.
Prefer `/fetch-remotes` (optional `[--fetch-only]`) for agent-driven sync.

## Advance assets

Commit/push in **project-twelve-assets**, then bump the parent gitlink via
[`assets-submodule-publish`](../assets-submodule-publish/SKILL.md)
(`python scripts/publish_assets_submodule.py`).

## Safety

- Never stage licensed blobs under wrong paths — [`docs/PAID_ASSETS.md`](../../../docs/PAID_ASSETS.md),
  `scripts/check_paid_assets.py`.
- Do not use `git submodule update --remote` unless explicitly maintaining a pointer bump.
- Humans may install hooks once: `python scripts/install_githooks.py` (agents do not). Hook table:
  [`references/hooks.md`](references/hooks.md).

## Related

- Compilation policy: [`.agent/SKILL_COMPILATION.md`](../../../.agent/SKILL_COMPILATION.md)
