"""Strip metadata from cell PNGs and rebuild clean base64 args for PixelLab."""
from __future__ import annotations

import base64
import json
from pathlib import Path

from PIL import Image

CELLS = Path(r"d:/Projects/project-twelve/Assets/Sprites/Tiles/Generated/wood_cells")
PICKS = ["0_0", "0_3", "1_0", "2_6"]
DESC = (
    "Keep the EXACT silhouette and empty pixels of this reference tile. "
    "Remake solid pixels as warm oak wooden planks: horizontal boards, soft tan "
    "highlights, dark brown seams. No blue, no purple, no stone. Dense opaque wood "
    "where the reference was solid. Classic 16-bit pixel art."
)


def clean_png(src: Path, dst: Path) -> None:
    im = Image.open(src).convert("RGBA")
    # Re-encode without ICC / text chunks
    im.save(dst, format="PNG", optimize=True)


def main() -> None:
    for key in PICKS:
        src16 = CELLS / f"cell_{key}.png"
        clean16 = CELLS / f"cell_{key}_clean.png"
        clean32 = CELLS / f"cell_{key}_32_clean.png"
        clean_png(src16, clean16)
        Image.open(clean16).resize((32, 32), Image.NEAREST).save(
            clean32, format="PNG", optimize=True
        )
        b64 = base64.b64encode(clean32.read_bytes()).decode("ascii")
        (CELLS / f"raw_{key}_clean.b64").write_text(b64)
        args = {
            "description": DESC,
            "width": 32,
            "height": 32,
            "view": "side",
            "outline": "selective outline",
            "shading": "basic shading",
            "detail": "low detail",
            "background_image": json.dumps({"type": "base64", "base64": b64}),
            "inpainting": json.dumps({"type": "rectangle", "fraction": 0.9}),
        }
        (CELLS / f"args_{key}_clean.json").write_text(json.dumps(args))
        print(key, "png", clean32.stat().st_size, "b64", len(b64))


if __name__ == "__main__":
    main()
