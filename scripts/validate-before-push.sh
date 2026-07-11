#!/bin/bash
# Validate all quality gates before pushing to remote.
# Run this from the repo root: bash scripts/validate-before-push.sh

set -o pipefail  # Fail if any command in a pipe fails

echo "🔍 Running ProjectTwelve quality gate validation..."
echo ""

# Determine the range of commits that will be pushed (committed-but-unpushed),
# so the gates inspect the same delta that CI sees — not the staging area, which
# is empty in the normal "commit, then validate, then push" flow.
if git rev-parse --abbrev-ref --symbolic-full-name @{upstream} >/dev/null 2>&1; then
    BASE_REF="$(git rev-parse --abbrev-ref --symbolic-full-name @{upstream})"
else
    # No upstream configured yet (e.g. a fresh branch): compare against origin/master.
    BASE_REF="origin/master"
fi
RANGE="${BASE_REF}...HEAD"
echo "Comparing against: ${RANGE}"
echo ""

# Files changed in the range under review.
CHANGED_FILES="$(git diff --name-only "${RANGE}")"

# Counter for pass/fail
CHECKS_PASSED=0
CHECKS_FAILED=0

# Helper function
run_check() {
    local name=$1
    local cmd=$2
    echo "▶ $name..."
    if eval "$cmd"; then
        echo "  ✅ PASS: $name"
        ((CHECKS_PASSED++))
    else
        echo "  ❌ FAIL: $name"
        ((CHECKS_FAILED++))
    fi
    echo ""
}

# 1. Markdown link validation
run_check "Markdown link validation" \
    "python3 scripts/check_markdown_links.py"

# 2. OKF frontmatter validation (if docs changed in the push range)
if echo "${CHANGED_FILES}" | grep -q "^docs/"; then
    run_check "OKF frontmatter validation" \
        "python scripts/ci/okf_lint_changed.py --base '${BASE_REF}' --head HEAD --profile project --fail-on error"
else
    echo "⊘ OKF validation skipped (no docs/ changes in ${RANGE})"
    echo ""
fi

# 3. Paid assets validation (mirrors the CI pre-push check)
run_check "Paid asset validation" \
    "python3 scripts/check_paid_assets.py --push"

# 4. Assistant tree sync validation (if canonical or generated assets changed in the push range)
if echo "${CHANGED_FILES}" | grep -qE "^\.claude/|^\.cursor/|^\.agents/skills/"; then
    run_check "Assistant tree sync" \
        "python scripts/sync_assistant_trees.py --check"
else
    echo "⊘ Tree sync skipped (no canonical/generated assistant changes in ${RANGE})"
    echo ""
fi

# 5. Python syntax validation. Build the file list explicitly so missing
# optional directories don't error, and let py_compile's own exit code through
# (no `|| true` — a real syntax error must fail this gate).
PY_FILES="$(git ls-files 'scripts/*.py')"
if [ -n "${PY_FILES}" ]; then
    run_check "Python script syntax" \
        "echo \"${PY_FILES}\" | xargs python -m py_compile"
else
    echo "⊘ Python syntax check skipped (no tracked scripts/*.py files)"
    echo ""
fi

# Summary
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Quality Gate Summary"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "✅ Passed: $CHECKS_PASSED"
echo "❌ Failed: $CHECKS_FAILED"
echo ""

if [ $CHECKS_FAILED -eq 0 ]; then
    echo "🎉 All quality gates passed! Ready to push."
    exit 0
else
    echo "⚠️  Fix the above errors before pushing."
    exit 1
fi
