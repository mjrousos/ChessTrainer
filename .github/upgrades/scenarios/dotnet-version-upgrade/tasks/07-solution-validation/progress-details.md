# 07-solution-validation progress details

## Validation summary

- **Task**: `07-solution-validation`
- **Repo root**: `C:\src\mjrousos\ChessTrainer`
- **Validated with**: .NET SDK `10.0.300`, host runtime `10.0.8`
- **Result**: In-scope upgrade validation passed. The 5 in-scope projects single-target `net10.0`, restore/build with `0 Warning(s)` and `0 Error(s)`, and the in-scope test project passes.
- **Intentional hand-off**: `src\ChessTrainerApp\ChessTrainerApp.csproj` fails restore/build because it still targets `netcoreapp3.1` while shared project references now support only `net10.0`.
- **Source/project changes**: None. This validation task only added this evidence file; `scenario.json` and `tasks.md` were already modified before validation began.

## Phase 1 - Inputs and constraints

Validated the scenario instructions and task plan. The authoritative scope is the partial-solution upgrade for the IngestionFunctions dependency chain only:

1. `src\Engine\Engine.csproj`
2. `src\ChessTrainer.Common\ChessTrainer.Common.csproj`
3. `src\ChessTrainer.Data\ChessTrainer.Data.csproj`
4. `src\IngestionFunctions\IngestionFunctions.csproj`
5. `test\IngestionFunctions.Test\IngestionFunctions.Test.csproj`

Out of scope:

- `src\ChessTrainerApp\ChessTrainerApp.csproj`
- `test\ChessTrainerApp.Test\ChessTrainerApp.Test.csproj`

## Phase 2 - Target framework verification

Command equivalent run from repo root: read each in-scope project file as XML and count `TargetFramework` / `TargetFrameworks` elements.

| Project | `TargetFramework` count | Value | `TargetFrameworks` count | Result |
|---|---:|---|---:|---|
| `src\Engine\Engine.csproj` | 1 | `net10.0` | 0 | Pass |
| `src\ChessTrainer.Common\ChessTrainer.Common.csproj` | 1 | `net10.0` | 0 | Pass |
| `src\ChessTrainer.Data\ChessTrainer.Data.csproj` | 1 | `net10.0` | 0 | Pass |
| `src\IngestionFunctions\IngestionFunctions.csproj` | 1 | `net10.0` | 0 | Pass |
| `test\IngestionFunctions.Test\IngestionFunctions.Test.csproj` | 1 | `net10.0` | 0 | Pass |

Evidence:

```text
src\Engine\Engine.csproj: TargetFrameworkCount=1; TargetFramework='net10.0'; TargetFrameworksCount=0; TargetFrameworks=''
src\ChessTrainer.Common\ChessTrainer.Common.csproj: TargetFrameworkCount=1; TargetFramework='net10.0'; TargetFrameworksCount=0; TargetFrameworks=''
src\ChessTrainer.Data\ChessTrainer.Data.csproj: TargetFrameworkCount=1; TargetFramework='net10.0'; TargetFrameworksCount=0; TargetFrameworks=''
src\IngestionFunctions\IngestionFunctions.csproj: TargetFrameworkCount=1; TargetFramework='net10.0'; TargetFrameworksCount=0; TargetFrameworks=''
test\IngestionFunctions.Test\IngestionFunctions.Test.csproj: TargetFrameworkCount=1; TargetFramework='net10.0'; TargetFrameworksCount=0; TargetFrameworks=''
```

## Phase 3 - Clean restore and build validation

Each in-scope project was restored and built individually, not through `ChessTrainer.sln`.

| Project | Restore exit code | Build exit code | Build result | Warnings | Errors | Output assembly |
|---|---:|---:|---|---:|---:|---|
| `src\Engine\Engine.csproj` | 0 | 0 | Succeeded | 0 | 0 | `bin\Engine\net10.0\MjrChess.Engine.dll` |
| `src\ChessTrainer.Common\ChessTrainer.Common.csproj` | 0 | 0 | Succeeded | 0 | 0 | `bin\ChessTrainer.Common\net10.0\MjrChess.Trainer.Common.dll` |
| `src\ChessTrainer.Data\ChessTrainer.Data.csproj` | 0 | 0 | Succeeded | 0 | 0 | `bin\ChessTrainer.Data\net10.0\MjrChess.Trainer.Data.dll` |
| `src\IngestionFunctions\IngestionFunctions.csproj` | 0 | 0 | Succeeded | 0 | 0 | `bin\IngestionFunctions\net10.0\IngestionFunctions.dll` |
| `test\IngestionFunctions.Test\IngestionFunctions.Test.csproj` | 0 | 0 | Succeeded | 0 | 0 | `bin\IngestionFunctions.Test\net10.0\IngestionFunctions.Test.dll` |

Build evidence excerpts:

```text
--- BUILD: src\Engine\Engine.csproj ---
Build succeeded.
    0 Warning(s)
    0 Error(s)

--- BUILD: src\ChessTrainer.Common\ChessTrainer.Common.csproj ---
Build succeeded.
    0 Warning(s)
    0 Error(s)

--- BUILD: src\ChessTrainer.Data\ChessTrainer.Data.csproj ---
Build succeeded.
    0 Warning(s)
    0 Error(s)

--- BUILD: src\IngestionFunctions\IngestionFunctions.csproj ---
WorkerExtensions -> C:\src\mjrousos\ChessTrainer\obj\IngestionFunctions\Debug\net10.0\WorkerExtensions\bin\Release\net8.0\Microsoft.Azure.Functions.Worker.Extensions.dll
IngestionFunctions -> C:\src\mjrousos\ChessTrainer\bin\IngestionFunctions\net10.0\IngestionFunctions.dll
Build succeeded.
    0 Warning(s)
    0 Error(s)

--- BUILD: test\IngestionFunctions.Test\IngestionFunctions.Test.csproj ---
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## Phase 4 - Code changes

No source files, project files, package files, or runtime configuration files were edited by this validation task. This was validation-only, with evidence captured in `progress-details.md`.

## Phase 5 - Test validation

Command run from repo root:

```powershell
dotnet test test\IngestionFunctions.Test\IngestionFunctions.Test.csproj
```

Result:

```text
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 3 ms - IngestionFunctions.Test.dll (net10.0)
```

Summary: total `1`, passed `1`, failed `0`, skipped `0`.

## Phase 6 - Expected out-of-scope build failure

Command run from repo root:

```powershell
dotnet build src\ChessTrainerApp\ChessTrainerApp.csproj
```

Result: failed as expected with exit code `1`.

Exact relevant output:

```text
Determining projects to restore...
C:\Program Files\dotnet\sdk\10.0.300\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.EolTargetFrameworks.targets(32,5): warning NETSDK1138: The target framework 'netcoreapp3.1' is out of support and will not receive security updates in the future. Please refer to https://aka.ms/dotnet-core-support for more information about the support policy. [C:\src\mjrousos\ChessTrainer\src\ChessTrainerApp\ChessTrainerApp.csproj]
C:\Program Files\dotnet\sdk\10.0.300\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.EolTargetFrameworks.targets(32,5): warning NETSDK1138: The target framework 'netcoreapp3.1' is out of support and will not receive security updates in the future. Please refer to https://aka.ms/dotnet-core-support for more information about the support policy. [C:\src\mjrousos\ChessTrainer\src\ChessTrainerApp\ChessTrainerApp.csproj]
C:\src\mjrousos\ChessTrainer\src\ChessTrainerApp\ChessTrainerApp.csproj : error NU1201: Project MjrChess.Trainer.Common is not compatible with netcoreapp3.1 (.NETCoreApp,Version=v3.1). Project MjrChess.Trainer.Common supports: net10.0 (.NETCoreApp,Version=v10.0)
C:\src\mjrousos\ChessTrainer\src\ChessTrainerApp\ChessTrainerApp.csproj : error NU1201: Project MjrChess.Trainer.Data is not compatible with netcoreapp3.1 (.NETCoreApp,Version=v3.1). Project MjrChess.Trainer.Data supports: net10.0 (.NETCoreApp,Version=v10.0)
C:\src\mjrousos\ChessTrainer\src\ChessTrainerApp\ChessTrainerApp.csproj : error NU1201: Project MjrChess.Engine is not compatible with netcoreapp3.1 (.NETCoreApp,Version=v3.1). Project MjrChess.Engine supports: net10.0 (.NETCoreApp,Version=v10.0)
Failed to restore C:\src\mjrousos\ChessTrainer\src\ChessTrainerApp\ChessTrainerApp.csproj (in 941 ms).
3 of 4 projects are up-to-date for restore.

Build FAILED.
    2 Warning(s)
    3 Error(s)
```

Assessment: the failure is purely the expected TFM mismatch on project references. The app restore stops before C# compilation, npm install, or webpack targets run, so there were no deeper .NET compilation errors captured in this validation pass.

Follow-up package/API attention for `ChessTrainerApp` once its TFM is raised:

- Update the app from `netcoreapp3.1` to the selected modern target and revalidate ASP.NET Core hosting/startup behavior.
- Replace or modernize `Microsoft.AspNetCore.Authentication.AzureADB2C.UI` / `AddAzureADB2C`, which is a legacy ASP.NET Core 3.1-era Azure AD B2C package/API surface.
- Review `Microsoft.ApplicationInsights.AspNetCore` and `Microsoft.ApplicationInsights.PerfCounterCollector` package versions from the app project.
- Remove/replace the app-local `Microsoft.CodeAnalysis.FxCopAnalyzers` package update with supported analyzer configuration if still needed.
- Revalidate EF Core migrations under `src\ChessTrainerApp\Migrations` against the upgraded `ChessTrainer.Data` EF Core 10 model.
- Revalidate the Node/npm/webpack targets after restore succeeds; they did not run because restore failed first.

Additional out-of-scope observation:

```powershell
dotnet build test\ChessTrainerApp.Test\ChessTrainerApp.Test.csproj
```

This test project currently builds successfully with `0 Warning(s)` and `0 Error(s)` because it has no `ProjectReference` to the app/shared libraries and still targets `netcoreapp3.0`. It remains out of scope and should be upgraded in the follow-up ChessTrainerApp scenario.

## Phase 7 - Negative-evidence grep and Functions host probe

### Removed/deprecated package/type searches

Equivalent recursive content searches were run for the requested patterns. All returned no matches in the requested in-scope paths.

| Pattern | Paths | Result |
|---|---|---|
| `Microsoft.Azure.WebJobs` | `src\IngestionFunctions`, `src\ChessTrainer.Data` (`*.cs`, `*.csproj`) | No matches |
| `Microsoft.Azure.Cosmos.Table` | `src\IngestionFunctions`, `src\ChessTrainer.Data` (`*.cs`, `*.csproj`) | No matches |
| `Microsoft.Azure.Functions.Extensions` | `src\IngestionFunctions` (`*.cs`, `*.csproj`) | No matches |
| `Microsoft.NET.Sdk.Functions` | `src\IngestionFunctions` (`*.csproj`) | No matches |
| `FunctionsStartup` | `src\IngestionFunctions` (`*.cs`) | No matches |
| `AzureFunctionsVersion` | `src\IngestionFunctions` (`*.csproj`) | No matches |
| `AutoMapper.Extensions.Microsoft.DependencyInjection` | `src\ChessTrainer.Data` (`*.csproj`) | No matches |
| `Microsoft.CodeAnalysis.FxCopAnalyzers` | `Directory.Build.targets` | No matches |

### Functions Core Tools probe

Command:

```powershell
func --version
```

Result:

```text
4.0.7030
```

Because Functions Core Tools v4 is installed, host startup was probed:

```powershell
cd src\IngestionFunctions
func start --no-build
```

Result: N/A for host startup/discovery. The command did not reach host startup or function discovery; it prompted interactively for a worker runtime instead:

```text
Use the up/down arrow keys to select a worker runtime:
1. dotnet (isolated worker model)
2. dotnet (in-process model)
3. node
4. python
5. powershell
6. custom
Choose option: ...
```

Interpretation: local startup requires local configuration/environment to specify the isolated worker runtime. Developers must set `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated` in their local settings/environment before using `func start --no-build`.

## Phase 8 - Hand-off follow-ups

- **ChessTrainerApp upgrade**: Run a follow-up scenario to upgrade `src\ChessTrainerApp` and `test\ChessTrainerApp.Test` to the selected modern TFM, update ASP.NET Core/auth/Application Insights packages, revalidate EF migrations, and rerun frontend webpack/npm build targets.
- **Polly v8 / Microsoft.Extensions.Http.Resilience**: `src\IngestionFunctions` remains on `Microsoft.Extensions.Http.Polly`; migrating to the newer resilience stack is intentionally deferred.
- **Real Functions test coverage**: The upgraded `test\IngestionFunctions.Test` project passes, but it still has only the existing minimal/stub coverage. Add meaningful function/unit/integration coverage in a follow-up.
- **EF migrations location**: EF migrations remain in `src\ChessTrainerApp\Migrations`; moving or regenerating them is a follow-up tied to the app upgrade.
- **Local Functions development**: Developers must update local configuration so `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated` before starting the function host locally.

## Done-when checklist

- [x] All 5 in-scope projects target `net10.0` only.
- [x] `dotnet restore` succeeds for each in-scope project.
- [x] `dotnet build` succeeds for each in-scope project with `0 Warning(s)` and `0 Error(s)`.
- [x] `dotnet test test\IngestionFunctions.Test\IngestionFunctions.Test.csproj` passes: total `1`, passed `1`, failed `0`.
- [x] Expected `src\ChessTrainerApp\ChessTrainerApp.csproj` failure captured with exact warnings/errors.
- [x] Negative-evidence greps show no remaining in-scope references to the removed/deprecated packages or types requested.
- [x] Functions Core Tools probe documented with N/A reason.
- [x] Hand-off follow-ups documented.
