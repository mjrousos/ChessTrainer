---
applyTo: "test/**"
---

# Testing instructions for ChessTrainer

Path-scoped guidance for anything under `test/`. The repo-wide rules in [`AGENTS.md`](../../AGENTS.md) (zero warnings, model-layering, code style, PR feedback workflow, etc.) still apply — this file adds test-architecture context on top.

## Test project layout

Test projects mirror the `src/` projects they cover; folder name and assembly name match the project under test plus `.Test`.

| Test project (`test/`)        | Covers (`src/`)        | Notes |
|-------------------------------|------------------------|-------|
| `ChessTrainer.Data.Test`      | `ChessTrainer.Data`    | EF Core / `PuzzleDbContextFactory` / migrations. |
| `ChessTrainerApp.Test`        | `ChessTrainerApp`      | Blazor component tests via **bUnit**. |
| `IngestionFunctions.Test`     | `IngestionFunctions`   | Azure Functions ingestion (currently a placeholder). |

Add a new `*.Test` project alongside any new `src/` project and reference it from `ChessTrainer.sln`.

## Stack & conventions

- **Test framework:** xUnit (`2.9.x`) with `xunit.runner.visualstudio` and `Microsoft.NET.Test.Sdk`.
- **Coverage:** `coverlet.collector`.
- **Blazor components:** bUnit — derive the test class from `BunitContext`, render with `Render<TComponent>()`, and resolve fakes (e.g. `BunitNavigationManager`) from `Services`. See `ChessTrainerApp.Test/MainCommandButtonsTests.cs`.
- **Target framework:** `net10.0`, `IsPackable=false`. Inherits `Nullable`, StyleCop, and `TreatWarningsAsErrors` (Release) from `Directory.Build.*`. Warnings in test code are errors too — fix them, don't suppress.
- **File scaffolding:** 4-space indent, file-scoped or block namespace matching folder, `using` directives outside the namespace, trailing newline.
- **Naming:** `MethodOrScenario_ExpectedBehavior` (e.g. `CreateDbContext_UsesEnvironmentVariableWhenSet`).

## Process-global state must opt out of parallelization

xUnit parallelizes tests across classes by default. Any test that mutates process-wide state (environment variables, `CurrentDirectory`, statics, `AppContext` switches) **must** join a non-parallel collection. Pattern from `ChessTrainer.Data.Test`:

```csharp
[CollectionDefinition(nameof(EnvironmentVariableCollection), DisableParallelization = true)]
public class EnvironmentVariableCollection { }

[Collection(nameof(EnvironmentVariableCollection))]
public class MyTests { ... }
```

Always save/restore the original value in a `try`/`finally` (see `WithEnvironmentVariable` in `PuzzleDbContextFactoryTests.cs`). Never leak mutated state to the next test.

## EF Core / connection-string tests

- `PuzzleDbContextFactory` reads the `PuzzleDbConnectionString` env var and falls back to the local SQL container connection string. Tests must run with that env var both set and unset — use the `WithEnvironmentVariable` helper.
- Compare SQL connection strings with `SqlConnectionStringBuilder` (order/casing of keys isn't stable); don't string-compare them.
- The migrations-discovery test in `PuzzleDbContextFactoryTests.Context_DiscoversMigrationsInDataAssembly` is a **regression guard** against reintroducing a `MigrationsAssembly("MjrChess.Trainer")` workaround. When you add a migration, also append its ID to `ExpectedMigrations`; do not weaken the assembly/namespace assertions.

## Running tests

```powershell
# Whole solution (Release matches CI behavior: zero warnings).
dotnet test ChessTrainer.sln -c Release

# A single test project.
dotnet test test/ChessTrainer.Data.Test

# A single test by fully-qualified name (xUnit filter).
dotnet test test/ChessTrainer.Data.Test --filter "FullyQualifiedName~PuzzleDbContextFactoryTests.CreateDbContext_UsesEnvironmentVariableWhenSet"

# Check for pending EF model changes (run from repo root).
dotnet ef migrations has-pending-model-changes --project src\ChessTrainer.Data
```

Always run both Debug and Release builds before declaring a test task done — `TreatWarningsAsErrors` only fires in Release.
