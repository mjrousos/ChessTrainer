---
name: code-review
description: Review code changes in ChessTrainer for correctness, performance, and consistency with project conventions. Use when reviewing PRs or code changes. Always launched as a parallel multi-model review (2-3 sub-agents with distinct model families) unless the environment cannot support it.
---

# ChessTrainer Code Review

Review code changes against the conventions, architecture, and gotchas documented in [`AGENTS.md`](../../../AGENTS.md). The "Code review checklist" section of that file is the source of truth for repo-specific review-blocking rules; this skill describes *how* to conduct the review.

> **Before doing anything else:** If you are responding to a review request (e.g., `/review`, "please review PR #X", "look at this change") and you have the `task` tool with a `model` parameter, your *next action* is to enumerate available models, select 2-3 from distinct families per the rules in [Step 0: Orchestration](#step-0-orchestration--fan-out-first), and launch them in parallel. Do not perform the review yourself, do not delegate the whole review to a single sub-agent, and do not read further until those sub-agents are running.

**Reviewer mindset:** Be polite but very skeptical. Your job is to help speed the review process for maintainers, which includes not only finding problems the PR author may have missed but also questioning the value of the PR in its entirety. Treat the PR description and linked issues as claims to verify, not facts to accept. Question the stated direction and probe edge cases.

## Two Roles, Two Responsibilities

This skill is executed by two distinct kinds of agents. Know which one you are before reading further.

- **Orchestrator** — the agent that received the review request from the user (e.g., the CLI agent responding to `/review`). Your job is to launch N parallel reviewer sub-agents with **distinct models** and synthesize their outputs into one unified review. You do **not** perform the review yourself, and you do **not** delegate the whole review to a single sub-agent. Skip to [Step 0: Orchestration](#step-0-orchestration--fan-out-first).
- **Reviewer sub-agent** — an agent invoked by the orchestrator via the `task` tool with `agent_type: code-review` and a specific `model`. Your job is to execute [Step 1: Gather Code Context](#step-1-gather-code-context-no-pr-narrative-yet) through [Step 5: Detailed Analysis](#step-5-detailed-analysis) on your assigned model and return findings to the orchestrator. Do **not** fan out further, and do **not** post comments to GitHub — the orchestrator handles synthesis and posting.

## When to Use This Skill

Use this skill when:
- Reviewing a PR or code change in `mjrousos/ChessTrainer`
- Checking your own code for correctness, performance, style, or consistency before opening a PR
- Asked to review, critique, or provide feedback on a change
- Validating that a change follows ChessTrainer conventions

## Review Process

### Step 0: Orchestration — Fan Out First

**This step is for the orchestrator only.** Reviewer sub-agents skip to Step 1.

**Multi-model review is the default execution mode.** A review is incomplete unless ≥2 model families have reviewed independently. Skip the fan-out *only* if the `task` tool has no `model` parameter, if fewer than 2 eligible model families exist after applying the selection rules below, or if there is no `task` tool available — and in these cases, explicitly state the reason in the final output.

**Model selection rules:**
- Pick only from models explicitly listed as available in the environment. Do not guess or assume model names.
- Select 2-3 models from distinct model families (e.g., one Claude, one GPT, one Gemini). If fewer than 2 eligible families are available, use what is available.
- From each selected family, pick the highest-capability tier ("premium" or "standard" over "fast/cheap"). Never pick models labeled "mini", "fast", or "cheap".
- Do not select the same model that is already running the primary/orchestrator session — the goal is diverse perspectives.
- **Do not use `gpt-5.4`** (last verified 2026-06-12; revisit after 2026-09-12 — remove this exclusion if the gpt-5.4 sub-agent timeout issue has been resolved upstream) — known sub-agent timeout issues. Prefer `gpt-5.5` or `gpt-5.3-codex`; otherwise the highest-version non-blocked GPT model that satisfies the other rules.
- If multiple standard-tier models exist in the same family (excluding blocked models), pick the highest version. Prefer `-codex` variants over general-purpose for code review.

**Orchestrator pattern (do this, not a single delegation):**

```
# In ONE response, launch all sub-agents in parallel:
parallel:
  task(agent_type=code-review, model=<claude family>,  mode=background, prompt=<full review prompt>)
  task(agent_type=code-review, model=<gpt family>,     mode=background, prompt=<full review prompt>)
  task(agent_type=code-review, model=<gemini family>,  mode=background, prompt=<full review prompt>)

# Then wait for completion notifications, read each with read_agent, and synthesize.
```

Give every sub-agent the **same prompt**: the PR number/diff, a pointer to this skill and [`AGENTS.md`](../../../AGENTS.md), instructions to produce findings in the severity format from [Review Output Format](#review-output-format), and an instruction **not to post comments to GitHub** — synthesis and posting are the orchestrator's job.

**Synthesis:**
1. Wait for all sub-agents. **Timeout handling:** if a sub-agent has not completed after 10 minutes and you have results from ≥2 others, proceed without it and note which model is missing.
2. Deduplicate findings that appear across models.
3. Elevate confidence on issues flagged by multiple models (mark with `✕N` where N is the count).
4. Include unique findings from individual models that meet the confidence bar in the [Detailed Analysis](#step-5-detailed-analysis) rules.
5. Produce one unified review in the [Review Output Format](#review-output-format). The `_Reviewed by: …_` line at the bottom is **mandatory** — see the output format section.
6. After posting the review, immediately exit. Do not wait for any remaining sub-agents.

**If no `task` tool is available or if the environment only has one eligible model**, perform the review yourself by executing Steps 1-5 below, and explicitly note the single-model limitation in the final output.

### Step 1: Gather Code Context (No PR Narrative Yet)

Before analyzing anything, collect as much relevant **code** context as you can. **Critically, do NOT read the PR description, linked issues, or existing review comments yet.** Form your own independent assessment of what the code does, why it might be needed, what problems it has, and whether the approach is sound — before being exposed to the author's framing. Reading the author's narrative first anchors your judgment and makes you less likely to find real problems.

1. **Diff and file list.** Fetch the full diff and the list of changed files (`gh pr diff <number>`, `gh pr view <number> --json files`).
2. **Full source files.** For every changed file, read the **entire file**, not just the diff hunks. You need the surrounding code to understand invariants, DI registrations, AutoMapper profiles, and call patterns. Diff-only review is the #1 cause of false positives and missed issues.
3. **Consumers and callers.** If the change modifies a repository interface, a public model, a Razor component, or anything in `ChessTrainer.Common` or `Engine`, search for callers across the solution. Pay special attention to whether `MjrChess.Trainer.Data.Models.*` types are leaking outside `ChessTrainer.Data` (see the checklist).
4. **Sibling code.** If the change fixes a bug or adds a pattern in one place, check whether sibling types have the same issue (e.g., other repositories deriving from `EFRepository<TEntity, TPublicModel>`, other Razor components in `Pages/` and `Components/`, the matching Lichess vs. Chess.com ingestion path).
5. **Configuration touchpoints.** If the change touches connection strings, auth, telemetry, or `Startup.cs`/`Program.cs`, verify both naming conventions (`ConnectionStrings:PuzzleDatabase` for the app vs. `PuzzleDbConnectionString` env var for IngestionFunctions and `PuzzleDbContextFactory`) and the "opt-in" rules for Application Insights are respected.
6. **README files in the area.** Walk from the changed directory up to the repo root and read any `README.md` files you find (especially `src/IngestionFunctions/README.md` and `src/ChessTrainerApp/app/README.md`). They contain build/run/auth recipes that the diff may break.
7. **Git history.** `git --no-pager log --oneline -20 -- <file>` on changed files reveals recent churn, reverts, or prior attempts to fix the same problem.

### Step 2: Form an Independent Assessment

Based **only** on the code context gathered above, answer:

1. **What does this change actually do?** Describe the behavioral change in your own words. What was the old behavior? What is the new behavior?
2. **Why might this change be needed?** Infer the motivation from the code itself. What bug, gap, or improvement does it appear to address?
3. **Is this the right approach?** Would a simpler alternative be more consistent with the codebase? Could the goal be achieved with existing functionality (e.g., the generic `EFRepository<TEntity, TPublicModel>` instead of a new specialized repository, an existing AutoMapper profile entry instead of hand-rolled mapping)? Are there correctness, performance, EF Core query, or thread-safety concerns?
4. **What problems do you see?** Bugs, edge cases, missing validation, missing `OrderBy` before paging (EF Core `EF[10102]`/`EF[10103]` warnings), nullable annotation gaps, test gaps, missing migrations, Blazor Server lifecycle issues (`OnInitializedAsync` vs. `OnAfterRenderAsync`, `await InvokeAsync(StateHasChanged)` from background threads), `IDisposable`/`IAsyncDisposable` leaks, and anything else that concerns you.

Write down your independent assessment before proceeding.

### Step 3: Incorporate PR Narrative and Reconcile

Now read the PR description, labels, linked issues (in full), author information, and any existing review comments. Treat all of this as **claims to verify**, not facts to accept.

1. Fetch metadata: `gh pr view <number> --comments`, and read each linked issue.
2. Reconcile your assessment with the author's claims. Where your independent reading of the code disagrees with the PR description or issue, investigate further — but do not simply defer to the author's framing.
3. Update your holistic assessment only if the additional context genuinely changes your evaluation (e.g., a linked issue proves the bug is real). Do not soften findings just because the PR description sounds reasonable.

### Step 4: Apply the ChessTrainer Code Review Checklist

Walk the diff against every rule in the **Code review checklist** section of [`AGENTS.md`](../../../AGENTS.md). Today that means at minimum:

- **Never silence warnings.** Compiler/analyzer warnings, `npm`/webpack `compiled with N warnings`, and EF Core runtime `warn:` (`EF[10102]`/`EF[10103]`) all count and all must be zero. No `#pragma warning disable`, `<NoWarn>`, `[SuppressMessage]`, or rule-set relaxation without explicit user approval. Don't disable `Nullable` to dodge a warning — fix the annotation.
- **Don't leak `MjrChess.Trainer.Data.Models.*` outside `ChessTrainer.Data`.** Consumers depend on `MjrChess.Trainer.Models.*` from `ChessTrainer.Common`; AutoMapper bridges via `EFRepository<TEntity, TPublicModel>` (or specialized repos like `TacticsPuzzleRepository`).
- **Connection strings have two names — update both.** `ConnectionStrings:PuzzleDatabase` (app) and `PuzzleDbConnectionString` env var (functions + `PuzzleDbContextFactory`).
- **Don't depend on Azure AD B2C user-flow IDs.** The configured B2C tenant is dead (issue #39, migrating to Entra External ID).
- **Application Insights is opt-in.** Only call `AddApplicationInsightsTelemetry()` when a connection string or instrumentation key is configured; use `GetService<TelemetryConfiguration>()`, never `GetRequiredService`.
- **Respect the code style rules** (file-final newline, 4-space indent, `using` directives outside the namespace, no hand edits to `wwwroot/dist`).

Also check the always-applicable items:

- **Tests.** New behavior needs tests. Follow the conventions in [`.github/instructions/testing.instructions.md`](../../../.github/instructions/testing.instructions.md) (xUnit + bUnit, parallelization, env-var helpers, the migration regression guard). For data-layer changes, run `dotnet ef migrations has-pending-model-changes --project src\ChessTrainer.Data` mentally — if the entity changed but no migration was added, flag it.

### Step 5: Detailed Analysis

1. **Focus on what matters.** Prioritize bugs, performance regressions (especially N+1 EF queries, missing `AsNoTracking`, missing `OrderBy` before `Skip`/`Take`), safety issues, race conditions, resource leaks, and incorrect assumptions about Blazor Server's circuit/scoped DI lifetime. Do not comment on trivial style issues unless they violate an explicit rule.
2. **Consider collateral damage.** For every changed code path, brainstorm: what other callers, scenarios, or inputs flow through this code? Could any of them break? Surface plausible risks even if you can't fully confirm them — the tradeoff is the author's decision; your job is to make it visible.
3. **Be specific and actionable.** Every comment should tell the author exactly what to change and why. Reference the relevant convention. Include evidence of how you verified the issue is real (e.g., "looked at all callers of `IRepository<Puzzle>.GetAsync` and none of them pass a tracking token").
4. **Flag severity clearly:**
   - ❌ **error** — Must fix before merge. Bugs, security issues, convention violations from the checklist, test gaps for behavior changes, missing EF migrations.
   - ⚠️ **warning** — Should fix. Performance issues, missing validation, inconsistency with established patterns, missing PR-feedback follow-up (reply + resolve).
   - 💡 **suggestion** — Consider changing. Minor readability wins, optional optimizations, follow-up ideas.
5. **Don't pile on.** If the same issue appears many times, flag it once with a note listing all affected files.
6. **Respect existing style.** When modifying existing files, the file's current style takes precedence over general guidelines.
7. **Don't flag what CI catches.** StyleCop, the Release-build warning gate, and `dotnet test` will catch their own things; don't duplicate them.
8. **Avoid false positives.** Before flagging any issue:
   - **Verify the concern actually applies** given the full context, not just the diff. Confirm the issue isn't already handled by a caller, callee, or wrapper layer (e.g., AutoMapper config, base repository).
   - **Skip theoretical concerns with negligible real-world probability.** "Could happen" is not the same as "will happen."
   - **If you're unsure**, either investigate further until you're confident, or surface it explicitly as a low-confidence question rather than a firm claim.
   - **Trust the author's context.** If a pattern seems odd but is consistent with the repo, assume it's intentional and ask rather than assert.
   - **Never assert that something "does not exist," "is deprecated," or "is unavailable" based on training data alone.** Your knowledge has a cutoff date. When uncertain, ask.
9. **Ensure code suggestions are valid.** Any code you suggest must compile under the repo's analyzers (Nullable on, StyleCop, `TreatWarningsAsErrors` in Release).
10. **Label in-scope vs. follow-up.** Distinguish between issues the PR should fix and out-of-scope improvements.

## Repository-Specific Review Guidelines

This section captures review-time gotchas specific to ChessTrainer that go beyond the always-applicable checklist in `AGENTS.md`. It will grow over time as we discover new things worth watching for. When you encounter a class of issue worth remembering, add it here.

### Data layer (EF Core, AutoMapper, migrations)

- **Schema changes need a migration.** Any change to a class under `src/ChessTrainer.Data/Models/` or to `PuzzleDbContext` configuration must come with a `dotnet ef migrations add` migration in the same PR. Verify with `dotnet ef migrations has-pending-model-changes --project src\ChessTrainer.Data` (also enforced by the migration regression test).
- **AutoMapper profile updates.** New properties on either side of an entity ↔ public model pair must be reflected in the AutoMapper profile. Silent drops are easy to miss in review — open the profile alongside the model change.
- **Query shape.** Watch for `Skip`/`Take` without a preceding `OrderBy` (causes the `EF[10102]` warning), and for unbounded `ToListAsync` on tables that can grow (puzzles, games).

### Blazor Server (`ChessTrainerApp`)

- **Lifecycle.** `OnInitializedAsync` runs twice in prerendered Server mode; reserve interop and `HttpContext`-bound work for `OnAfterRenderAsync(firstRender)`.
- **Threading.** State mutations from background threads (timers, `IObservable` callbacks, ingestion notifications) must go through `await InvokeAsync(StateHasChanged)`.
- **Disposal.** Components that subscribe to engine events or hold `IDisposable` services must implement `IDisposable`/`IAsyncDisposable` and unsubscribe.
- **Front-end assets.** Source goes in `src/ChessTrainerApp/app/` and is bundled by webpack into `wwwroot/dist/`. Reject hand edits to `wwwroot/dist` (they will be clobbered).

### Engine (`MjrChess.Engine`)

- The engine is **not** UCI (issue #35). Don't introduce code that assumes a UCI handshake, stdio protocol, or external engine process.
- Move generation and validation are performance-sensitive — flag allocations on hot paths (e.g., LINQ over move lists per ply, boxing of `struct` move values).

### IngestionFunctions (Azure Functions, isolated worker)

- Reads `PuzzleDbConnectionString` from environment (not `ConnectionStrings:*`). When a PR adds a new setting, confirm both `local.settings.json.example` and the README are updated.
- HTTP calls to Lichess/Chess.com must use the injected `IHttpClientFactory` clients (named) so retry/timeout policies stay consistent. Flag `new HttpClient(...)` in services.

### Auth (Microsoft Identity Web, migrating to Entra External ID)

- Don't add new code that hard-codes `B2C_1_*` user-flow IDs or assumes the B2C tenant is reachable — issue #39 is replacing it.
- Don't register new `[Authorize]` policies without confirming the role/claim is actually emitted by the configured identity provider.

### PR feedback follow-up

- A round of review is "done" only when **every** thread has both a reply (citing the fix commit SHA when possible) and a resolved state. If you notice unresolved threads or threads with a code fix but no reply, flag it as ⚠️.

---

## Review Output Format

When presenting the final review (whether as a PR comment or as output to the user), use this structure.

> 📝 **AI-generated content disclosure:** When posting review content to GitHub under a user's credentials (i.e., the account is **not** a dedicated "copilot" or "bot" account/app), include a concise visible note (e.g. a `> [!NOTE]` alert) indicating the content was AI/Copilot-generated. Skip this only if the user explicitly asks you to omit it.

### Structure

```
## 🤖 Copilot Code Review — PR #<number>

### Holistic Assessment

**Motivation**: <1-2 sentences on whether the PR is justified and the problem is real>

**Approach**: <1-2 sentences on whether the change takes the right approach for this codebase>

**Summary**: <✅ LGTM / ⚠️ Needs Human Review / ⚠️ Needs Changes / ❌ Reject>. <2-3 sentence summary of the verdict. If "Needs Human Review," explicitly state which findings you want a human to weigh in on.>

### Findings

#### ❌ Errors (must fix)
- **<file>:<line>** — <description>. <why it matters + suggested fix>

#### ⚠️ Warnings (should fix)
- **<file>:<line>** — <description>.

#### 💡 Suggestions
- **<file>:<line>** — <description>.

### Out of Scope / Follow-ups
- <any improvements deliberately left for a later PR>

_Reviewed by: <model A>, <model B>, <model C>. Issues marked ✕N were flagged by N models._
```

**The `_Reviewed by: …_` line is mandatory.** Every posted review must end with it, listing every model that contributed (including the orchestrator's synthesis model only if it added analysis beyond synthesis). If only one model is listed, the orchestrator skipped multi-model and the review is incomplete — re-run with the orchestrator pattern in [Step 0](#step-0-orchestration--fan-out-first). If multi-model was genuinely impossible (no `model` parameter, or <2 eligible families), say so explicitly on that line instead, e.g. `_Reviewed by: claude-opus-4.7 (single-model: only one eligible family available)._`

If no issues were found, say so explicitly with ✅ LGTM and skip the empty sections.
