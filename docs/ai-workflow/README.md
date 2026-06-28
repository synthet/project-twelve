# AI Workflow

ProjectTwelve follows a lightweight spec-first loop adapted from `synthet-code-framework`.

1. **Spec** — identify the gameplay, tooling, or documentation outcome and affected canonical sources.
2. **Plan** — list the files to change, validation commands, and Unity asset/meta implications.
3. **Implement** — make focused diffs; avoid unrelated formatting or broad rewrites.
4. **Test and fix** — run the relevant commands from `AGENTS.md`; document environment limitations when Unity is unavailable.
5. **PR-ready** — review `git status --short`, run `python3 scripts/check_paid_assets.py --staged` (or `--push`), summarize changes, cite tests, and note any follow-up work.

## Unity MCP
When the Unity Editor is open, agents can call Editor tools (scenes, assets, console) through [Unity MCP](https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.9/manual/integration/unity-mcp-overview.html). Setup: `AGENTS.md` → **Unity MCP (Editor bridge)**. Package: `com.unity.ai.assistant` in `Packages/manifest.json`.

See `.agent/SAFETY.md` for hard safety rules and `.agent/AGENT_INFRA_INVENTORY.md` for the adopted agent infrastructure.
