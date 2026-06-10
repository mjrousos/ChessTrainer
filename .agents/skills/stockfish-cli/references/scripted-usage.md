# Scripted & Batch Usage

How to drive Stockfish from shell scripts, parse its output, and run efficient batch analysis.

## A critical caveat first

**`go ... \n quit` in a piped script can terminate the search early.** When stdin is piped to Stockfish, `go` returns immediately, then the engine reads the next command. If that next command is `quit` (or stdin reaches EOF), the engine calls `stop` and the search ends before reaching the requested depth/time/node limit. The engine still emits `bestmove`, but at a lower depth than you asked for.

**On Windows PowerShell this failure mode is much worse than on bash.** In practice the pipe is closed so quickly that the engine often emits *zero* `info` lines and returns a near-instant `bestmove` from a depth-0/1 search — a wrong answer, not a low-depth answer. There is no error message. **Do not use the pipe pattern on Windows; use the PowerShell co-process helper below.**

**Detection rule (any platform):** if your captured output contains `bestmove ...` but no preceding `info depth N ...` line, the search was aborted by EOF. Switch to the co-process pattern and retry.

The truly reliable pattern is the **co-process** approach (further down) — it explicitly waits for the `bestmove` line before sending `quit`. Use the simple one-shot pipes below only when:

- An approximate / partial-depth result is acceptable, **or**
- You use `go infinite` + an explicit sleep + `stop` (giving the search known wall-clock budget), **or**
- You can rely on EOF-driven natural termination because `bestmove` is emitted before stdin closes for fast searches.

## Two basic patterns

### One-shot pipe (approximate)

```bash
printf 'uci\nucinewgame\nisready\nposition fen %s\ngo depth 20\nquit\n' "$FEN" \
  | stockfish
```

Simple, hands-off. Engine starts, runs the script, exits. The reported depth may be lower than requested per the caveat above. Good enough for casual analysis; use the co-process pattern for anything where the actual depth matters.

### Wall-clock-bounded pipe (deterministic)

```bash
# Search for exactly 2 seconds, then stop and quit
( printf 'uci\nucinewgame\nisready\nposition fen %s\ngo infinite\n' "$FEN"
  sleep 2
  printf 'stop\nquit\n'
) | stockfish
```

The `sleep` guarantees the search runs for the full duration before `stop` is read. Reliable for fixed-time analysis.

### Persistent process (for batches)

For multiple positions, **don't spawn a new process per position** — each cold start re-initializes NNUE (tens to hundreds of ms wasted). Run one Stockfish process and feed it `ucinewgame` + `isready` between positions:

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

For truly long-running interactive control (e.g., a daemon), use a co-process or FIFOs — see "Co-process pattern" below.

## Output parsing

### Extract just the best move

```bash
bestmove=$(
  printf 'position fen %s\ngo depth 18\nquit\n' "$FEN" \
    | stockfish | grep '^bestmove' | awk '{print $2}'
)
echo "$bestmove"   # e.g. e2e4
```

### Extract the final score

```bash
cp=$(
  printf 'position fen %s\ngo depth 18\nquit\n' "$FEN" \
    | stockfish \
    | grep '^info' | grep ' multipv 1 ' \
    | grep -v 'lowerbound\|upperbound' \
    | tail -1 \
    | sed -n 's/.*score cp \(-\{0,1\}[0-9]\{1,\}\).*/\1/p'
)
echo "$cp"   # e.g. -34
```

### Detect mate vs centipawn scores

```bash
last=$(echo "$output" | grep '^info' | grep ' multipv 1 ' \
         | grep -v 'lowerbound\|upperbound' | tail -1)

if [[ "$last" =~ score\ mate\ (-?[0-9]+) ]]; then
  echo "MATE in ${BASH_REMATCH[1]}"
elif [[ "$last" =~ score\ cp\ (-?[0-9]+) ]]; then
  echo "CP ${BASH_REMATCH[1]}"
fi
```

### Extract top-N moves from MultiPV output

```bash
printf '%s\n' \
  "setoption name MultiPV value 5" \
  isready \
  "position fen $FEN" \
  "go depth 18" \
  quit \
  | stockfish \
  | grep ' multipv ' \
  | awk '
      {
        for (i=1; i<=NF; i++) {
          if ($i == "depth")   d = $(i+1)
          if ($i == "multipv") m = $(i+1)
          if ($i == "score")  { st = $(i+1); sv = $(i+2) }
          if ($i == "pv")     { pv = ""; for (j=i+1; j<=NF; j++) pv = pv " " $j; break }
        }
        if (d > maxd) maxd = d
        line[d, m] = m " " st " " sv " pv" pv
      }
      END { for (k=1; k<=5; k++) print line[maxd, k] }
    '
```

## PowerShell equivalents

> **Do not use `$cmds | stockfish` on Windows.** The pipe closes stdin so fast that Stockfish typically returns a `bestmove` from a depth-0/1 search with zero `info` lines — a silently wrong answer. Use the `Invoke-Stockfish` helper below, which is the PowerShell equivalent of the bash co-process pattern: it writes commands to stdin, then reads stdout until `bestmove` arrives before sending `quit`.

```powershell
function Invoke-Stockfish {
  <#
    .SYNOPSIS Drive a one-shot Stockfish search and return all engine output lines.
    .PARAMETER Commands   UCI commands to send (do NOT include 'quit' — the function adds it).
    .PARAMETER TimeoutMs  Max wall-clock to wait for 'bestmove' before giving up.
    .PARAMETER ExePath    Stockfish executable (defaults to 'stockfish' on PATH).
  #>
  param(
    [Parameter(Mandatory)][string[]]$Commands,
    [int]$TimeoutMs = 60000,
    [string]$ExePath = 'stockfish'
  )
  $psi = [System.Diagnostics.ProcessStartInfo]@{
    FileName               = $ExePath
    RedirectStandardInput  = $true
    RedirectStandardOutput = $true
    UseShellExecute        = $false
    CreateNoWindow         = $true
  }
  $p = [System.Diagnostics.Process]::Start($psi)
  try {
    foreach ($c in $Commands) { $p.StandardInput.WriteLine($c) }
    $p.StandardInput.Flush()

    $lines    = [System.Collections.Generic.List[string]]::new()
    $deadline = (Get-Date).AddMilliseconds($TimeoutMs)
    while (-not $p.StandardOutput.EndOfStream) {
      if ((Get-Date) -gt $deadline) { throw "Stockfish timed out waiting for bestmove" }
      $l = $p.StandardOutput.ReadLine()
      if ($null -ne $l) {
        $lines.Add($l)
        if ($l -like 'bestmove*') { break }
      }
    }
    $lines
  }
  finally {
    if (-not $p.HasExited) { $p.StandardInput.WriteLine('quit'); $p.WaitForExit(2000) | Out-Null }
    if (-not $p.HasExited) { $p.Kill() }
  }
}

# One-shot evaluation
$fen = "r1bqkb1r/pppp1ppp/2n2n2/4p3/2B1P3/5N2/PPPP1PPP/RNBQK2R w KQkq - 4 4"
$output = Invoke-Stockfish @(
  "uci"
  "setoption name MultiPV value 1"
  "ucinewgame"
  "isready"
  "position fen $fen"
  "go depth 20"
)

# Sanity-check: did the search actually run?
if (-not ($output | Where-Object { $_ -match '^info depth ' })) {
  throw "Stockfish returned bestmove with no info lines — search was aborted."
}

# Best move
$bestmove = ($output | Where-Object { $_ -match '^bestmove' }) -split '\s+' | Select-Object -Index 1

# CP / mate score from final multipv-1 info line
$scoreLine = $output | Where-Object {
  $_ -match '^info' -and $_ -match ' multipv 1 ' -and $_ -notmatch 'upperbound|lowerbound'
} | Select-Object -Last 1

if ($scoreLine -match 'score (cp|mate) (-?\d+)') {
  $kind = $matches[1]; $val = $matches[2]
  Write-Host "Best: $bestmove   $kind $val"
}
```

For batch analysis on Windows, keep one `System.Diagnostics.Process` alive across positions and send `ucinewgame` + `isready` between them — the same persistent-process principle as the bash example above. The `Invoke-Stockfish` helper spawns a fresh process per call, so it's fine for ad-hoc analysis but inefficient for large batches.

## Co-process pattern (long-running engine, send & receive)

For interactive control of a persistent engine from bash:

```bash
coproc SF { stockfish; }

send() { printf '%s\n' "$@" >&"${SF[1]}"; }
read_until() {
  local marker=$1 line
  while IFS= read -r line <&"${SF[0]}"; do
    echo "$line"
    [[ "$line" == "$marker"* ]] && break
  done
}

send "uci"
read_until "uciok"

send "setoption name Threads value 4" "isready"
read_until "readyok"

for FEN in "${FENS[@]}"; do
  send "ucinewgame" "isready"
  read_until "readyok"
  send "position fen $FEN" "go depth 18"
  read_until "bestmove"   # contains both final info line and bestmove
done

send "quit"
wait "$SF_PID"
```

## Tips

1. **Always include `isready` after `setoption` and `ucinewgame`** — without it, you may send `position`/`go` before the engine has finished initializing, leading to silently wrong results or crashes.
2. **Reuse one engine process** for batches — saves NNUE init per position.
3. **Pin `Threads` and `Hash` once at startup** — don't change them per position unless necessary (changes trigger reallocation).
4. **Wait for `bestmove` to know a search ended** — `bestmove` is the only guarantee that all `info` lines have been emitted.
5. **Validate that the search actually ran**: if your captured output contains a `bestmove` line but no preceding `info depth N ...` line, the search was aborted by EOF before any iteration completed. The `bestmove` is meaningless. Switch to the co-process pattern (PowerShell users: `Invoke-Stockfish` above).
6. **Use `Debug Log File`** to capture full UCI I/O when debugging:
   ```
   setoption name Debug Log File value /tmp/sf.log
   ```
7. **Validate FENs before sending** — modern Stockfish exits with `std::exit(1)` on illegal FENs / illegal UCI moves, killing your batch.
8. **Use `nodes` instead of `movetime` for reproducibility** — wall-clock searches vary by hardware load; `go nodes N` is deterministic.
9. **Set sensible timeouts** in your driver — if Stockfish hangs (very rare), you don't want your script to hang forever waiting for `bestmove`.
