# Copilot instructions for ChessTrainer

> The full project conventions, architecture notes, build commands, and PR feedback workflow live in [`AGENTS.md`](../AGENTS.md) at the repo root. That file is the source of truth, and is read by Copilot Chat, Copilot CLI, and the Copilot cloud agent (as well as by Codex and Claude Code).
>
> This file mirrors only the **Code review checklist** from `AGENTS.md`, so that Copilot Code Review (which truncates any instruction file past ~4,000 characters) reliably sees every review-actionable rule. When you change a rule below, update the matching section in `AGENTS.md` too.

**When reviewing pull requests, use the [`code-review`](../.agents/skills/code-review/SKILL.md) skill unless the user has stated they will review the changes themselves.**

## Code review checklist

Apply these rules to every change.

### Never silence warnings
- Warnings include compiler/analyzer warnings, npm/webpack `compiled with N warnings`, and framework runtime `warn:` output during smoke tests (e.g. EF Core `EF[10102]`/`EF[10103]`). All count; all must be zero.
- Fix the underlying cause (add the missing `OrderBy`, tighten the nullable annotation, etc.).
- Don't disable `Nullable` at file or project level — fix the annotation.
- No `#pragma warning disable`, `<NoWarn>` entries, `[SuppressMessage]` attributes, or analyzer rule-set relaxations unless the user explicitly approves that specific suppression. Approved suppressions must be narrow and include a justification comment.
- Third-party deprecation noise (e.g. legacy SCSS in `node_modules`) may be quieted only at the tool boundary (e.g. `quietDeps: true` for sass-loader), never inside our source.

### Don't leak `Data.Models.*` outside `ChessTrainer.Data`
EF entities (`MjrChess.Trainer.Data.Models.*`) are deliberately separate from public domain models (`MjrChess.Trainer.Models.*` in `ChessTrainer.Common`). Repositories are registered as `IRepository<TPublicModel>`; the generic `EFRepository<TEntity, TPublicModel>` (or specialized repos like `TacticsPuzzleRepository`) does the AutoMapper bridging. Consumers depend on the public `Models` namespace only.

### Connection string has two names — update both
- **ChessTrainerApp** → `ConnectionStrings:PuzzleDatabase` (key is `PuzzleDatabase`, not `PuzzleDb`).
- **IngestionFunctions** → `PuzzleDbConnectionString` env var. Also what `PuzzleDbContextFactory` reads for `dotnet ef` tooling. See `src/IngestionFunctions/README.md` for the full recipe.

### Auth: don't depend on Azure AD B2C user-flow IDs
`Startup.cs` wires Microsoft Identity Web against B2C, but the configured tenant is dead (issue #39, migrating to Entra External ID). Don't add code that depends on `B2C_1_*` user-flow IDs — they're going away.

### Application Insights is opt-in
Register `AddApplicationInsightsTelemetry()` **only** when `ApplicationInsights:ConnectionString` or `ApplicationInsights:InstrumentationKey` is configured — the 3.x SDK throws on startup if called with neither. Use `GetService<TelemetryConfiguration>()`, not `GetRequiredService`.

### Code style
- C# `latest`, `Nullable` enabled solution-wide (`Directory.Build.props`).
- StyleCop.Analyzers is added by `Directory.Build.targets`; rules tuned in `rules.ruleset` and `stylecop.json`: 4-space indent; system `using` directives NOT required first; blank line between using groups; `using` directives outside the namespace; file must end with newline.
- Razor: `@page` routes in `src/ChessTrainerApp/Pages/`; reusable components in `src/ChessTrainerApp/Components/` and `Shared/`.
- Static web assets go under `src/ChessTrainerApp/app/` (webpack bundles them into `wwwroot/dist/`, which `Program.cs` serves via `UseWebRoot("wwwroot/dist")`). Don't hand-edit `wwwroot/dist`.
