---
type: Technical Specification
title: Visual Catalog Import Pipeline
description: Reproducible inputs, configuration precedence, outputs, failure modes, and verification for licensed visual catalogs.
resource: wiki/visual-catalog-import-pipeline.md
tags: [docs, wiki, visual, catalogs, assets, import]
timestamp: 2026-07-16T00:00:00Z
okf_version: 0.1
---

# Visual catalog import pipeline

> **Status:** Canonical pipeline contract for P2-VISUAL-001.
> **Decisions:** Licensed sources and generated catalogs stay in `Assets/_Licensed`; both Unity and offline generation consume one resolved import config and emit deterministically ordered catalog entries.
> **Invariants:** World and registry state never depend on licensed asset paths. A code-only checkout remains runnable. Licensed blobs and generated licensed catalogs never move into the public repository.

## Ownership and configuration

The private `project-twelve-assets` submodule owns both the source art and generated catalogs. The
public repository owns importer code, runtime catalog types, scene-facing mappings, and this
contract.

Import configuration is a **whole-file selection**, not a merge:

1. Use gitignored `config/visual-import.local-only.txt` when it exists.
2. Otherwise use `Assets/_Licensed/config/visual-import.txt`.
3. If neither file exists, Unity importers warn and make no change; the offline generator exits
   nonzero without writing catalogs.

The local override must therefore contain every key needed by the operation being run. Copy
[`config/visual-import.local-only.example.txt`](../../config/visual-import.local-only.example.txt)
as a starting point. Relative paths are resolved from the repository root; `/` and `\` path
separators are accepted and normalized.

| Key | Consumer | Required catalog input |
|-----|----------|------------------------|
| `tile_sprites_root` | Autotile importer | `Ground/*.png` and `Cover/*.png` Unity sprite sheets |
| `hero_sprites_root` | Character importer | One layer directory per equipment/body category, containing `*.png` textures |
| `hero_extra_layer`, `hero_extra_layer.N` | Character importer | Optional additional layer directories, evaluated in config order |
| `monster_prefabs_root` | Monster importer | Recursive `*.prefab` tree; `Common/Prefabs` is excluded |
| `avatar_prefab` | Runtime avatar factory | Licensed character prefab; not serialized into a generated catalog |
| `strip_demo_script_type`, `strip_demo_script_type.N` | Runtime avatar factory | Optional vendor component names to remove from instantiated avatars |

## Catalog contracts

All outputs live under `Assets/_Licensed/Settings/Visual/` and retain their existing `.meta` GUIDs
when regenerated.

| Catalog | Input selection | Deterministic content | Output |
|---------|-----------------|-----------------------|--------|
| Autotile | Top-level PNGs under `Ground` and `Cover` | Files use ordinal path order; sprite references use their numeric slice order. Ground sheets must contain exactly 32 sprites. | `AutotileCatalog.asset` with ground and cover `AutotileTileset` entries |
| Character layers | Direct child directories of `hero_sprites_root`, then configured extra roots | Layer directories and textures use ordinal path order. The first layer name wins when an extra root duplicates it. | `CharacterLayerCatalog.asset` with named texture lists |
| Monsters | Recursive prefabs below `monster_prefabs_root` except `Common/Prefabs` | Prefabs use ordinal path order, then entries sort by monster ID. The first ordinal path wins for a duplicate filename/ID. | `MonsterVisualCatalog.asset` with monster ID-to-prefab entries |

The Unity and Python paths are equivalent when they use the same resolved config and imported Unity
metadata: they select the same sources, apply the ordering and duplicate rules above, preserve the
catalog GUIDs, and serialize references to the same Unity assets. Unity additionally validates
ground sheet layout and reports cover sprite PPU/bounds/mesh warnings because those checks require
loaded `Sprite` objects.

## Regeneration entry points

Use one of these paths from the repository root:

- Unity menus:
  - **ProjectTwelve → Visual → Import Autotile Catalog from Local Source**
  - **ProjectTwelve → Visual → Import Character Layer Catalog from Local Source**
  - **ProjectTwelve → Visual → Import Monster Visual Catalog from Local Source**
- Unity batch mode:

  ```powershell
  Unity -batchmode -quit -projectPath . `
    -executeMethod ProjectTwelve.Editor.Visual.VisualCatalogBatchImporter.ImportAllFromCommandLine `
    -logFile Logs/unity-visual-import.log
  ```

- Offline, without opening Unity:

  ```bash
  python3 scripts/generate_visual_catalogs.py
  ```

The offline command validates that all three required config keys and source directories exist
before writing anything. It is the fastest reproducibility check, but a Unity import/validation run
remains the authority for sprite import settings and loaded asset references.

## Failure behavior and code-only checkout

Missing licensed content is a supported degraded mode, not a reason to copy assets into the public
tree.

| Surface | Missing config/source behavior | Runtime result |
|---------|--------------------------------|----------------|
| Unity catalog menu | Logs a subsystem-specific warning and leaves that catalog unchanged. The other menu importers can still run. | No runtime state is mutated by import failure. |
| Unity batch importer | Each missing subsystem warns and returns; the batch entry point completes after attempting all three. Treat warnings as a failed catalog verification even if Unity exits zero. | Existing catalogs, if any, remain as they were. |
| Offline generator | Missing config, required key, source root, Unity `.meta`, or invalid GUID is a hard nonzero failure. | Validation happens before catalog writes for missing keys/source roots. |
| Terrain runtime | A missing or empty autotile catalog makes `SandboxChunkRenderer` use its legacy atlas/vertex-color mesh path. | World, collision, lighting, and tile edits continue. |
| Player runtime | A missing avatar prefab or character catalog logs warnings and skips the avatar child. | Player simulation and collision continue without the licensed avatar presentation. |
| Monster runtime | `SandboxEnemySpawner` does not spawn without a catalog; a direct helper call with a missing ID logs a warning and returns `null`. | No placeholder enemy visual is created. |

## Contributor verification and publishing

1. Initialize the pinned submodule commit with `python scripts/fetch_remotes.py --local-sync`.
2. Confirm the submodule has no unrelated catalog changes. Preserve unrelated licensed work.
3. Run one regeneration entry point. For the current PixelFantasy config, expect 9 ground
   tilesets, 16 cover tilesets, non-empty character layers, and non-empty monster entries.
4. Review changes under `Assets/_Licensed/Settings/Visual/` only. Unexpected source-art or GUID
   changes fail verification.
5. When Unity is available, run **ProjectTwelve → Visual → Validate Ground Autotile Sheets** and
   confirm the import log has no errors.
6. Run the public-repo guard before publishing:

   ```bash
   python3 scripts/check_paid_assets.py --staged
   ```

7. Commit and push catalog changes in `project-twelve-assets`, then bump the public repo gitlink with
   `scripts/publish_assets_submodule.py`. Never stage licensed files as ordinary public-repo files.

See [Paid assets](../PAID_ASSETS.md) for repository boundaries and
[Visual setup](../VISUAL_SETUP.md) for machine and scene wiring.
