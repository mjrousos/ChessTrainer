---
name: chess-visualizer
description: "Render a chess position from a FEN string to a PNG image. USE WHEN: you need to **visualize** or share the state of a chess board described by a FEN. DO NOT USE WHEN: you need to evaluate a position to determine best moves, legal moves, or game outcomes"
allowed-tools: Bash(python:*) Bash(python3:*) Bash(py:*) Bash(pip:*)
---

# Chess board visualizer (FEN → PNG)

Takes a FEN string and writes a PNG of the resulting chess position, using
pre-rendered board and piece assets that ship with the skill.

- **Runtime dependency:** Pillow only (`pip install Pillow`).
- **Cross-platform:** runs unchanged on Windows, Linux, and macOS — no system
  packages, no native SVG renderer, no system fonts.
- **No board state is inferred:** only the piece-placement field of the FEN is
  consumed; side-to-move / castling / en-passant / clocks are tolerated but
  ignored.

## Quick start

```bash
# Linux / macOS
pip install Pillow
python3 .agents/skills/chess-visualizer/scripts/fen_to_png.py \
    "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1" \
    -o start.png
```

```powershell
# Windows
pip install Pillow
python .agents\skills\chess-visualizer\scripts\fen_to_png.py `
    "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1" `
    -o start.png
# `py -3 ...` works too.
```

Mid-game position with black at the bottom and no coordinate labels:

```bash
python fen_to_png.py "r1bqkb1r/pppp1ppp/2n2n2/4p3/2B1P3/5N2/PPPP1PPP/RNBQK2R w KQkq - 4 4" \
    -o middlegame.png --size 1024 --flip --no-coords
```

## CLI

```
fen_to_png.py <FEN> -o <output.png> [--size N] [--flip] [--no-coords]
```

| Flag           | Default | Notes                                                   |
| -------------- | ------- | ------------------------------------------------------- |
| `<FEN>`        | —       | Positional; full FEN ok, only the board field is used.  |
| `-o, --output` | —       | Required. Destination PNG path.                         |
| `--size N`     | `512`   | Square edge in pixels. Validated to `128 ≤ N ≤ 2048`.   |
| `--flip`       | off     | Render with black at the bottom (files & ranks reverse).|
| `--no-coords`  | off     | Suppress the a–h / 1–8 labels around the board.         |

Exit codes: `0` success · `1` I/O or missing-asset error · `2` invalid input
(bad FEN or out-of-range `--size`).

## When to use it

- Quickly visualize a FEN while debugging engine output, puzzle data, or game
  ingestion.
- Embed a board image in PR descriptions, issue reports, or generated docs.
- Verify by eye that a FEN string round-trips through your code as expected.

## When NOT to use it

- You need full SVG output or a vector format → render `board.svg` and
  `Chess_Pieces_Sprite.svg` directly with a tool like Inkscape.
- You need move arrows, square highlights, or annotations → out of scope for
  v1; add post-processing on top of the PNG or extend the script.

## Regenerating the cached assets

The 13 PNGs under `assets/` (`board.png` plus `pieces/{w,b}{K,Q,R,B,N,P}.png`)
are **canonical and committed**. You only need to regenerate them if
`board.svg` or `Chess_Pieces_Sprite.svg` changes.

```bash
pip install resvg-py Pillow
python .agents/skills/chess-visualizer/scripts/prepare_assets.py
```

`resvg-py` is a Rust-backed renderer distributed as cross-platform wheels on
PyPI (Windows / Linux / macOS); no system libraries required. The prep script
verifies each generated piece is mode `RGBA` with transparent corners and
fails loudly if not — so a bad regen can't quietly land opaque-background
pieces.

## Files

```
.agents/skills/chess-visualizer/
├── SKILL.md
├── scripts/
│   ├── fen_to_png.py        # runtime (Pillow only)
│   └── prepare_assets.py    # one-time regen (resvg-py + Pillow)
└── assets/
    ├── board.svg                  # source
    ├── Chess_Pieces_Sprite.svg    # source (Wikipedia 270×90 sprite)
    ├── board.png                  # generated, committed
    └── pieces/
        ├── wK.png wQ.png wR.png wB.png wN.png wP.png   # generated
        └── bK.png bQ.png bR.png bB.png bN.png bP.png   # generated
```
