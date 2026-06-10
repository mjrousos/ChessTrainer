# UCI Protocol Reference

Complete reference for the Universal Chess Interface (UCI) commands Stockfish accepts and the output it produces. The canonical upstream source is the [Stockfish wiki UCI & Commands page](https://official-stockfish.github.io/docs/stockfish-wiki/UCI-&-Commands.html).

## Protocol basics

- **Line-based plain text** on stdin/stdout. One command per line.
- **Case-insensitive** for command/option names. Option *string values* (e.g. `bench` parameters, file paths) are case-sensitive.
- **Lines beginning with `#` are ignored** (comments).
- **Unknown commands** print `Unknown command: '<cmd>'. Type help for more information.`
- **`debug` and `register`** from the UCI spec are **not implemented** in Stockfish.
- **EOF on stdin** is treated as `quit`.

## Standard UCI commands

### `uci`

Enter UCI mode. Engine responds with `id name`, `id author`, all `option ...` lines, then `uciok`.

```
uci
id name Stockfish 18
id author the Stockfish developers (see AUTHORS file)

option name Threads type spin default 1 min 1 max 1024
option name Hash type spin default 16 min 1 max 33554432
option name MultiPV type spin default 1 min 1 max 218
option name EvalFile type string default nn-71d6d32cb962.nnue
... (many more) ...
uciok
```

### `isready`

Ping. Engine **always** replies `readyok` — even during a search (it does not interrupt the search). Use this to synchronize after `setoption`, `ucinewgame`, or any command that may trigger initialization work.

### `ucinewgame`

Signals that the next position will be from a different game. Clears the transposition table and per-thread search history. **May take noticeable time** — always follow with `isready` and wait for `readyok`.

### `setoption name <id> [value <x>]`

Change an engine option. For `button` type options, omit `value`. **Best practice: send `stop` and wait for `bestmove` before changing options during an active search** — only the `Threads` option has internal synchronization with the search; other options (including `Hash`) apply immediately and can race against the running search. After `setoption`, send `isready` and wait for `readyok` to ensure any initialization work (Hash resize, EvalFile load, Syzygy probing) has completed before sending the next `position`/`go`.

```
setoption name Threads value 8
setoption name Hash value 4096
setoption name MultiPV value 5
setoption name UCI_ShowWDL value true
setoption name SyzygyPath value C:\tb\3-4-5;C:\tb\6
setoption name Clear Hash
```

Unknown option names produce `No such option: <name>`.

### `position`

```
position [fen <FEN> | startpos] [moves <m1> <m2> ...]
```

Set the internal board state. Does **not** trigger search.

- `startpos` → standard starting position (`rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1`)
- `fen <FEN>` → arbitrary FEN
- `moves <m1> ...` → optional list of moves in long algebraic notation, applied to the base position

Prefer `position startpos moves ...` over a FEN when you have the move list — the history enables correct threefold-repetition detection.

### `go`

Start a search. Full syntax:

```
go [searchmoves <m1>...<mi>] [ponder]
   [wtime <ms>] [btime <ms>] [winc <ms>] [binc <ms>] [movestogo <n>]
   [depth <n>] [nodes <n>] [mate <n>] [movetime <ms>]
   [infinite] [perft <n>]
```

**Limits OR together** — the search ends when any one of them is reached. `go depth 15 movetime 5000` stops at depth 15 *or* 5 seconds, whichever first. Bare `go` (no parameters) runs `go depth 245` — effectively infinite.

| Subcommand | Behavior |
|---|---|
| `depth N` | Stop after iterative deepening reaches depth N. |
| `nodes N` | Stop after approximately N nodes searched (may slightly overshoot). |
| `movetime MS` | Stop after MS ms of wall clock (may overshoot by one iteration). |
| `infinite` | Search until `stop` is received. Engine will not exit on its own. |
| `mate N` | Find a forced mate in ≤ N moves. Since SF 17 also stops when *being mated* in ≤ N. |
| `perft N` | Walk the move-generation tree to depth N; print per-move leaf counts. No evaluation. |
| `wtime` / `btime` | Remaining clock time in ms for each color. |
| `winc` / `binc` | Increment per move in ms. |
| `movestogo N` | Moves until next time control (sudden death if absent). |
| `searchmoves <m1>...` | Restrict root to listed moves only. **Must be the last `go` token** — greedily consumes all remaining tokens. |
| `ponder` | Search in pondering mode; stop on `ponderhit` or `stop`. |

### `stop`

Halt the running search. Engine emits one final `info` line followed by `bestmove`.

### `ponderhit`

The user played the predicted ponder move. Engine continues searching but switches from ponder mode to normal timed search (and uses already-accumulated work).

### `quit`

Exit the process immediately.

## Stockfish-specific (non-standard) commands

> These commands **must not** be used during a search — Stockfish provides no protection.

### `d`

Display the current board as ASCII art, plus FEN, Zobrist key, and active checkers.

### `eval`

Static NNUE evaluation, no search. Prints per-square piece contributions, NNUE bucket breakdown, and final score. **Reported from White's perspective** (unlike `info score`, which is side-to-move's perspective). The wiki explicitly recommends *against* using `eval` for analysis — use `go` instead.

### `bench [ttSize] [threads] [limit] [fenFile] [limitType]`

Run a fixed benchmark over a built-in set of positions. The total node count is a **fingerprint** of the binary's search algorithm (used by Fishtest workers). Defaults: `16 1 13 default depth`.

```
stockfish bench                       # run with defaults
stockfish bench 16 1 1 current depth  # current position, depth 1
stockfish bench 16 1 5 current perft  # perft 5 on current position
```

> **String parameters are case-sensitive** with no fallback or error on invalid input.

### `speedtest [threads] [hash_MiB] [runtime_s]`

Realistic hardware NPS benchmark (SF 17.1+). Defaults: all available threads, `threads × 128` MiB hash, 150 s.

### `compiler`

Print the compiler, architecture, and build settings used to produce this binary.

### `export_net [bigNet] [smallNet]`

Write the loaded NNUE network(s) to file. If the embedded network is loaded and no filename is given, saves to the default name. If a custom `EvalFile` is active, filename is required.

### `flip`

Flip the side to move in the current position (debugging utility).

### `help` / `--help` / `license` / `--license`

All four aliases print version info, GPL license summary, and the GitHub README link.

## Output reference

### Handshake / sync responses

| Response | Trigger | Meaning |
|---|---|---|
| `id name <name>` | `uci` | Engine name and version |
| `id author <author>` | `uci` | Engine author |
| `option name <id> type <t> [default <x>] [min <x>] [max <x>] [var <x> ...]` | `uci` | One per supported option |
| `uciok` | `uci` | Engine has finished reporting and is ready for `setoption` / `isready` |
| `readyok` | `isready` | Engine has finished any pending initialization |

### `info` lines (streamed during search)

```
info depth <d> seldepth <sd> multipv <n>
     score cp <val> [upperbound|lowerbound]
     [wdl <W> <D> <L>]
     nodes <n> nps <n> hashfull <n> tbhits <n> time <ms>
     pv <move1> ... <moveN>
```

| Token | Meaning |
|---|---|
| `depth N` | Iterative deepening iteration (plies) |
| `seldepth N` | Max selective depth (incl. quiescence/extensions) |
| `multipv N` | PV rank — 1 = best; only > 1 when `MultiPV` > 1 |
| `score cp N` | Centipawns from **side-to-move's** perspective |
| `score mate N` | Mate in N moves (`+N` = mating, `−N` = being mated) |
| `upperbound` / `lowerbound` | Score is a fail-high / fail-low bound, not exact |
| `wdl W D L` | Win/Draw/Loss in per-mille (0–1000). Only when `UCI_ShowWDL = true` |
| `nodes N` | Cumulative nodes searched since `go` |
| `nps N` | Nodes per second |
| `hashfull N` | TT occupancy in per-mille (1000 = 100% full) |
| `tbhits N` | Syzygy tablebase hits |
| `time MS` | Elapsed ms since `go` |
| `pv <moves>` | Principal variation (best continuation) |
| `currmove <m>` | Current root move being searched |
| `currmovenumber N` | Index of current root move |
| `string <text>` | Informational message (e.g. `info string NNUE evaluation using nn-XXX.nnue`) |

### `bestmove` (terminates every search)

```
bestmove <move> [ponder <move>]
```

`ponder <move>` is the expected reply (for pondering). If there is no legal move, the engine emits `bestmove (none)`.

## Notable quirks

1. **`go` default is `depth 245`**, not `infinite`. For interactive analysis use `go infinite` + `stop`.
2. **`searchmoves` must be last** — it consumes all remaining tokens as move names.
3. **`bench` string parameters are case-sensitive** with undefined behavior on bad input.
4. **`isready` during search** returns `readyok` immediately without stopping the search.
5. **`setoption` is synchronous** — waits for the in-flight search to finish.
6. **`debug` and `register`** are defined in the UCI spec but not implemented in Stockfish.
7. **Malformed FEN syntax** triggers a critical-error log and `std::exit(1)` in modern Stockfish. **Illegal moves in `position ... moves ...` are a separate case**: they are silently truncated at the first invalid move with no diagnostic — the engine then analyzes whatever earlier position the truncated move list produced. Always validate both, and verify with `d` after sending `position` in automation.
8. **CLI args are one-shot**: `stockfish bench` runs and exits. Bare `stockfish` enters the interactive REPL.
