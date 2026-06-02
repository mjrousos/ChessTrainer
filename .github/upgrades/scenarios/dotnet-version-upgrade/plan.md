# .NET Version Upgrade Plan

## Overview

**Target**: Upgrade the IngestionFunctions dependency chain to `net10.0`, including Azure Functions v3 in-process to v4 isolated worker migration.
**Scope**: 5 in-scope SDK-style projects, 3,192 assessed LOC, with shared libraries single-targeted to `net10.0`; `ChessTrainerApp` and its tests are explicitly out of scope and are expected to stop building.

### Selected Strategy
**Bottom-Up (Dependency-First)** — Upgrade from leaf nodes to root applications, tier by tier.
**Rationale**: 5 in-scope projects with a 4-level dependency chain plus a standalone test project; leaf-first ordering lets the shared libraries, data access layer, and Functions app be validated at their natural dependency boundaries while honoring the confirmed single-target `net10.0` scope.

### Dependency Graph
```text
Tier 4: [IngestionFunctions]
          ↓
Tier 3: [ChessTrainer.Data]
          ↓             ↘
Tier 2: [ChessTrainer.Common]
          ↓
Tier 1: [Engine]

Standalone after Tier 4: [IngestionFunctions.Test]
```

### Tier Summary
- **Prerequisites**: solution-wide build settings in `Directory.Build.props`, `Directory.Build.targets`, and `global.json`; no individual project TFM changes.
- **Tier 1**: `src\Engine\Engine.csproj`; no internal project dependencies, used by Common and Data; completion requires `Engine` to restore and build on `net10.0`.
- **Tier 2**: `src\ChessTrainer.Common\ChessTrainer.Common.csproj`; depends on Engine; completion requires Common and Engine to restore and build together on `net10.0`.
- **Tier 3**: `src\ChessTrainer.Data\ChessTrainer.Data.csproj`; depends on Common and Engine; completion requires EF Core, AutoMapper, and data access APIs to compile against `net10.0`.
- **Tier 4**: `src\IngestionFunctions\IngestionFunctions.csproj`; depends on Data; completion requires isolated worker packages, Azure Tables replacement, queue vulnerability update, and inline API fixes.
- **Standalone test**: `test\IngestionFunctions.Test\IngestionFunctions.Test.csproj`; upgrade immediately after Tier 4 so test tooling validates against the completed Functions migration.

## Tasks

### 01-upgrade-foundations: Prepare shared build settings for net10.0

Update only solution-wide build configuration before project-level changes. This includes verifying `global.json` remains compatible with the installed .NET 10 SDK line, updating `Directory.Build.props` so the repository can compile modern C# (`LangVersion` should be `latest`), and updating `Directory.Build.targets` to remove deprecated FxCop analyzers while keeping StyleCop compatible with the Roslyn toolchain used by .NET 10.

The assessment shows deprecated analyzer references across all 5 in-scope projects, so this task is the shared prerequisite for every later tier. Research starting points are `Directory.Build.props`, `Directory.Build.targets`, `rules.ruleset`, and `global.json`; keep the change focused and do not introduce Central Package Management unless a later task proves it necessary.

**Done when**: `Directory.Build.props` uses a .NET 10-compatible language configuration, `Directory.Build.targets` no longer references `Microsoft.CodeAnalysis.FxCopAnalyzers`, StyleCop remains referenced at a compatible version, `global.json` is confirmed at `10.0.300` or another compatible .NET 10 SDK, and no in-scope `.csproj` file has been retargeted yet.

---

### 02-engine: Retarget the Engine library

Upgrade `src\Engine\Engine.csproj`, the leaf library with no internal project references, from `netstandard2.0` to single-target `net10.0`. The assessment reports 1 optional package/analyzer issue and no API compatibility issues, so this should be a small library retarget with validation focused on compiler/analyzer fallout from the framework jump.

Because both `ChessTrainer.Common` and `ChessTrainer.Data` depend on Engine, preserve public model and utility APIs unless compilation requires a targeted fix. Research starting points are the project file, its StyleCop/analyzer references inherited from shared targets, and any nullable or analyzer warnings surfaced by building Engine on the new target.

**Done when**: `src\Engine\Engine.csproj` targets only `net10.0`, `dotnet restore src\Engine\Engine.csproj` succeeds, `dotnet build src\Engine\Engine.csproj` succeeds with no errors, and no multi-targeting was added to preserve out-of-scope consumers.

---

### 03-common-models: Retarget shared common models

Upgrade `src\ChessTrainer.Common\ChessTrainer.Common.csproj` from `netstandard2.0` to single-target `net10.0` after Engine is validated. This project depends on Engine, has 194 LOC, and the assessment reports only the shared deprecated analyzer issue with no API compatibility findings.

Keep the task narrowly scoped to the common model surface and its reference to the upgraded Engine library. Research starting points are the project file, project reference metadata, and the public types consumed by `ChessTrainer.Data`; avoid compatibility multi-targeting because the confirmed decision is that shared libraries become `net10.0` only.

**Done when**: `src\ChessTrainer.Common\ChessTrainer.Common.csproj` targets only `net10.0`, it references the upgraded Engine project without compatibility workarounds, `dotnet build src\ChessTrainer.Common\ChessTrainer.Common.csproj` succeeds, and `dotnet build src\Engine\Engine.csproj` still succeeds.

---

### 04-data-access: Upgrade EF Core and AutoMapper data layer

Upgrade `src\ChessTrainer.Data\ChessTrainer.Data.csproj` from `netstandard2.1` to single-target `net10.0` after Engine and Common are stable. The assessment reports EF Core 3.1 package upgrades to the .NET 10 line, `Microsoft.Extensions.Configuration` alignment, AutoMapper package modernization, 1 source-incompatible API finding, and the shared analyzer cleanup.

This tier owns the EF model, repository abstractions, and AutoMapper configuration consumed by IngestionFunctions. Research starting points are `DataExtensions.cs`, `PuzzleDbContext.cs`, repository implementations, `AutoMapperProfile.cs`, and the existing `MigrationsAssembly("MjrChess.Trainer")` decision; do not move migrations from out-of-scope `ChessTrainerApp` in this scenario.

Resolve unsupported packages inline. Remove deprecated `AutoMapper.Extensions.Microsoft.DependencyInjection` if the selected AutoMapper version provides DI directly, pick compatible AutoMapper/ExpressionMapping versions together, align EF Core packages to `10.0.x`, and preserve public APIs such as `AddChessTrainerData` and `IRepository<T>` for the Functions tier.

**Done when**: `src\ChessTrainer.Data\ChessTrainer.Data.csproj` targets only `net10.0`, EF Core and configuration packages are aligned to `10.0.x`, AutoMapper packages compile without the deprecated DI extension dependency, `dotnet build src\ChessTrainer.Data\ChessTrainer.Data.csproj` succeeds, and Engine/Common builds still succeed.

---

### 05-ingestion-functions: Migrate Functions app to isolated worker

Upgrade `src\IngestionFunctions\IngestionFunctions.csproj` from `netcoreapp3.1` and Azure Functions v3 in-process to `net10.0` on Functions v4 isolated worker. This is the highest-churn tier: assessment reports 15 issues including mandatory TFM and Functions model changes, package replacements, the `Azure.Storage.Queues` vulnerability update to `12.26.0`, 2 source-incompatible API findings, and 4 behavioral API findings.

Resolve package and API issues inline. Remove `Microsoft.NET.Sdk.Functions`, `Microsoft.Azure.Functions.Extensions`, `Microsoft.Azure.WebJobs.Extensions.Storage`, and `Microsoft.Azure.Cosmos.Table`; add `Microsoft.Azure.Functions.Worker` `2.52.0`, `Microsoft.Azure.Functions.Worker.Sdk` `2.0.7`, `Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore`, timer and queue worker extensions, and `Azure.Data.Tables`. Keep `Microsoft.Extensions.Http.Polly` on the Polly v7 path for this scenario and flag Polly v8 / `Microsoft.Extensions.Http.Resilience` as follow-up work.

Research starting points are `Startup.cs`, any new `Program.cs`, function attribute usages, queue/timer/http trigger signatures, `HttpPolicies.cs`, `GameIngestionFunctions.cs`, and `Models\IngestionRecord.cs`. Preserve the existing `HttpRequest` / `IActionResult` HTTP trigger surface using `Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore` rather than rewriting to `HttpRequestData`, and migrate Cosmos Table APIs to `Azure.Data.Tables` types such as `TableClient`/`ITableEntity`.

**Done when**: `src\IngestionFunctions\IngestionFunctions.csproj` targets `net10.0` with isolated worker package references and `OutputType` appropriate for the worker app, removed in-process/WebJobs/Cosmos.Table packages are absent, `Azure.Storage.Queues` is updated to `12.26.0`, no `Microsoft.Azure.WebJobs.*` or `Microsoft.Azure.Cosmos.Table` references remain in the project, `dotnet build src\IngestionFunctions\IngestionFunctions.csproj` succeeds, and the host can discover `ReviewPlayers`, `HealthCheck`, `AddQueuedPlayer`, and `AddPlayer` when run with Functions Core Tools v4 if available.

---

### 06-ingestion-test: Retarget the Functions test project

Upgrade `test\IngestionFunctions.Test\IngestionFunctions.Test.csproj` from `netcoreapp3.0` to `net10.0` immediately after the Functions app migration. The assessment reports 3 issues: mandatory TFM change plus deprecated test package findings, and the current test code is only a 13 LOC stub with no project reference.

Keep this task focused on test infrastructure rather than adding new coverage. Research starting points are the test project file and the single stub test; update `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, and `coverlet.collector` to versions compatible with .NET 10, but treat meaningful Functions coverage as a follow-up after the isolated-worker migration is stable.

**Done when**: `test\IngestionFunctions.Test\IngestionFunctions.Test.csproj` targets `net10.0`, test package references are compatible with the .NET 10 SDK, `dotnet test test\IngestionFunctions.Test\IngestionFunctions.Test.csproj` passes, and the previous IngestionFunctions build still succeeds.

---

### 07-solution-validation: Validate upgrade scope and document hand-off

Run final validation across the 5 in-scope projects and document the intentional partial-solution outcome. This task should verify restore/build/test status for Engine, Common, Data, IngestionFunctions, and IngestionFunctions.Test, then capture any runtime smoke-test result available for the isolated Functions host.

The final validation must explicitly document that `src\ChessTrainerApp\ChessTrainerApp.csproj` and `test\ChessTrainerApp.Test\ChessTrainerApp.Test.csproj` are out of scope and are expected to stop building because shared libraries were single-targeted to `net10.0`. Research starting points are `ChessTrainer.sln`, the in-scope project list, Functions Core Tools availability, and any errors from attempting to build the out-of-scope app for hand-off to a future scenario.

Also confirm no deferred modernization was silently mixed into this scenario. Polly v8 / `Microsoft.Extensions.Http.Resilience`, real Functions test coverage, and upgrading `ChessTrainerApp` remain follow-up work; the success criteria here are an accurate, repeatable validation record and clean in-scope builds.

**Done when**: all 5 in-scope projects target `net10.0`, restore/build succeeds for each in-scope project, `dotnet test test\IngestionFunctions.Test\IngestionFunctions.Test.csproj` passes, the Functions host startup/discovery is smoke-tested or the missing local prerequisite is recorded, repository search finds no remaining in-scope references to removed Functions/Cosmos.Table packages, and the expected `ChessTrainerApp` build failure is captured for hand-off.
