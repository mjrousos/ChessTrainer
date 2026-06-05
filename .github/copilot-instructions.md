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