---
name: unity-tests
description: Run ProjectTwelve Unity batch validation and EditMode/PlayMode test suites via Unity 6000.5.1f1. Use when the user or ticket asks to run Unity tests, EditMode fixtures, batchmode validation, or verify test results from TestResults/*.xml.
---

# Unity tests (ProjectTwelve)

> **Editor:** Unity **6000.5.1f1** (Unity 6). Project root = repo clone folder (same path for git and `-projectPath`).

## When to use

- Ticket exit evidence or `/test-and-fix` asks for EditMode or PlayMode runs.
- After changing C# under `Assets/Scripts/` or `Assets/Tests/`.
- Before closing a ticket that lists Unity verification in its exit checklist.
- Diagnosing `Scripts have compiler errors` or stale `Library/` after asmdef/package changes.

## Resolve Unity executable

**Preferred:** repo-root `.env` (gitignored — never commit):

```env
UNITY_ROOT=D:\Soft\Unity
UNITY_EDITOR=D:\Soft\Unity\Hub\Editor\6000.5.1f1\Editor\Unity.exe
```

Adjust paths for the machine. Hub layout: `%UNITY_ROOT%\Hub\Editor\6000.5.1f1\Editor\Unity.exe`.

**PowerShell — load `.env` and verify:**

```powershell
$Unity = $env:UNITY_EDITOR
if (-not $Unity -and (Test-Path .env)) {
  Get-Content .env | ForEach-Object {
    if ($_ -match '^UNITY_EDITOR=(.+)$') { $Unity = $Matches[1].Trim() }
  }
}
if (-not $Unity) { throw 'Set UNITY_EDITOR in .env or environment' }
& $Unity -version   # expect: 6000.5.1f1
```

**Fallback:** Unity Hub default install or `Unity` on `PATH` if already configured.

## Output directories

Create once per repo (not tracked):

```powershell
New-Item -ItemType Directory -Force -Path TestResults, Logs | Out-Null
```

| Artifact | Path |
|----------|------|
| EditMode NUnit XML | `TestResults/editmode.xml` |
| EditMode log | `Logs/unity-editmode-tests.log` |
| PlayMode NUnit XML | `TestResults/playmode.xml` |
| PlayMode log | `Logs/unity-playmode-tests.log` |
| Batch load only | `Logs/unity-validate.log` |

## Batch project load (compile check)

Quick smoke — confirms scripts compile and project opens:

```powershell
& $Unity -batchmode -quit -projectPath . -logFile Logs/unity-validate.log
```

Exit code **0** = load succeeded. On failure, read the log tail for `error CS` lines.

## EditMode test suite (full)

**Canonical command** (from [`AGENTS.md`](../../../AGENTS.md) test vocabulary):

```powershell
& $Unity -batchmode -projectPath . `
  -runTests -testPlatform EditMode `
  -testResults TestResults/editmode.xml `
  -logFile Logs/unity-editmode-tests.log
```

### Windows batchmode quirk (Unity 6)

**Do not pass `-quit` on the same invocation as `-runTests` on Windows.** Unity can exit right after the initial refresh and **never run tests** (log shows `Batchmode quit successfully invoked` with no Test Runner lines; no `editmode.xml`).

- **Correct:** batchmode + `-runTests` **without** `-quit`; Unity exits after the run completes.
- **Also works:** `Start-Process -Wait` with explicit absolute paths to `-testResults` and `-logFile`.

Allow **60–120 s** for a full EditMode run after compile is warm; first run after asmdef changes can take several minutes.

### Focused filter (diagnosis)

```powershell
& $Unity -batchmode -projectPath . `
  -runTests -testPlatform EditMode `
  -testFilter "SandboxNavPathfinderTests|SandboxSpawnRulesTests" `
  -testResults TestResults/editmode-nav.xml `
  -logFile Logs/unity-editmode-nav.log
```

Pipe-separated names; NUnit filter syntax.

## PlayMode test suite

Requires Play Mode test setup; slower and may need a display/GPU context:

```powershell
& $Unity -batchmode -projectPath . `
  -runTests -testPlatform PlayMode `
  -testResults TestResults/playmode.xml `
  -logFile Logs/unity-playmode-tests.log
```

See ticket docs (e.g. P1-COLL-001) for when PlayMode is required vs EditMode-only.

## Parse results

**PowerShell summary from XML:**

```powershell
[xml]$x = Get-Content TestResults/editmode.xml
$tr = $x.'test-run'
"total=$($tr.total) passed=$($tr.passed) failed=$($tr.failed) result=$($tr.result)"
$x.SelectNodes("//test-case[@result='Failed']") | ForEach-Object { $_.name }
```

**Pass criteria:** `failed=0`, `result=Passed` (or process exit code **0**).

**Log diagnosis** when XML is missing:

```powershell
Select-String -Path Logs/unity-editmode-tests.log -Pattern "error CS|Scripts have compiler errors|Test Runner|result="
Get-Content Logs/unity-editmode-tests.log -Tail 40
```

Common blockers:

| Symptom | Likely fix |
|---------|------------|
| `Newtonsoft` not found in EditMode tests | `EditModeTests.asmdef`: `overrideReferences` + `precompiledReferences: ["nunit.framework.dll","Newtonsoft.Json.dll"]` |
| `Sprite.Create` arity errors | Unity 6 overload — drop legacy `SpriteMeshType`/name args or add `SecondarySpriteTexture[]` parameter |
| Tests never run, exit 0, no XML | Remove `-quit` from `-runTests` invocation (Windows) |
| Submodule `.meta` YAML warnings | Fix or ignore `_Licensed` meta; usually non-blocking |

## Offline parity (no Unity)

When touching autotile or terrain logic, also run JS parity tests:

```bash
cd tools/tile-viz && npm test
cd tools/world-viz && npm test
```

See [`tools/tile-viz/README.md`](../../../tools/tile-viz/README.md) and history-regression-guard rule.

## Agent-safe patterns

- Run from **repo root**; use absolute paths if the shell cwd is unreliable.
- Set `block_until_ms` ≥ **600000** (10 min) for full EditMode suite in agent terminals.
- Do not commit `TestResults/`, `Logs/`, or `.env`.
- After changing `.claude/skills/**`, run `python scripts/sync_assistant_trees.py`.
- Unity Editor can lock the project — close the Editor before batch `-runTests` if imports hang.

## Related docs

- Test vocabulary table: [`AGENTS.md`](../../../AGENTS.md)
- Quality gates: [`docs/wiki/quality-gates.md`](../../../docs/wiki/quality-gates.md)
- CI: [`.github/workflows/unit-tests.yml`](../../../.github/workflows/unit-tests.yml) (GameCI; skipped without `UNITY_LICENSE` / `UNITY_SERIAL`)
