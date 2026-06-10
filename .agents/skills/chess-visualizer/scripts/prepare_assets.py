#!/usr/bin/env python3
"""One-time rasterization of the chess-visualizer SVG assets to PNGs.

Renders ``board.svg`` and slices ``Chess_Pieces_Sprite.svg`` into 12 transparent
piece PNGs. The generated files are committed alongside the SVGs and are the
canonical inputs for ``fen_to_png.py`` at runtime.

Run this only when the source SVGs change. Requires:
    pip install resvg-py Pillow

Cross-platform: works unchanged on Windows, Linux, and macOS. All filesystem
paths use ``pathlib`` and are resolved relative to this file.
"""

from __future__ import annotations

import io
import sys
from pathlib import Path

import resvg_py
from PIL import Image

SCRIPT_DIR = Path(__file__).resolve().parent
ASSETS_DIR = SCRIPT_DIR.parent / "assets"
PIECES_DIR = ASSETS_DIR / "pieces"

BOARD_SVG = ASSETS_DIR / "board.svg"
SPRITE_SVG = ASSETS_DIR / "Chess_Pieces_Sprite.svg"

BOARD_OUT = ASSETS_DIR / "board.png"
BOARD_PX = 1024

# Render the sprite large, then downscale each cell to this size with LANCZOS
# for crisp pieces at any reasonable runtime --size.
SPRITE_RENDER_WIDTH = 2160  # 6 columns -> 360 px per cell at render time
PIECE_OUT_PX = 256

# Sprite layout: row 0 = white, row 1 = black; columns: K Q B N R P.
COLUMN_PIECES = ["K", "Q", "B", "N", "R", "P"]
ROW_COLORS = ["w", "b"]


def _render_svg(svg_path: Path, *, width: int) -> Image.Image:
    """Rasterize an SVG to a Pillow RGBA image at the requested pixel width."""
    if not svg_path.is_file():
        raise FileNotFoundError(f"missing SVG asset: {svg_path}")
    png_bytes = resvg_py.svg_to_bytes(svg_path=str(svg_path), width=width)
    img = Image.open(io.BytesIO(bytes(png_bytes)))
    return img.convert("RGBA")


def _assert_transparent_corners(img: Image.Image, label: str) -> None:
    w, h = img.size
    corners = [(0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)]
    for x, y in corners:
        alpha = img.getpixel((x, y))[3]
        if alpha != 0:
            raise RuntimeError(
                f"piece '{label}' corner pixel ({x},{y}) has alpha {alpha}; "
                "expected 0 (transparent). Refusing to write opaque piece PNGs."
            )


def prepare_board() -> tuple[Path, tuple[int, int], str]:
    img = _render_svg(BOARD_SVG, width=BOARD_PX)
    if img.size[0] != img.size[1]:
        # board.svg has a square viewBox; resvg may produce 1-pixel rounding diffs.
        side = min(img.size)
        img = img.resize((side, side), Image.LANCZOS)
    ASSETS_DIR.mkdir(parents=True, exist_ok=True)
    img.save(BOARD_OUT, format="PNG")
    return BOARD_OUT, img.size, img.mode


def prepare_pieces() -> list[tuple[Path, tuple[int, int], str]]:
    sprite = _render_svg(SPRITE_SVG, width=SPRITE_RENDER_WIDTH)
    width, height = sprite.size
    if width % 6 != 0 or height % 2 != 0:
        raise RuntimeError(
            f"sprite render dims {width}x{height} are not divisible by 6x2; "
            "cannot slice into 12 equal cells."
        )
    cell_w = width // 6
    cell_h = height // 2

    PIECES_DIR.mkdir(parents=True, exist_ok=True)
    written: list[tuple[Path, tuple[int, int], str]] = []
    for row_idx, color in enumerate(ROW_COLORS):
        for col_idx, piece in enumerate(COLUMN_PIECES):
            box = (
                col_idx * cell_w,
                row_idx * cell_h,
                (col_idx + 1) * cell_w,
                (row_idx + 1) * cell_h,
            )
            cell = sprite.crop(box)
            if cell.mode != "RGBA":
                cell = cell.convert("RGBA")
            cell = cell.resize((PIECE_OUT_PX, PIECE_OUT_PX), Image.LANCZOS)
            label = f"{color}{piece}"
            _assert_transparent_corners(cell, label)
            out_path = PIECES_DIR / f"{label}.png"
            cell.save(out_path, format="PNG")
            written.append((out_path, cell.size, cell.mode))
    return written


def main() -> int:
    try:
        board_path, board_size, board_mode = prepare_board()
        pieces = prepare_pieces()
    except (FileNotFoundError, RuntimeError) as exc:
        print(f"prepare_assets: {exc}", file=sys.stderr)
        return 1

    print(f"wrote {board_path}  {board_size[0]}x{board_size[1]} ({board_mode})")
    for path, size, mode in pieces:
        rel = path.relative_to(ASSETS_DIR)
        print(f"wrote {ASSETS_DIR / rel}  {size[0]}x{size[1]} ({mode})")
    print(f"done: 1 board + {len(pieces)} pieces")
    return 0


if __name__ == "__main__":
    sys.exit(main())
