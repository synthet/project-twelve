---
type: Reference
title: Licensed Assets Reference
description: Where public behavior contracts and private submodule inventory live; regen checklist for maintainers with assets access.
resource: wiki/licensed-assets-reference.md
tags: [docs, wiki, assets, licensed, visual, reference]
timestamp: 2026-07-08T00:00:00Z
---

# Licensed assets reference

ProjectTwelve separates **public behavior contracts** (main repo) from **private asset inventory** (git submodule). This page tells developers and agents where to look for each.

## Submodule mount

Licensed art and generated catalogs live in the private [project-twelve-assets](https://github.com/synthet/project-twelve-assets) repository, mounted at:

```text
Assets/_Licensed/
```

Initialize with `git submodule update --init --recursive`. See [Paid and licensed assets](../PAID_ASSETS.md) and [Visual setup](../VISUAL_SETUP.md).

## Public contracts (main repo)

Use these for implementation and parity — they do not require submodule access:

| Topic | Document |
| --- | --- |
| Visual behavior (autotile, character sheet, animation API) | [VISUAL_BEHAVIOR_SPEC.md](../VISUAL_BEHAVIOR_SPEC.md) |
| 32 ground autotile masks and acceptance | [ground-autotile-32-rules.md](ground-autotile-32-rules.md) |
| Mask build and normalization | [autotile-algorithm.md](autotile-algorithm.md) |
| Sandbox integration status | [visual-integration.md](visual-integration.md) |
| Offline JS resolver parity | `tools/tile-viz/README.md` |
| Asset integration requirements (generic) | [15-assets-integration.md](15-assets-integration.md) |

## Private inventory (submodule only)

**Requires project-twelve-assets access.** Full script API, sprite/tile/monster catalogs, and equipment name lists:

| Document | Contents |
| --- | --- |
| [`Assets/_Licensed/docs/README.md`](../../Assets/_Licensed/docs/README.md) | Documentation hub and asset counts |
| [`Assets/_Licensed/docs/vendor/README.md`](../../Assets/_Licensed/docs/vendor/README.md) | Vendor script API and asset catalogs |
| [`Assets/_Licensed/docs/integration/README.md`](../../Assets/_Licensed/docs/integration/README.md) | Catalog regen, runtime wiring, parity tests |
| [`Assets/_Licensed/docs/LICENSE-INTAKE.md`](../../Assets/_Licensed/docs/LICENSE-INTAKE.md) | License intake and team workflow |

Do **not** copy vendor PNG lists, script bodies, or full equipment tables into the public repo.

## Regeneration checklist

After changing licensed source art (in **project-twelve-assets**):

1. Regenerate catalogs (Unity menus or `python3 scripts/generate_visual_catalogs.py`).
2. Commit updated `Settings/Visual/*.asset` in the assets repo.
3. Bump the submodule pointer in project-twelve.
4. Run `python3 scripts/check_paid_assets.py --staged` before main-repo commit.
5. Run autotile parity: Unity EditMode autotile tests and `cd tools/tile-viz && npm test`.

Import path detail: [`Assets/_Licensed/docs/integration/catalogs-config.md`](../../Assets/_Licensed/docs/integration/catalogs-config.md).

## What ProjectTwelve uses at runtime

| Licensed asset | ProjectTwelve consumer |
| --- | --- |
| Ground/cover tile PNGs | `AutotileCatalog` → `AutotileResolver` → `SandboxChunkRenderer` |
| `Character.prefab` | `PlayerAvatarFactory` (vendor demo scripts stripped) |
| Hero layer PNGs | `CharacterLayerCatalog` → `CharacterComposer` |
| Monster prefabs | `MonsterVisualCatalog` → `MonsterSpawnHelper` |
| Props (102 PNGs) | Not wired yet (P2-VISUAL-005) |
| Vendor demo scenes | Not used |

## See also

- [PAID_ASSETS.md](../PAID_ASSETS.md) — policy and guard scripts
- [VISUAL_SETUP.md](../VISUAL_SETUP.md) — machine setup
- [CANONICAL_SOURCES.md](../CANONICAL_SOURCES.md) — authority map
