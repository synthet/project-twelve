---
capability: "critical-commit-audit agent asset workflow"
side_effect_level: local_write
approval_required: false
requires_tools: "git log, git diff, read/search tools"
output_schema: "Markdown audit report; optional minimal fix PR when critical bug confirmed"
risk_class: medium
---

> **Claude Code:** Same intent as Cursor `/critical-commit-audit`. When customizing, keep in sync with `.cursor/commands/critical-commit-audit.md`.

# /critical-commit-audit — Deep post-commit bug hunt

Use when you want a **high-severity** correctness review of recent commits (data loss, crashes, security holes, major user-facing breakage). This is read-first investigation; fixes only when a concrete trigger is proven.

## Inputs

- Optional commit range (default: last 20 commits or `main..HEAD`).
- **AGENTS.md** for test commands.

## Authority

Follow **`.claude/skills/critical-commit-audit/SKILL.md`** (canonical playbook). Optionally delegate to the `critical-commit-audit` subagent for autonomous execution.

## Output

- **When nothing critical:** short summary — commits reviewed, focus areas, **no critical issues** with concrete trigger.
- **When critical bug found:** bug/impact, root cause, minimal fix, tests run, validation evidence.

## Done when

- Recent commits are reviewed with full code-path tracing (not diff-only pattern matching).
- Findings meet the skill's confidence bar (concrete trigger required before opening a PR).
- Any fix is minimal, tested per AGENTS.md, and scoped to the defect.

## Related

- `/run-subagent-review` — optional external second opinion before opening a fix PR.
- `/pr-ready` — merge-ready description after a fix.
