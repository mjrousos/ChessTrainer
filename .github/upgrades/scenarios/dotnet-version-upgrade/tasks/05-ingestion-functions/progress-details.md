# Progress Details: 05-ingestion-functions

## Summary
- Migrated `src\IngestionFunctions\IngestionFunctions.csproj` from `netcoreapp3.1` Azure Functions v3 in-process to `net10.0` Azure Functions isolated worker.
- Deleted `Startup.cs` and added isolated-worker `Program.cs` using `FunctionsApplication.CreateBuilder(args)` and `ConfigureFunctionsWebApplication()` to preserve the existing ASP.NET Core `HttpRequest` / `IActionResult` surface.
- Migrated table storage from `Microsoft.Azure.Cosmos.Table` / `CloudTable` to `Azure.Data.Tables` / `TableClient` / `ITableEntity`.
- Updated queue, timer, HTTP trigger attributes from WebJobs/in-process APIs to isolated worker attributes.
- Fixed all warnings observed in `IngestionFunctions`; no warnings were suppressed.

## Research
- Loaded `migrating-azure-functions-startup` and followed the isolated worker startup migration guidance.
- Loaded `building-projects` and used targeted `dotnet restore` / `dotnet build` validation.
- Verified current NuGet stable versions from NuGet.org flat-container metadata.
- Checked Microsoft Learn samples for isolated worker startup and Azure.Data.Tables `GetEntityAsync` / `UpsertEntityAsync` usage.

## Decomposition Gate
- Executed as a single tightly-coupled task. Splitting the project/package migration from `Program.cs`, function attributes, and table API migration would have left a non-building intermediate Functions app.

## Files Changed
- `src\IngestionFunctions\IngestionFunctions.csproj`
- `src\IngestionFunctions\Program.cs` (added)
- `src\IngestionFunctions\Startup.cs` (deleted)
- `src\IngestionFunctions\GameIngestionFunctions.cs`
- `src\IngestionFunctions\Models\IngestionRecord.cs`
- `src\IngestionFunctions\HttpPolicies.cs`
- `.github\upgrades\scenarios\dotnet-version-upgrade\tasks\05-ingestion-functions\progress-details.md` (this report)

## Package Version Changes
Removed:
- `Microsoft.Azure.Functions.Extensions` `1.0.0`
- `Microsoft.NET.Sdk.Functions` `3.0.3`
- `Microsoft.Azure.WebJobs.Extensions.Storage` `3.0.10`
- `Microsoft.Azure.Cosmos.Table` `1.0.6`

Updated / added:
- `Azure.Storage.Queues` `12.2.0` -> `12.26.0` (CVE fix)
- `Azure.Data.Tables` added at `12.11.0`
- `Microsoft.Azure.Functions.Worker` added at `2.52.0`
- `Microsoft.Azure.Functions.Worker.Sdk` added at `2.0.7` with `OutputItemType="Analyzer"`
- `Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore` added at `2.1.0`
- `Microsoft.Azure.Functions.Worker.Extensions.Storage.Queues` added at `5.5.4`
- `Microsoft.Azure.Functions.Worker.Extensions.Timer` added at `4.3.1`
- `Microsoft.Extensions.Http` `3.1.2` -> `10.0.8`
- `Microsoft.Extensions.Http.Polly` `3.1.2` -> `10.0.8`; verified this current stable package still depends on Polly `7.2.4` and `Polly.Extensions.Http` `3.0.0`, preserving the scenario's Polly v7 decision.

## Behavioral Migrations
- `Startup.Configure(IFunctionsHostBuilder)` registrations moved to `Program.cs`; the previous `BuildServiceProvider()` anti-pattern was removed.
- Table and queue startup provisioning now uses async `CreateIfNotExistsAsync()` calls in top-level async `Program.cs`.
- `CloudTable.ExecuteAsync(TableOperation.Retrieve<T>)` became `TableClient.GetEntityAsync<T>` with `RequestFailedException Status == 404` handling for not found.
- `TableOperation.InsertOrReplace` became `TableClient.UpsertEntityAsync(..., TableUpdateMode.Replace)`.
- Health check storage probes now use idempotent `CreateIfNotExistsAsync()` checks instead of `ExistsAsync()` / `CreateAsync()`.
- Storage exception handling migrated from legacy `StorageException` to `Azure.RequestFailedException`.
- Timer next-run logging changed from in-process `timer.Schedule.GetNextOccurrence(DateTime.Now)` to isolated worker `timer.ScheduleStatus?.Next`.
- `IngestionRecord` now implements `ITableEntity` and maps existing `ChessSite` / `Player` accessors to `PartitionKey` / `RowKey`.
- `HttpContent.ReadAsStreamAsync()` and absolute `new Uri(...)` usages in `LiChessService` were reviewed. No source change was required; `ReadAsStreamAsync()` keeps the same behavior without a cancellation token, and the Uri value is an absolute HTTPS URL.

## Local Settings Note
- `src\IngestionFunctions\local.settings.json` is not present and was not created.
- For local development, `FUNCTIONS_WORKER_RUNTIME` must be `dotnet-isolated` and the existing connection-string settings (`PuzzleDbConnectionString`, `StorageConnectionString`, queue/table names, and `LichessToken`) must be provided as before.

## Validation
Baseline before migration:
- `dotnet build src\IngestionFunctions\IngestionFunctions.csproj` failed because `IngestionFunctions` targeted `netcoreapp3.1` while `ChessTrainer.Data` already targets `net10.0`; it also reported the known `Azure.Storage.Queues` `12.2.0` NU1902 vulnerability.

Post-migration validation:
- `dotnet restore src\IngestionFunctions\IngestionFunctions.csproj` succeeded.
- `dotnet build src\IngestionFunctions\IngestionFunctions.csproj` succeeded with `0 Warning(s), 0 Error(s)`.
- `dotnet build src\IngestionFunctions\IngestionFunctions.csproj -c Release` succeeded with `0 Warning(s), 0 Error(s)`.
- `dotnet build src\Engine\Engine.csproj` succeeded with `0 Warning(s), 0 Error(s)`.
- `dotnet build src\ChessTrainer.Common\ChessTrainer.Common.csproj` succeeded with `0 Warning(s), 0 Error(s)`.
- `dotnet build src\ChessTrainer.Data\ChessTrainer.Data.csproj` succeeded with `0 Warning(s), 0 Error(s)`.
- `git diff --check` on modified IngestionFunctions files passed.
- Repository search in `src\IngestionFunctions` found no remaining references to `Microsoft.Azure.WebJobs`, `Microsoft.Azure.Cosmos.Table`, `Microsoft.Azure.Functions.Extensions`, `FunctionsStartup`, `FunctionName`, `CloudTable`, `TableOperation`, or `StorageException`.

## Smoke Test
- Azure Functions Core Tools is installed: `func --version` -> `4.0.7030`.
- `func start` was not run because `local.settings.json` is absent and running the host would require real storage settings or a local storage emulator. Per the quality bar, no hard local-emulator dependency was introduced.
- Build-generated `functions.metadata` was inspected and contains all four functions: `ReviewPlayers`, `HealthCheck`, `AddQueuedPlayer`, and `AddPlayer`.

## Done-When Checklist
- [x] `IngestionFunctions.csproj` targets `net10.0`.
- [x] `<OutputType>Exe</OutputType>` added.
- [x] Worker SDK/package references added.
- [x] Removed `Microsoft.NET.Sdk.Functions`.
- [x] Removed `Microsoft.Azure.Functions.Extensions`.
- [x] Removed `Microsoft.Azure.WebJobs.Extensions.Storage`.
- [x] Removed `Microsoft.Azure.Cosmos.Table`.
- [x] `Azure.Storage.Queues` is `12.26.0`.
- [x] `Startup.cs` deleted.
- [x] `Program.cs` exists with isolated worker bootstrap.
- [x] All `[FunctionName(...)]` attributes replaced with `[Function(...)]`.
- [x] `IngestionRecord` implements `ITableEntity` and no longer inherits `TableEntity`.
- [x] Tables calls use `TableClient` / `Azure.Data.Tables`.
- [x] `dotnet build src\IngestionFunctions\IngestionFunctions.csproj` succeeds with 0 warnings and 0 errors.
- [x] Removed-reference grep checks pass.
- [x] Frozen projects `Engine`, `ChessTrainer.Common`, and `ChessTrainer.Data` still build clean.

## Warning Suppression
- No warnings were suppressed.

## Follow-ups / Notes
- No follow-up is required for this task's done-when criteria.
- Future scenario work may still migrate from Polly v7 policies to `Microsoft.Extensions.Http.Resilience`, but that was intentionally not done here.
