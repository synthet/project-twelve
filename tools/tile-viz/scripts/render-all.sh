#!/usr/bin/env bash
# Render every snippet fixture to PNG (local scratch output).
#
# Usage:
#   ./scripts/render-all.sh [out-dir]
#   TILE_VIZ_ASSETS_ROOT=/path/to/Tiles ./scripts/render-all.sh out
#
# Requires licensed sprite sheets (default: repo Assets/_Licensed/PixelFantasy/PixelTileEngine/Tiles).

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
REPO_ROOT="$(cd "$ROOT/../.." && pwd)"
ASSETS_ROOT="${TILE_VIZ_ASSETS_ROOT:-$REPO_ROOT/Assets/_Licensed/PixelFantasy/PixelTileEngine/Tiles}"
OUT_DIR="${1:-out}"
SNIPPET_SCALE="${SNIPPET_SCALE:-16}"

if [[ ! -f "$ASSETS_ROOT/Ground/Humus.png" ]]; then
  echo "error: licensed assets not found at $ASSETS_ROOT" >&2
  echo "set TILE_VIZ_ASSETS_ROOT or initialize the Assets/_Licensed submodule" >&2
  exit 1
fi

mkdir -p "$ROOT/$OUT_DIR"

render_one() {
  local space="$1"
  local png_out="$2"
  local scale="$3"
  node "$ROOT/src/cli.js" render \
    --space "$space" \
    --assets-root "$ASSETS_ROOT" \
    --scale "$scale" \
    --flat-light \
    --png "$png_out"
}

count=0
for json in "$ROOT"/test/fixtures/snippets/*.json; do
  [[ -f "$json" ]] || continue
  name="$(basename "$json" .json)"
  render_one "$json" "$ROOT/$OUT_DIR/$name.png" "$SNIPPET_SCALE"
  count=$((count + 1))
done

echo "wrote $count PNGs to $ROOT/$OUT_DIR"
