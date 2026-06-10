# Engine Configuration & Tuning

Practical guidance for setting `Threads`, `Hash`, `MultiPV`, Syzygy paths, NUMA policy, and Skill/Elo limiting.

## Set Threads before Hash

```
> setoption name Threads value 8       # command sent to engine
> isready                                # command sent to engine
< readyok                                # ← engine response (do NOT send)
> setoption name Hash value 4096
> isready
< readyok
```

> The `> ` prefix indicates commands you send; `< ` indicates engine responses. In a script piped to `stockfish`, send only the `>` lines.

**Always set `Threads` first, then `Hash`.** The hash table is reallocated when Threads changes (to preserve per-thread hash ratios), so setting Hash first wastes the work. This is explicitly documented in the Stockfish wiki.

## Threads

- **Default:** 1
- **Range:** 1 – 1024
- **Recommendation:** `logical_cores − 1` (leave one for OS/GUI/network). Hyperthreading/SMT helps — use the SMT thread count, not just physical cores.
- **Elo gain:** ~178 Elo from 1 → 8 threads at long time control. Threading is roughly 82% efficient (8× threads ≈ 6.6× effective search power). Still > 50% efficient at 64+ threads on long searches.

## Hash

- **Default:** 16 MB
- **Range:** 1 – 33,554,432 MB (32 TB on 64-bit). In practice, set to what fits in RAM.
- **Recommended sizes:**

| Time control | Threads | Hash |
|---|---|---|
| Ultra-bullet (10s + 0.1s) | 1 | 16 MB (default) |
| Bullet (60s + 0.6s) | 1 | 64 MB |
| Blitz (3+2) | 1 | 192 MB |
| Long analysis (hours) | many | as much as RAM allows |

- **Rule of thumb:** scale Hash linearly with thread count — `Hash = base_MB × thread_count`.
- **Keep `hashfull` below 300‰ (30%)** for best strength. The Stockfish FAQ: "the data suggests keeping the average hashfull below 30% is best to maintain strength." Watch the `hashfull <N>` token in `info` output.
- **Large pages**: Stockfish automatically uses OS large/huge pages when available, accelerating TT access.

## MultiPV

```
setoption name MultiPV value 5
```

- **Default:** 1 (best for playing strength)
- **Range:** 1 – 218 (theoretical max legal moves) / 1 – 500 depending on version
- **Cost:** roughly **−234 Elo at MultiPV=5** vs MultiPV=1 in self-play (the engine spends search time on suboptimal lines). Use only for analysis, never for actual play.
- **Parsing:** at each depth, Stockfish emits one `info ... multipv N ...` line per rank (1 = best, ordered best-to-worst). Take only the lines from the **final** depth for the definitive ranking.

## Skill Level vs UCI_Elo

Two mutually-exclusive ways to weaken the engine:

### `Skill Level` (0–20, default 20)

```
setoption name Skill Level value 5
```

Lower values play weaker by internally enabling MultiPV and probabilistically picking a non-best move. Crude but simple. Overridden when `UCI_LimitStrength` is on.

### `UCI_LimitStrength` + `UCI_Elo` (1320–3190, default 1320)

```
setoption name UCI_LimitStrength value true
setoption name UCI_Elo value 1800
```

Target a specific Elo rating, calibrated at 120s+1s anchored to CCRL 40/4. Much more accurate than `Skill Level` for matching human opponents.

## Syzygy tablebases

For perfect endgame play with ≤ 7 pieces (incl. kings):

```
# Windows: semicolons; Unix: colons. No spaces around separator.
setoption name SyzygyPath value C:\tb\3-4-5;C:\tb\6
isready
readyok
```

Related options:

| Option | Default | Notes |
|---|---|---|
| `SyzygyPath` | (empty) | Directories containing `.rtbw`/`.rtbz` files |
| `SyzygyProbeDepth` | 1 | Minimum remaining search depth before probing. Raise to reduce NPS impact. |
| `Syzygy50MoveRule` | true | Set false for ICCF correspondence (TB wins ignore 50-move rule) |
| `SyzygyProbeLimit` | 7 | Max piece count (incl. kings + pawns) — set < 7 to skip 6/7-piece probes |

**Watch for `tbhits N` in `info` output** to confirm probes are happening. Always validate tablebase files with `md5sum -c checksum.md5` after download — corruption causes engine crashes.

## NUMA policy (multi-socket systems)

```
setoption name NumaPolicy value hardware
```

Values:

| Value | Behavior |
|---|---|
| `auto` | Default; detect and bind only if useful |
| `none` | Let OS schedule freely |
| `system` | Use OS-reported NUMA topology |
| `hardware` | Override OS, use raw hardware topology — fixes Windows 10 / ChessBase thread under-utilization |
| `0-7:8-15` | Custom: thread groups bound to listed logical-CPU ranges |

Only matters on multi-CPU workstations or NUMA-aware servers. Single-socket: leave `auto`.

## Move Overhead

```
setoption name Move Overhead value 50
```

- **Default:** 10 ms
- **Range:** 0 – 5000 ms
- **What it does:** Stockfish subtracts this from its allotted think time to account for GUI/network latency. Increase for network play, GUIs like Arena or ChessGUI, or heavily loaded systems. Otherwise the engine may time out by a few ms.

## EvalFile / EvalFileSmall

```
setoption name EvalFile value /custom/path/nn-experimental.nnue
isready
readyok
```

- **Default:** the embedded network's filename (e.g. `nn-71d6d32cb962.nnue` for SF 18)
- Stockfish locates the file: embedded data → absolute path → CWD → binary directory
- Validates SHA-256 (first 12 hex chars must match the filename)
- Distributed binaries embed the network — only relevant for source builds or experimenting with custom networks (see [official-stockfish/networks](https://github.com/official-stockfish/networks))

`EvalFileSmall` (SF 16+) names a smaller/faster network used for shallow nodes. Same loading rules.

## Debug logging

```
setoption name Debug Log File value /tmp/stockfish.log
```

Captures **all UCI I/O in both directions** to the named file. Essential for debugging automation that talks to Stockfish.

## Complete configuration recipe

For deep analysis on a 16-core / 32 GB machine (lines marked `<` are engine responses — send only the `>` lines):

```
> uci
< ... id name / options ... uciok
> isready
< readyok
> setoption name Threads value 15
> isready
< readyok
> setoption name Hash value 16384
> isready
< readyok
> setoption name MultiPV value 1
> setoption name UCI_ShowWDL value true
> setoption name SyzygyPath value /tb/3-4-5
> isready
< readyok
> ucinewgame
> isready
< readyok
> position fen <FEN>
> go depth 35
< ... info lines streaming ... bestmove <move>
```
