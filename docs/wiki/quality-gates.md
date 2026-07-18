---
type: Guide
title: Quality Gates
description: Required automated and manual verification steps before merge — tests, link hygiene, deterministic checks, and profiler targets.
resource: wiki/quality-gates.md
tags: [docs, wiki, quality, ci, testing]
timestamp: 2026-07-18T01:00:00Z
okf_version: 0.1
---

# Quality Gates

> **Status:** Active baseline.
> **Decisions:** Automated tests validate data contracts; deterministic checks prove reproducibility; manual Unity checks catch performance and integration bugs.
> **Invariants:** Every merge must pass: local edit-mode tests (when Unity is available), link hygiene, asset validation, and Unity build validation. CI does **not** run Unity EditMode (no Actions license).

## Overview

Quality gates are the automated and manual checks that determine when a task is complete and ready to merge. This page consolidates all required verification steps for ProjectTwelve, organized by verification method.

## Required checks per commit

### 1. Automated tests (Unity Test Runner / NUnit)

Every commit should run the full EditMode test suite. Tests must validate:

| Area | Assertion | Command | Exit code |
|------|-----------|---------|-----------|
| **Coordinate conversion** | Local coords in `[0, Size)²`; round-trips for negative coords | `Unity -batchmode -quit -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile Logs/unity-editmode-tests.log` | 0 = all pass |
| **Chunk lookup/edit** | `SetTile` → `GetTile` matches; dirty flags set correctly | (included in EditMode suite) | — |
| **Lighting** | Propagation falloff is correct; dirty-region relight matches full relight | (included in EditMode suite) | — |
| **Fluids** | Mass conservation within epsilon; settles without infinite jitter | (included in EditMode suite) | — |
| **Generation** | Same seed ⇒ identical tiles; on-demand chunk ≡ full-world chunk | (included in EditMode suite) | — |
| **Save/load** | Round-trip equality; old-version saves migrate correctly | (included in EditMode suite) | — |
| **Network serialization** | Tile-delta encode/decode round-trips; sequence ordering preserved | (included in EditMode suite) | — |
| **Visual invariants** | Character sheet clip rows/keys match spec; autotile pick is deterministic, weighted, flip-aware | (included in EditMode suite) | — |

**When to run:** Locally before every push that touches C# / visual behavior. Not run in GitHub Actions (no Unity license).

**Local command (batch mode):**
```bash
Unity -batchmode -quit -projectPath . -runTests -testPlatform EditMode \
  -testResults TestResults/editmode.xml -logFile Logs/unity-editmode-tests.log
```

On failure: Fix the test or the code; commit the fix; re-run before push.

#### Unity EditMode tests in CI (GameCI)

**Policy:** this repository does **not** provision a Unity license for GitHub Actions.
EditMode is a **local** gate (developer machine / Unity Editor). The GameCI workflow
(`.github/workflows/unit-tests.yml`, P2-QA-001 / [#117](https://github.com/synthet/project-twelve/issues/117))
stays wired for optional activation but **skips with a notice** when
`UNITY_LICENSE` / `UNITY_SERIAL` are absent — it must not fail the PR check for a missing secret.

**CI gates that do run without Unity:** `agent-checks` (docs links, OKF, paid-assets,
assistant-tree sync) and offline `tools/tile-viz` / `tools/world-viz` npm tests where configured.

**If secrets are ever added** (repository → Settings → Secrets and variables → Actions):

| Secret | When | Contents |
|--------|------|----------|
| `UNITY_LICENSE` | Personal license | Full text of the `.ulf` activation file (obtain via the [GameCI activation guide](https://game.ci/docs/github/activation)) |
| `UNITY_EMAIL` / `UNITY_PASSWORD` | Both | Unity account credentials used for activation |
| `UNITY_SERIAL` | Pro/Plus license | Serial key (instead of `UNITY_LICENSE`) |

No license file is ever committed; the runner would activate from secrets at job start.

**Enforcement semantics:**

- License secret absent (current policy) → job posts a notice and **succeeds** (skip, not fail).
- License secret present → GameCI runs EditMode; **any test failure fails the check**.
  Terrain parity: CI deletes `tools/world-viz/test/fixtures/surface.seed1337.json` so
  `TerrainFixtureExportTests` must re-author it; then `node --test` in `tools/world-viz`
  verifies against that fixture.

**Note on `Assets/_Licensed`:** public CI never mounts the private submodule. Runtime code and
EditMode tests must not hard-depend on licensed assets (see `docs/PAID_ASSETS.md`).

**Local reproduction (required before merge when C# / visual behavior changes):**

```bash
# Windows: omit -quit when combined with -runTests (see .claude/skills/unity-tests/SKILL.md)
Unity -batchmode -projectPath . -runTests -testPlatform EditMode \
  -testResults TestResults/editmode.xml -logFile Logs/unity-editmode-tests.log
cd tools/world-viz && npm test
```

### 2. Link and asset hygiene

Before every commit, verify:

#### 2a. Markdown link validation
```bash
python3 scripts/check_markdown_links.py
```

**Checks:**
- All markdown links resolve to existing files (e.g., relative paths like `docs/wiki/file.md`).
- No relative paths pointing outside the repo.
- Cross-refs in frontmatter (e.g., `github_issue`) are reachable.

**On failure:** Fix broken links or move the referenced file.

#### 2b. OKF frontmatter validation (wiki and docs)

All files in `docs/` must include valid OKF frontmatter. The CI command runs with `--fail-on error`, so a **missing `type` field is the only frontmatter omission that blocks merge**; missing `description`, `resource`, `tags`, or `timestamp` are reported as warnings. Add all six regardless — warnings keep docs discoverable and may be promoted to errors later.

```bash
python scripts/ci/okf_lint_changed.py \
  --base origin/main \
  --head HEAD \
  --profile project \
  --fail-on error
```

**Checks:**
- Every markdown file has a `type` field (merge-blocking) plus `title`, `description`, `resource`, `tags`, `timestamp` (warnings).
- Field values are syntactically valid (proper YAML formatting).
- `resource` paths match the actual file location relative to the `docs/` bundle root (e.g., `wiki/00-overview.md`); the linter also accepts a `docs/`-prefixed form for compatibility.
- Timestamps are ISO 8601 UTC format.

**On failure:** Add or fix frontmatter per [`.claude/rules/okf-frontmatter.md`](../../.claude/rules/okf-frontmatter.md). Common fixes:
- Add missing fields (copy from an existing wiki file as a template).
- Fix `resource` path: must be relative to the `docs/` bundle root (e.g., `wiki/00-overview.md`).
- Fix `timestamp` format: use ISO 8601 UTC (`YYYY-MM-DDTHH:MM:SSZ`).
- Use proper YAML syntax (colons followed by space, no tabs).

**Run this locally before every push to docs/:**
```bash
# Check changed files only (faster)
python scripts/ci/okf_lint_changed.py --base origin/main --head HEAD --profile project --fail-on error
```

#### 2c. Paid asset validation
```bash
python3 scripts/check_paid_assets.py --staged
```

**Checks:**
- No Asset Store, Unity Marketplace, or licensed content in `Assets/PixelFantasy/` or as regular files under `Assets/`.
- Submodule pointer at `Assets/_Licensed/` is allowed (points to private repo).
- No vendor pack names in staged files.

**On failure:** Unstage licensed files; move them to `project-twelve-assets` (private submodule).

### 3. CI/CD assistant tree sync

Before every commit that touches `.claude/`, `.cursor/`, or `.agents/skills/` directories:

```bash
python scripts/sync_assistant_trees.py --check
```

**Checks:**
- `.claude/`, `.cursor/`, and `.agents/skills/` trees are in sync.
- No duplicated or orphaned files.

**On failure:** Run `python scripts/sync_assistant_trees.py` to auto-sync; commit the canonical and
generated trees together.

### 4. Unity project validation (batch mode)

Before opening a PR, validate the project builds and imports cleanly:

```bash
Unity -batchmode -quit -projectPath . -logFile Logs/unity-validate.log
```

**Checks:**
- No C# compilation errors.
- No missing asset references.
- No Unity version conflicts.
- Editor-only code does not leak into runtime.

**On failure:** Fix compilation errors, restore missing assets, or update meta files.

## Deterministic gates

These checks prove that world generation, save/load, and network serialization are reproducible.

### Deterministic world generation

**Assertion:** Same seed and chunk coordinate always produce identical tile data.

**Verification:**
```csharp
[Test]
public void GenerateSameSeedProducesSameTiles()
{
    var tiles1 = worldGenerator.GenerateChunk(seed: 12345, chunkPos: (0, 0));
    var tiles2 = worldGenerator.GenerateChunk(seed: 12345, chunkPos: (0, 0));
    Assert.AreEqual(tiles1, tiles2, "Same seed must produce identical tiles");
}
```

**When:** Every commit that touches `SandboxWorld.cs` or generation logic.

**Acceptance:** Test passes with 100% tile equality (no floating-point drift, no randomness leakage).

### Save/load round-trip

**Assertion:** Saving and loading a chunk returns data identical to the original in-memory state.

**Verification:**
```csharp
[Test]
public void SaveLoadRoundTripPreservesData()
{
    var originalChunk = new SandboxChunk { /* ... */ };
    var bytes = SaveChunk(originalChunk);
    var loadedChunk = LoadChunk(bytes);
    Assert.AreEqual(originalChunk, loadedChunk, "Round-trip must preserve all data");
}
```

**When:** Every commit that touches `SandboxWorld.cs`, `SandboxChunk.cs`, or save/load logic.

**Acceptance:** Test passes with 100% data equality.

### Deterministic seed verification

**Assertion:** Changing one seed bit changes only the intended world, not others.

**Verification:**
- Generate world with seed S.
- Generate world with seed S+1.
- Compare: chunks should differ, but chunk (0,0) should differ from chunk (1,0) consistently.

**When:** Every major generation algorithm update.

**Acceptance:** No unexplained correlations between seed bits and tile placement.

## Manual Unity checks

These checks catch performance, integration, and gameplay bugs that automated tests cannot.

### Play-mode traversal and dirty flags

**Scenario:** Player moves across chunk boundaries; render and collision dirty flags fire correctly.

**Steps:**
1. Open `Scenes/Prototype.unity`.
2. Press Play.
3. Move the player across 2-3 chunk boundaries (watch the chunk border overlay).
4. Observe: dirty-flag count increases only for affected chunks.
5. Observe: no lag spikes; rendering smooth.

**Acceptance:** Dirty flags fire for visible chunks only; no missed or spurious rebuilds.

### Collision edge cases

**Scenario:** Player movement respects terrain and avoids tunneling.

**Steps:**
1. Place solid tiles in irregular patterns (stairs, overhangs, gaps).
2. Jump and fall; attempt to clip through tiles.
3. Observe: player slides smoothly; never tunnels.
4. Observe: collision is chunk-local (profiler shows no global CompositeCollider2D rebuilds).

**Acceptance:** No clipping; collision cost is chunk-local.

### Profiler targets (known cliffs)

Before every significant feature merge, profile on representative content:

| Target | Metric | Acceptable | Command |
|--------|--------|-----------|---------|
| **Chunk rebuild (render)** | Time to rebuild one dirty chunk's mesh | < 1 ms | Window → Analysis → Profiler; select chunk and edit tile; measure Tilemap.SetTile + mesh rebuild |
| **Chunk rebuild (collision)** | Time to rebuild one dirty chunk's collider | < 2 ms | (same; watch Physics2D) |
| **Lighting propagation** | Time to relight one dirty chunk | < 5 ms | (same; watch any custom lighting update) |
| **Fluid iteration** | Time to step active fluid cells once | < 3 ms | (same; watch active-set iteration) |
| **Memory allocations** | Per-frame GC allocations in Play Mode | 0 bytes (steady-state) | Window → Analysis → Profiler; Memory tab; play for 60 seconds and check GC.Alloc spike pattern |
| **Draw calls per visible region** | Draw calls for 4 visible chunks | < 20 DC | (Profiler; Rendering tab) |

**When:** Before merging any performance-critical feature (rendering, collision, lighting, fluids).

**Acceptance:** All targets green or explicitly recorded as a follow-up optimization task.

### Manual QA checklist

Before closing P1 (prototype) or any vertical-slice demo:

- [ ] Player spawns and controls (move left/right, jump).
- [ ] Chunk generation is deterministic (same seed reloads same world).
- [ ] Tile editing (place/break) updates render, collision, and neighboring borders.
- [ ] No memory leaks during 5-minute play session.
- [ ] Frame rate stable at target (60 FPS desktop; 30 FPS if mobile-targeted).
- [ ] All debug overlays toggle without crashing.

## Exit evidence template

When closing a task, attach or link:

| Item | Example |
|------|---------|
| **Commit hash** | `abc1234def567...` |
| **Test output** | `TestResults/editmode.xml` (copy relevant lines or link to CI log) |
| **Profiler capture** | Screenshot of Profiler window showing target metrics |
| **Screenshots** | Before/after or gameplay demonstration |
| **Manual QA notes** | "Tested chunk boundaries; no lag spikes; collision is chunk-local." |
| **Follow-up tasks** | "Task P1-PERF-X: Optimize lighting BFS for large lit areas." |

## Non-goals

Quality gates **do not** include:

- **Full-world validation** (e.g., "check every generated chunk"): use deterministic fixtures instead.
- **Device certification** (e.g., on-device profiling for mobile): platform tasks handle that in P5.
- **Content reviews** (e.g., "are biome themes interesting?"): balance/design tasks own that.
- **Accessibility compliance** (e.g., color blindness, audio cues): UX and accessibility tasks own that.

## See also

- [Tooling, Testing & Profiling](13-tooling-testing.md) — detailed testing philosophy and debug visualizations.
- [Backlog Workflow](../project/00-backlog-workflow.md) — how tasks link to issues and exit evidence.
- [CLAUDE.md](../../CLAUDE.md) § Commands — all quality-gate commands in one place.
- [AGENTS.md](../../AGENTS.md) § Test vocabulary — test scope terminology and what qualifies as EditMode vs Play-mode.
