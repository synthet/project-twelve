"""Cut a Ground sheet into 16x16 cells and 32x32 NN refs for PixelLab style matching."""
from __future__ import annotations

import json
from pathlib import Path

from PIL import Image

SRC = Path(
    r"d:/Projects/project-twelve/Assets/_Licensed/PixelFantasy/PixelTileEngine/Tiles/Ground/BricksC.png"
)
OUT_DIR = Path(r"d:/Projects/project-twelve/Assets/Sprites/Tiles/Generated/wood_cells")
TW = TH = 16


def main() -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    im = Image.open(SRC).convert("RGBA")
    cells = []
    cols = im.width // TW
    rows = im.height // TH
    for row in range(rows):
        for col in range(cols):
            cell = im.crop((col * TW, row * TH, (col + 1) * TW, (row + 1) * TH))
            px = list(cell.getdata())
            solid = sum(
                1
                for r, g, b, a in px
                if a >= 8 and (0.3 * r + 0.59 * g + 0.11 * b) >= 14
            )
            idx = row * cols + col
            info = {
                "index": idx,
                "row": row,
                "col": col,
                "solid_pixels": solid,
                "empty": solid < 8,
            }
            if not info["empty"]:
                path = OUT_DIR / f"cell_{row}_{col}.png"
                cell.save(path)
                up = cell.resize((32, 32), Image.NEAREST)
                up_path = OUT_DIR / f"cell_{row}_{col}_32.png"
                up.save(up_path)
                info["path"] = str(path)
                info["path32"] = str(up_path)
            cells.append(info)

    manifest = {
        "source": str(SRC),
        "tile": 16,
        "cells": cells,
        "solid_count": sum(1 for c in cells if not c["empty"]),
    }
    (OUT_DIR / "manifest.json").write_text(json.dumps(manifest, indent=2))
    print("solid", manifest["solid_count"], "of", len(cells))
    for c in cells:
        if not c["empty"]:
            print(f"  r{c['row']}c{c['col']} solid={c['solid_pixels']}")


if __name__ == "__main__":
    main()
