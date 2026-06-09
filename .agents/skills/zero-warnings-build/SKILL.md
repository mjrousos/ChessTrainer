---
name: zero-warnings-build
description: >-
  **WORKFLOW SKILL** — Run the full zero-warnings build verification for ChessTrainer
  (Debug + Release + webpack-prod) and report every warning AND error with
  file/line/code so the agent can fix the underlying cause. Enforces the repo
  policy of zero warnings and zero errors on every build; suppressions require
  explicit user approval.
  USE FOR: zero warnings, no warnings, fix warnings, fix build errors, build is
  failing, release build failure, TreatWarningsAsErrors, treat warnings as errors,
  verify build is clean before PR, pre-PR validation, webpack compiled with N
  warnings, EF Core warning, full build check, run Debug and Release builds.
  DO NOT USE FOR: a single targeted build of one project (use `dotnet build` on
  that project directly), runtime warnings from a running app (those come from
  smoke tests, not builds), or suppressing/silencing warnings (the project policy
  in AGENTS.md bans suppressions without explicit user approval).
allowed-tools: shell
---

# zero-warnings-build

Verifies the ChessTrainer solution builds cleanly per the repo's zero-warnings, zero-errors policy. Runs all three required builds, parses each one's output, and emits a structured diagnostic report covering both warnings and errors.

## When to use

Run this skill whenever you need to:
- Verify the solution is clean (no warnings, no errors) before opening or merging a PR.
- Investigate why Release is failing while Debug succeeds (almost always a warning that became an error via `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` in `Directory.Build.props`).
- Triage a build failure — the report surfaces structured errors with file/line/code, and the script's safety net captures unparsed failures too (see "Edge cases" below).
- Get a consolidated list of every warning and error across the back-end and front-end after a refactor or dependency bump.
- Confirm a fix actually eliminated a diagnostic (rerun and check the report shrinks).

Do **not** use it for single-project builds — `dotnet build src/<Project>/<Project>.csproj` is faster and more focused.

## Workflow

1. Run the bundled script from the repo root:

   ```powershell
   pwsh ./.agents/skills/zero-warnings-build/check-warnings.ps1
   ```

2. Read the JSON report on stdout. Each entry has `Build` (Debug/Release/Webpack), `Level` (`warning` or `error`), `Code` (e.g. `CS8602`, `EF1002`, `NU1101`, or `UNPARSED` for the safety-net fallback), `File`, `Line`, `Column`, `Message`, and `Project`. The `summary` block shows per-stage exit code plus warning and error counts.

3. For each entry, **fix the underlying cause** per the policy in [`AGENTS.md`](../../../AGENTS.md#never-silence-warnings):
   - Compiler/analyzer (CS####, CA####, SA####, IDE####) → fix the code; common patterns include adding `OrderBy` to EF queries, tightening nullable annotations, or following the StyleCop rule the warning points to.
   - EF Core (`EF[10102]`, `EF[10103]`, etc.) → usually a missing `OrderBy` on a tracked query or a query-translation issue; restructure the LINQ.
   - NuGet errors (NU####) → restore/feed issues; check `dotnet restore` separately and verify package sources.
   - Webpack warnings → almost always our source (legacy `node_modules` SCSS deprecations should already be tool-bounded via `quietDeps: true` for sass-loader).
   - `UNPARSED` entries → the build failed in a non-standard way; read the embedded tail of the build output to diagnose, then re-run the underlying command (`dotnet build ...` or `npm run webpack-prod`) directly to see the full output.

4. **Never** add `#pragma warning disable`, `<NoWarn>`, `[SuppressMessage]`, or relax the analyzer ruleset without explicit user approval. If a warning genuinely cannot be fixed at the source, stop and ask before suppressing. Errors must be fixed; they are never suppressed.

5. Rerun the script after the fix to confirm the diagnostic is gone and no new ones appeared.

## Script options

| Flag | Effect |
|---|---|
| `-SkipDebug` | Skip the Debug build (use when you've already verified Debug separately). |
| `-SkipRelease` | Skip the Release build (use when triaging a specific Debug-only warning). |
| `-SkipFrontend` | Skip the webpack-prod build (use when only touching .NET code). |

Default is all three. Exit code is 0 when clean, 1 when any warnings or errors are reported.

## Edge cases the script handles

- **Errors as well as warnings.** Both are parsed via the same MSBuild diagnostic pattern (`file(line,col): warning|error CODE: ...`) and the same webpack `WARNING in` / `ERROR in` block detection. They appear in the report with `Level: "warning"` or `Level: "error"` and are counted separately in the per-stage summary.
- **Build failed but no diagnostic matched.** If `dotnet build` or `npm run webpack-prod` exits non-zero and the parser finds nothing matching the standard format (e.g. `MSB1003` project-not-found, `NU1101` without a file prefix, `npm ERR!` lines, build-runner crashes, missing SDK), the script emits a synthetic `Level: "error", Code: "UNPARSED"` entry containing the last 20 non-blank output lines so the failure is still visible in the report.
- **Duplicate diagnostics.** Multiple project references that hit the same warning are deduplicated by `(file, line, column, code)` so you see each issue once.
- **Missing `node_modules/`.** The script auto-runs `npm ci` before `npm run webpack-prod` if needed.

## What it does NOT cover

- **Runtime `warn:` output** from framework loggers during smoke tests (e.g. EF Core query warnings emitted at runtime). Those require running the app — see [AGENTS.md](../../../AGENTS.md#never-silence-warnings) for the full warning surface. This script only covers build-time diagnostics.
- **Single-project test runs.** Use `dotnet test test/<Project>.Test --filter "FullyQualifiedName~..."` for that.
- **CI verification.** There is no CI yet (tracked in issue #36); local runs are the only enforcement.

## Reference

- [`AGENTS.md` → Never silence warnings](../../../AGENTS.md#never-silence-warnings) — the full policy this script enforces, including the warning surface and the explicit-approval rule for suppressions.
- `Directory.Build.props` — defines `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` (Release only) and enables nullable solution-wide.
- `Directory.Build.targets` — adds StyleCop.Analyzers to every project.
