# .NET Version Upgrade

## Preferences
- **Flow Mode**: Automatic
- **Target Framework**: net10.0
- **Scope**: Partial-solution upgrade — only the IngestionFunctions dependency chain.

## Upgrade Options
- **Upgrade Strategy**: Bottom-Up
- **Unsupported Packages**: Resolve Inline
- **Unsupported API Handling**: Fix Inline

## Strategy
**Selected**: Bottom-Up (Dependency-First)
**Rationale**: 5 in-scope SDK-style projects with a 4-level dependency chain (`Engine` → `ChessTrainer.Common` → `ChessTrainer.Data` → `IngestionFunctions`) plus a standalone test project. Leaf-first ordering isolates package/API migrations and matches the confirmed single-target `net10.0` scope.

### Execution Constraints
- **Strict tier ordering**: complete and validate `Engine`, then `ChessTrainer.Common`, then `ChessTrainer.Data`, then `IngestionFunctions`; upgrade `IngestionFunctions.Test` immediately after the function app.
- **Tier validation**: after each tier, the upgraded project(s) and all already-completed tiers must restore/build successfully; known higher-tier breaks are addressed in their later tier.
- **Single-target net10.0 only** for shared libraries — do NOT introduce multi-targeting to keep ChessTrainerApp alive. The ChessTrainerApp build will fail at the end; this is expected and documented.
- **Inline package + API resolution**: each task resolves its own incompatible packages, security updates, and API breakages in-flight; no deferred stubs.
- **Per-task commits**: Commit Strategy is After Each Task — each validated task commits independently for easy revert.

## In-Scope Projects

These projects will be upgraded to `net10.0`:

- `src/Engine/Engine.csproj`
- `src/ChessTrainer.Common/ChessTrainer.Common.csproj`
- `src/ChessTrainer.Data/ChessTrainer.Data.csproj`
- `src/IngestionFunctions/IngestionFunctions.csproj`
- `test/IngestionFunctions.Test/IngestionFunctions.Test.csproj`

## Out-of-Scope Projects

These projects share `Engine` / `ChessTrainer.Common` / `ChessTrainer.Data` with the in-scope set and **will stop building** once the shared libs are single-targeted to net10.0. This is accepted; a follow-up scenario will upgrade them.

- `src/ChessTrainerApp/ChessTrainerApp.csproj`
- `test/ChessTrainerApp.Test/ChessTrainerApp.Test.csproj`

## Decisions (user-confirmed)

- **Shared library target strategy**: single-target `net10.0` (not multi-target). Accept that ChessTrainerApp breaks.
- **Azure Functions hosting model**: migrate IngestionFunctions from Functions v3 **in-process** to Functions v4 **isolated worker** model (required for net10.0 on Functions).
- **Storage SDK**: migrate from deprecated `Microsoft.Azure.Cosmos.Table` to modern `Azure.Data.Tables` as part of this scenario.
- **HTTP trigger surface**: keep `HttpRequest` / `IActionResult` by using `Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore`, instead of rewriting to `HttpRequestData` / `HttpResponseData`.
- **Polly**: stay on Polly v7 (`Microsoft.Extensions.Http.Polly`) in this scenario; flag migration to `Microsoft.Extensions.Http.Resilience` as a follow-up.
- **Test project**: included in the upgrade. The existing single stub test stays as-is; writing real coverage is a follow-up.
- **EF migrations**: live in `ChessTrainerApp` (`MigrationsAssembly("MjrChess.Trainer")`). Not moved in this scenario.

## Source Control
- **Source Branch**: `master`
- **Working Branch**: `upgrade-dotnet-10`
- **Commit Strategy**: After Each Task
- **Branch Sync**: Auto (Merge)

## Reference: scoping plan from session

Original detailed scoping plan: `C:\Users\mikerou\.copilot\session-state\c46e75f4-6561-4c9f-ae65-696eb010c660\plan.md` — kept as input to the assessment/planning stages; the scenario's own `plan.md` (produced by the planning stage) will be the authoritative execution plan.



