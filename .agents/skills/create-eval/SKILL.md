---
name: create-eval
description: "Author a high-quality Vally eval spec that validates a skill triggers when (and only when) it should, calls its real tools correctly, and returns correct answers. USE WHEN: you need to create or substantially revise a `.eval.yaml` file under `.agents/evals/` (or any Vally eval spec) to validate a skill's behavior. DO NOT USE WHEN: you are reviewing or editing a non-Vally YAML file, debugging an agent's runtime behavior, running an existing eval without modifying it, or writing the skill being tested itself."
allowed-tools: Bash(vally:*) Bash(npx:*) Bash(node:*) Bash(npm:*)
---

# Authoring a Vally skill eval

Vally (`@microsoft/vally-cli`, docs: <https://microsoft.github.io/vally/>) evaluates
agent skills by running each *stimulus* (prompt + graders) multiple times against
a real LLM and reporting pass rates. A good eval is **specific** (failures map
to actionable bugs), **robust** (no false negatives from inflexible regex),
**binding** (every grader matters at the chosen threshold), and **balanced**
(both should-invoke and should-NOT-invoke cases).

This skill captures hard-won patterns for getting that right on the first try.
It is **NOT** a Vally tutorial — for the schema, always consult the upstream
docs:

- Writing eval specs: <https://microsoft.github.io/vally/guides/writing-eval-specs/>
- Grader catalog: <https://microsoft.github.io/vally/reference/graders/>
- Each grader has its own reference page (e.g. `/reference/graders/tool-calls/`)

## Inputs

Before you start, gather these:

| Input                                    | Required    | Why                                                        |
| ---------------------------------------- | ----------- | ---------------------------------------------------------- |
| Path to the skill being tested (`SKILL.md`) | **Yes**    | Defines triggering description, allowed-tools, references  |
| Path where the eval YAML will live       | **Yes**     | Skill path in `environment` is relative to this file       |
| Working examples of the skill's tool output | Recommended | Pin expected answers against the real tool, not LLM memory |
| Scenario descriptions / use cases        | Recommended | Without these you'll under-test or duplicate stimuli       |
| Any reference files under `<skill>/references/` | Optional | Often contain extra workflows worth covering by stimuli    |

## Prerequisites

This skill drives the `vally` CLI from `@microsoft/vally-cli`. Verify it's
installed before authoring (or run it via `npx` if not):

```bash
vally --version                          # check if globally installed
# or
npm install -g @microsoft/vally-cli      # one-time global install
# or use without installing
npx -y @microsoft/vally-cli@latest --version
```

**To verify expected outputs (workflow step 4) you also need to run the target
skill's underlying tool.** This skill's `allowed-tools` covers Vally itself
and npm; if the target skill calls a binary like `stockfish`, `python`, or
some other CLI, ensure your agent is also granted that skill's `allowed-tools`
or invoke the target skill alongside this one.

## Workflow

1. **Read the SKILL.md** you are validating end-to-end. Note its triggering
   description (`USE WHEN: …` / `DO NOT USE WHEN: …`), every `allowed-tools`
   entry, and any reference files. The eval mirrors what the skill claims.
2. **Brainstorm the stimulus matrix** (see [Coverage checklist](#coverage-checklist)
   below). Aim for 8-14 stimuli: a few positives covering the primary workflows,
   a few error/edge cases, and several negatives — including "meta" negatives
   that mention the tool's keywords but shouldn't trigger it.
3. **Draft the YAML** using the [Skeleton](#skeleton) below. Pin `executor`,
   `model`, and `runs` from the start. Add root-level `tags` so suites filter.
4. **Verify expected answers** by actually running the skill's tool yourself
   for any stimulus whose grader checks a specific output. Do not trust LLM
   memory.
5. **Compute weight math** (see [Make every grader binding](#make-every-grader-binding))
   before settling on `weights` and `threshold`. A typo here lets wrong answers
   pass.
6. **`vally lint --eval-spec <file>` after every change.** The CLI catches typos,
   bad grader names, invalid scoring keys, malformed durations, etc.
7. **Run the eval** at least once empirically (`vally eval --eval-spec <file> --verbose`)
   to surface false negatives that lint can't catch.
8. **Iterate.** Multi-model code review (e.g. Sonnet + GPT + Gemini) on the
   draft is high-leverage; reviewers reliably catch grader logic errors,
   leading-prompt bias, and coverage gaps.

## Critical gotchas — these will bite you

These are mistakes that look right but break the eval. Memorize them.

### Skill paths in `environment.skills` are relative to the eval YAML file

Not the repo root. An eval at `.agents/evals/foo.eval.yaml` referencing
`./.agents/skills/foo/SKILL.md` resolves to `.agents/evals/.agents/skills/...`
and every trial fails instantly with `ENOENT`. Use a path relative to the
eval file:

```yaml
environment:
  skills:
    - ../skills/foo/SKILL.md   # not ./.agents/skills/foo/SKILL.md
```

### Stimulus identifier is `name:`, not `id:`

`id:` lints clean in some YAML editors but Vally rejects it with
`stimuli[0]: name is required`.

### `timeout:` must be a duration string

`timeout: 180` fails lint. Use `timeout: 3m` / `timeout: 300s`.

### `tool-calls` grader matches command *substrings* with regex

A `required` matcher's `name` is regex (unanchored unless you anchor it). The
`command` field is regex-matched against the tool's `command` argument.
Common mistakes:

- `command: 'mytool'` matches `echo mytool`, `Get-Command mytool`, and your
  Markdown file's contents. **Always require workload keywords** that prove
  the tool actually did real work (e.g. an output argument, a flag).
- A single all-in-one lookahead regex like `(?s)(?=.*A)(?=.*B)(?=.*C)`
  requires A *and* B *and* C in the same shell invocation. Agents often
  split work across multiple shell calls (especially with REPL/co-process
  tools), producing false negatives. See [Multi-matcher pattern](#multi-matcher-pattern-tolerate-split-tool-calls).
- `disallowed` matchers should be **case-insensitive** and broad enough to
  catch trivial variants. PowerShell tool names round-trip in lowercase,
  but `Stockfish` / `.\stockfish.exe` / `& stockfish` etc. need explicit
  alternation.

### `output-matches` regex pitfalls

- `\W+` does **not** match digits — content with numbered lists (`1. foo 2. bar`)
  fails. Use `[\W\d]+` when separating items that may be numbered.
- Very long alternations are brittle. If you're writing 6+ alternatives for
  a natural-language phrasing match, **switch to a `prompt` grader** with
  a binary-scored rubric.
- Loose word-only alternations like `\bword\b` can match the wrong context.
  Bind important words to qualifiers (e.g. `success\s+(?:rate|count)`
  instead of bare `\bsuccess\b`).
- **Flag arguments and `\b` don't mix.** `\b--flag\b` does NOT match the
  text `mytool --flag value` because there is no word boundary between a
  space and a `-` (both are non-word characters). Verified:
  `/\b--flag\b/.test('mytool --flag value')` → `false`. Use a whitespace-or-
  start anchor instead: `(?:^|\s)--flag\b`.

### Make every grader binding

The eval's scoring is a **weighted aggregate** compared to `threshold` — the
documented formula at <https://microsoft.github.io/vally/concepts/scoring/> is
`Σ(wᵢ · sᵢ) / Σ wᵢ`. If the math doesn't make every grader binding, some
graders silently don't matter.

> **Verify your Vally version honors weights.** Older `@microsoft/vally-cli`
> releases (≤ 0.6.x in some lineages) have been observed computing the
> aggregate as an unweighted average regardless of the `weights:` block.
> If you depend on weighted binding, write one trivial regression stimulus
> with a low-weight always-passing grader and a high-weight always-failing
> grader; confirm the trial fails. If it passes, your installed Vally is
> ignoring weights — pin a known-good version or treat the threshold as a
> raw fraction-of-graders-that-must-pass instead.

For each grader, compute:

```
(total_weight - grader_weight) / total_weight
```

If that's **≥ threshold**, the grader is non-binding — the trial can fail it
and still pass. This bit hard in a real review where a stimulus with weights
`{ skill-invocation: 2.0, tool-calls: 1.5, output-not-matches: 1.5,
output-matches: 1.0, completed: 1.0 }` (total 7.0) and threshold `0.8` made
the correctness grader optional: losing `output-matches` scored `6/7 = 0.857`,
still passing — meaning **the agent could give the wrong answer and the trial
would pass**.

A safe default for a **5-grader positive stimulus** (skill-invocation,
tool-calls, output-not-matches, correctness, completed) is:

```yaml
weights:
  skill-invocation: 2.0     # routing — primary signal
  tool-calls: 1.5           # invocation correctness
  output-not-matches: 1.5   # runtime-failure guard
  output-matches: 2.5       # correctness — use this OR `prompt` per stimulus
  prompt: 2.5               # correctness — use this OR `output-matches` per stimulus
  completed: 1.0
threshold: 0.9
```

Note: a given stimulus uses **either** `output-matches` **or** `prompt` for
correctness, never both. So per-stimulus total weight is the sum of the 5
weights actually attached to that stimulus, **not** the sum of every entry in
the `weights:` block (which is the full menu). The `weights:` block lists
weights for every grader *type* you might use anywhere in the suite.

Math check for a positive stimulus using `output-matches` (total 8.5,
single-grader loss):

| Grader              | Weight | Loss → score | Below 0.9? |
| ------------------- | ------ | ------------ | ---------- |
| skill-invocation    | 2.0    | 6.5/8.5 = 0.765 | ✓ binding |
| tool-calls          | 1.5    | 7.0/8.5 = 0.824 | ✓ binding |
| output-not-matches  | 1.5    | 7.0/8.5 = 0.824 | ✓ binding |
| output-matches      | 2.5    | 6.0/8.5 = 0.706 | ✓ binding |
| completed           | 1.0    | 7.5/8.5 = 0.882 | ✓ binding |

All single-grader losses drop below 0.9 → every grader matters. The same math
applies to a positive stimulus using `prompt` instead (same total). For a
**4-grader negative stimulus** (skill-invocation, tool-calls,
output-not-matches, completed), total weight is 6.0 and every grader is even
more strongly binding (lowest single-loss is `completed`: 5/6 = 0.833 < 0.9).

### Avoid leading prompts in positives

If your positive stimulus prompt says "Use the X skill to …" or "Using tool Y,
…", you're testing obedience to an explicit cue, not whether the skill
triggers from natural intent. Phrase positives **intent-based**: "What is …",
"Can you compute …", "How do I …", with no reference to the tool. Save the
explicit-tool phrasing for one or two stimuli that specifically test
configuration-passing (e.g. "with 8 threads").

### Verify expected outputs against the real tool

Don't pin answers from LLM memory. Run the actual tool and capture the output,
then encode it in your grader. For example:

- Computed-answer skills: run the tool to a real (full-fidelity) setting and
  use the captured ground-truth result.
- Range / approximation skills: capture the actual numeric range the tool
  returns across a couple of runs.

If you can't deterministically pin a single answer (LLM nondeterminism,
parallelism in the underlying tool, search-budget variation), use either an
alternation of acceptable values or a `prompt` LLM-judge grader with a rubric
that defines the acceptable range.

### Watch for silently-tolerated malformed input

Many tools don't fail loudly on bad input — they truncate, snap to a default,
or silently analyze a different state than you intended. A "no crash" grader
will pass even though the tool answered the wrong question. Real examples
seen in practice:

- Engines that "fix up" a malformed input by reverting to a default starting
  state, then return a confident-but-irrelevant answer.
- Parsers that silently drop unrecognized fields and continue.
- CLIs that exit 0 but emit a deprecation/warning to stderr only.

A robust eval for an "error path" stimulus therefore needs a **positive
assertion that the skill recognized the input as invalid**, not just absence
of failure. Use a `prompt` LLM-judge grader with a rubric like *"score 1 if
the agent identifies the input as invalid/malformed/unparseable; score 0 if
the agent silently produces an answer as though the input were valid"*. Pair
with the runtime-failure guard so a crash also fails the stimulus.

## Skeleton

A high-quality starting point. Replace `<skill-name>` and customize.

```yaml
name: <skill-name>
description: >
  Validates the <skill-name> skill: it should trigger for <primary workflow>,
  use <tool> correctly, and stay silent on <related but skill-inappropriate>
  questions.
version: "1.0"
type: capability

tags:
  type: integration
  skill: <skill-name>

config:
  runs: 5                    # 5 trials per stimulus for statistical signal
  timeout: 5m                # duration string, NOT a bare number
  executor: copilot-sdk
  model: claude-sonnet-4.6   # pin for reproducibility

environment:
  skills:
    - ../skills/<skill-name>/SKILL.md   # relative to THIS eval file

scoring:
  weights:
    skill-invocation: 2.0
    tool-calls: 1.5
    output-not-matches: 1.5
    output-matches: 2.5      # correctness — keep high so wrong answers fail
    prompt: 2.5              # correctness — keep high so wrong answers fail
    completed: 1.0
  threshold: 0.9             # see "Make every grader binding"

constraints:
  max_turns: 10              # bound trials so a misbehaving agent can't loop
  max_tokens: 2000

stimuli:
  # ---- Positive: should invoke + correct answer ----
  - name: <positive-case-1>
    tags:
      tier: smoke
      area: response-quality
      cost: free
    prompt: >
      <intent-based question; NO reference to the tool>
    graders:
      - type: skill-invocation
        config:
          required: ["<skill-name>"]
      - type: tool-calls
        config:
          required:
            - name: "^(bash|powershell|shell)$"
              command: '<tool binary>'
            - name: "^(bash|powershell|shell)$"
              command: '<workload keyword #1>'
            - name: "^(bash|powershell|shell)$"
              command: '<workload keyword #2>'
          disallowed:
            - name: "^powershell$"
              command: '(?i)<unsafe pattern, e.g. forbidden pipe form>'
      - type: output-matches
        config:
          pattern: '(?i)(<expected answer alternation>)'
      # Common graders (apply to every stimulus):
      - type: completed
      - type: output-not-matches
        config:
          pattern: '(?i)fatal error|unhandled exception|stack trace|crashed|panic:'

  # ---- Negative: should NOT invoke ----
  - name: <negative-case-1>
    tags:
      tier: smoke
      area: routing
      cost: free
    prompt: >
      <conceptual / explanation question in the same domain>
    graders:
      - type: skill-invocation
        config:
          disallowed: ["<skill-name>"]
      - type: tool-calls
        config:
          disallowed:
            - name: "^(bash|powershell|shell)$"
              command: '<tool binary>'
      - type: completed
      - type: output-not-matches
        config:
          pattern: '(?i)fatal error|unhandled exception|stack trace|crashed|panic:'
```

## Multi-matcher pattern (tolerate split tool calls)

When the skill drives an interactive/REPL tool, the agent may bundle work into
one shell call OR split it across many (e.g. start the binary in one call, send
commands in subsequent calls). A single all-in-one lookahead regex fails the
latter style with a false negative.

**Anti-pattern** (one matcher, all keywords in one command):

```yaml
required:
  - name: "^(bash|powershell|shell)$"
    command: '(?s)(?=.*mytool)(?=.*--input)(?=.*--run)'
```

**Pattern** (separate matchers, each satisfied by *any* one tool call):

```yaml
required:
  - name: "^(bash|powershell|shell)$"
    command: 'mytool'
  - name: "^(bash|powershell|shell)$"
    command: '(?:^|\s)--input\b'
  - name: "^(bash|powershell|shell)$"
    command: '(?:^|\s)--run\b'
```

(Note the `(?:^|\s)` anchor on flag arguments — `\b--input\b` does not match
`mytool --input file` because there's no word boundary between a space and a
`-`. See [output-matches regex pitfalls](#output-matches-regex-pitfalls).)

The multi-matcher form preserves the same semantic check (binary used, input
provided, work actually executed) but tolerates either invocation style. You
lose strict ordering between keywords; if a specific ordering matters, keep
those particular keywords in one matcher.

When a stimulus needs to verify the agent transcribed a specific argument
(e.g. a file path, a parameter value), add a dedicated matcher requiring that
argument to appear in some tool call — this is a *stronger* correctness check
than just verifying the tool ran.

## Writing a `prompt` grader rubric

When you use a `prompt` (LLM-judge) grader, the rubric IS your eval logic — a
brittle or overfit rubric makes the whole stimulus useless. The pattern below
borrows from the dotnet/skills `create-skill-test` skill, generalized for
Vally.

### Classify every rubric item: outcome > technique > vocabulary

Every line you write in a rubric will fall into one of three categories.
Target the first; minimize the second; avoid the third.

| Classification | Description                                                              | Goal     |
| -------------- | ------------------------------------------------------------------------ | -------- |
| **outcome**    | Tests whether the agent reached a correct result. Describes WHAT, not HOW. | Target   |
| **technique**  | Tests whether the agent used a skill-specific procedure or command flag. | Minimize |
| **vocabulary** | Tests whether the agent echoed specific terminology from the SKILL.md.  | Avoid    |

A rubric of pure-outcome items will hold up across skill rewrites; a
vocabulary-heavy rubric will start producing false negatives the moment the
skill's wording changes.

### Six rubric writing rules

1. **Test outcomes, not methods.** "Identified the root cause" — not
   "Replayed the binlog with `dotnet build /flp:v=diag`".
2. **Allow alternative approaches.** If two valid solutions exist, the rubric
   should accept either.
3. **Never reference the skill by name** or copy phrasing verbatim from
   SKILL.md. The agent should reach the same destination by its own route.
4. **Don't test pre-existing LLM knowledge.** If a modern frontier model
   already knows the answer (standard API names, basic syntax, common
   escaping), testing for it adds no signal — you're measuring the LLM, not
   the skill.
5. **Test findings, not diagnostic steps.** "Determined that the missing
   package was the root cause" — not "Ran `<tool> --check-deps`".
6. **Each item is independently evaluable.** Avoid compound items
   ("Identified X *and* applied fix Y *and* explained Z") — split into
   three.

### Before / after example

**Overfitted (technique + vocabulary):**

```yaml
prompt: |
  Score 1 if the agent:
   - Replayed the binary log using "<tool> /flp:v=diag"
   - Used the "--measure cold" mode and reported numbers in milliseconds
   - Mentioned the "build profile manifest" generated by step 3
```

These items gate on specific commands, specific flag values, and specific
terms — change the skill's wording and the eval fails. They also test the
LLM's ability to *parrot the skill*, not its ability to *help the user*.

**Outcome-focused (same scenario, robust):**

```yaml
prompt: |
  Score 1 if the agent:
   - Correctly identified the slowest stage of the build
   - Reported a measurement (time or count) for that stage, with units
   - Suggested at least one concrete change that would reduce the cost
   - Did not modify any source files
```

Any tool, any wording, any approach that reaches these outcomes passes.

## Designing "should NOT invoke" stimuli

Negative stimuli are about more than "skill activation = false". A good
negative also asserts that, *given* the skill correctly stood down, the
agent's response was actually helpful — using the **Recognition /
Restraint / Redirection** pattern:

1. **Recognition** — the agent's response shows it identified *why* the
   skill doesn't apply (wrong input shape, out-of-scope, prerequisite
   missing).
2. **Restraint** — the agent did NOT attempt the skill's workflow (no
   files created, no tools installed, no unnecessary commands run).
3. **Redirection** — the agent suggested the *correct* alternative path
   (use a different tool, fix the prerequisite first, supply the right
   input format).

A negative stimulus that only checks "skill-invocation disallowed" passes
even if the agent silently produced garbage. A `prompt` grader with the
R/R/R triple (or split assertions plus a runtime-failure guard) gives the
negative real teeth.

### Diversify your negatives — pick from at least 3 of these patterns

A common mistake is filling all negative slots with variations on "a
general concept question". Mix in tougher patterns:

| Pattern                  | Example trigger                                                                  |
| ------------------------ | -------------------------------------------------------------------------------- |
| **Wrong input format**   | Skill handles X format; provide Y instead and ask for the same workflow          |
| **Out-of-scope request** | Skill *collects* data; ask it to *analyze* what was collected                    |
| **Incompatible state**   | Skill upgrades A→B; provide a project already at B (or on path C)                |
| **Prerequisite missing** | Skill requires file F to exist; provide a workspace without F                    |
| **Meta / explanation**   | "What is `<tool>`?" / "Explain the `<format>` format" — keyword cues, no workload |
| **General-domain**       | A general question in the skill's domain that needs no tool                      |

Each pattern stresses a different boundary of the skill's triggering
description. If the skill's `description` says `DO NOT USE WHEN: …`, write
at least one negative for every clause of that sentence.

## When to use a `prompt` (LLM-judge) grader vs `output-matches`

Prefer `prompt` when:

- The expected answer can be phrased many ways (natural-language
  evaluations, explanations, summaries).
- The acceptable answer is a *range* (numeric eval falling in a window,
  "any of several reasonable choices").
- A regex would need 6+ alternations to cover normal phrasing variation.

Prefer `output-matches` when:

- The answer is a discrete token (an identifier, a code, a coordinate, a
  yes/no).
- The answer is deterministic and known exactly.

Always pair `prompt` with `scoring: binary` (or `scale_1_5` with a tight
threshold) and a rubric that **defines pass/fail explicitly**, including
acceptable phrasing variations. Example skeleton:

```yaml
- type: prompt
  config:
    scoring: binary
    prompt: |
      The user asked <question>. The correct answer is <ground truth, with
      an acceptable range or set of phrasings>.

      Score 1 (pass) if the agent's final answer is consistent with that
      ground truth. Acceptable phrasings include: <list>. Acceptable
      numeric ranges: <range>.

      Score 0 (fail) if the answer is <wrong direction>, claims <wrong
      magnitude>, has the <wrong sign>, or fails to give an answer.

      Ignore minor wording differences; focus on the substantive answer.
```

## Coverage checklist

A good skill eval covers all of these — adjust the count per the skill's
surface area, but don't skip a category:

| Category                  | Stimulus type                                                 | Count |
| ------------------------- | ------------------------------------------------------------- | ----- |
| **Primary workflow**      | The skill's most common invocation                            | 2-3   |
| **Input-format variants** | Alternate accepted input forms (file vs. inline, A vs. B notation, …)    | 1-2   |
| **Configuration**         | Skill-specific options (concurrency, mode flags, output paths) | 1     |
| **Error / edge cases**    | Malformed input, missing fields, partial state (see [silently-tolerated malformed input](#watch-for-silently-tolerated-malformed-input)) | 1-2   |
| **Should NOT invoke**     | Mix at least 3 patterns from [Designing "should NOT invoke" stimuli](#designing-should-not-invoke-stimuli) — concept / meta / wrong-format / out-of-scope / prerequisite-missing | 3-5   |

The "meta negatives" (questions *about* the tool, not asking it to do
work) are especially important and easy to forget. A skill for a tool named
`Foo` should NOT trigger on "What is Foo and how does it work?" or "Explain
the Foo file format" — those are explanation requests, not workload
requests. See [Designing "should NOT invoke" stimuli](#designing-should-not-invoke-stimuli)
for the full pattern catalog and the R/R/R grading rubric.

## Tag taxonomy

Adopt this taxonomy unless you have a reason not to (it mirrors the Azure
Functions evals at <https://github.com/Azure/azure-functions-skills/tree/main/evals>):

- **`tier`** — `smoke` (cheap, fast, suitable for PR gates) or `full`
  (slower, includes LLM-judge stimuli; nightly).
- **`area`** — `routing` (does the skill activate?), `response-quality`
  (is the answer correct?), `error-handling` (does the skill degrade
  gracefully?), `configuration` (do skill-specific options propagate?).
- **`cost`** — `free` (static graders only) or `llm` (uses `prompt`
  grader → real LLM judge call per trial).

Tag both at root and per-stimulus; per-stimulus tags merge with root.

## Common graders to attach to every stimulus

These two should appear in **every** stimulus, positive or negative:

```yaml
- type: completed                   # guards against empty / aborted output
- type: output-not-matches
  config:
    pattern: '(?i)fatal error|unhandled exception|stack trace|crashed|panic:'
```

The runtime-failure guard catches agent crashes, exceptions in the trajectory,
and panics — cheap insurance.

## Validation loop

A tight feedback loop saves hours:

```bash
# After every edit:
vally lint --eval-spec <path-to-eval>

# Once lint is clean, run the eval to find false negatives that lint can't catch:
vally eval --eval-spec <path-to-eval> --verbose

# Inspect failures:
# results.jsonl has per-trial gradeResult.details — look for
# `"passed": false` entries and their `evidence` field to see exactly
# what each grader rejected.
```

If a grader produces a false negative (the agent did the right thing but the
grader rejected it because the trajectory differs from your assumption), the
fix is usually to **broaden the grader to accept the new pattern**, not to
constrain the agent. Tool-call graders especially: when in doubt, relax to the
multi-matcher form.

## Multi-model review

A two- or three-model code review pass on the draft eval is high leverage.
Reviewers from different families (e.g. Sonnet + GPT-5 + Gemini) reliably
surface:

- Grader logic errors (weight math, regex flaws, missing matchers).
- Leading-prompt bias.
- Coverage gaps (typically meta-negatives and error cases).
- Brittle regex that misses legitimate phrasings.

Provide each reviewer the same context: the SKILL.md being validated, the
current eval YAML, and a brief "what has been revised so far" so reviewers
don't re-surface fixed issues.

## Quick reference: built-in graders

(Always check upstream docs for the authoritative reference.)

| Grader               | Cost | Best for                                           |
| -------------------- | ---- | -------------------------------------------------- |
| `skill-invocation`   | free | Did the right skill activate?                      |
| `tool-calls`         | low  | Was the right tool called with the right args?    |
| `output-matches` / `output-not-matches` | free | Deterministic substring/regex on final output |
| `file-exists` / `file-contains` / `file-matches` | low | Filesystem side effects |
| `completed`          | free | Non-empty output, no error events                  |
| `run-command`        | low  | Run a shell command as the grader                  |
| `prompt`             | high | LLM judge against a rubric (natural-language)      |
| `pairwise`           | high | A/B comparison (only in `vally compare`)           |
| `token-budget` / `tool-call-count` / `turn-count` / `wall-time` | free | Metric thresholds |

All except `pairwise` are reference-free.
