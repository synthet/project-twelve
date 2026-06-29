#!/bin/bash
# Validate all quality gates before pushing to remote.
# Run this from the repo root: bash scripts/validate-before-push.sh

set -o pipefail  # Fail if any command in a pipe fails

echo "🔍 Running ProjectTwelve quality gate validation..."
echo ""

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

# 2. OKF frontmatter validation (if docs changed)
if git diff --cached --name-only | grep -q "docs/"; then
    run_check "OKF frontmatter validation" \
        "python scripts/ci/okf_lint_changed.py --base origin/master --head HEAD --profile project --fail-on warning"
else
    echo "⊘ OKF validation skipped (no docs/ changes staged)"
    echo ""
fi

# 3. Paid assets validation
run_check "Paid asset validation" \
    "python3 scripts/check_paid_assets.py --staged"

# 4. Assistant tree sync validation (if .claude/ or .cursor/ changed)
if git diff --cached --name-only | grep -qE "^\.claude/|^\.cursor/"; then
    run_check "Assistant tree sync" \
        "python scripts/sync_assistant_trees.py --check"
else
    echo "⊘ Tree sync skipped (no .claude/ or .cursor/ changes staged)"
    echo ""
fi

# 5. Python syntax validation
run_check "Python script syntax" \
    "find scripts -type f -name '*.py' -print0 | xargs -0 --no-run-if-empty python -m py_compile"

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
