# Skill inventory — ProjectTwelve

**Last reviewed:** 2026-07-22

Compilation policy: [SKILL_COMPILATION.md](./SKILL_COMPILATION.md).

Frontmatter invariants (enforced by `scripts/ci/check_agent_frontmatter.py`): skills, agents, and
commands require `capability`, `side_effect_level`, `approval_required`, `requires_tools`,
`output_schema`, and `risk_class`. Use `python scripts/migrate_agent_frontmatter.py` to backfill.

| Skill | Path | Tier | Notes |
|-------|------|------|-------|
| unity-tests | `.claude/skills/unity-tests/SKILL.md` | L1 | Compiled: `scripts/run_unity_tests.py` (Unity 6000.5.1f1) |
| agent-memory | `.claude/skills/agent-memory/SKILL.md` | L1 | Compiled bootloader over `scripts/agent-memory/*` |
| backlog-queue | `.claude/skills/backlog-queue/SKILL.md` | L1 | Pick step uses `pick_next_task.py`; claim/PR still prose |
| pick-next-task | `.claude/skills/pick-next-task/SKILL.md` | L1 | Compiled: `scripts/pick_next_task.py` (read-only rank) |
| repo-sync | `.claude/skills/repo-sync/SKILL.md` | L1 | Compiled bootloader over `scripts/fetch_remotes.py` |
| assets-submodule-publish | `.claude/skills/assets-submodule-publish/SKILL.md` | L1 | Commit assets submodule + bump gitlink |
| commit-conventions | `.claude/skills/commit-conventions/SKILL.md` | L1 | Commit message conventions |
| commit-and-push | `.claude/skills/commit-and-push/SKILL.md` | L2 | Compiled harness + paid-assets / licensed-path gates |
| karpathy-guidelines | `.claude/skills/karpathy-guidelines/SKILL.md` | L1 | Assumptions, simplicity, surgical diffs |
| systematic-debugging | `.claude/skills/systematic-debugging/SKILL.md` | L1 | Evidence-first RCA loop |
| test-driven-development | `.claude/skills/test-driven-development/SKILL.md` | L1 | Red-green-refactor; Unity/tile-viz seams |
| verification-before-completion | `.claude/skills/verification-before-completion/SKILL.md` | L1 | Compiled harness; fresh command proof before "done" |
| skill-authoring | `.claude/skills/skill-authoring/SKILL.md` | L2 | First-party skill quality / triggering |
| search-tool-selection | `.claude/skills/search-tool-selection/SKILL.md` | L1 | Compiled harness router (fd/rg/ast-grep/fff) |
| safe-command-patterns | `.claude/skills/safe-command-patterns/SKILL.md` | L1 | Bounded, safe command workflows |
| search-and-navigation | `.claude/skills/search-and-navigation/SKILL.md` | L1 | rg, fd, fzf, tree, bat |
| git-and-diff-workflows | `.claude/skills/git-and-diff-workflows/SKILL.md` | L1 | git, gh, delta, gitleaks |
| cli-tools-overview | `.claude/skills/cli-tools-overview/SKILL.md` | L1 | CLI tooling map for agents |
| task-env-package-tools | `.claude/skills/task-env-package-tools/SKILL.md` | L1 | just, mise, uv, pnpm, Docker |
| structural-code-search | `.claude/skills/structural-code-search/SKILL.md` | L1 | ast-grep, semgrep, ctags |
| data-config-tools | `.claude/skills/data-config-tools/SKILL.md` | L1 | jq, yq, sqlite, curl, httpie |
| install-checklist | `.claude/skills/install-checklist/SKILL.md` | L2 | Human workstation provisioning only |
| lint-format-security | `.claude/skills/lint-format-security/SKILL.md` | L2 | shellcheck, trivy, gitleaks, ruff |
| mcp-code-intelligence | `.claude/skills/mcp-code-intelligence/SKILL.md` | L2 | MCP / indexed / embedding search layers |
| graphify | `.claude/skills/graphify/SKILL.md` | L2 | Local knowledge graph (CLI + optional MCP); architecture / relationship queries |
| pixellab-mcp | `.claude/skills/pixellab-mcp/SKILL.md` | L2 | Remote pixel-art generation; paid writes, capability URLs, and destructive/remote-execution gates |
| windows-agent-tooling | `.claude/skills/windows-agent-tooling/SKILL.md` | L2 | Native Windows / PowerShell agent setup |
| wsl2-agent-tooling | `.claude/skills/wsl2-agent-tooling/SKILL.md` | L2 | WSL2 Ubuntu agent execution environment |
| critical-commit-audit | `.claude/skills/critical-commit-audit/SKILL.md` | L2 | High-severity bug hunt |
| mcp-server-design | `.claude/skills/mcp-server-design/SKILL.md` | L2 | MCP server design patterns |
| security-review | `.claude/skills/security-review/SKILL.md` | L2 | Security review checklist |
| subagent-review | `.claude/skills/subagent-review/SKILL.md` | L2 | External CLI review via orchestrator |
| threat-modeling-agentic-tools | `.claude/skills/threat-modeling-agentic-tools/SKILL.md` | L2 | Agentic tooling threat model |
| validate-implementation | `.claude/skills/validate-implementation/SKILL.md` | L1 | Compiled harness; AC evidence table before /pr-ready |
| eval | `.claude/skills/eval/SKILL.md` | L1 | Task quality signals → agent memory |
| release-bump | `.claude/skills/release-bump/SKILL.md` | L2 | Compiled harness; semver + changelog when VERSION exists |

CLI skill maintenance spec: [`.agent/cli-tools-skills-spec.md`](./cli-tools-skills-spec.md).

After adding or materially changing a skill, update this table and run:

```bash
python scripts/validate_cli_skills.py
python scripts/sync_assistant_trees.py
```

The sync generates both `.cursor/skills/` and Codex-native `.agents/skills/`.

See also [AGENT_INFRA_INVENTORY.md](./AGENT_INFRA_INVENTORY.md) for the full agent asset catalog.
