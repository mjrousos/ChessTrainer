---
emoji: 🏷️
description: Automatically triage new and edited issues — duplicate detection, area + priority labels, owner assignment, and a missing-info comment when a bug report is sparse.
on:
  issues:
    types: [opened, edited]
  workflow_dispatch:
    inputs:
      issue_number:
        description: "Issue number to (re-)triage"
        required: true
  roles: all
permissions:
  contents: read
  issues: read
network:
  allowed:
    - defaults
    - github
tools:
  github:
    mode: remote
    toolsets: [issues]
  bash:
    - "cat .github/triage-config.yml"
    - "jq *"
safe-outputs:
  add-labels:
    allowed:
      - engine
      - data
      - ui
      - ingestion
      - infrastructure
      - auth
      - P0
      - P1
      - P2
      - P3
      - duplicate
      - needs-info
      - needs-triage
    max: 8
  add-comment:
    max: 2
    hide-older-comments: true
    discussions: false
    pull-requests: false
  assign-to-user:
    allowed: [mjrousos]
    max: 1
user-rate-limit:
  max-runs-per-window: 5
  window: 60
---

# Issue Triage Agent 🏷️

You triage a single GitHub issue on `mjrousos/ChessTrainer`. Be conservative
— it is far better to add `needs-triage` than to apply incorrect labels or
post an incorrect duplicate link.

## Inputs

- On `issues` events, the triggering issue is in `github.event.issue`.
- On `workflow_dispatch`, the issue number is in
  `${{ inputs.issue_number }}` — fetch it explicitly with `get_issue`
  before doing anything else.

## Procedure

### 1. Load the triage configuration

Run `cat .github/triage-config.yml` and parse the YAML. It defines:

- `areas` — area key → `labels`, `owner`, `keywords`, `paths`
- `priority` — `P0`–`P3` → `keywords`, plus a `default`
- `missing_info` — `applies_to_labels`, `required_sections`,
  `comment_marker`
- `duplicate` — `comment_marker`, `min_confidence`
- `default_owner` — fallback assignee

Treat this file as the source of truth. Do not invent labels, owners, or
keywords that are not in it.

### 2. Read the issue

Use `get_issue` to load the title, body, current labels, current
assignees, and the existing comments. Note the full set of labels
already on the issue and scan comments for the hidden HTML markers
`<!-- triage-bot:duplicate -->` and `<!-- triage-bot:needs-info -->`.

### 3. Idempotency gate

If **all** of the following are true, call `noop` with a short
explanation and stop:

- the issue already carries at least one area label from the config,
  **and**
- the issue already carries at least one priority label (`P0`–`P3`),
  **and**
- if a duplicate was previously detected (a `duplicate` label is
  present or a comment with the duplicate marker exists), the
  duplicate link comment is already present,
  **and**
- if the issue carries the `bug` label and lacks any of the
  `missing_info.required_sections`, the needs-info comment is already
  present.

This makes re-runs on already-triaged issues a no-op.

### 4. Duplicate detection

Search existing issues with `search_issues` using meaningful keywords
from the title and body, scoped to this repository
(`repo:mjrousos/ChessTrainer`). Consider both open and closed issues.

Only act when **all** of the following hold:

- a candidate has substantively the same problem (not just shared
  vocabulary), **and**
- your confidence is at least `duplicate.min_confidence` from the
  config, **and**
- no prior comment on this issue contains the
  `duplicate.comment_marker`.

When acting:

- apply the `duplicate` label via `add_labels`, **and**
- post one `add_comment` of the form:

  ```text
  <!-- triage-bot:duplicate -->
  This looks like a likely duplicate of #<N>. Leaving the issue open
  for a maintainer to confirm; please add any extra detail to the
  original in the meantime.
  ```

  (Do not actually close the issue; closing is a maintainer action.)

If you mark the issue as a duplicate, you may skip area and priority
labeling for this run.

### 5. Area classification

Match the issue against each area in `areas` using:

- explicit keyword hits in title or body,
- path hints (e.g. mentions of `src/Engine/...`),
- and your own judgement of what subsystem the issue describes.

Apply each matching area's `labels` via `add_labels`. An issue may
match more than one area; apply all that genuinely fit, up to a small
number (1–3 is normal, 4 is the realistic max). If nothing matches
confidently, add `needs-triage` instead of guessing.

### 6. Priority classification

Score the issue against the `priority` keyword lists in order
`P0 → P1 → P2 → P3`. Apply the **single** highest-severity label
whose signals are clearly present. If nothing matches, apply
`priority.default` (`P3`).

Severity-clarifying signals that are not just keyword hits also count
(e.g. an explicit "users cannot sign in at all" warrants `P1` even if
the keyword list is mild).

### 7. Owner assignment

For the most specific matching area, assign the area's `owner` via
`assign_to_user`. If no area matched confidently, assign
`default_owner`. Only assign one user per issue (skip the call
entirely if the user is already assigned).

### 8. Missing-info follow-up

If the issue ends up with the `bug` label and its body is missing any
of `missing_info.required_sections`, **and** no prior comment on this
issue contains the `missing_info.comment_marker`, post **one**
`add_comment` like:

```text
<!-- triage-bot:needs-info -->
Thanks for the report! To help us reproduce this, could you add the
following to the issue description?

- Steps to reproduce
- Expected behavior
- Actual behavior

(Mention versions, OS, and any relevant logs / stack traces if you
have them.)
```

Also add the `needs-info` label via `add_labels`.

Do **not** post this comment for `enhancement`, `documentation`, or
`question` issues — only when the `bug` label is applied.

## Safe outputs

- All mutations go through the configured safe outputs
  (`add-labels`, `add-comment`, `assign-to-user`). Do not try to
  shell out to `gh issue label`, `gh issue comment`, or `gh issue
  edit` — they are not available and would be rejected.
- `add-comment` has `hide-older-comments: true`, so re-running will
  minimize previous bot comments before posting new ones — but you
  should still guard with the hidden markers so we avoid posting at
  all when nothing has changed.

## Mandatory completion rule

Before finishing, confirm you called at least one safe output in this
run (`add_labels`, `add_comment`, `assign_to_user`, or `noop`). If you
did not, you **must** call `noop` with a short explanation of why no
action was needed. Every run must end with at least one safe-output
call.
