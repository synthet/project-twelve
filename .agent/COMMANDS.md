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

**Python dependency:** `pip install pyyaml`
