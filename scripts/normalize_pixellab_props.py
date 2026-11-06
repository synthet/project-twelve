#!/usr/bin/env python3
"""Normalize approved PixelLab world-prop drafts into Unity production sprites.

PixelLab's minimum canvas is 32 px, so props are generated at 32x32 (small) and
64x64 (large) and imported at **PPU 32**. That puts a small prop on one 16 px
world tile and a large prop on two, matching the footprints of the existing prop
set, while keeping the generated art at full detail.

The tradeoff is deliberate: props carry 2x the pixel density of the 16 px
terrain layer. docs/wiki/15-assets-integration.md permits this only when "the
import preset and PPU policy make their in-game size explicit", which is why the
PPU is pinned here and documented in the generated README.

Validation is therefore a conformance check, not a rescale: exact dimensions,
no anti-aliasing (binary alpha), a limited palette, and a non-empty subject.
Drafts that fail are reported for re-roll, never resampled.

Usage:
    python scripts/normalize_pixellab_props.py --frames DIR --manifest JSON
"""

from __future__ import annotations

import argparse
import hashlib
import json
import sys
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
from normalize_pixellab_hud_assets import (  # noqa: E402
    NEAREST,
    keep_largest_alpha_component,
)

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "Assets" / "Sprites" / "Props" / "Generated"
PIXELS_PER_UNIT = 32
PACKING_TAG = "props"
MAX_COLORS = 48

# Mirrors Assets/Sprites/Core/core_tile_grass_00.png.meta except for the PPU:
# Sprite (Single), Point filter, no mipmaps, uncompressed, center pivot.
META_TEMPLATE = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
  textureType: 8
  spriteMode: 1
  spritePixelsToUnits: {ppu}
  spriteMeshType: 1
  spritePivot: {{x: 0.5, y: 0.5}}
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  alphaIsTransparency: 1
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  textureCompression: 0
  spritePackingTag: {tag}
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: {sprite_id}
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
"""

FOLDER_META = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def stable_guid(seed: str) -> str:
    """Deterministic 32-hex Unity GUID so re-runs keep asset identity stable."""
    return hashlib.md5(f"project-twelve/props/{seed}".encode("utf-8")).hexdigest()


def sprite_id(guid: str) -> str:
    """Unity's internal sprite ID: 16 hex of entropy then a fixed suffix."""
    return guid[:16] + "0800000000000000"


def write_meta(png_path: Path, tag: str = PACKING_TAG) -> None:
    meta = png_path.with_suffix(png_path.suffix + ".meta")
    guid = stable_guid(png_path.name)
    meta.write_text(
        META_TEMPLATE.format(
            guid=guid, ppu=PIXELS_PER_UNIT, tag=tag, sprite_id=sprite_id(guid)
        ),
        encoding="utf-8",
        newline="\n",
    )


def write_folder_meta(folder: Path) -> None:
    meta = folder.with_suffix(".meta")
    if meta.exists():
        return
    rel = folder.relative_to(ROOT).as_posix()
    meta.write_text(FOLDER_META.format(guid=stable_guid(rel)), encoding="utf-8", newline="\n")


def inspect(image: Image.Image) -> tuple[int, int]:
    """Return (partial-alpha pixel count, distinct opaque colour count)."""
    data = image.convert("RGBA").tobytes()  # 4 bytes per pixel, RGBA order
    partial = sum(1 for i in range(3, len(data), 4) if 0 < data[i] < 255)
    colors = {data[i : i + 3] for i in range(0, len(data), 4) if data[i + 3]}
    return partial, len(colors)


def contact_sheet(entries: list[tuple[str, Image.Image]], scale: int, columns: int = 8) -> Image.Image:
    """Grid of every prop at `scale`x on a mid-gray backdrop, for visual review."""
    cell = max(max(img.size) for _, img in entries) * scale + 10
    rows = (len(entries) + columns - 1) // columns
    sheet = Image.new("RGBA", (cell * columns, cell * rows), (70, 70, 78, 255))
    for index, (_, img) in enumerate(entries):
        big = img.resize((img.width * scale, img.height * scale), NEAREST)
        x = (index % columns) * cell + (cell - big.width) // 2
        y = (index // columns) * cell + (cell - big.height) // 2
        sheet.alpha_composite(big, (x, y))
    return sheet


def normalize(
    frames: Path,
    manifest: Path,
    *,
    keep_largest: bool,
    dry_run: bool,
) -> int:
    spec = json.loads(manifest.read_text(encoding="utf-8"))
    items = spec["items"]

    if not dry_run:
        OUTPUT.mkdir(parents=True, exist_ok=True)
        write_folder_meta(OUTPUT)
        write_folder_meta(OUTPUT.parent)

    accepted: list[tuple[str, Image.Image]] = []
    rejected: list[tuple[str, str]] = []

    for item in items:
        name = item["name"]
        expected = tuple(item["size"])
        source = frames / item["frame"]
        if not source.exists():
            rejected.append((name, f"missing frame {item['frame']}"))
            continue

        draft = Image.open(source).convert("RGBA")
        if draft.size != expected:
            rejected.append((name, f"{draft.size} != expected {expected}"))
            continue

        partial, colors = inspect(draft)
        if partial:
            rejected.append((name, f"{partial} anti-aliased pixels (alpha must be 0 or 255)"))
            continue
        if colors > MAX_COLORS:
            rejected.append((name, f"{colors} colours exceeds the {MAX_COLORS} limited-palette ceiling"))
            continue

        result = keep_largest_alpha_component(draft) if keep_largest else draft
        if result.getchannel("A").getbbox() is None:
            rejected.append((name, "sprite is fully transparent"))
            continue

        accepted.append((name, result))
        print(f"  OK   {name:36s} {draft.width:>2}x{draft.height:<2}  colours {colors:>2}")
        if not dry_run:
            target = OUTPUT / f"{name}.png"
            result.save(target, format="PNG", optimize=False)
            write_meta(target)

    for name, reason in rejected:
        print(f"  FAIL {name:36s} {reason}")

    if accepted and not dry_run:
        for label, size, scale in (("small", 32, 4), ("large", 64, 2)):
            group = [(n, i) for n, i in accepted if i.width == size]
            if not group:
                continue
            path = OUTPUT / f"props_contact_sheet_{label}.png"
            contact_sheet(group, scale).save(path, format="PNG")
            # Review artifact, not gameplay art: keep it out of the props atlas.
            write_meta(path, tag="")
            print(f"contact sheet -> {path.relative_to(ROOT).as_posix()} ({len(group)} props)")

    print(f"\n{len(accepted)} accepted, {len(rejected)} rejected (re-roll, do not resample)")
    return 1 if rejected else 0


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--frames", type=Path, required=True, help="directory of raw PixelLab frame PNGs")
    parser.add_argument("--manifest", type=Path, required=True, help="JSON mapping frames to prop names and sizes")
    parser.add_argument(
        "--keep-largest",
        action="store_true",
        help="strip all but the largest connected blob; off by default because many props "
        "(pebble clusters, mushroom pairs) are legitimately multi-part",
    )
    parser.add_argument("--dry-run", action="store_true", help="report only, write nothing")
    args = parser.parse_args()

    raise SystemExit(
        normalize(
            args.frames,
            args.manifest,
            keep_largest=args.keep_largest,
            dry_run=args.dry_run,
        )
    )


if __name__ == "__main__":
    main()
