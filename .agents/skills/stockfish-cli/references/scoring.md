# Score Interpretation

How to read Stockfish's `score cp` / `score mate` / `wdl` output correctly.

## Centipawn scores — the modern convention

Stockfish reports `score cp N` on every `info` line. **Two important rules:**

1. **Scores are always from the side-to-move's perspective.** `+24 cp` means whoever's turn it is has an advantage. If White is to move and the score is `cp +24`, White is +0.24 better. If Black is to move and the score is `cp -50`, Black is 0.50 worse — i.e. White is +0.50 better. **The sign flips with the side to move**, so when displaying scores from White's perspective, multiply by −1 when Black is to move.

2. **Stockfish uses WDL-normalized centipawns, not literal pawn-count.** Since SF 12 the conversion is:
   ```
   reported_cp = round(100 × internal_value / a)
   ```
   where `a` is a position-material-dependent constant from the engine's WDL (Win/Draw/Loss) model. The calibration point: **`+100 cp` corresponds to a 50% win probability** in engine self-play at 60+0.6s time control (CCRL 40/4-anchored). This is *not* the same as classical engines where 100 cp meant "one pawn material advantage."

| `score cp` | Interpretation (rough) |
|---|---|
| `0` | Equal / drawn |
| `±30` | Slight edge |
| `±100` | Significant advantage (≈50% win probability for the stronger side) |
| `±300` | Winning advantage |
| `±500+` | Decisive |

### `lowerbound` / `upperbound`

When you see `score cp 42 upperbound` or `score cp 42 lowerbound`, the score is a **fail-high or fail-low bound from alpha-beta search**, not an exact value. The true score is at least (lowerbound) or at most (upperbound) the reported value. These appear during iteration; the final exact score replaces them at the end of the depth.

### Tablebase scores

When Syzygy tablebases are configured and a TB result is found, Stockfish reports large cp values near ±20000:

```
score cp 19975    # TB win, 25 plies from root
score cp -19920   # TB loss, 80 plies from root
```

Formula: `cp = ±20000 − ply_distance`. The sign indicates win or loss for the side to move.

## Mate scores

```
score mate N
```

| Value | Meaning |
|---|---|
| `mate +1` | Side to move mates in 1 |
| `mate +3` | Side to move mates in 3 moves |
| `mate -2` | Side to move will be mated in 2 (opponent has forced M2) |
| `mate -1` | Side to move will be mated in 1 (every legal reply is met with checkmate on the opponent's next move) |

**Distance is in moves, not plies.** Stockfish internally tracks ply distance, then converts:
- Delivering mate: `moves = (plies + 1) / 2`
- Being mated: `moves = plies / 2`

The `mate -N` (being mated) reporting was added in **Stockfish 17**. Earlier versions only reported when the engine itself was delivering mate; being-mated positions just showed a very negative cp score.

If `go mate N` finds no mate within the requested depth, Stockfish returns the best move with a centipawn score — no error, no empty line.

## Win/Draw/Loss probabilities

Enable with `setoption name UCI_ShowWDL value true`. Then every `info` line includes:

```
info depth 18 ... score cp 58 wdl 140 859 1 ... pv d2d4 ...
```

| Field | Meaning |
|---|---|
| `wdl W` | Win probability in per-mille (0–1000), from side-to-move's perspective |
| `wdl D` | Draw probability in per-mille |
| `wdl L` | Loss probability in per-mille |

The three values sum to 1000. In the example: 14.0% win, 85.9% draw, 0.1% loss for the side to move. Calibration anchor: engine self-play at 60+0.6s, CCRL 40/4.

WDL is often more interpretable for UI display than centipawns — "85% draw, 14% win" is more honest than "+0.58" for a clearly-drawish position.

## Parsing tips

When parsing search output programmatically:

1. **Take only the deepest `multipv 1` `info` line** before `bestmove` — earlier-depth lines are intermediate iterations and should be ignored.
2. **Distinguish `cp` from `mate`** — both can appear in `score`. A mate score must always take precedence over the previous cp score.
3. **Skip `lowerbound` / `upperbound` lines** for the definitive result — wait for the unbounded one (or for `bestmove`).
4. **Handle `bestmove (none)`** — happens in checkmate or stalemate positions where there is no legal move.
5. **Convert to White-perspective** for human display: if the side to move is Black, negate the score.

Example bash parser for "best move + cp score from White's perspective":

```bash
side_to_move=$(echo "$FEN" | awk '{print $2}')   # 'w' or 'b'

last_line=$(echo "$output" | grep '^info' | grep ' multipv 1 ' \
              | grep -v 'lowerbound\|upperbound' | tail -1)

cp=$(echo "$last_line" | sed -n 's/.*score cp \(-\{0,1\}[0-9]\{1,\}\).*/\1/p')
bestmove=$(echo "$output" | grep '^bestmove' | awk '{print $2}')

if [ "$side_to_move" = "b" ]; then cp=$((-cp)); fi
echo "Best: $bestmove   White-perspective cp: $cp"
```
