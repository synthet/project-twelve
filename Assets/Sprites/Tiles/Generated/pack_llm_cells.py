"""Extract LLM tile samples to 16x16 via content bbox + nearest-neighbor."""
from __future__ import annotations

from pathlib import Path

from PIL import Image

ASSETS = Path(r"C:/Users/dmnsy/.cursor/projects/d-Projects-project-twelve/assets")
CELLS = Path(r"d:/Projects/project-twelve/Assets/Sprites/Tiles/Generated/wood_cells")
GEN = Path(r"d:/Projects/project-twelve/Assets/Sprites/Tiles/Generated")
KEYS = ["0_0", "0_3", "1_0", "2_6"]


def content_bbox(im: Image.Image, thr: int = 18) -> tuple[int, int, int, int]:
    px = im.load()
    w, h = im.size
    min_x, min_y, max_x, max_y = w, h, -1, -1
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a > 8 and (r + g + b) > thr * 3:
                min_x = min(min_x, x)
                min_y = min(min_y, y)
                max_x = max(max_x, x)
                max_y = max(max_y, y)
    if max_x < 0:
        return (0, 0, w, h)
    # pad to square
    bw = max_x - min_x + 1
    bh = max_y - min_y + 1
    side = max(bw, bh)
    cx = (min_x + max_x) // 2
    cy = (min_y + max_y) // 2
    left = max(0, cx - side // 2)
    top = max(0, cy - side // 2)
    right = min(w, left + side)
    bottom = min(h, top + side)
    left = max(0, right - side)
    top = max(0, bottom - side)
    return (left, top, right, bottom)


def to_16(path: Path) -> Image.Image:
    im = Image.open(path).convert("RGBA")
    box = content_bbox(im)
    crop = im.crop(box)
    return crop.resize((16, 16), Image.NEAREST)


def main() -> None:
    for key in KEYS:
        src = ASSETS / f"wood_llm_sq_{key}.png"
        im16 = to_16(src)
        dest = CELLS / f"llm_{key}_16.png"
        im16.save(dest)
        (GEN / f"wood_llm_{key}_full.png").write_bytes(src.read_bytes())
        n = sum(1 for p in im16.getdata() if p[3] > 8 and sum(p[:3]) > 20)
        print(key, "src", Image.open(src).size, "bbox->16 nonempty", n)

    # orig | pixellab | llm
    gap = 6
    cell_w = 16 * 3 + 2
    W = 4 * cell_w + 3 * gap
    H = 20
    comp = Image.new("RGBA", (W, H), (24, 24, 28, 255))
    x = 0
    for key in KEYS:
        orig = Image.open(CELLS / f"cell_{key}.png").convert("RGBA")
        pl = CELLS / f"wood_{key}_16.png"
        pix = (
            Image.open(pl).convert("RGBA")
            if pl.exists()
            else Image.new("RGBA", (16, 16), (0, 0, 0, 255))
        )
        llm = Image.open(CELLS / f"llm_{key}_16.png").convert("RGBA")
        y = 2
        comp.paste(orig, (x, y))
        x += 16
        comp.paste(pix, (x, y))
        x += 16
        comp.paste(llm, (x, y))
        x += 16 + gap

    out = GEN / "WoodCells_llm_compare.png"
    comp.resize((comp.width * 8, comp.height * 8), Image.NEAREST).save(out)
    print("wrote", out)


if __name__ == "__main__":
    main()
