# Progress Details: 04-data-access

## Status
complete

## Summary
- Retargeted `src\ChessTrainer.Data\ChessTrainer.Data.csproj` from `netstandard2.1` to single-target `net10.0`.
- Updated EF Core and configuration packages to the latest stable `10.0.x` line available on NuGet.org (`10.0.8`).
- Replaced deprecated `AutoMapper.Extensions.Microsoft.DependencyInjection` with explicit `AutoMapper` and upgraded expression mapping to a compatible stable pair.
- Updated scoped AutoMapper configuration to pass `ILoggerFactory` to `MapperConfiguration` and enabled expression mapping for existing `MapExpression` repository queries.
- Preserved `AddChessTrainerData(this IServiceCollection services, string dbConnectionString)` and `IRepository<T>` public signatures.

## Package Versions Chosen
- `AutoMapper` `16.1.1`
- `AutoMapper.Extensions.ExpressionMapping` `11.0.0`
- `Microsoft.EntityFrameworkCore.Design` `10.0.8`
- `Microsoft.EntityFrameworkCore.SqlServer` `10.0.8`
- `Microsoft.Extensions.Configuration` `10.0.8`

## Research Notes
- `AutoMapper.Extensions.ExpressionMapping` `11.0.0` declares `AutoMapper` dependency range `[16.1.1, 17.0.0)` for `net10.0`; selected `AutoMapper` `16.1.1` to match.
- `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design`, and `Microsoft.Extensions.Configuration` latest stable `10.0.x` version observed was `10.0.8`.
- Audited `PuzzleDbContext`, `EFRepository`, concrete repositories, `AutoMapperProfile`, and data models for EF Core API changes. No source rewrites were required beyond AutoMapper DI/configuration changes.
- Existing `MigrationsAssembly("MjrChess.Trainer")` TODO is clear and was left in place per task scope.

## Build Status
- Baseline before retarget failed as expected: `ChessTrainer.Data` targeting `netstandard2.1` could not reference upgraded `Engine` and `ChessTrainer.Common` `net10.0` projects (`NU1201`).
- `dotnet restore src\ChessTrainer.Data\ChessTrainer.Data.csproj`: succeeded.
- `dotnet build src\ChessTrainer.Data\ChessTrainer.Data.csproj --verbosity minimal`: succeeded, 0 warnings, 0 errors.
- `dotnet build src\Engine\Engine.csproj --verbosity minimal`: succeeded, 0 warnings, 0 errors.
- `dotnet build src\ChessTrainer.Common\ChessTrainer.Common.csproj --verbosity minimal`: succeeded, 0 warnings, 0 errors.
- Output assemblies verified under `bin\ChessTrainer.Data\net10.0`, `bin\Engine\net10.0`, and `bin\ChessTrainer.Common\net10.0`.

## Test Status
- N/A: no test project directly covers `ChessTrainer.Data` in this task scope.

## Done-When Verification
- `ChessTrainer.Data.csproj` targets only `net10.0`.
- EF Core packages are aligned to `10.0.8`.
- `Microsoft.Extensions.Configuration` is aligned to `10.0.8`.
- `AutoMapper.Extensions.Microsoft.DependencyInjection` is no longer referenced.
- Explicit `AutoMapper` package reference exists at `16.1.1`.
- `AutoMapper.Extensions.ExpressionMapping` is stable `11.0.0`, compatible with AutoMapper `16.1.1`.
- `DataExtensions.cs` passes `ILoggerFactory` to `MapperConfiguration` and compiles.
- `AddChessTrainerData(IServiceCollection, string)` signature unchanged.
- `IRepository<T>` interface unchanged.
- Data, Engine, and Common builds all succeed with 0 warnings and 0 errors.

## Issues Encountered
- None remaining.
- No warnings were suppressed.

## Files Changed
- `src\ChessTrainer.Data\ChessTrainer.Data.csproj`
- `src\ChessTrainer.Data\DataExtensions.cs`
- `.github\upgrades\scenarios\dotnet-version-upgrade\tasks\04-data-access\progress-details.md`
