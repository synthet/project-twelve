# Verified commands — ProjectTwelve

Quick reference of commands known to work in this repo. Keep in sync with `AGENTS.md`.

## Build / test / lint (unity)

```bash
Unity -batchmode -quit -projectPath . -logFile Logs/unity-validate.log
Unity -batchmode -quit -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile Logs/unity-editmode-tests.log
python3 scripts/check_markdown_links.py && python3 scripts/check_paid_assets.py --staged
```

## Docs / OKF

```bash
python scripts/okf_lint.py --profile project --exclude-prefix archive/ docs
python scripts/wiki_lint.py --exclude-prefix archive/
python scripts/ci/okf_lint_changed.py --base origin/main --head HEAD   # CI / PR
```

## Project memory

```bash
python scripts/agent-memory/log_session.py --summary "..." --outcome "..." --candidate "text|working_rule|high"
python scripts/agent-memory/dream.py
python scripts/agent-memory/promote_dream.py --dream .agent-memory/dreams/<timestamp>.md
python scripts/agent-memory/context.py
```

## Agent assets

```bash
python scripts/sync_assistant_trees.py           # regenerate .cursor/ from .claude/
python scripts/sync_assistant_trees.py --check   # CI drift gate
python scripts/validate_cli_skills.py            # CLI skill headings/structure
```

## Paid assets guard

```bash
python3 scripts/check_paid_assets.py --staged   # before commit
python3 scripts/check_paid_assets.py --push       # before push
```

## Visual catalogs (submodule)

```bash
python3 scripts/generate_visual_catalogs.py
```

## Repo sync (main + assets submodule)

```bash
python scripts/fetch_remotes.py              # fetch both remotes, ff-only pull, sync gitlink
python scripts/fetch_remotes.py --fetch-only   # fetch only; no pull on main branch
python scripts/fetch_remotes.py --local-sync   # align submodule checkout to gitlink (hook path)
python scripts/fetch_remotes.py --verify       # fail if submodule misaligned (hook path)
python scripts/install_githooks.py             # one-time: enable .githooks on parent + submodule
python scripts/install_githooks.py --check     # verify hooksPath configured
```

Agent slash command: `/fetch-remotes` (skill: `.claude/skills/repo-sync/SKILL.md`).

| Hook | Repo | Behavior |
|------|------|----------|
| `pre-commit` | parent | `check_paid_assets.py --staged` + `fetch_remotes.py --verify` |
| `pre-push` | parent | `check_paid_assets.py --push` + `fetch_remotes.py --verify` |
| `post-merge` | parent | `fetch_remotes.py --local-sync` |
| `post-checkout` | parent | `--local-sync` when switching branches |
| `submodule/pre-push` | assets | `fetch_remotes.py --warn-parent-gitlink` |

**Python dependency:** `pip install pyyaml`
