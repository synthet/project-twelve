---
name: unity-tests
description: Run ProjectTwelve Unity batch validation and EditMode/PlayMode test suites via Unity 6000.5.1f1. Use when the user or ticket asks to run Unity tests, EditMode fixtures, batchmode validation, or verify test results from TestResults/*.xml.
---

# Unity tests (compiled harness)

> **Editor:** Unity **6000.5.1f1**. Project root = repo clone. Deterministic runner:
> [`scripts/run_unity_tests.py`](../../../scripts/run_unity_tests.py).

## When to use

- Ticket exit evidence or `/test-and-fix` asks for EditMode or PlayMode.
- After changing C# under `Assets/Scripts/` or `Assets/Tests/`.
- Diagnosing compile errors or missing `TestResults/*.xml`.

## Run (repo root)

```bash
python scripts/run_unity_tests.py validate
python scripts/run_unity_tests.py editmode
python scripts/run_unity_tests.py editmode --filter "SandboxNavPathfinderTests|SandboxSpawnRulesTests"
python scripts/run_unity_tests.py playmode
python scripts/run_unity_tests.py editmode --parity          # also npm test tile-viz + world-viz
python scripts/run_unity_tests.py editmode --parity --parity-print-only
```

Requires `UNITY_EDITOR` (or `UNITY_ROOT`) in the environment or gitignored `.env`. The harness:

- creates `TestResults/` + `Logs/`
- **omits `-quit` with `-runTests` on Windows** (Unity 6 batchmode quirk)
- prints `total/passed/failed/result` and failed case names from NUnit XML
- exits nonzero on missing Unity, missing XML, or failures

Agent terminals: set `block_until_ms` ≥ **600000** for full EditMode.

## LLM judgment (only if needed)

If the harness fails, diagnose from the printed failed names and the log path it reports
(`Logs/unity-*.log`). Common blockers: Editor locking the project, missing `UNITY_EDITOR`,
`Newtonsoft` asmdef refs, leftover `-quit` on Windows `-runTests`.

Do **not** re-derive Unity flags by hand — re-run or extend the harness.

## Related

- [`AGENTS.md`](../../../AGENTS.md) test vocabulary
- [`docs/wiki/quality-gates.md`](../../../docs/wiki/quality-gates.md)
- Compilation policy: [`.agent/SKILL_COMPILATION.md`](../../../.agent/SKILL_COMPILATION.md)
