# ProjectTwelve — PixelLab asset import

How to land PixelLab-generated art in this repo after MCP download.

## Output paths

| Asset type | Destination | Never use |
|------------|-------------|-----------|
| HUD sprites (panels, slots, hearts, icons) | `Assets/Sprites/UI/Generated/` | `Assets/_Licensed/` |
| Public world / UI experiments | `Assets/Sprites/` (appropriate subfolder) | `Assets/_Licensed/` |
| Licensed vendor art | `Assets/_Licensed/` (submodule only) | — |

Policy: [`docs/PAID_ASSETS.md`](../../../../docs/PAID_ASSETS.md). Run `python3 scripts/check_paid_assets.py --staged` before commit.

## HUD contract

Authoritative specs:

- [`docs/wiki/hud-assets-manifest.md`](../../../../docs/wiki/hud-assets-manifest.md)
- [`docs/specs/hud-assets.json`](../../../../docs/specs/hud-assets.json)

### Required Unity import settings (HUD)

| Setting | Value |
|---------|-------|
| Sprite mode | Single |
| Pixels per unit | 100 |
| Filter mode | Point (no filter) |
| Compression | None |
| Mip maps | Disabled |
| Wrap mode | Clamp |
| Mesh type | Full Rect |

Sliced sprites (panels, frames, slots): borders per manifest (e.g. health frame 14/14/14/14).

### Dimension gate

PixelLab output is **draft** until it matches manifest dimensions exactly. Resize or reject before replacing committed sprites. Example targets:

| Asset | Size |
|-------|------|
| Health frame | 210×70 |
| Portrait frame | 56×56 |
| Heart icons | 12×12 |
| Hotbar slot normal | 52×52 |
| Slot selected overlay | 54×54 |

Use `create_ui_asset` with explicit `width`/`height` when possible; post-process with nearest-neighbor resize if needed.

### Style matching

When iterating on existing HUD art, pass a screenshot or existing sprite via `background_image` on `create_map_object`, or describe the chunky 2 px module, ornate fantasy frame, and Point-filtered pixel look from current `Assets/Sprites/UI/Generated/` references.

## Post-download workflow

1. **Download** PNG from URL returned by `get_ui_asset` / `get_map_object` / etc.
2. **Save** to target path under `Assets/Sprites/UI/Generated/` (or agreed draft subpath).
3. **Preserve `.meta`** when replacing an existing sprite; copy border/sprite settings from the old `.meta` if dimensions unchanged.
4. **Verify import** in Unity: PPU, filter, slice borders, alpha.
5. **Wire references** in [`SandboxHudController.cs`](../../../../Assets/Scripts/Sandbox/UI/SandboxHudController.cs) or [`Assets/Prefabs/UI/SandboxHUD.prefab`](../../../../Assets/Prefabs/UI/SandboxHUD.prefab) if filenames changed.
6. **Get user approval** before treating draft as production replacement.

## Validation

When HUD assets or layout change:

```powershell
# Targeted EditMode tests (Unity 6000.5.1f1)
Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter SandboxHudTests -testResults TestResults/editmode-hud.xml -logFile Logs/unity-editmode-hud.log
```

See [`.claude/skills/unity-tests/SKILL.md`](../../unity-tests/SKILL.md) for Windows `-quit` quirks and result parsing.

Visual smoke: attach Game View screenshot at 1280×720; compare against [`docs/images/hud-mockups/`](../../../../docs/images/hud-mockups/).

## World / tile art

ProjectTwelve uses autotile resolution in C# ([`Assets/Scripts/Visual/Tiles/`](../../../../Assets/Scripts/Visual/Tiles/)). Sidescroller tilesets from PixelLab are experimental inputs — integrating them requires matching tile size (16×16 default), autotile rule parity, and EditMode + `tools/tile-viz` tests if resolver logic changes.

Do not hand-edit exported rule JSON under `tools/tile-viz/data/`.

## Approval gate

From hud-assets-manifest:

> Production sprites remain under `Assets/Sprites/UI/Generated/` and should only be replaced after visual approval and Unity import verification.

Agents must not silently overwrite committed HUD sprites. Present draft output for review first.
