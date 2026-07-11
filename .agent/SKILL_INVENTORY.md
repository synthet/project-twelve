# Skill inventory — ProjectTwelve

**Last reviewed:** 2026-07-10

| Skill | Path | Tier | Notes |
|-------|------|------|-------|
| unity-tests | `.claude/skills/unity-tests/SKILL.md` | L1 | Unity 6000.5.1f1 batch load, EditMode/PlayMode, `.env` paths |
| agent-memory | `.claude/skills/agent-memory/SKILL.md` | L1 | Session log → dream → promote |
| backlog-queue | `.claude/skills/backlog-queue/SKILL.md` | L1 | ProjectTwelve fork: wiki tickets + issues |
| pick-next-task | `.claude/skills/pick-next-task/SKILL.md` | L1 | Read-only next-ticket recommendation |
| repo-sync | `.claude/skills/repo-sync/SKILL.md` | L1 | Main repo + assets submodule sync |
| commit-conventions | `.claude/skills/commit-conventions/SKILL.md` | L1 | Commit message conventions |
| search-tool-selection | `.claude/skills/search-tool-selection/SKILL.md` | L1 | When to use fd, rg, grep, ast-grep, fzf |
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
| windows-agent-tooling | `.claude/skills/windows-agent-tooling/SKILL.md` | L2 | Native Windows / PowerShell agent setup |
| wsl2-agent-tooling | `.claude/skills/wsl2-agent-tooling/SKILL.md` | L2 | WSL2 Ubuntu agent execution environment |
| critical-commit-audit | `.claude/skills/critical-commit-audit/SKILL.md` | L2 | High-severity bug hunt |
| mcp-server-design | `.claude/skills/mcp-server-design/SKILL.md` | L2 | MCP server design patterns |
| security-review | `.claude/skills/security-review/SKILL.md` | L2 | Security review checklist |
| subagent-review | `.claude/skills/subagent-review/SKILL.md` | L2 | External CLI review via orchestrator |
| threat-modeling-agentic-tools | `.claude/skills/threat-modeling-agentic-tools/SKILL.md` | L2 | Agentic tooling threat model |
| validate-implementation | `.claude/skills/validate-implementation/SKILL.md` | L1 | AC evidence table before /pr-ready |
| eval | `.claude/skills/eval/SKILL.md` | L1 | Task quality signals → agent memory |
| release-bump | `.claude/skills/release-bump/SKILL.md` | L2 | Semver + changelog when VERSION exists |

CLI skill maintenance spec: [`.agent/cli-tools-skills-spec.md`](./cli-tools-skills-spec.md).

After adding or materially changing a skill, update this table and run:

```bash
python scripts/validate_cli_skills.py
python scripts/sync_assistant_trees.py
```

The sync generates both `.cursor/skills/` and Codex-native `.agents/skills/`.

See also [AGENT_INFRA_INVENTORY.md](./AGENT_INFRA_INVENTORY.md) for the full agent asset catalog.
