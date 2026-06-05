# Copilot instructions for ChessTrainer

These notes capture project conventions for AI coding assistants (e.g. the
GitHub Copilot CLI) working in this repository. Human contributors are
welcome to follow them too.

## PR feedback workflow

When addressing review comments on a pull request, the task is **not**
complete after pushing a fix commit. Always:

1. **Reply to every review comment** with a short note explaining what
   was changed and, where possible, citing the commit SHA of the fix.
2. **Mark each thread as resolved** once a reply has been posted and
   the fix has landed on the PR branch.

A PR feedback round is only "done" when every comment thread has both
(a) a reply and (b) a resolved state. Pushing the code change without
the reply + resolve is treated as incomplete work.

## Build hygiene

Builds must complete with **zero errors and zero warnings** before any
change is considered done. This applies to:

* The local Debug build you use while iterating.
* The Release build (which has `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
  in `Directory.Build.props`, so warnings will fail the build outright).
* Front-end / npm builds (e.g. `npm run webpack-prod` for `ChessTrainerApp`)
  — `compiled successfully` only, no `compiled with N warnings`.
* Runtime logs during smoke tests — fix anything that surfaces as a
  `warn:` from framework loggers (e.g. EF Core query warnings such as
  EF[10102]/[10103]), not just compiler warnings.

Rules when a warning appears:

1. **Fix the underlying cause.** Add the missing `OrderBy`, tighten the
   nullable annotation, etc. — don't paper over it.
2. **Never silence a warning to make the build pass** — no
   `#pragma warning disable`, `<NoWarn>` entries, `[SuppressMessage]`
   attributes, or analyzer rule-set tweaks unless the user explicitly
   approves that specific suppression. Third-party deprecation noise
   (e.g. legacy SCSS deep inside `node_modules`) can be quieted only at
   the tool boundary (e.g. `quietDeps: true` for sass-loader), never
   inside our own source.
3. **Run both Debug and Release** at least once before declaring a task
   complete, since `TreatWarningsAsErrors` only fires in Release.
4. **Pre-existing warnings count too.** If you touch a file or project,
   you own the warnings it surfaces — clean them up as part of the
   change rather than leaving them for the next person.

## Solution layout

This is a multi-project .NET 10 solution. **Project folder names don't
match their root namespaces** — keep this in mind when adding `using`
directives or searching for symbols:

| Project (`src/`)     | Root namespace                          | Role |
|----------------------|-----------------------------------------|------|
| `ChessTrainerApp`    | `MjrChess.Trainer`                      | Blazor Server web app + Razor Pages auth UI. Entry point (`Program.cs`/`Startup.cs`). |
| `ChessTrainer.Data`  | `MjrChess.Trainer.Data`                 | EF Core `PuzzleDbContext`, repositories, migrations, AutoMapper profile. |
| `ChessTrainer.Common`| `MjrChess.Trainer` (`MjrChess.Trainer.Models` for the model types, which live under `Models/`) | Public domain models shared across projects. |
| `Engine`             | `MjrChess.Engine`                       | In-house chess move generator/validator (`ChessEngine`). Not a UCI engine — see issue #35. |
| `IngestionFunctions` | `IngestionFunctions` (`IngestionFunctions.Services`, `IngestionFunctions.Models`) | Azure Functions (isolated worker) that scrape Lichess / Chess.com and queue games. See `src/IngestionFunctions/README.md` for the full setup story (Azurite + SQL container + `func start`); don't re-derive it. |

Tests in `test/` mirror project names (e.g. `test/ChessTrainer.Data.Test`). xUnit + Coverlet, .NET 10.

## Build, test, and run

```powershell
# Full solution (Release fails on any warning — see Directory.Build.props)
dotnet build ChessTrainer.sln -c Release
dotnet test  ChessTrainer.sln -c Release

# Single test method (xUnit filter syntax)
dotnet test test/ChessTrainer.Data.Test --filter "FullyQualifiedName~MyClass.MyMethod"

# Or a whole class / namespace
dotnet test test/ChessTrainer.Data.Test --filter "FullyQualifiedName~MyClass"

# Run the Blazor app (Debug)
dotnet run --project src/ChessTrainerApp

# Front-end (auto-invoked by the ChessTrainerApp build via the
# `DebugRunWebpack` MSBuild target when wwwroot/dist is missing, so a
# plain `dotnet build` is usually enough). Direct commands:
cd src/ChessTrainerApp
npm install
npm run webpack-dev      # one-off dev build
npm run watch            # rebuild on change while iterating on SCSS / JS
npm run webpack-prod     # production build (zero-warning requirement)
```

The web app serves static assets from **`wwwroot/dist`**, not `wwwroot/`
— `Program.cs` calls `UseWebRoot("wwwroot/dist")` and webpack writes
there. New static assets go under `src/ChessTrainerApp/app/` (let
webpack bundle/copy them); don't hand-edit `wwwroot/dist`.

There's no GitHub Actions CI workflow yet (tracked in issue #36), so
**run both Debug and Release builds locally** before claiming a task is
done — `TreatWarningsAsErrors` only fires in Release.

## Architecture notes

### Data layer: `Data.Models` ↔ public `Models` via AutoMapper

`ChessTrainer.Data` deliberately keeps EF entities separate from the
public domain models, and AutoMapper bridges between them:

- `MjrChess.Trainer.Data.Models.*` — EF Core entities (on-disk shape).
- `MjrChess.Trainer.Models.*` — public domain models exposed to the
  rest of the app (lives in `ChessTrainer.Common`).

Repositories are registered as `IRepository<TPublicModel>` (see
`DataExtensions.AddChessTrainerData`) and the generic
`EFRepository<TEntity, TPublicModel>` (or a specialized repo like
`TacticsPuzzleRepository`) does the mapping. **Don't leak
`Data.Models.*` types out of `ChessTrainer.Data`** — every consumer
depends on the public `Models` namespace.

### Database connection string has two names

The same SQL database is read under two different configuration keys
depending on the host. When changing connection details, update both:

- **ChessTrainerApp** → `ConnectionStrings:PuzzleDatabase` (the key is
  `PuzzleDatabase`, not `PuzzleDb`).
- **IngestionFunctions** → `PuzzleDbConnectionString` env var. This is
  also the variable `PuzzleDbContextFactory` reads, which is why
  `dotnet ef` commands work against `ChessTrainer.Data` directly with
  no startup project. See `src/IngestionFunctions/README.md` for the
  full `dotnet ef` recipe.

### Authentication

`Startup.cs` wires Microsoft Identity Web
(`AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAdB2C"))`).
The configured Azure AD B2C tenant's OIDC metadata endpoint no longer
responds, so sign-in is currently broken — tracked in issue #39, with
migration to Microsoft Entra External ID planned. Don't add code that
depends on B2C-specific user-flow IDs (`B2C_1_*`); they'll go away with
the migration.

### Application Insights is opt-in

`AddApplicationInsightsTelemetry()` is registered **only** when
`ApplicationInsights:ConnectionString` or
`ApplicationInsights:InstrumentationKey` is configured. The 3.x SDK
throws on startup if called with neither, so don't unconditionally
register telemetry, and use `GetService<TelemetryConfiguration>()`
(not `GetRequiredService`) anywhere you reach for it.

### Health endpoint already exists

`endpoints.MapHealthChecks("/hc")` is wired up in `Startup.cs`. Issue
#38 tracks splitting it into `/healthz` (liveness) and `/readyz`
(readiness) with tagged DB checks — don't add a parallel endpoint.

### Azure deployment

ARM-based deployment lives in `infrastructure/ChessTrainerRG/`
(`.deployproj`, `azuredeploy.json`, `azuredeploy.parameters.json`,
`Deployment.md`). Issue #31 tracks modernizing this to Bicep +
GitHub Actions; until then, treat the existing files as the source of
truth for prod resource shape.

## Code style

- C# `latest` with `Nullable` enabled solution-wide
  (`Directory.Build.props`). Don't disable nullable at file or project
  level to silence warnings — fix the annotation.
- StyleCop.Analyzers is added to every project by
  `Directory.Build.targets`, with rules tuned in `rules.ruleset` and
  `stylecop.json`: 4-space indent, system `using` directives NOT
  required first, blank line required between using groups, `using`
  directives outside the namespace, file must end with a newline. Match
  this style — don't relax the ruleset to make warnings go away
  (Build hygiene above applies).
- Razor `@page` routes live under `src/ChessTrainerApp/Pages/`;
  reusable Razor components live under
  `src/ChessTrainerApp/Components/` and `Shared/`.