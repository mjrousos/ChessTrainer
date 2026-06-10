# Installation

How to install the Stockfish CLI on each platform, pick the right binary variant, build from source, and manage the NNUE evaluation file.

## Current version

| Field | Value |
|---|---|
| Stable release | **Stockfish 18** |
| Tag | `sf_18` |
| Released | 2026-01-31 |
| Network | `nn-71d6d32cb962.nnue` (SFNNv10) — embedded in all official binaries |
| Releases page | https://github.com/official-stockfish/Stockfish/releases/tag/sf_18 |
| Download page | https://stockfishchess.org/download/ |

## Picking the right binary (x86-64)

Stockfish ships separate binaries for different CPU instruction-set levels. Pick the highest level your CPU supports.

| Variant | Min CPU |
|---|---|
| `x86-64-avx512icl` | Intel Ice Lake / AMD Zen 4 (full AVX-512) |
| `x86-64-vnni512` | AVX-512 + VNNI 512-bit |
| `x86-64-avx512` | Skylake-X+, Zen 4 |
| `x86-64-avxvnni` | Alder Lake+, Zen 4 (AVX-VNNI 256-bit) |
| `x86-64-bmi2` | Haswell/Broadwell (with PEXT) |
| **`x86-64-avx2`** | **Haswell (2013) and later — safest modern default** |
| `x86-64-sse41-popcnt` | Penryn/Nehalem (2008+) — safe choice for older CPUs |
| `x86-64-ssse3` | Core 2 era |
| `x86-64` | Generic (SSE2 only) — maximum compatibility |

> `x86-64-modern` was **deprecated** in SF 18; the Makefile now emits a warning and falls back to `sse41-popcnt`. Do not use it.

For ARM64:
- **Apple Silicon (M1–M4):** `macos-m1-apple-silicon`
- **Windows on ARM (Surface Pro X, Snapdragon X):** `windows-armv8-dotprod` (faster) or `windows-armv8` (safe)

### Checking CPU capabilities

**Windows (PowerShell):**
```powershell
[System.Runtime.Intrinsics.X86.Avx2]::IsSupported       # True / False
[System.Runtime.Intrinsics.X86.Avx512F]::IsSupported
[System.Runtime.Intrinsics.X86.Bmi2]::IsSupported
```

**Linux:**
```bash
grep -m1 flags /proc/cpuinfo | grep -oE 'avx2|avx512f|bmi2|popcnt'
```

**macOS:**
```bash
sysctl -a | grep machdep.cpu.features   # Intel
sysctl -a | grep machdep.cpu.brand_string   # Apple Silicon (always pick apple-silicon)
```

## Windows

### Direct download (recommended for latest version)

```powershell
$url = "https://github.com/official-stockfish/Stockfish/releases/download/sf_18/stockfish-windows-x86-64-avx2.zip"
Invoke-WebRequest $url -OutFile sf.zip
Expand-Archive sf.zip -DestinationPath C:\stockfish
[Environment]::SetEnvironmentVariable("PATH", $env:PATH + ";C:\stockfish", "User")
```

### Package managers

```powershell
# Chocolatey
choco install stockfish

# winget (community package; check availability)
winget search stockfish
winget install --id Stockfish.Stockfish
```

Note: community packages may lag the official release by one or more versions. Check with `echo "uci" | stockfish | findstr "id name"`.

## macOS

### Homebrew (recommended)

```bash
brew install stockfish
```

Homebrew compiles from source using the appropriate `ARCH` for the host:
- Apple Silicon → `apple-silicon` (installed to `/opt/homebrew/bin/stockfish`)
- Intel Mac → `x86-64-sse41-popcnt` or `x86-64-ssse3` (installed to `/usr/local/bin/stockfish`)

Bottles (prebuilt) are available for recent macOS versions for fast install.

Upgrade with `brew upgrade stockfish`.

### Direct download

```bash
# Apple Silicon
curl -LO https://github.com/official-stockfish/Stockfish/releases/download/sf_18/stockfish-macos-m1-apple-silicon.tar
tar xf stockfish-macos-m1-apple-silicon.tar

# Clear Gatekeeper quarantine if needed
xattr -d com.apple.quarantine stockfish
./stockfish
```

## Linux

> **Distribution packages typically lag.** Ubuntu 24.04 LTS ships SF 16; Debian/Fedora similar. For SF 18 use the direct download.

### Package managers

```bash
# Debian / Ubuntu / Mint
sudo apt update && sudo apt install stockfish

# Fedora / RHEL
sudo dnf install stockfish

# Arch Linux / Manjaro
sudo pacman -S stockfish

# openSUSE
sudo zypper install stockfish
```

### Direct download (latest SF 18)

```bash
wget https://github.com/official-stockfish/Stockfish/releases/download/sf_18/stockfish-ubuntu-x86-64-avx2.tar
tar xf stockfish-ubuntu-x86-64-avx2.tar
sudo mv stockfish /usr/local/bin/
stockfish    # interactive test
```

Pick the variant matching your CPU:
```bash
grep -m1 flags /proc/cpuinfo | grep -oE 'avx512f|avx2|bmi2|popcnt'
```

## Building from source

For maximum performance (Profile-Guided Optimization tuned to your exact CPU):

### Prerequisites

| Platform | Requirements |
|---|---|
| Linux/macOS | `g++` ≥ 9 or `clang++` ≥ 11, `make`, `git` |
| Windows | MinGW-w64, MSYS2, or WSL2 with GCC |

### Build

```bash
git clone https://github.com/official-stockfish/Stockfish.git
cd Stockfish/src

make help                                # see all ARCH and COMP options

# Auto-detect and PGO-build for the local CPU
make -j profile-build ARCH=native

# Or explicit ARCH
make -j profile-build ARCH=x86-64-avx2

# Skip PGO (faster compile, ~5% weaker binary)
make -j build ARCH=x86-64-avx2

# System install
sudo make install PREFIX=/usr/local
```

`profile-build` runs a 4-step PGO process: instrument → benchmark → rebuild with profile → cleanup. Adds a few minutes to the build but produces a noticeably faster binary.

### Compiler selection

```bash
make -j profile-build ARCH=x86-64-avx2 COMP=clang
# Options: gcc (default) | clang | mingw | icx | ndk
```

## The NNUE evaluation file

Stockfish's evaluation is a neural network stored in a `nn-<sha>.nnue` file. The filename is itself the SHA-256 checksum (first 12 hex chars) of the file — `net.sh` validates this on download.

### Embedded vs external

**All official distributed binaries embed the NNUE network** via the `INCBIN` macro. No external file is needed when running a downloaded binary.

Source builds either:
- Auto-download via `make net` → `scripts/net.sh` fetches from `https://tests.stockfishchess.org/api/nn/nn-71d6d32cb962.nnue` (primary) or `https://github.com/official-stockfish/networks/raw/master/nn-71d6d32cb962.nnue` (fallback)
- Use a local file via `setoption name EvalFile value <path>`

### File lookup order

When `EvalFile` is set (not the embedded network):

1. **Absolute path** → use as-is
2. **Just a filename** → look in the current working directory
3. **Then** the binary's containing directory

### Using a custom network

```
setoption name EvalFile value /custom/networks/nn-experimental.nnue
isready
readyok
```

The first 12 chars of the file's SHA-256 must match the filename, or the engine refuses to load it.

### Where to get networks

- All historical Stockfish networks: https://github.com/official-stockfish/networks
- Testing/development networks: https://tests.stockfishchess.org/

## Verifying the install

```bash
stockfish
```

Expected interactive transcript:

```
Stockfish 18 by T. Romstad, M. Costalba, J. Kiiski, G. Linscott

uci
id name Stockfish 18
id author the Stockfish developers (see AUTHORS file)

option name Threads type spin default 1 min 1 max 1024
option name Hash type spin default 16 min 1 max 33554432
... (many more) ...
option name EvalFile type string default nn-71d6d32cb962.nnue
uciok

quit
```

Key things to confirm:
- `id name Stockfish 18` (or newer) — confirms version
- `option name EvalFile type string default nn-...` — confirms the embedded NNUE
- `uciok` — confirms UCI compliance

Quick non-interactive smoke test:

```bash
echo "uci" | stockfish | grep "id name"
# → id name Stockfish 18
```
