#!/usr/bin/env python3
"""Render a FEN position to a PNG using cached board + piece assets.

Runtime dependency: Pillow only.

Usage:
    python fen_to_png.py "<FEN>" -o board.png [--size 512] [--flip] [--no-coords]

Only the board (piece-placement) field of the FEN is consumed; trailing fields
(side-to-move, castling, en passant, halfmove, fullmove) are tolerated but
ignored.

Exit codes:
    0  success
    1  I/O or asset error
    2  invalid input (bad FEN or out-of-range --size)

Cross-platform: works unchanged on Windows, Linux, and macOS. Uses
``pathlib.Path`` exclusively, Pillow's built-in default font (no system font
paths), and explicit UTF-8 for text I/O.
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path
from typing import Optional

from PIL import Image, ImageDraw, ImageFont

SCRIPT_DIR = Path(__file__).resolve().parent
ASSETS_DIR = SCRIPT_DIR.parent / "assets"
BOARD_ASSET = ASSETS_DIR / "board.png"
PIECES_DIR = ASSETS_DIR / "pieces"

MIN_SIZE = 128
MAX_SIZE = 2048

PIECE_LETTERS = set("KQRBNPkqrbnp")
FILES = "abcdefgh"
RANKS = "12345678"


class FenError(ValueError):
    """Raised when the FEN board field is malformed."""


def parse_fen_board(fen: str) -> list[list[Optional[str]]]:
    """Return an 8x8 grid indexed [rank_idx_from_top][file_idx_from_left].

    Rank 0 corresponds to FEN rank 8 (top of board in standard orientation).
    """
    if not fen or not fen.strip():
        raise FenError("FEN is empty")
    board_field = fen.strip().split()[0]
    ranks = board_field.split("/")
    if len(ranks) != 8:
        raise FenError(f"FEN must have 8 ranks separated by '/', got {len(ranks)}")
    grid: list[list[Optional[str]]] = []
    for rank_idx, rank in enumerate(ranks):
        row: list[Optional[str]] = []
        for ch in rank:
            if ch.isdigit():
                n = int(ch)
                if n < 1 or n > 8:
                    raise FenError(
                        f"rank {8 - rank_idx} has invalid empty-square digit '{ch}'"
                    )
                row.extend([None] * n)
            elif ch in PIECE_LETTERS:
                row.append(ch)
            else:
                raise FenError(
                    f"rank {8 - rank_idx} has illegal character '{ch}'"
                )
        if len(row) != 8:
            raise FenError(
                f"rank {8 - rank_idx} resolves to {len(row)} squares, expected 8"
            )
        grid.append(row)
    return grid


def _piece_asset(piece_char: str) -> Path:
    color = "w" if piece_char.isupper() else "b"
    return PIECES_DIR / f"{color}{piece_char.upper()}.png"


def _load_font(target_px: int) -> ImageFont.ImageFont:
    # Pillow 10+ supports a scalable default font via the ``size`` kwarg.
    try:
        return ImageFont.load_default(size=max(10, target_px))
    except TypeError:
        return ImageFont.load_default()


def render(
    fen: str,
    *,
    size: int = 512,
    flip: bool = False,
    coords: bool = True,
) -> Image.Image:
    if size < MIN_SIZE or size > MAX_SIZE:
        raise ValueError(
            f"--size must be between {MIN_SIZE} and {MAX_SIZE}, got {size}"
        )

    grid = parse_fen_board(fen)

    coord_gutter = max(12, size // 20) if coords else 0
    inner = size - 2 * coord_gutter
    if inner < 8:
        raise ValueError(f"--size {size} too small for an 8-square board")
    square_px = inner // 8
    board_px = square_px * 8
    origin_x = (size - board_px) // 2
    origin_y = (size - board_px) // 2

    canvas = Image.new("RGBA", (size, size), (255, 255, 255, 255))

    if not BOARD_ASSET.is_file():
        raise FileNotFoundError(
            f"missing board asset {BOARD_ASSET}; run prepare_assets.py first."
        )
    board_img = Image.open(BOARD_ASSET).convert("RGBA")
    board_img = board_img.resize((board_px, board_px), Image.LANCZOS)
    canvas.alpha_composite(board_img, (origin_x, origin_y))

    for rank_idx in range(8):  # 0 = FEN rank 8 (top in standard orientation)
        for file_idx in range(8):  # 0 = file 'a' (left in standard orientation)
            piece = grid[rank_idx][file_idx]
            if piece is None:
                continue
            asset = _piece_asset(piece)
            if not asset.is_file():
                raise FileNotFoundError(
                    f"missing piece asset {asset}; run prepare_assets.py first."
                )
            piece_img = Image.open(asset).convert("RGBA")
            piece_img = piece_img.resize((square_px, square_px), Image.LANCZOS)

            if flip:
                draw_file = 7 - file_idx
                draw_rank_from_top = 7 - rank_idx
            else:
                draw_file = file_idx
                draw_rank_from_top = rank_idx
            x = origin_x + draw_file * square_px
            y = origin_y + draw_rank_from_top * square_px
            canvas.alpha_composite(piece_img, (x, y))

    if coords:
        draw = ImageDraw.Draw(canvas)
        font = _load_font(target_px=max(10, coord_gutter * 2 // 3))
        text_fill = (40, 40, 40, 255)

        for col in range(8):
            file_char = FILES[7 - col] if flip else FILES[col]
            cx = origin_x + col * square_px + square_px // 2
            cy = origin_y + board_px + coord_gutter // 2
            _draw_centered(draw, file_char, (cx, cy), font, text_fill)

        for row in range(8):
            rank_char = RANKS[row] if flip else RANKS[7 - row]
            cx = origin_x - coord_gutter // 2
            cy = origin_y + row * square_px + square_px // 2
            _draw_centered(draw, rank_char, (cx, cy), font, text_fill)

    return canvas


def _draw_centered(
    draw: ImageDraw.ImageDraw,
    text: str,
    center: tuple[int, int],
    font: ImageFont.ImageFont,
    fill: tuple[int, int, int, int],
) -> None:
    left, top, right, bottom = draw.textbbox((0, 0), text, font=font)
    w = right - left
    h = bottom - top
    x = center[0] - w // 2 - left
    y = center[1] - h // 2 - top
    draw.text((x, y), text, font=font, fill=fill)


def main(argv: Optional[list[str]] = None) -> int:
    parser = argparse.ArgumentParser(
        description="Render a FEN chess position to a PNG.",
    )
    parser.add_argument("fen", help="FEN string (only the board field is required)")
    parser.add_argument(
        "-o", "--output", required=True, type=Path,
        help="Destination PNG path.",
    )
    parser.add_argument(
        "--size", type=int, default=512,
        help=f"Output image edge in pixels ({MIN_SIZE}-{MAX_SIZE}). Default 512.",
    )
    parser.add_argument(
        "--flip", action="store_true",
        help="Render with black at the bottom.",
    )
    parser.add_argument(
        "--no-coords", dest="coords", action="store_false",
        help="Omit a-h / 1-8 coordinate labels.",
    )
    args = parser.parse_args(argv)

    try:
        img = render(args.fen, size=args.size, flip=args.flip, coords=args.coords)
    except FenError as exc:
        print(f"fen_to_png: invalid FEN: {exc}", file=sys.stderr)
        return 2
    except ValueError as exc:
        print(f"fen_to_png: {exc}", file=sys.stderr)
        return 2
    except FileNotFoundError as exc:
        print(f"fen_to_png: {exc}", file=sys.stderr)
        return 1

    try:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        img.save(args.output, format="PNG")
    except OSError as exc:
        print(f"fen_to_png: failed to write {args.output}: {exc}", file=sys.stderr)
        return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
