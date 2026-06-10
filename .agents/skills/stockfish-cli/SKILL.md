---
name: stockfish-cli
description: "Use a powerful chess engine to analyze positions, find best moves, and evaluate game states. USE WHEN: you need to evaluate a chess position or find the best chess move given a position."
allowed-tools: Bash(stockfish:*) Bash(printf:*) Bash(echo:*) Bash(grep:*) Bash(awk:*) Bash(sed:*) Bash(tail:*) Bash(head:*) Bash(brew:*) Bash(apt:*) Bash(winget:*) Bash(choco:*)
---

# Chess analysis with stockfish

Stockfish is a free, GPL-licensed UCI (Universal Chess Interface) chess engine. Unlike a typical one-shot CLI, **Stockfish is an interactive REPL** that reads UCI commands on stdin and streams analysis on stdout. You drive it either by piping a script of commands or by running it interactively and feeding commands line by line.

## Quick start

> **Important caveat about piping commands.** When you pipe a script to Stockfish, `go` returns immediately and the engine then reads the next command. If that next command is `quit` (or stdin reaches EOF), the engine calls `stop` and the search terminates **before** reaching the requested depth/time/node limit.
>
> On Linux/macOS bash this usually still produces a useful (lower-depth) `bestmove`. **On Windows PowerShell the failure mode is much worse:** the search is frequently aborted before *any* `info` line is emitted, and the engine returns a near-instant `bestmove` from a depth-0 / depth-1 search — a wrong answer, not a low-depth answer, with no visible warning. **Do not use the pipe pattern on Windows.** Use the PowerShell co-process recipe in [references/scripted-usage.md](references/scripted-usage.md#powershell-co-process-pattern) instead.
>
> Detection rule: if `bestmove` arrives with no preceding `info depth N ...` line in the captured output, the search was aborted by EOF — switch to the co-process pattern.

```bash
# Evaluate the starting position to depth 20 — prints the best move (depth may be partial)
printf 'uci\nucinewgame\nisready\nposition startpos\ngo depth 20\nquit\n' | stockfish

# Evaluate an arbitrary FEN
FEN="r1bqkb1r/pppp1ppp/2n2n2/4p3/2B1P3/5N2/PPPP1PPP/RNBQK2R w KQkq - 4 4"
printf 'uci\nucinewgame\nisready\nposition fen %s\ngo depth 20\nquit\n' "$FEN" | stockfish

# Just the best move
printf 'position startpos moves e2e4 e7e5\ngo depth 18\nquit\n' \
  | stockfish | grep '^bestmove' | awk '{print $2}'
```

PowerShell — **do not pipe**; use the co-process helper from [references/scripted-usage.md](references/scripted-usage.md#powershell-co-process-pattern):

```powershell
# Invoke-Stockfish is defined in references/scripted-usage.md
$fen = "r1bqkb1r/pppp1ppp/2n2n2/4p3/2B1P3/5N2/PPPP1PPP/RNBQK2R w KQkq - 4 4"
Invoke-Stockfish @(
  "uci"
  "ucinewgame"
  "isready"
  "position fen $fen"
  "go depth 20"
) | Select-String "multipv 1|^bestmove" | Select-Object -Last 2
```

## How a UCI session works

Stockfish is a stateful REPL. Every analysis follows the same pattern:

1. **Handshake** — send `uci`, wait for `uciok`. The engine prints its `id`, every supported `option`, then `uciok`.
2. **Configure** (optional) — `setoption name <X> value <Y>`, then `isready` / wait for `readyok`.
3. **Reset** — `ucinewgame`, then `isready` / wait for `readyok`. Clears the transposition table between unrelated positions.
4. **Set position** — `position startpos [moves ...]` or `position fen <FEN> [moves ...]`. Does **not** start search.
5. **Search** — `go depth N` / `go movetime MS` / `go nodes N` / `go infinite`. Engine streams `info ...` lines, then `bestmove <move>`.
6. **Quit** — `quit`. (EOF on stdin also terminates the engine.)

Moves use **long algebraic notation** (`from_sq + to_sq`, promotions append the piece: `e7e8q`). SAN like `Nf3` is **not** accepted. Castling is encoded as the king's move: `e1g1`, `e1c1`, `e8g8`, `e8c8`.

## Commands

### Handshake & sync

```
uci          # Enter UCI mode. Engine prints id/options, then `uciok`
isready      # Ping. Engine replies `readyok` (always — even mid-search)
ucinewgame   # Clear hash + history. Required between unrelated positions
quit         # Exit immediately
```

> **Always** send `isready` and wait for `readyok` after `setoption`, `ucinewgame`, or `position` changes that involve loading data (Hash resize, EvalFile, SyzygyPath). The engine may need significant time to initialize.

### Set up a position (`position`)

```
position startpos
position startpos moves e2e4 e7e5 g1f3 b8c6 f1b5
position fen <FEN>
position fen <FEN> moves <m1> <m2> ...
```

Prefer `position startpos moves ...` over a FEN whenever you have the move list — the move history is required for correct threefold-repetition detection. `position` itself does **no** computation; it only sets the internal board.

### Search a position (`go` / `stop`)

```
go depth N       # Stop after iterative deepening reaches depth N
go movetime MS   # Stop after MS milliseconds of wall clock
go nodes N       # Stop after ~N nodes searched
go infinite      # Search forever — must be terminated with `stop`
go mate N        # Find a forced mate in ≤ N moves (also reports being mated in ≤ N since SF17)
go perft N       # Move-generation test: count all legal leaves at depth N (no evaluation)
go searchmoves e2e4 d2d4   # Restrict root to listed moves. MUST be the last `go` token
go               # Bare `go` runs `go depth 245` (effectively infinite)
stop             # Halt current search; engine emits final `info` + `bestmove`
```

Limits combine with OR — `go depth 15 movetime 5000` stops at depth 15 **or** 5 seconds, whichever hits first.

### Display visual game state (`d`)

```
d
```

Prints an ASCII board, the current FEN, the Zobrist key, and any checkers. Useful for sanity-checking that your `position fen ...` was parsed as you expected.

```
+---+---+---+---+---+---+---+---+
| r | n | b | q | k | b | n | r | 8
+---+---+---+---+---+---+---+---+
| p | p | p | p | p | p | p | p | 7
...
| R | N | B | Q | K | B | N | R | 1
+---+---+---+---+---+---+---+---+
  a   b   c   d   e   f   g   h

Fen: rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1
Key: 8F8F01D4562F59FB
Checkers:
```

### Static evaluation (`eval`)

```
eval
```

NNUE static evaluation with **no search** — shows per-square NNUE piece contributions and a final score. **Do not use this for serious analysis** (the wiki explicitly warns against it); use `go depth N` instead. `eval` is reported from **White's perspective**, unlike `info score` which is reported from the **side-to-move's** perspective.

### Configure (`setoption`)

```
setoption name <id> [value <x>]
```

Common options (defaults shown):

| Option | Default | Notes |
|---|---|---|
| `Threads` | 1 | CPU thread count. **Set this before Hash.** |
| `Hash` | 16 (MB) | TT size in MB. Set *after* Threads (Hash is reallocated when Threads changes). |
| `MultiPV` | 1 | Output top N PV lines. Use > 1 only for analysis (costs ~−234 Elo at 5). |
| `Clear Hash` | (button) | Wipe TT immediately. |
| `UCI_Chess960` | false | Fischer Random mode. |
| `UCI_LimitStrength` | false | Enable Elo-targeted weaker play. |
| `UCI_Elo` | 1320 | Target rating (1320–3190) when LimitStrength is on. |
| `Skill Level` | 20 | 0–20 alternative weakening (lower = weaker). |
| `UCI_ShowWDL` | false | Append `wdl W D L` to info lines. |
| `EvalFile` | `nn-<sha>.nnue` | Path/filename of NNUE network. |
| `SyzygyPath` | (empty) | `;`-sep (Win) or `:`-sep (Unix) TB directories. |
| `Move Overhead` | 10 (ms) | Assumed GUI/network latency. |
| `Debug Log File` | (empty) | Path to capture all UCI I/O for debugging. |

See [references/configuration.md](references/configuration.md) for tuning guidance (when to raise Threads/Hash, MultiPV trade-offs, Syzygy setup).

### Quit (`quit`)

```
quit
```

Exits the process immediately. Closing stdin (EOF) has the same effect.

## Engine output format

During search the engine streams `info` lines, then terminates with `bestmove`:

```
info depth 20 seldepth 30 multipv 1 score cp 24 nodes 1834622 nps 532715 hashfull 261 tbhits 0 time 3444 pv e1g1 d7d6 d2d3 f8e7 b1c3 e8g8
bestmove e1g1 ponder d7d6
```

| Token | Meaning |
|---|---|
| `depth N` | Iterative deepening depth (plies) |
| `seldepth N` | Max selective depth (incl. quiescence/extensions) |
| `multipv N` | PV rank (1 = best); > 1 only when `MultiPV` > 1 |
| `score cp N` | Centipawns from **side-to-move's** perspective |
| `score mate N` | Mate in N moves: `+N` = mating, `−N` = being mated |
| `wdl W D L` | Win/Draw/Loss probability in per-mille (only with `UCI_ShowWDL true`) |
| `nodes N` / `nps N` | Cumulative nodes / nodes per second |
| `hashfull N` | TT occupancy in per-mille (1000 = 100%) |
| `tbhits N` | Syzygy tablebase hits |
| `time MS` | Elapsed ms since `go` |
| `pv <m1> <m2> ...` | Principal variation (best continuation) |
| `upperbound` / `lowerbound` | Score is a fail-high/fail-low bound, not exact |

**Score convention:** Scores are always relative to the **side to move**. `+24 cp` means whoever's turn it is has a +0.24 advantage. Stockfish uses WDL-normalized centipawns where `+100 cp ≈ 50% win probability` (not literally "one pawn"). See [references/scoring.md](references/scoring.md) for full details and mate-distance rules.

When parsing output for the definitive result, take the **last** `info ... multipv 1 ...` line before `bestmove`. Earlier lines are intermediate iterations.

## Example: evaluate one position end-to-end

```bash
FEN="r1bqkb1r/pppp1ppp/2n2n2/4p3/2B1P3/5N2/PPPP1PPP/RNBQK2R w KQkq - 4 4"

printf '%s\n' \
  uci \
  ucinewgame \
  isready \
  "position fen $FEN" \
  "go depth 20" \
  quit \
  | stockfish
```

Output ends with:

```
info depth 20 seldepth 30 multipv 1 score cp 24 nodes 1834622 nps 532715 hashfull 261 tbhits 0 time 3444 pv e1g1 d7d6 d2d3 f8e7 b1c3 e8g8 h2h3 a7a6
bestmove e1g1 ponder d7d6
```

→ Best move is `e1g1` (castle short). Score `+0.24` from White's view (White to move).

## Example: top N candidate moves (MultiPV)

```bash
FEN="r1bqkb1r/pppp1ppp/2n2n2/4p3/2B1P3/5N2/PPPP1PPP/RNBQK2R w KQkq - 4 4"

printf '%s\n' \
  uci \
  "setoption name MultiPV value 5" \
  isready \
  ucinewgame \
  isready \
  "position fen $FEN" \
  "go depth 18" \
  quit \
  | stockfish | grep " multipv " | tail -5
```

```
info depth 18 ... multipv 1 score cp 26 ... pv e1g1 d7d6 d2d3 ...
info depth 18 ... multipv 2 score cp 18 ... pv d2d3 d7d6 b1c3 ...
info depth 18 ... multipv 3 score cp 11 ... pv b1c3 d7d6 d2d3 ...
info depth 18 ... multipv 4 score cp  8 ... pv c4b3 d7d6 d2d3 ...
info depth 18 ... multipv 5 score cp  5 ... pv d2d4 e5d4 f3d4 ...
```

Lower MultiPV ranks are weaker alternatives, ordered best-to-worst.

## Example: mate search

```bash
# Fool's Mate setup — Black to move, mates in 1
printf 'position startpos moves g2g4 e7e5 f2f3\ngo mate 1\nquit\n' | stockfish
# → score mate 1 pv d8h4   bestmove d8h4
```

`score mate +N` = side-to-move delivers mate in N. `score mate -N` = side-to-move is being mated in N (reporting added in SF 17). Distance is in **moves**, not plies.

## Example: time-budgeted analysis

```bash
# 2 seconds wall clock (good interactive default)
printf 'position startpos\ngo movetime 2000\nquit\n' | stockfish

# 1M nodes (deterministic, hardware-independent budget)
printf 'position startpos\ngo nodes 1000000\nquit\n' | stockfish

# Open-ended infinite search, terminated explicitly
( printf 'position startpos\ngo infinite\n'; sleep 5; printf 'stop\nquit\n' ) | stockfish
```

## Example: batch analysis of many positions

When analyzing many positions, **reuse a single Stockfish process** — each cold start re-initializes NNUE. Send `ucinewgame` + `isready` between positions to clear stale TT entries:

```bash
{
  echo "uci"
  echo "setoption name Threads value 4"
  echo "setoption name Hash value 256"
  echo "isready"
  for FEN in "${FENS[@]}"; do
    echo "ucinewgame"
    echo "isready"
    echo "position fen $FEN"
    echo "go depth 18"
  done
  echo "quit"
} | stockfish
```

## Common pitfalls

1. **Always `isready` after `setoption` / `ucinewgame`** — Hash resize, Threads change, EvalFile load, and Syzygy probing can all take noticeable time. Send a command before `readyok` and the engine may not have processed your config yet.
2. **`ucinewgame` between unrelated positions** — without it, TT entries from a previous position leak into the next analysis and can subtly skew scores.
3. **`position` does NOT start a search** — only `go` does. Multiple `position` commands without `go` just overwrite each other.
4. **`stop` is required to end `go infinite`** — the engine will never terminate the search on its own.
5. **Move notation is long algebraic only** (`e2e4`, `g1f3`, `e7e8q`). SAN (`e4`, `Nf3`) is **not** accepted.
6. **Malformed FENs can crash the engine** — modern Stockfish prints `info string CRITICAL ERROR: ...` and calls `std::exit(1)` for invalid FEN syntax. **Illegal moves in `position ... moves ...` are different** — they are silently truncated at the first invalid move, with no error, leaving you analyzing a position different from what you intended. Validate FENs *and* check the resulting position with `d` in automation.
7. **`searchmoves` must be the last `go` token** — it greedily consumes all remaining tokens as move names.
8. **Score perspective differs by command** — `info score` is from side-to-move's view; `eval` is from White's view. Don't mix them up.
9. **Set `Threads` before `Hash`** — Hash is reallocated when Threads changes, so setting Hash first wastes the work.
10. **Don't use `eval` for analysis** — it has no search and the wiki explicitly recommends against it. Use `go depth N` instead.

## Installation

If `stockfish` is not on `PATH`, install with the platform's package manager. See [references/installation.md](references/installation.md) for full details (architecture-specific binaries, build-from-source with PGO, NNUE network file handling).

```bash
# macOS
brew install stockfish

# Windows
winget install --id Stockfish.Stockfish
# or
choco install stockfish

# Ubuntu / Debian (often lags upstream — use direct download for SF 18)
sudo apt install stockfish

# Verify
echo "uci" | stockfish | grep "id name"
# → id name Stockfish 18
```

For the latest version, download a per-architecture binary directly from the [Stockfish 18 release page](https://github.com/official-stockfish/Stockfish/releases/tag/sf_18). On x86-64 the safe default is `*-x86-64-avx2.*` (works on any CPU from 2013 onward).

## Reference topics

* **Full UCI protocol reference** — every command, output token, and option [references/uci-protocol.md](references/uci-protocol.md)
* **Score interpretation** — centipawn normalization, mate distance, WDL, sign conventions [references/scoring.md](references/scoring.md)
* **Engine configuration & tuning** — Threads/Hash sizing, MultiPV trade-offs, Syzygy, NUMA, Skill/Elo limiting [references/configuration.md](references/configuration.md)
* **Scripted & batch usage** — bash/PowerShell parsing patterns, persistent-process workflows, output parsing recipes [references/scripted-usage.md](references/scripted-usage.md)
* **Installation deep-dive** — per-platform binaries, build-from-source with PGO, NNUE file handling [references/installation.md](references/installation.md)
