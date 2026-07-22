"""Procedural wood remake of BricksC (CLI-task equivalent when Codex/Gemini cannot run)."""
from __future__ import annotations

from pathlib import Path

from PIL import Image

ROOT = Path(r"d:/Projects/project-twelve")
SRC = ROOT / "Assets/_Licensed/PixelFantasy/PixelTileEngine/Tiles/Ground/BricksC.png"
OUT = ROOT / "Assets/Sprites/Tiles/Generated/WoodPlatform_cli_local.png"
PREVIEW = ROOT / "Assets/Sprites/Tiles/Generated/WoodPlatform_cli_local_preview.png"

# highlight, body, mid, shadow, seam
PAL = [
    (210, 160, 96),
    (172, 118, 66),
    (138, 90, 48),
    (96, 58, 30),
    (58, 34, 16),
]


def main() -> None:
    src = Image.open(SRC).convert("RGBA")
    out = Image.new("RGBA", src.size, (0, 0, 0, 0))
    sp = src.load()
    op = out.load()
    w, h = src.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = sp[x, y]
            lum = (0.3 * r + 0.59 * g + 0.11 * b) / 255.0
            if a < 8 or lum < 0.055:
                continue
            ly = y % 16
            lx = x % 16
            band = ly % 4
            if band == 3:
                base = PAL[4]
            elif band == 0:
                base = PAL[0] if lum > 0.45 else PAL[1]
            elif band == 1:
                base = PAL[1] if lum > 0.35 else PAL[2]
            else:
                base = PAL[2] if lum > 0.25 else PAL[3]
            if lx % 8 == 7:
                base = tuple(max(0, c - 28) for c in base)
            if lum > 0.55:
                base = tuple(min(255, c + 18) for c in base)
            if lum < 0.2:
                base = tuple(max(0, c - 28) for c in base)
            op[x, y] = (*base, 255)

    OUT.parent.mkdir(parents=True, exist_ok=True)
    out.save(OUT)
    out.resize((w * 8, h * 8), Image.NEAREST).save(PREVIEW)
    nonempty = sum(1 for y in range(h) for x in range(w) if op[x, y][3] > 8)
    print("wrote", OUT)
    print("preview", PREVIEW)
    print("size", out.size, "nonempty", nonempty)


if __name__ == "__main__":
    main()
