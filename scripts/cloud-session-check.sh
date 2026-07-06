#!/bin/bash
# SessionStart hook: re-verify cloud session tooling on startup/resume.
# Skips locally when CLAUDE_CODE_REMOTE is unset.
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

root="${CLAUDE_PROJECT_DIR:-.}"
cd "$root"

python3 -m pip install --quiet pyyaml

python scripts/sync_assistant_trees.py --check

if [ -f .agent-memory/memory.md ]; then
  echo "--- Project memory (.agent-memory/memory.md) ---"
  python scripts/agent-memory/context.py
  echo "--- End project memory ---"
fi

cd tools/tile-viz && npm test
cd ../world-viz && npm test
