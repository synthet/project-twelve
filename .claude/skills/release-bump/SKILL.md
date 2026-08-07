---
name: release-bump
description: Use when cutting a release, bumping project version, promoting the changelog Unreleased section, tagging release prep, or applying a semver rubric. Includes verification steps before committing release changes.
capability: "release-bump agent asset workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "python .claude/skills/release-bump/scripts/harness.py; git log; project verify commands"
output_schema: "Harness JSON/summary with current/proposed version, Unreleased bullets, verify commands"
risk_class: medium
---

# Release bump (compiled harness)

Thin bootloader. Deterministic work lives in the harness; you only decide the semver level.

## Invoke

```bash
# Inspect current version + Unreleased bullets (no writes)
python .claude/skills/release-bump/scripts/harness.py --json

# After choosing major|minor|patch, apply VERSION + CHANGELOG
python .claude/skills/release-bump/scripts/harness.py --level minor --apply --json
```

## LLM judgment slots

1. **Choose bump level** from `git log <last-tag>..HEAD` and harness `unreleased_bullets`:
   - **major** — breaking API/config/schema/CLI/MCP/autotile contract change
   - **minor** — backward-compatible capability
   - **patch** — bug fix, docs, internal refactor
2. State the level and 1–2 justifying changes, then pass `--level`.

If the harness reports no version source, ask the user before creating `VERSION` /
`CHANGELOG.md`. Unity bundle version in `ProjectSettings` is a human Editor workflow,
not this harness.

## Human authority

Do **not** commit, tag, or push unless the user explicitly asks. Suggest
`chore(release): vX.Y.Z` and annotated tag only after verify commands succeed.

## Verify

Run every command listed in harness `verify_commands` (and `check_paid_assets.py` /
`check_markdown_links.py` as listed) before any release commit.
