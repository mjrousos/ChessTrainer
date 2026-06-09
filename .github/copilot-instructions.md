# Copilot instructions for ChessTrainer

Multi-project .NET 10 solution: Blazor Server app, EF Core data layer, Azure Functions ingestion, in-house chess engine.

## Code review checklist

Apply these rules to every change.

### Never silence warnings
- Warnings include compiler/analyzer warnings, npm/webpack `compiled with N warnings`, and framework runtime `warn:` output during smoke tests (e.g. EF Core `EF[10102]`/`EF[10103]`). All count; all must be zero.
- Fix the underlying cause (add the missing `OrderBy`, tighten the nullable annotation, etc.).
- Don't disable `Nullable` at file or project level — fix the annotation.
- No `#pragma warning disable`, `<NoWarn>` entries, `[SuppressMessage]` attributes, or analyzer rule-set relaxations unless the user explicitly approves that specific suppression. Approved suppressions must be narrow and include a justification comment.
- Third-party deprecation noise (e.g. legacy SCSS in `node_modules`) may be quieted only at the tool boundary (e.g. `quietDeps: true` for sass-loader), never inside our source.

### Don't leak `Data.Models.*` outside `ChessTrainer.Data`
EF entities (`MjrChess.Trainer.Data.Models.*`) are deliberately separate from public domain models (`MjrChess.Trainer.Models.*` in `ChessTrainer.Common`). Repositories are registered as `IRepository<TPublicModel>`; the generic `EFRepository<TEntity, TPublicModel>` (or specialized repos like `TacticsPuzzleRepository`) does the AutoMapper bridging. Consumers depend on the public `Models` namespace only.

### Connection string has two names — update both
- **ChessTrainerApp** → `ConnectionStrings:PuzzleDatabase` (key is `PuzzleDatabase`, not `PuzzleDb`).
- **IngestionFunctions** → `PuzzleDbConnectionString` env var. Also what `PuzzleDbContextFactory` reads for `dotnet ef` tooling. See `src/IngestionFunctions/README.md` for the full recipe.

### Auth: don't depend on Azure AD B2C user-flow IDs
`Startup.cs` wires Microsoft Identity Web against B2C, but the configured tenant is dead (issue #39, migrating to Entra External ID). Don't add code that depends on `B2C_1_*` user-flow IDs — they're going away.

### Application Insights is opt-in
Register `AddApplicationInsightsTelemetry()` **only** when `ApplicationInsights:ConnectionString` or `:InstrumentationKey` is configured — the 3.x SDK throws on startup if called with neither. Use `GetService<TelemetryConfiguration>()`, not `GetRequiredService`.

### Code style
- C# `latest`, `Nullable` enabled solution-wide (`Directory.Build.props`).
- StyleCop.Analyzers is added by `Directory.Build.targets`; rules tuned in `rules.ruleset` and `stylecop.json`: 4-space indent; system `using` directives NOT required first; blank line between using groups; `using` directives outside the namespace; file must end with newline.
- Razor: `@page` routes in `src/ChessTrainerApp/Pages/`; reusable components in `src/ChessTrainerApp/Components/` and `Shared/`.
- Static web assets go under `src/ChessTrainerApp/app/` (webpack bundles them into `wwwroot/dist/`, which `Program.cs` serves via `UseWebRoot("wwwroot/dist")`). Don't hand-edit `wwwroot/dist`.

## PR feedback workflow

When addressing review comments, the task is **not** complete after pushing a fix commit. For every comment thread:
1. **Reply** with a short note explaining the change, citing the fix commit SHA when possible.
2. **Mark the thread resolved.**

A round is "done" only when every thread has both a reply and a resolved state.

## Solution layout

**Project folder names don't match root namespaces:**

| Project (`src/`)     | Root namespace | Role |
|----------------------|----------------|------|
| `ChessTrainerApp`    | `MjrChess.Trainer` | Blazor Server web app + Razor Pages auth UI. Entry point `Program.cs`/`Startup.cs`. |
| `ChessTrainer.Data`  | `MjrChess.Trainer.Data` | EF Core `PuzzleDbContext`, repositories, migrations, AutoMapper profile. |
| `ChessTrainer.Common`| `MjrChess.Trainer` (models under `MjrChess.Trainer.Models` in `Models/`) | Public domain models shared across projects. |
| `Engine`             | `MjrChess.Engine` | In-house chess move generator/validator (`ChessEngine`). Not UCI (issue #35). |
| `IngestionFunctions` | `IngestionFunctions(.Services, .Models)` | Azure Functions (isolated worker) scraping Lichess/Chess.com. See `src/IngestionFunctions/README.md`. |

Tests in `test/` mirror project names (xUnit + Coverlet, .NET 10).

Azure deployment lives in `infrastructure/ChessTrainerRG/` (ARM templates). Issue #31 tracks modernizing to Bicep + GitHub Actions.

## Companion repository (ChessPuzzleFinder)

This solution works along with the [ChessPuzzleFinder](https://github.com/mjrousos/ChessPuzzleFinder/) repository, which contains a Go utility for finding candidate puzzles from games queued by this app. The puzzles are then written to the database for use by this app. The two repos are separate but are usually deployed together in a containerized environment.

## Build hygiene

**Run both Debug and Release** before declaring a task done — `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` in `Directory.Build.props` only fires in Release, and there's no CI yet (issue #36).

## Build, test, and run

```powershell
# Full solution (Release fails on any warning)
dotnet build ChessTrainer.sln -c Release
dotnet test  ChessTrainer.sln -c Release

# Single test (xUnit filter)
dotnet test test/ChessTrainer.Data.Test --filter "FullyQualifiedName~MyClass.MyMethod"

# Run the Blazor app
dotnet run --project src/ChessTrainerApp

# Front-end (auto-invoked by ChessTrainerApp's DebugRunWebpack target
# when wwwroot/dist is missing — plain `dotnet build` is usually enough)
cd src/ChessTrainerApp
npm install
npm run webpack-dev    # one-off dev build
npm run watch          # rebuild on change
npm run webpack-prod   # production build (zero-warning requirement)
```
