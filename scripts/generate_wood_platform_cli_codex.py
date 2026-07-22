"""Generate a warm-oak pixel-art remake of the BricksC ground tilesheet."""

from pathlib import Path

from PIL import Image


SHEET_SIZE = (128, 64)
TILE_SIZE = 16
PREVIEW_SCALE = 8

SOURCE_RELATIVE = Path(
    "Assets/_Licensed/PixelFantasy/PixelTileEngine/Tiles/Ground/BricksC.png"
)
OUTPUT_RELATIVE = Path(
    "Assets/Sprites/Tiles/Generated/WoodPlatform_cli_codex.png"
)
PREVIEW_RELATIVE = Path(
    "Assets/Sprites/Tiles/Generated/WoodPlatform_cli_codex_preview.png"
)

# Warm oak browns, darkest groove to brightest fresh-cut highlight.
PALETTE = (
    (68, 34, 17),
    (88, 45, 21),
    (111, 58, 26),
    (137, 76, 33),
    (164, 96, 42),
    (190, 119, 55),
    (211, 143, 72),
)


def noise(x: int, y: int, salt: int = 0) -> int:
    """Return a stable byte-sized hash for subtle hand-pixelled variation."""
    value = x * 73_856_093 ^ y * 19_349_663 ^ salt * 83_492_791
    value ^= value >> 13
    value *= 1_274_126_177
    return (value ^ (value >> 16)) & 0xFF


def paint_oak(mask: Image.Image) -> Image.Image:
    """Paint horizontal oak boards, clipped exactly to the supplied alpha mask."""
    result = Image.new("RGBA", SHEET_SIZE, (0, 0, 0, 0))
    pixels = result.load()
    alpha = mask.load()

    for y in range(SHEET_SIZE[1]):
        tile_y, local_y = divmod(y, TILE_SIZE)
        board, board_y = divmod(local_y, 5)

        for x in range(SHEET_SIZE[0]):
            if alpha[x, y] == 0:
                continue

            tile_x, local_x = divmod(x, TILE_SIZE)
            board_seed = noise(tile_x, tile_y, board)
            base_index = 3 + (board_seed % 2)

            # A dark lower edge and a fine upper glint make each board readable.
            if board_y == 0:
                color_index = 5
            elif board_y == 4 or local_y == TILE_SIZE - 1:
                color_index = 1
            else:
                color_index = base_index

            # Shade the board ends, while keeping the interior warmly lit.
            if local_x in (0, TILE_SIZE - 1):
                color_index = min(color_index, 2)
            elif local_x in (1, TILE_SIZE - 2) and board_y not in (0, 4):
                color_index = max(2, color_index - 1)

            # Sparse horizontal grain is deterministic and never changes alpha.
            grain = noise(x, y, board_seed)
            if board_y in (2, 3) and grain < 22:
                color_index = max(2, color_index - 1)
            elif board_y == 1 and grain > 241:
                color_index = min(6, color_index + 1)

            pixels[x, y] = (*PALETTE[color_index], 255)

    # Staggered butt joints sell the plank construction without obscuring tiles.
    for tile_y in range(SHEET_SIZE[1] // TILE_SIZE):
        for tile_x in range(SHEET_SIZE[0] // TILE_SIZE):
            origin_x = tile_x * TILE_SIZE
            origin_y = tile_y * TILE_SIZE
            for board in range(4):
                top = origin_y + board * 5
                if top >= origin_y + TILE_SIZE:
                    continue
                joint_x = origin_x + 5 + noise(tile_x, tile_y, board + 17) % 7
                for joint_y in range(top + 1, min(top + 5, origin_y + TILE_SIZE)):
                    if alpha[joint_x, joint_y] != 0:
                        pixels[joint_x, joint_y] = (*PALETTE[0], 255)
                    highlight_x = joint_x + 1
                    if (
                        highlight_x < origin_x + TILE_SIZE
                        and alpha[highlight_x, joint_y] != 0
                        and joint_y != min(top + 4, origin_y + TILE_SIZE - 1)
                    ):
                        pixels[highlight_x, joint_y] = (*PALETTE[5], 255)

    return result


def main() -> None:
    repo_root = Path(__file__).resolve().parents[1]
    source_path = repo_root / SOURCE_RELATIVE
    output_path = repo_root / OUTPUT_RELATIVE
    preview_path = repo_root / PREVIEW_RELATIVE

    with Image.open(source_path) as source:
        source_rgba = source.convert("RGBA")
    if source_rgba.size != SHEET_SIZE:
        raise ValueError(f"Expected source size {SHEET_SIZE}, got {source_rgba.size}")

    mask = source_rgba.getchannel("A")
    if set(mask.getextrema()) - {0, 255}:
        raise ValueError("Expected a binary source alpha mask")

    output = paint_oak(mask)
    if output.getchannel("A").tobytes() != mask.tobytes():
        raise RuntimeError("Generated alpha silhouette differs from the source")

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output.save(output_path, format="PNG", optimize=False)

    preview = output.resize(
        (SHEET_SIZE[0] * PREVIEW_SCALE, SHEET_SIZE[1] * PREVIEW_SCALE),
        resample=Image.Resampling.NEAREST,
    )
    preview.save(preview_path, format="PNG", optimize=False)

    nonempty_pixels = sum(output.getchannel("A").histogram()[1:])
    print(output_path)
    print(preview_path)
    print(f"nonempty pixel count: {nonempty_pixels}")


if __name__ == "__main__":
    main()
