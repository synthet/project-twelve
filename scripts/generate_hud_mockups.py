#!/usr/bin/env python3
"""Generate deterministic ProjectTwelve HUD mockups from docs/specs/hud-assets.json."""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any

from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_SPEC = ROOT / "docs" / "specs" / "hud-assets.json"
DEFAULT_OUTPUT = ROOT / "docs" / "images" / "hud-mockups"

SUPPORTED_SPEC_ID = "project-twelve/hud-assets/v3"


def rgba(value: str) -> tuple[int, int, int, int]:
    value = value.removeprefix("#")
    if len(value) != 8:
        raise ValueError(f"Expected #RRGGBBAA, received {value!r}")
    return tuple(int(value[index:index + 2], 16) for index in range(0, 8, 2))  # type: ignore[return-value]


def rectangle(draw: ImageDraw.ImageDraw, box: tuple[int, int, int, int], fill: str, palette: dict[str, str]) -> None:
    draw.rectangle(box, fill=rgba(palette[fill]))


def frame(image: Image.Image, border: int, palette: dict[str, str], selected: bool = False) -> None:
    draw = ImageDraw.Draw(image)
    width, height = image.size
    rectangle(draw, (border, border, width - border - 1, height - border - 1), "panel", palette)
    rectangle(draw, (2, 2, width - 3, height - 3), "silver_dark", palette)
    rectangle(draw, (4, 4, width - 5, height - 5), "shadow", palette)
    rectangle(draw, (6, 6, width - 7, height - 7), "silver", palette)
    rectangle(draw, (8, 8, width - 9, height - 9), "panel", palette)
    corner = max(8, border)
    for x in (2, width - corner - 2):
        for y in (2, height - corner - 2):
            rectangle(draw, (x, y, x + corner - 1, y + corner - 1), "silver_dark", palette)
            rectangle(draw, (x + 2, y + 2, x + corner - 3, y + corner - 3), "silver_light", palette)
            inset = 4 if corner >= 10 else 3
            rectangle(draw, (x + inset, y + inset, x + corner - inset - 1, y + corner - inset - 1), "gold", palette)
    if selected:
        rectangle(draw, (6, 6, width - 7, 7), "gold_light", palette)
        rectangle(draw, (6, height - 8, width - 7, height - 7), "gold_light", palette)
        rectangle(draw, (6, 6, 7, height - 7), "gold_light", palette)
        rectangle(draw, (width - 8, 6, width - 7, height - 7), "gold_light", palette)


HEART_PIXELS = [
    "..XXX....XXX..",
    ".XXXXX..XXXXX.",
    "XXXXXXXXXXXXXX",
    "XXXXXXXXXXXXXX",
    "XXXXXXXXXXXXXX",
    ".XXXXXXXXXXXX.",
    "..XXXXXXXXXX..",
    "...XXXXXXXX...",
    "....XXXXXX....",
    ".....XXXX.....",
    "......XX......",
]


def heart(image: Image.Image, palette: dict[str, str], amount: float) -> None:
    draw = ImageDraw.Draw(image)
    offset_x, offset_y = 1, 2
    rows = HEART_PIXELS
    for row_index, row in enumerate(rows):
        for col_index, value in enumerate(row):
            if value != "X":
                continue
            neighbors = (
                rows[row_index - 1][col_index] if row_index > 0 else ".",
                rows[row_index + 1][col_index] if row_index + 1 < len(rows) else ".",
                row[col_index - 1] if col_index > 0 else ".",
                row[col_index + 1] if col_index + 1 < len(row) else ".",
            )
            outline = any(neighbor != "X" for neighbor in neighbors)
            fill_split = offset_x + len(row) * amount
            color = "shadow" if outline else ("heart" if col_index + offset_x < fill_split else "heart_dark")
            draw.point((col_index + offset_x, row_index + offset_y), fill=rgba(palette[color]))
    if amount > 0:
        draw.rectangle((4, 4, 5, 5), fill=rgba(palette["heart_light"]))


def tile_icon(image: Image.Image, palette: dict[str, str], asset_id: str) -> None:
    draw = ImageDraw.Draw(image)
    rectangle(draw, (2, 2, 29, 29), "shadow", palette)
    if asset_id in {"tile_dirt", "tile_grass"}:
        draw.rectangle((3, 3, 28, 28), fill=(132, 78, 38, 255))
        for x, y in ((7, 9), (20, 14), (12, 23), (25, 26), (5, 27)):
            draw.rectangle((x, y, x + 2, y + 1), fill=(87, 47, 28, 255))
        if asset_id == "tile_grass":
            draw.rectangle((3, 3, 28, 7), fill=(88, 190, 64, 255))
            for x in range(4, 29, 4):
                draw.point((x, 8), fill=(42, 126, 53, 255))
    else:
        draw.rectangle((3, 3, 28, 28), fill=(97, 104, 119, 255))
        for box in ((5, 5, 12, 10), (15, 4, 25, 12), (4, 15, 15, 26), (18, 17, 27, 27)):
            draw.rectangle(box, fill=(119, 128, 144, 255))
        if asset_id == "tile_copper_ore":
            for box in ((8, 7, 11, 10), (19, 13, 23, 16), (12, 21, 16, 24)):
                draw.rectangle(box, fill=(205, 126, 62, 255))


def render_asset(asset: dict[str, Any], palette: dict[str, str]) -> Image.Image:
    size = tuple(asset["source_size_px"])
    image = Image.new("RGBA", size, rgba(palette["transparent"]))
    draw = ImageDraw.Draw(image)
    role = asset["role"]
    asset_id = asset["id"]
    border = asset["slice_border_px"][0]

    if role == "nine_slice_frame":
        frame(image, border, palette)
        if asset_id == "portrait_frame":
            draw.rectangle((border - 2, border - 2, size[0] - border + 1, size[1] - border + 1), fill=rgba(palette["transparent"]))
    elif role == "portrait":
        draw.rectangle((11, 5, 28, 14), fill=(103, 55, 31, 255))
        draw.rectangle((13, 13, 26, 22), fill=(225, 164, 102, 255))
        draw.rectangle((11, 22, 28, 36), fill=(48, 104, 176, 255))
        draw.rectangle((16, 24, 23, 36), fill=(44, 76, 139, 255))
        draw.point((16, 17), fill=rgba(palette["shadow"]))
        draw.point((23, 17), fill=rgba(palette["shadow"]))
    elif role.startswith("heart_"):
        heart(image, palette, {"heart_empty": 0.0, "heart_half": 0.5, "heart_full": 1.0}[role])
    elif role == "panel":
        rectangle(draw, (0, 0, size[0] - 1, size[1] - 1), "panel_soft", palette)
        rectangle(draw, (0, 0, size[0] - 1, 1), "silver_dark", palette)
        rectangle(draw, (0, size[1] - 2, size[0] - 1, size[1] - 1), "shadow", palette)
    elif role == "slot":
        frame(image, border, palette)
    elif role == "slot_selected":
        frame(image, border, palette, selected=True)
        draw.rectangle((border - 2, border - 2, size[0] - border + 1, size[1] - border + 1), fill=rgba(palette["transparent"]))
    elif role in {"label_panel", "debug_panel"}:
        rectangle(draw, (0, 0, size[0] - 1, size[1] - 1), "panel_soft", palette)
        rectangle(draw, (0, 0, size[0] - 1, 0), "silver_dark", palette)
        rectangle(draw, (0, size[1] - 1, size[0] - 1, size[1] - 1), "silver_dark", palette)
        rectangle(draw, (0, 0, 0, size[1] - 1), "silver_dark", palette)
        rectangle(draw, (size[0] - 1, 0, size[0] - 1, size[1] - 1), "silver_dark", palette)
    elif role == "tile_icon":
        tile_icon(image, palette, asset_id)
    elif role == "cursor":
        points = [(1, 1), (1, 13), (5, 9), (8, 15), (11, 13), (8, 8), (14, 8)]
        draw.polygon(points, fill=rgba(palette["silver_light"]), outline=rgba(palette["shadow"]))
        draw.point((2, 2), fill=rgba(palette["gold_light"]))
    else:
        raise ValueError(f"Unsupported asset role: {role}")

    if image.size != size:
        raise AssertionError(f"Renderer changed {asset_id} dimensions")
    return image


def nine_slice(image: Image.Image, borders: list[int], target: tuple[int, int]) -> Image.Image:
    """Composite a sliced sprite at display size: fixed corners, stretched bands."""
    left, top, right, bottom = borders[0], borders[1], borders[2], borders[3]
    src_w, src_h = image.size
    dst_w, dst_h = target
    if (src_w, src_h) == (dst_w, dst_h):
        return image.copy()
    if dst_w < left + right or dst_h < top + bottom:
        raise ValueError(f"Display size {target} smaller than slice borders {borders}")
    xs_src = [0, left, src_w - right, src_w]
    ys_src = [0, top, src_h - bottom, src_h]
    xs_dst = [0, left, dst_w - right, dst_w]
    ys_dst = [0, top, dst_h - bottom, dst_h]
    result = Image.new("RGBA", target, (0, 0, 0, 0))
    for row in range(3):
        for col in range(3):
            box = (xs_src[col], ys_src[row], xs_src[col + 1], ys_src[row + 1])
            cell = (xs_dst[col], ys_dst[row], xs_dst[col + 1], ys_dst[row + 1])
            cell_size = (cell[2] - cell[0], cell[3] - cell[1])
            if 0 in cell_size or box[0] >= box[2] or box[1] >= box[3]:
                continue
            piece = image.crop(box)
            if piece.size != cell_size:
                piece = piece.resize(cell_size, Image.Resampling.NEAREST)
            result.paste(piece, (cell[0], cell[1]))
    return result


def validate_spec(spec: dict[str, Any]) -> None:
    if spec["spec_id"] != SUPPORTED_SPEC_ID:
        raise ValueError("Unsupported HUD asset specification")
    ids: set[str] = set()
    files: set[str] = set()
    for asset in spec["assets"]:
        for key in ("id", "file", "role", "source_size_px", "display_size_px", "scale", "slice_border_px", "prompt"):
            if key not in asset:
                raise ValueError(f"Asset is missing {key}: {asset}")
        if asset["id"] in ids or asset["file"] in files:
            raise ValueError(f"Duplicate asset id or file: {asset['id']}")
        ids.add(asset["id"])
        files.add(asset["file"])
        sliced = any(asset["slice_border_px"])
        if asset["scale"] != 1:
            raise ValueError(f"{asset['id']} violates the one-to-one reference-scale contract")
        if not sliced and asset["source_size_px"] != asset["display_size_px"]:
            raise ValueError(f"{asset['id']} is not sliced and must display at source size")
        if sliced:
            left, top, right, bottom = asset["slice_border_px"]
            if asset["display_size_px"][0] < left + right or asset["display_size_px"][1] < top + bottom:
                raise ValueError(f"{asset['id']} sliced display size is smaller than its slice borders")
        for pair in (asset["source_size_px"], asset["display_size_px"]):
            if any(value <= 0 or int(value) != value for value in pair):
                raise ValueError(f"{asset['id']} has invalid dimensions")


def displayed(asset: dict[str, Any], rendered: dict[str, Image.Image]) -> Image.Image:
    return nine_slice(rendered[asset["id"]], asset["slice_border_px"], tuple(asset["display_size_px"]))


def compose_overlay(spec: dict[str, Any], rendered: dict[str, Image.Image]) -> Image.Image:
    width, height = spec["coordinate_system"]["reference_resolution_px"]
    canvas = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    draw = ImageDraw.Draw(canvas)
    font = ImageFont.load_default()
    assets = {asset["id"]: asset for asset in spec["assets"]}

    vitals = spec["modules"]["vitals"]
    vx, vy = vitals["position_px"]
    panel = nine_slice(rendered["panel_main"], assets["panel_main"]["slice_border_px"], tuple(vitals["size_px"]))
    canvas.alpha_composite(panel, (vx, vy))
    fx, fy = vitals["portrait_frame_rect_px"][0], vitals["portrait_frame_rect_px"][1]
    canvas.alpha_composite(displayed(assets["portrait_frame"], rendered), (vx + fx, vy + fy))
    px, py = vitals["portrait_rect_px"][0], vitals["portrait_rect_px"][1]
    canvas.alpha_composite(rendered["player_portrait"], (vx + px, vy + py))
    heart_stride = vitals["heart_size_px"][0] + vitals["heart_spacing_px"]
    hx0, hy0 = vitals["hearts_origin_px"]
    for index in range(vitals["heart_count"]):
        canvas.alpha_composite(rendered["heart_full"], (vx + hx0 + index * heart_stride, vy + hy0))

    hotbar = spec["modules"]["hotbar"]
    hx, hy = hotbar["position_px"]
    backing = nine_slice(rendered["hotbar_backing"], assets["hotbar_backing"]["slice_border_px"], tuple(hotbar["size_px"]))
    canvas.alpha_composite(backing, (hx, hy))
    icons = ["tile_dirt", "tile_grass", "tile_stone", "tile_copper_ore"]
    icon_dx, icon_dy = hotbar["icon_offset_px"]
    slot_w = hotbar["slot_size_px"][0]
    selected = 1
    for index in range(hotbar["slot_count"]):
        sx = hx + hotbar["slot_origin_px"][0] + index * hotbar["slot_stride_px"]
        sy = hy + hotbar["slot_origin_px"][1] - (hotbar["selected_lift_px"] if index == selected else 0)
        canvas.alpha_composite(rendered["slot_normal"], (sx, sy))
        if index < len(icons):
            canvas.alpha_composite(rendered[icons[index]], (sx + icon_dx, sy + icon_dy))
            draw.text((sx + slot_w - 13, sy + slot_w - 14), "∞", font=font, fill=(242, 243, 244, 255))
        draw.text((sx + 7, sy + 5), "0" if index == 9 else str(index + 1), font=font, fill=(242, 243, 244, 255))
        if index == selected:
            canvas.alpha_composite(rendered["slot_selected"], (sx, sy))
            label_spec = spec["modules"]["selected_item_label"]
            label = displayed(assets["selected_item_label"], rendered)
            label_x = sx + (slot_w - label.width) // 2
            label_y = sy - label_spec["gap_px"] - label.height
            canvas.alpha_composite(label, (label_x, label_y))
            text_box = draw.textbbox((0, 0), "GRASS", font=font)
            draw.text((label_x + (label.width - (text_box[2] - text_box[0])) // 2, label_y + 5), "GRASS", font=font, fill=(242, 243, 244, 255))

    debug = spec["modules"]["debug"]
    dx, dy = debug["position_px"]
    canvas.alpha_composite(displayed(assets["debug_panel"], rendered), (dx, dy))
    for index, line in enumerate(debug["text"]):
        draw.text((dx + 8, dy + 6 + index * 13), line, font=font, fill=(226, 229, 231, 255))
    return canvas


def make_contact_sheet(spec: dict[str, Any], rendered: dict[str, Image.Image]) -> Image.Image:
    sheet = Image.new("RGBA", (1280, 960), rgba(spec["mockup_outputs"][1]["background"]))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    draw.text((24, 18), "ProjectTwelve HUD asset mockups — exact source pixels", font=font, fill=(242, 243, 244, 255))
    x, y, row_height = 24, 48, 0
    for asset in spec["assets"]:
        image = rendered[asset["id"]]
        preview_scale = max(1, min(4, 180 // max(image.size)))
        preview = image.resize((image.width * preview_scale, image.height * preview_scale), Image.Resampling.NEAREST)
        cell_width = max(210, preview.width + 24)
        cell_height = max(120, preview.height + 48)
        if x + cell_width > sheet.width - 24:
            x = 24
            y += row_height + 16
            row_height = 0
        draw.rectangle((x, y, x + cell_width - 1, y + cell_height - 1), fill=(7, 9, 15, 255), outline=(84, 91, 104, 255))
        sheet.alpha_composite(preview, (x + 12, y + 28))
        label = f"{asset['id']}  {image.width}x{image.height}"
        draw.text((x + 8, y + 8), label, font=font, fill=(226, 229, 231, 255))
        x += cell_width + 16
        row_height = max(row_height, cell_height)
    return sheet


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--spec", type=Path, default=DEFAULT_SPEC)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    args = parser.parse_args()

    spec = json.loads(args.spec.read_text(encoding="utf-8"))
    validate_spec(spec)
    args.output.mkdir(parents=True, exist_ok=True)
    palette = spec["palette"]
    rendered: dict[str, Image.Image] = {}
    for asset in spec["assets"]:
        image = render_asset(asset, palette)
        image.save(args.output / asset["file"], format="PNG", optimize=False)
        rendered[asset["id"]] = image

    overlay = compose_overlay(spec, rendered)
    overlay.save(args.output / "hud-overlay-1280x720.png", format="PNG", optimize=False)
    contact_sheet = make_contact_sheet(spec, rendered)
    contact_sheet.save(args.output / "hud-asset-contact-sheet.png", format="PNG", optimize=False)
    print(f"Generated {len(rendered)} assets and 2 composites in {args.output}")


if __name__ == "__main__":
    main()
