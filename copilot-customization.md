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
