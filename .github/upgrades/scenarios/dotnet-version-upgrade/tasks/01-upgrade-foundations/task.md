# 01-upgrade-foundations: Prepare shared build settings for net10.0

Execute task 01-upgrade-foundations.

## Research Findings

### Projects Affected
- Solution-wide build configuration applies to all in-scope projects: `src\Engine\Engine.csproj`, `src\ChessTrainer.Common\ChessTrainer.Common.csproj`, `src\ChessTrainer.Data\ChessTrainer.Data.csproj`, `src\IngestionFunctions\IngestionFunctions.csproj`, and `test\IngestionFunctions.Test\IngestionFunctions.Test.csproj`.
- No project file should be modified in this task; project retargeting is deferred to later tier tasks.

### Files to Modify
- `Directory.Build.props`: change `LangVersion` from `8.0` to `latest`; keep nullable enabled.
- `Directory.Build.targets`: remove deprecated `Microsoft.CodeAnalysis.FxCopAnalyzers`; update `StyleCop.Analyzers` from `1.1.1-beta.61` to a Roslyn 4.x-compatible version.
- `global.json`: already pins .NET SDK `10.0.300` with `latestFeature` roll-forward; no change needed.
- `rules.ruleset`: existing CA and StyleCop suppressions remain in place; no change needed unless validation shows it cannot load.

### Packages to Update
- `StyleCop.Analyzers`: verified `1.2.0-beta.556` exists on NuGet.org and is the selected compatible version.
- `Microsoft.CodeAnalysis.FxCopAnalyzers`: remove from shared targets because it is deprecated and flagged by the assessment across all in-scope projects.

### API Changes
- None. This task only changes shared MSBuild configuration and analyzer package references.

### Dependencies & Risks
- Analyzer changes affect every project through `Directory.Build.targets`; validation is limited to restore of `src\Engine\Engine.csproj` per task scope.
- `src\ChessTrainerApp\ChessTrainerApp.csproj` contains an out-of-scope `PackageReference Update` for FxCopAnalyzers; it is not modified by this foundations task.
- Full builds may still surface analyzer warnings before later project retargeting tasks; this is expected and outside this task's validation scope.

### Decisions Made
- Execute as-is; no decomposition needed because this is one uniform solution-wide configuration concern touching four or fewer foundation files.
- Do not introduce Central Package Management.
- Do not touch any `.csproj` file or target frameworks in this task.
