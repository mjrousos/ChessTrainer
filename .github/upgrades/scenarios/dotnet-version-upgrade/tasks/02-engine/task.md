# 02-engine: Retarget the Engine library

## Objective
Retarget `src\Engine\Engine.csproj` from `netstandard2.0` to single-target `net10.0` and validate the Engine library only.

## Research Findings
- Scenario decisions require shared libraries to target single `net10.0`; multi-targeting is explicitly out of scope.
- Assessment for `src\Engine\Engine.csproj` shows SDK-style class library, current `netstandard2.0`, 9 source files, no API compatibility issues, and one optional deprecated analyzer/package finding.
- `Engine.csproj` defines the TFM locally with a singular `<TargetFramework>` element and has no direct package references.
- Repository-wide properties enable nullable analysis and warning level 4; Release builds treat warnings as errors, so validation must produce zero warnings.
- Engine source consists of pure C# chess engine/model/formatter code with nullable annotations already present in key nullable model members.

## Decomposition Decision
No decomposition needed: this is a single leaf project with no package references and a one-line TFM retarget plus project-level validation.

## Done When
- `src\Engine\Engine.csproj` targets only `net10.0`.
- `dotnet restore src\Engine\Engine.csproj` succeeds.
- `dotnet build src\Engine\Engine.csproj` succeeds with 0 errors and 0 warnings.
- No `<TargetFrameworks>` element or `netstandard*` target remains in `Engine.csproj`.
