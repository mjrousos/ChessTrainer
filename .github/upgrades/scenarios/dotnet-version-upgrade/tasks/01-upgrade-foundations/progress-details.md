## Files Modified
- `Directory.Build.props`
- `Directory.Build.targets`
- `.github\upgrades\scenarios\dotnet-version-upgrade\tasks\01-upgrade-foundations\task.md`
- `.github\upgrades\scenarios\dotnet-version-upgrade\tasks\01-upgrade-foundations\progress-details.md`

## Build Result
- Errors: 0
- Warnings: 0
- Projects built: None (per task scope; restore validation only)
- Projects restored: `src\Engine\Engine.csproj`

## Test Result
- Tests run: 0
- Passed: 0
- Failed: 0

## Changes Summary
- Updated shared language configuration to use `LangVersion` `latest` while keeping nullable enabled.
- Removed deprecated `Microsoft.CodeAnalysis.FxCopAnalyzers` from shared build targets.
- Updated `StyleCop.Analyzers` to `1.2.0-beta.556`, verified on NuGet.org.
- Verified `global.json` uses .NET SDK `10.0.300` and `rules.ruleset` is XML-valid and resolves through MSBuild.

## Issues Encountered
- None
