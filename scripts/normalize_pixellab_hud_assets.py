#!/usr/bin/env python3
"""Normalize approved PixelLab HUD drafts into exact Unity production sprites.

Modes:
  (default)          v2 individual-asset normalization from pixellab-individual/.
  --v3-sheet PATH    Crop the v3 hero sheet per docs/specs/hud-assets.json
                     (pixellab_generation_v3_plan.hero_sheet.crops), verify the
                     integer pixel scale, downscale, and write review drafts to
                     docs/images/hud-mockups/pixellab-v3/.
"""

from __future__ import annotations

import argparse
import json
from collections import Counter
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "docs" / "images" / "hud-mockups" / "pixellab-individual"
OUTPUT = ROOT / "Assets" / "Sprites" / "UI" / "Generated"
SPEC = ROOT / "docs" / "specs" / "hud-assets.json"
V3_DRAFTS = ROOT / "docs" / "images" / "hud-mockups" / "pixellab-v3"
NEAREST = Image.Resampling.NEAREST
HEART_OUTLINE = (75, 17, 25, 255)
HEART_EMPTY = (117, 20, 26, 255)
HEART_EMPTY_LIGHT = (150, 35, 49, 255)


def alpha_crop(image: Image.Image) -> Image.Image:
    rgba = image.convert("RGBA")
    bounds = rgba.getchannel("A").getbbox()
    if bounds is None:
        raise ValueError("Source image has no visible pixels")
    return rgba.crop(bounds)


def square_pad(image: Image.Image) -> Image.Image:
    size = max(image.size)
    result = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    result.alpha_composite(image, ((size - image.width) // 2, (size - image.height) // 2))
    return result


def keep_largest_alpha_component(image: Image.Image) -> Image.Image:
    rgba = image.convert("RGBA")
    alpha = rgba.getchannel("A")
    visible = {(x, y) for y in range(rgba.height) for x in range(rgba.width) if alpha.getpixel((x, y)) > 0}
    components: list[set[tuple[int, int]]] = []
    while visible:
        pending = [visible.pop()]
        component: set[tuple[int, int]] = set()
        while pending:
            point = pending.pop()
            component.add(point)
            x, y = point
            for neighbor in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                if neighbor in visible:
                    visible.remove(neighbor)
                    pending.append(neighbor)
        components.append(component)

    if not components:
        raise ValueError("Source image has no visible component")
    keep = max(components, key=len)
    pixels = rgba.load()
    for y in range(rgba.height):
        for x in range(rgba.width):
            if (x, y) not in keep:
                pixels[x, y] = (0, 0, 0, 0)
    return rgba


def normalize_square(source: str, target: str, size: int, *, crop: bool = True) -> None:
    image = Image.open(SOURCE / source).convert("RGBA")
    if crop:
        image = alpha_crop(image)
    image = square_pad(image).resize((size, size), NEAREST)
    image.save(OUTPUT / target, format="PNG", optimize=False)


def heart_edge_pixels(image: Image.Image) -> set[tuple[int, int]]:
    """Return the visible one-pixel perimeter of a heart alpha mask."""
    alpha = image.getchannel("A")
    edge: set[tuple[int, int]] = set()
    for y in range(image.height):
        for x in range(image.width):
            if alpha.getpixel((x, y)) == 0:
                continue
            for nx, ny in (
                (x - 1, y - 1),
                (x, y - 1),
                (x + 1, y - 1),
                (x - 1, y),
                (x + 1, y),
                (x - 1, y + 1),
                (x, y + 1),
                (x + 1, y + 1),
            ):
                if nx < 0 or ny < 0 or nx >= image.width or ny >= image.height:
                    edge.add((x, y))
                    break
                if alpha.getpixel((nx, ny)) == 0:
                    edge.add((x, y))
                    break
    return edge


def normalize_heart_set(source: str, size: int) -> None:
    """Derive all health states from one PixelLab heart silhouette and palette."""
    master = alpha_crop(Image.open(SOURCE / source).convert("RGBA"))
    master = square_pad(master).resize((size, size), NEAREST)
    edge = heart_edge_pixels(master)
    midpoint = size // 2

    for state in ("full", "half", "empty"):
        result = master.copy()
        pixels = result.load()
        for y in range(size):
            for x in range(size):
                if pixels[x, y][3] == 0:
                    continue
                if (x, y) in edge:
                    pixels[x, y] = HEART_OUTLINE
                elif state == "empty" or (state == "half" and x >= midpoint):
                    # Retain a restrained highlight from the master so the empty
                    # cavity reads as the same material instead of a flat cutout.
                    red, green, blue, _ = master.getpixel((x, y))
                    pixels[x, y] = HEART_EMPTY_LIGHT if red + green + blue > 420 else HEART_EMPTY

        result.save(OUTPUT / f"hud_heart_{state}.png", format="PNG", optimize=False)


def normalize_exact(source: str, target: str, size: tuple[int, int]) -> None:
    image = Image.open(SOURCE / source).convert("RGBA")
    if image.size != size:
        raise ValueError(f"{source}: expected exact source {size}, received {image.size}")
    image.save(OUTPUT / target, format="PNG", optimize=False)


def normalize_centered_subject(source: str, target: str, size: tuple[int, int]) -> None:
    image = keep_largest_alpha_component(Image.open(SOURCE / source))
    image = alpha_crop(image)
    if image.width > size[0] or image.height > size[1]:
        image.thumbnail(size, NEAREST)
    result = Image.new("RGBA", size, (0, 0, 0, 0))
    result.alpha_composite(image, ((size[0] - image.width) // 2, (size[1] - image.height) // 2))
    result.save(OUTPUT / target, format="PNG", optimize=False)


def normalize_horizontal_frame(
    source: str,
    target: str,
    size: tuple[int, int],
    border: int,
) -> None:
    image = alpha_crop(Image.open(SOURCE / source))
    scaled_width = max(border * 2 + 1, round(image.width * size[1] / image.height))
    scaled = image.resize((scaled_width, size[1]), NEAREST)
    if scaled.width < border * 2 + 1:
        raise ValueError(f"{source}: frame is too narrow for {border}px borders")

    result = Image.new("RGBA", size, (0, 0, 0, 0))
    left = scaled.crop((0, 0, border, scaled.height))
    center = scaled.crop((border, 0, scaled.width - border, scaled.height))
    right = scaled.crop((scaled.width - border, 0, scaled.width, scaled.height))
    center_width = size[0] - border * 2
    result.alpha_composite(left, (0, 0))
    result.alpha_composite(center.resize((center_width, size[1]), NEAREST), (border, 0))
    result.alpha_composite(right, (size[0] - border, 0))
    result.save(OUTPUT / target, format="PNG", optimize=False)


def block_uniformity(image: Image.Image, factor: int) -> float:
    """Fraction of factor-by-factor blocks whose pixels are all identical."""
    if image.width % factor or image.height % factor:
        raise ValueError(f"Image {image.size} is not divisible by scale factor {factor}")
    pixels = image.load()
    uniform = 0
    total = (image.width // factor) * (image.height // factor)
    for by in range(0, image.height, factor):
        for bx in range(0, image.width, factor):
            first = pixels[bx, by]
            if all(pixels[bx + dx, by + dy] == first for dy in range(factor) for dx in range(factor)):
                uniform += 1
    return uniform / total


def downscale_majority(image: Image.Image, factor: int) -> Image.Image:
    """Downscale by an exact integer factor using the majority color per block."""
    result = Image.new("RGBA", (image.width // factor, image.height // factor))
    src = image.load()
    dst = result.load()
    for y in range(result.height):
        for x in range(result.width):
            counts = Counter(
                src[x * factor + dx, y * factor + dy]
                for dy in range(factor)
                for dx in range(factor)
            )
            dst[x, y] = counts.most_common(1)[0][0]
    return result


def snap_palette(image: Image.Image, palette: dict[str, str]) -> Image.Image:
    """Snap opaque pixels to the nearest spec palette color, preserving alpha."""
    colors = []
    for value in palette.values():
        raw = value.removeprefix("#")
        colors.append(tuple(int(raw[i:i + 2], 16) for i in (0, 2, 4)))
    result = image.copy()
    pixels = result.load()
    for y in range(result.height):
        for x in range(result.width):
            r, g, b, a = pixels[x, y]
            if a == 0:
                continue
            nearest = min(colors, key=lambda c: (c[0] - r) ** 2 + (c[1] - g) ** 2 + (c[2] - b) ** 2)
            pixels[x, y] = (*nearest, a)
    return result


def normalize_v3_sheet(sheet_path: Path, *, min_uniformity: float = 0.85, snap: bool = False) -> None:
    """Crop hero-sheet pieces per the v3 spec and downscale them to source size."""
    spec = json.loads(SPEC.read_text(encoding="utf-8"))
    if spec["spec_id"] != "project-twelve/hud-assets/v3":
        raise ValueError("Expected the v3 HUD asset specification")
    crops = spec["pixellab_generation_v3_plan"]["hero_sheet"]["crops"]
    sheet = Image.open(sheet_path).convert("RGBA")
    expected = tuple(spec["pixellab_generation_v3_plan"]["hero_sheet"]["output_size_px"])
    if sheet.size != expected:
        raise ValueError(f"Hero sheet is {sheet.size}, spec expects {expected}")

    V3_DRAFTS.mkdir(parents=True, exist_ok=True)
    for piece_id, crop in crops.items():
        if "virtual_rect" not in crop:
            continue
        x, y, w, h = crop["virtual_rect"]
        factor = crop["downscale_factor"]
        target = tuple(crop["target_source_px"])
        region = sheet.crop((x, y, x + w, y + h))
        bounds = region.getchannel("A").getbbox()
        uniformity = block_uniformity(region, factor)
        print(f"{piece_id}: crop=({x},{y},{w},{h}) alpha_bbox={bounds} uniformity={uniformity:.2%}")
        if bounds is None:
            raise ValueError(f"{piece_id}: cropped region is fully transparent — piece placement mismatch")
        if uniformity < min_uniformity:
            raise ValueError(
                f"{piece_id}: only {uniformity:.2%} of {factor}x{factor} blocks are uniform "
                f"(needs >= {min_uniformity:.0%}); the sheet is not on the expected pixel grid"
            )
        draft = downscale_majority(region, factor)
        if draft.size != target:
            raise AssertionError(f"{piece_id}: downscaled to {draft.size}, expected {target}")
        if crop.get("symmetrize") == "top_left_quadrant":
            half_w, half_h = draft.width // 2, draft.height // 2
            quadrant = draft.crop((0, 0, half_w, half_h))
            draft.paste(quadrant.transpose(Image.Transpose.FLIP_LEFT_RIGHT), (half_w, 0))
            draft.paste(quadrant.transpose(Image.Transpose.FLIP_TOP_BOTTOM), (0, half_h))
            draft.paste(quadrant.transpose(Image.Transpose.ROTATE_180), (half_w, half_h))
        fill = crop.get("interior_fill")
        if fill:
            ring = fill["ring_px"]
            raw = spec["palette"][fill["palette_color"]].removeprefix("#")
            color = tuple(int(raw[i:i + 2], 16) for i in (0, 2, 4, 6))
            pixels = draft.load()
            for py in range(ring, draft.height - ring):
                for px in range(ring, draft.width - ring):
                    pixels[px, py] = color
        punch = crop.get("punch_center")
        if punch:
            ring = punch["ring_px"]
            pixels = draft.load()
            for py in range(ring, draft.height - ring):
                for px in range(ring, draft.width - ring):
                    pixels[px, py] = (0, 0, 0, 0)
        if snap:
            draft = snap_palette(draft, spec["palette"])
        draft.save(V3_DRAFTS / f"{piece_id}.png", format="PNG", optimize=False)
        preview = draft.resize((draft.width * 4, draft.height * 4), NEAREST)
        preview.save(V3_DRAFTS / f"{piece_id}-preview.png", format="PNG", optimize=False)
    print(f"v3 drafts written to {V3_DRAFTS}")


def normalize_v3_objects(source_dir: Path) -> None:
    """Normalize promoted v3 object candidates into production sprites.

    Expects heart_full.png (32), player_portrait.png (80), and tile_*.png (32)
    inside source_dir, downloaded from the promoted PixelLab objects.
    """
    global SOURCE
    SOURCE = source_dir
    OUTPUT.mkdir(parents=True, exist_ok=True)

    if (source_dir / "heart_full.png").exists():
        normalize_heart_set("heart_full.png", 16)
    else:
        print("heart_full.png not present; keeping current production hearts")
    if (source_dir / "player_portrait.png").exists():
        normalize_centered_subject("player_portrait.png", "hud_player_portrait.png", (40, 40))
    else:
        print("player_portrait.png not present; keeping current production portrait")
    for tile in ("dirt", "grass", "stone", "copper_ore"):
        candidate = source_dir / f"tile_{tile}.png"
        if not candidate.exists():
            print(f"tile_{tile}.png not present; keeping current production icon")
            continue
        icon = Image.open(candidate).convert("RGBA")
        if icon.width % 32 or icon.size != (icon.width, icon.width):
            raise ValueError(f"tile_{tile}.png must be a square multiple of 32, got {icon.size}")
        if icon.size != (32, 32):
            icon = icon.resize((32, 32), NEAREST)
        icon.save(OUTPUT / f"hud_tile_{tile}.png", format="PNG", optimize=False)
    print("Normalized v3 object sprites into Assets/Sprites/UI/Generated")


def normalize_v2() -> None:
    OUTPUT.mkdir(parents=True, exist_ok=True)

    normalize_horizontal_frame("health_panel_frame.png", "hud_panel_main.png", (210, 70), 14)
    normalize_horizontal_frame("hotbar_backing.png", "hud_hotbar_backing.png", (612, 60), 6)
    normalize_horizontal_frame("debug_panel.png", "hud_panel_info.png", (160, 62), 2)

    normalize_square("slot_normal.png", "hud_slot_normal.png", 52)
    normalize_square("slot_selected.png", "hud_slot_selected.png", 54)
    normalize_heart_set("heart_full.png", 12)
    normalize_square("hud_cursor.png", "hud_cursor.png", 16)

    normalize_centered_subject("player_portrait.png", "hud_player_portrait.png", (38, 38))
    normalize_exact("tile_dirt.png", "hud_tile_dirt.png", (32, 32))
    normalize_exact("tile_grass.png", "hud_tile_grass.png", (32, 32))
    normalize_exact("tile_stone.png", "hud_tile_stone.png", (32, 32))
    normalize_exact("tile_copper_ore.png", "hud_tile_copper_ore.png", (32, 32))

    print("Normalized 14 approved PixelLab HUD sprites into Assets/Sprites/UI/Generated")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--v3-sheet", type=Path, help="Path to the downloaded v3 hero-sheet PNG")
    parser.add_argument("--v3-objects", type=Path, help="Directory of promoted v3 object PNGs to normalize into production")
    parser.add_argument("--snap-palette", action="store_true", help="Snap v3 draft colors to the spec palette")
    parser.add_argument("--min-uniformity", type=float, default=0.85, help="Required fraction of uniform pixel blocks")
    args = parser.parse_args()

    if args.v3_sheet:
        normalize_v3_sheet(args.v3_sheet, min_uniformity=args.min_uniformity, snap=args.snap_palette)
    elif args.v3_objects:
        normalize_v3_objects(args.v3_objects)
    else:
        normalize_v2()


if __name__ == "__main__":
    main()
