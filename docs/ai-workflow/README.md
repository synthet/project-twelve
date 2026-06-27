# AI Workflow

ProjectTwelve follows a lightweight spec-first loop adapted from `synthet-code-framework`.

1. **Spec** — identify the gameplay, tooling, or documentation outcome and affected canonical sources.
2. **Plan** — list the files to change, validation commands, and Unity asset/meta implications.
3. **Implement** — make focused diffs; avoid unrelated formatting or broad rewrites.
4. **Test and fix** — run the relevant commands from `AGENTS.md`; document environment limitations when Unity is unavailable.
5. **PR-ready** — review `git status --short`, summarize changes, cite tests, and note any follow-up work.

See `.agent/SAFETY.md` for hard safety rules and `.agent/AGENT_INFRA_INVENTORY.md` for the adopted agent infrastructure.
