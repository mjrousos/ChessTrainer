# Copilot customization strategy

This document records decisions about **how** we customize GitHub Copilot (and adjacent AI coding agents) for this repository, and **why** each decision was made. It is a living document — when the customization strategy changes, update this file alongside the change.

It is intended for human maintainers reasoning about the customization stack. The actual instructions Copilot/Codex/Claude Code consume live in [`AGENTS.md`](AGENTS.md) and [`.github/copilot-instructions.md`](.github/copilot-instructions.md); this file is *about* those files.

---

## Tools in scope

The team uses, in expected order of frequency:
1. **GitHub Copilot** — primary AI assistant (Chat, CLI, Cloud agent, Code Review, IDE agent mode).
2. **OpenAI Codex CLI** — occasional use.
3. **Claude Code** — occasional use.

Strategy decisions are made with Copilot as the primary target, but prefer cross-tool compatibility where it costs little.

---

## Decision log

### 1. Instruction file layout: `AGENTS.md` is the source of truth; `.github/copilot-instructions.md` mirrors the review checklist

**Decision.** Put the full project conventions (architecture, build commands, PR workflow, code review checklist, etc.) in [`AGENTS.md`](AGENTS.md) at the repo root. Keep [`.github/copilot-instructions.md`](.github/copilot-instructions.md) as a slim file containing **only** the "Code review checklist" section plus a pointer to `AGENTS.md`.

**Why.**
- **Cross-tool reach.** OpenAI Codex reads `AGENTS.md` natively ([docs](https://developers.openai.com/codex/guides/agents-md): *"Codex reads `AGENTS.md` files before doing any work"*). Claude Code also reads `AGENTS.md`. Copilot Chat, CLI, and Cloud agent read `AGENTS.md` too (documented as "agent instructions" alongside `CLAUDE.md`/`GEMINI.md`).
- **Copilot Code Review cap.** Copilot Code Review only reads the first ~4,000 characters of any custom instruction file ([source](https://docs.github.com/en/copilot/concepts/prompting/response-customization), "Using custom instructions" note). The docs do not explicitly confirm that `AGENTS.md` is in scope of "any custom instruction file", but the natural reading is yes. To eliminate that uncertainty, we keep a self-contained slim copy of the review checklist in `.github/copilot-instructions.md` so Code Review is guaranteed to see every review-actionable rule.
- **Pointers don't follow.** A previous iteration of `AGENTS.md` was just a Markdown pointer to `.github/copilot-instructions.md`. That works for humans but not for Codex or Claude Code — both read the file as literal text and don't follow links. The inversion (full content in `AGENTS.md`) fixes this.
- **Cost of duplication is small.** Only the Code review checklist is duplicated (~2 KB). Contextual sections (PR workflow, solution layout, build commands, companion repo) live only in `AGENTS.md`.

**Drift mitigation.** Both files contain explicit preamble notes reminding contributors that when a checklist rule changes, both files must be updated. The duplicated content is intentionally limited to the checklist to keep the drift surface small.

---

### 2. Order instruction content so code-review-actionable rules come first

**Decision.** Both `AGENTS.md` and `.github/copilot-instructions.md` lead with the "Code review checklist". Contextual sections (Solution layout, PR feedback workflow, build hygiene, build commands) come after.

**Why.** Copilot Code Review truncates at ~4,000 chars. Putting actionable rules first guarantees they are seen by Code Review even if the file grows past the cap. The PR feedback workflow is *author* behavior (replying to and resolving review comments), so Copilot Code Review can't enforce it — no value in spending pre-cap budget on it.

---

### 3. Skills location: `.agents/skills/`, with selective duplication to `.claude/skills/` when needed

**Decision.** Project skills live in `.agents/skills/<skill-name>/` at the repo root. If a specific skill turns out to be valuable while working with Claude Code, duplicate (or symlink) that individual skill into `.claude/skills/<skill-name>/` on demand. Do not duplicate by default.

**Why.**
- **No location covers all three tools.** Each tool reads a different default project-skills path:

  | Path | Copilot | Claude Code | OpenAI Codex |
  |---|:---:|:---:|:---:|
  | `.github/skills/` | ✅ | ❌ | ❌ |
  | `.claude/skills/` | ✅ | ✅ | ❌ |
  | `.agents/skills/` | ✅ | ❌ | ✅ |

- `.agents/skills/` is the only location read by both **Copilot** (primary) and **Codex** (secondary), the two tools we expect to use most often. It's also the path with the most tool-neutral name, which matches our `AGENTS.md`-as-source-of-truth choice (decision 1).
- Claude Code is the lowest-frequency tool in our mix, so optimizing for it by default isn't worth duplicating every skill. Duplicating individual skills on demand keeps the maintenance surface small.

**Tradeoff.** Skills authored here are invisible to Claude Code until duplicated. If we find ourselves duplicating most skills, revisit and consider making `.claude/skills/` the primary location instead.

---

### 4. First repo-specific skill: `zero-warnings-build`

**Decision.** Added `.agents/skills/zero-warnings-build/` (SKILL.md + bundled `check-warnings.ps1`) that runs Debug + Release + `npm run webpack-prod`, parses each one's output, deduplicates by `(file, line, column, code)`, and emits a single JSON report covering both warnings and errors. Non-standard build failures (e.g. `MSB1003`, `NU1101` without file prefix, `npm ERR!`) are caught by a synthetic `UNPARSED` entry containing the tail of the build output.

**Why a skill instead of leaving this to Copilot to do ad-hoc.**

Copilot *can* run the three builds itself and read the output, so a skill isn't strictly necessary. But several things go right with a skill that go wrong (or are inefficient) without one:

- **Deterministic procedure.** The script always runs all three builds in the same order with the same flags. Without it, Copilot sometimes runs only Debug, sometimes forgets `npm run webpack-prod`, sometimes uses `dotnet build` against a single project — none of which exercise the `<TreatWarningsAsErrors>` Release gate or the front-end build.
- **Structured output beats raw parsing.** A single JSON document with `(Build, Level, Code, File, Line, Column, Message, Project)` per entry is dramatically easier for the model to act on than 800+ lines of MSBuild + webpack output across three runs. Especially across many warnings, raw parsing eats context window and produces inconsistent triage.
- **Deduplication.** Multi-project references frequently surface the same warning twice. The script dedups by `(file, line, column, code)` so the agent sees each issue once and fixes it once.
- **Encodes the policy at the gate.** The script's exit-message reminds the agent of the "fix the cause; suppressions require explicit user approval" policy from `AGENTS.md`. The SKILL.md body repeats it in step 4 of the workflow. This means the policy is enforced at the point of action, not just stated in an always-on instructions file the model might gloss over.
- **Stable cost.** The body and script load on-demand. Adding more workflow detail to the skill doesn't inflate every chat turn the way the same detail would in `AGENTS.md`.
- **Safety net for non-standard failures.** When a build fails in a way the parser doesn't recognize, the synthetic `UNPARSED` entry surfaces the tail of the output so the agent can still diagnose. Ad-hoc parsing tends to silently miss these.

**Tradeoff.** A skill must be maintained as build tooling evolves (new MSBuild output format, new analyzer codes, webpack version upgrades). The maintenance cost is small relative to the reliability gain, but worth budgeting.

---

### 5. Prefer the `playwright-cli` skill over a Playwright MCP server

**Decision.** Use the existing `playwright-cli` skill in `.agents/skills/playwright-cli/` for browser automation needs. Do **not** install a Playwright MCP server (e.g. `@playwright/mcp` or `@microsoft/mcp-server-playwright`).

**Why.**
- **Context-window friendliness via progressive disclosure.** A skill's body and reference files load **only when the skill is selected**, and a referenced reference file loads **only when the body links to it during a turn**. Per [the OpenAI Codex skills docs](https://developers.openai.com/codex/skills): *"Codex starts with each skill's name, description, and file path. Codex loads the full SKILL.md instructions only when it decides to use a skill."* That means a sprawling skill (the `playwright-cli` skill has 11 reference files for tracing, video recording, request mocking, spec-driven testing, etc.) costs only its `description` field on most turns.
- **MCP servers are always-on once enabled.** An MCP server registered in `mcp.json` advertises its full tool list to the model on every chat turn for sessions where the server is loaded — that's permanent context window cost regardless of whether the task involves browsers. The Playwright MCP server exposes ~25 tools; that's a meaningful baseline tax for a tool we'd use intermittently.
- **Equivalent or better capability.** The `playwright-cli` skill drives the same underlying Playwright machinery (`playwright-cli` is a wrapper over Playwright), with documented coverage for snapshots, refs-based interaction, tracing, video, request mocking, and storage state. There's no MCP-only feature we lose by using the skill.
- **Single auth/install path.** The skill works the same way for Copilot CLI, Copilot Cloud agent, and Codex (via `.agents/skills/`). An MCP server would need separate registration for each surface (workspace `.mcp.json` for Copilot CLI, repo settings for Cloud agent, and per-IDE config for VS Code).

**Tradeoff.** MCP tools surface in tool pickers/UI lists that some surfaces render explicitly. Skills don't show up the same way (they're invoked by name or auto-selected). For our usage pattern this is fine; revisit if we ever need a more discoverable, always-on Playwright surface.

**Generalizable principle.** Prefer a skill over an MCP server whenever the capability is *intermittently* needed and can be exposed through CLI commands. Reserve MCP servers for capabilities that are *always relevant* in a session (e.g. the built-in `github` MCP for repo-aware work) or that require persistent state across many turns.

---

### 6. Adopt awesome-copilot `dependency-license-checker` and `secrets-scanner` as `sessionEnd` hooks

**Decision.** Installed both hooks from [`github/awesome-copilot`](https://github.com/github/awesome-copilot) at `.github/hooks/dependency-license-checker/` and `.github/hooks/secrets-scanner/`. Both fire on `sessionEnd` in **`block` mode** (exit non-zero on findings to prevent the session from auto-committing). Each hook ships two interchangeable scripts — the upstream `.sh` and a hand-ported PowerShell equivalent (`.ps1`) — and both are referenced from `hooks.json` via the `bash` and `powershell` fields so Copilot picks the right one per OS automatically. Hook output goes to `logs/copilot/{license-checker,secrets}/` which is gitignored.

**Why a hook (not a skill or instruction).**
- **Guardrails belong on the lifecycle, not on the model's discretion.** A license violation or a leaked secret is exactly the kind of thing we don't want depending on the model remembering to check. `sessionEnd` runs deterministically before the session is considered done — Copilot can't forget to invoke it.
- **Skills require the model to choose to load them.** For background safety scans the model never knows it should run, that's the wrong invocation pattern. Skills are right for *workflows the model executes deliberately* (`zero-warnings-build`); hooks are right for *invariants the runtime enforces*.
- **Instructions are weaker still** — they ask the model to remember rules, with no enforcement.

**Why adopt rather than author from scratch.**
- The awesome-copilot versions are maintained, cover multiple ecosystems (npm/pip/Go/Ruby/Rust for licenses; ~25 secret patterns including cloud creds, GitHub tokens, private keys, connection strings, JWTs), include allowlist support, write structured JSON logs, and redact matched secrets in the logs. Re-authoring this from scratch would add zero value and add maintenance burden.

**Default to `block`, not `warn`.**
- Both env-var modes (`LICENSE_MODE=block`, `SCAN_MODE=block`) exit non-zero on findings so the session is stopped before any auto-commit. Stricter default than awesome-copilot's `warn`-by-default, chosen because the cost of a leaked credential or a copyleft dep landing in a PR is higher than the inconvenience of resolving an occasional false positive.
- Fallback path when a finding turns out to be a false positive: add the offending substring to `SECRETS_ALLOWLIST` / `LICENSE_ALLOWLIST` in the corresponding `hooks.json`, or flip that one hook back to `warn` temporarily.

**Cross-platform via dual `bash` + `powershell` scripts.**
- Hooks must run on whatever platform Copilot is invoked from. The Cloud agent runs Linux, but local Copilot CLI runs on Windows (primary dev platform here).
- Each hook's `hooks.json` references both a `.sh` (kept byte-identical to upstream awesome-copilot) and a `.ps1` (hand-ported with identical env-var contract, JSON log shape, console output, and exit codes). Copilot picks the matching field per OS.
- The PowerShell ports are local additions — not in awesome-copilot upstream. If awesome-copilot ships breaking changes to the `.sh` files in the future, the `.ps1` files will need a corresponding edit. This is the maintenance cost of cross-platform parity.

**Self-exclude the scanner's own directory.**
- The secrets-scanner would otherwise find its own regex pattern definitions (e.g. `-----BEGIN PGP PRIVATE KEY BLOCK-----` is a literal inside the script) and its README's documentation examples (`postgresql://user:pass@host/db`, `192.168.1.1:8080`, `wJalr...`, etc.) as "secrets" — ~7 false positives per session.
- An earlier iteration tried to handle this with `SECRETS_ALLOWLIST` substrings, but the upstream allowlist mechanism does substring matching against the matched value (not the file path), so entries like `BEGIN RSA PRIVATE KEY` or `service_account` would have suppressed *real* secrets anywhere in the repo, not just the scanner's own files. (Caught in code review.)
- Final fix: a small local divergence from upstream — both `scan-secrets.sh` and `scan-secrets.ps1` skip any file under `.github/hooks/secrets-scanner/`. This is a path-scoped exclusion that can't accidentally mask real secrets elsewhere. The change is small (one `case` arm in bash, one `-like` check in PowerShell) and isolated, so future awesome-copilot updates can still be re-pulled with minimal merge.

**Operational notes.**
- **Line endings.** Git's `core.autocrlf` may convert the `.sh` files to CRLF on Windows checkout, which breaks `#!/bin/bash` on Linux. The shell scripts' executable bit is set in the git index (`git update-index --chmod=+x`); if line-ending issues bite, add `.gitattributes` entries forcing LF for `.github/hooks/**/*.sh`.
- **Scope on Cloud agent.** The Cloud agent commits and pushes autonomously, so `sessionEnd` is the right last-chance gate.
- **Hooks do not run inside MCP server processes.** Per the Copilot hooks docs, `preToolUse` covers Copilot's own tools; MCP server tool calls are out of scope. Fine for these session-end safety checks; just worth knowing.
- **PowerShell port specifics learned the hard way (code review caught all of these).**
  - `Get-Content` returns a `[String]` (not an array) for single-line files; indexing then iterates character-by-character. The scanner wraps `Get-Content` in `@(...)` to force array semantics so single-line files (`.env`, compact JSON, etc.) are scanned as one line, not one character at a time.
  - `Get-ChildItem -Filter` matches only the directory leaf name, so a pattern containing path separators (e.g. for nested Go modules like `github.com/foo/bar`) never matches. The Go module lookup filters on `FullName -like` instead of `-Filter`.
  - The bash scripts wrap `npm view`, `pip show`, `gem spec`, and `cargo metadata` with `timeout 5`. The PowerShell port mirrors this with a `Start-Job` + `Wait-Job -Timeout` helper so a hung package-manager CLI can't blow the 60s hook timeout in `block` mode.

**Revisit triggers.**
- If `block` mode interrupts work too often → flip individual env var to `warn` while we triage, or extend per-script self-exclusion.
- If upstream awesome-copilot ships a meaningful update → re-pull the `.sh` files (re-apply the self-exclusion patch on top) and bring the `.ps1` ports back into sync.
- If we add a new ecosystem the license checker doesn't recognize (e.g. NuGet — currently absent) → either contribute upstream or fork the script.

---

### 7. Customize Copilot Code Review behavior via a `code-review` skill at `.agents/skills/`, pointed at from both instruction files

**Decision.** Author repo-specific code-review guidance as a skill at [`.agents/skills/code-review/SKILL.md`](.agents/skills/code-review/SKILL.md) — modeled on [`dotnet/runtime`'s skill of the same name](https://github.com/dotnet/runtime/blob/main/.github/skills/code-review/SKILL.md) — and add a one-line pointer to it in **both** [`AGENTS.md`](AGENTS.md) and [`.github/copilot-instructions.md`](.github/copilot-instructions.md) instructing reviewing agents to use it.

The skill covers:
- A "reviewer mindset" persona (polite but skeptical; treat the PR description as claims to verify).
- A staged review process: gather code context → form independent assessment *before* reading the PR description → reconcile with the author's narrative → apply the repo checklist → detailed analysis with severity labels.
- A "Multi-Model Review" section instructing agents to run the review across 2-3 distinct model families in parallel when the environment supports it.
- Repository-specific review guidelines (data-layer, Blazor Server lifecycle, Engine performance, IngestionFunctions, auth, PR feedback follow-up) — seeded small, expected to grow.
- A standard review output format with AI-generated-content disclosure.

**Why a skill (not just more bullets in `AGENTS.md`).**
- **Cost.** Review process narrative is long (hundreds of lines). Putting it in `AGENTS.md` would push past the Copilot Code Review ~4 KB cap and inflate every non-review chat turn. As a skill, the body loads on-demand only when an agent is reviewing.
- **Discoverable by name.** Codex/Copilot CLI/Cloud agent see skill `name` + `description` during the discovery phase, so a pointer like *"use the `code-review` skill"* in `AGENTS.md` resolves automatically.
- **Mirrors a known-good pattern.** `dotnet/runtime` validated this layout at scale; copying their structure means future contributors familiar with that repo recognize ours.

**Why `.agents/skills/` (consistent with decision #3).**
- This keeps the skill in the same location as the rest of our project skills (e.g. `zero-warnings-build`, `playwright-cli`) — the location chosen in decision #3 because it's read by both Copilot and Codex.
- We initially considered `.github/skills/` (mirroring `dotnet/runtime`'s layout) because Copilot Code Review on github.com is the primary consumer and reads other `.github/` files by convention. However, since `copilot-instructions.md` references the skill by **explicit relative path** (`../.agents/skills/code-review/SKILL.md`), Code Review can resolve the file from anywhere in the repo — there's no requirement that the file itself live under `.github/`. Given that, the consistency win of decision #3 dominates.
- The other expected consumers — Copilot Cloud agent, Copilot CLI, IDE agent mode — also resolve relative-path references from instruction files.
- Claude Code does not auto-discover `.agents/skills/`; if code review under Claude Code becomes a frequent task, duplicate the skill into `.claude/skills/code-review/` on demand (same on-demand strategy decision #3 takes generally).

**Why the pointer in *both* instruction files (small, deliberate duplication).**
- `AGENTS.md` is read by Copilot Chat/CLI/Cloud agent and by Codex/Claude Code — that pointer routes those tools to the skill.
- `.github/copilot-instructions.md` is the only instruction file Copilot Code Review on github.com is documented to read. Without a pointer there, the github.com PR review surface never learns about the skill.
- The pointer is one sentence (~150 chars), placed immediately after the intro notes so it lands well inside the ~4 KB cap.

**Drift mitigation.** The skill is the source of truth for *how* to review; the **Code review checklist** sections in `AGENTS.md` / `copilot-instructions.md` remain the source of truth for *what* rules to enforce. The skill links to the checklist rather than re-stating it. Repository-specific gotchas that emerge during real reviews go into the skill's "Repository-Specific Review Guidelines" section; checklist-grade rules (always-applicable, blocking) go in `AGENTS.md` and get mirrored to `copilot-instructions.md` per decision #1.

---

## Key constraints to remember

- **Copilot Code Review cap:** ~4,000 chars per custom instruction file. Content past the cap is silently truncated *for Code Review only*. Chat, CLI, and Cloud agent have no documented cap. Source: [docs.github.com/en/copilot/concepts/prompting/response-customization](https://docs.github.com/en/copilot/concepts/prompting/response-customization).
- **Codex `AGENTS.md` cap:** 32 KiB combined, configurable via `project_doc_max_bytes`. Source: [developers.openai.com/codex/guides/agents-md](https://developers.openai.com/codex/guides/agents-md).
- **Codex `AGENTS.md` discovery:** walks from repo root down to cwd, merging files at every level (deeper files override). Supports `AGENTS.override.md` for layered overrides.
- **Copilot CLI MCP config sources:** user `~/.copilot/mcp-config.json`, workspace `.mcp.json`, and installed plugins (workspace overrides user on name collision).
- **Skills are read on-demand**, not always-on — installing many skills has minimal token cost since only the `name` + `description` fields are read during the discovery phase.

---

## Out-of-scope (yet)

The following customization mechanisms are documented in our research but **not yet adopted** for this repo. Each is a candidate for future work:

- **Path-specific instructions** (`.github/instructions/*.instructions.md` with `applyTo` globs) — would let us scope per-project rules (Data layer, Ingestion Functions, Razor pages, tests) instead of keeping them in `AGENTS.md`.
- **Prompt files** (`.github/prompts/*.prompt.md`) — for repeatable tasks (PR-feedback workflow, EF migration adds, warning-cleanup runs).
- **Custom agents** (`.github/agents/*.agent.md`) — for personas with restricted tool surfaces.
- **Hooks** (`.github/hooks/*.json`) — e.g. a `preToolUse` Tool Guardian that refuses edits introducing `#pragma warning disable` to enforce the warning policy deterministically.
- **MCP servers** — workspace `.mcp.json` (Copilot CLI) and/or repo-settings JSON (Copilot Cloud agent).
- **`.github/workflows/copilot-setup-steps.yml`** — to provision the Copilot Cloud agent's ephemeral environment with the .NET 10 SDK + Node 20 + `npm ci` so it can build the solution on first try.

A more detailed research report on each of these lives in this session's research notes; promote items here when we decide to adopt them.

---

## How to update this file

Any time we make or change a customization-strategy decision — adding/removing an instruction file, changing the warning policy, adopting a new mechanism (skills, hooks, prompt files, etc.), or revising the tool-compatibility tradeoffs — add or update an entry above. Each entry should capture:

1. **Decision** — what we're doing.
2. **Why** — the reasoning, including any tradeoffs considered.
3. **Sources** — link to docs we relied on, especially if a behavior is non-obvious.

The instruction files themselves (`AGENTS.md`, `.github/copilot-instructions.md`) should be kept concise and tactical. This file is the place for the longer-form *why*.
