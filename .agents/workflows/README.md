# Workflow Entry Points

Canonical workflow entry points for SoloDevBoard PM and delivery operations.

## Mirror pattern

Each workflow has **one** canonical file in this directory. Tool-specific discovery layers point here only — never duplicate procedural content elsewhere:

| Layer | Location | Purpose |
|-------|----------|---------|
| Canonical | `.agents/workflows/*.md` | Contract pointer, easy-to-miss specifics, invocation examples |
| Copilot | `.github/prompts/*.prompt.md` | Thin pointer for palette discovery |
| Cursor | `.cursor/commands/*.md` | Thin pointer for slash commands |

**Maintenance rule:** When a workflow changes, edit **one** file in `.agents/workflows/`. Do not duplicate content into `.github/prompts/` or `.cursor/commands/`.

Orchestration, rituals, and step-by-step guidance live in [`plan/PM_RUNBOOK.md`](../../plan/PM_RUNBOOK.md). Role boundaries live in [`.agents/contracts/`](../contracts/). Repo-specific operations live in [`.agents/skills/`](../skills/).

## GitHub comment invocation

Cursor Cloud Agents can be started from a GitHub Issue or Pull Request by commenting `@cursor` or `@cursoragent` plus an instruction ([Cursor Cloud Agent docs](https://cursor.com/docs/cloud-agent)). Put the mention at the **start** of the comment.

Agents must treat those comments as first-class workflow invocations, not as vague chat. Follow this binding and routing before choosing a workflow ([DEC-031](../../plan/DECISIONS.md#dec-031-github-comment-workflow-invocation)).

### Binding

- On an **issue**, "this", "this issue", "the issue", and a missing `#N` mean the issue being commented on.
- On a **pull request**, "this", "this PR", and a missing PR number mean that pull request. Linked `Closes #N` / `Fixes #N` is the issue for delivery or verify.
- Do not ask the user for a number that the thread already implies.
- Each `@cursor` mention on an **issue** starts a **new** agent session. Follow-up on the same issue is not the same conversation. On a **PR**, later `@cursor` comments can continue the branch session.
- Still refuse `status/blocked` and `status/ice-box` implementation.

### GitHub Issue comment → workflow

| Human-friendly comment (after `@cursor` / `@cursoragent`) | Workflow |
|-----------------------------------------------------------|----------|
| implement this issue; implement this; deliver this; do this; work on this; fix this; build this; please implement | `deliver-issue` (happy path: implement + verify + PR) |
| implement this without a PR; implement only; do not open a pull request | `implement-issue` |
| preflight this; scout this; discover the codebase; do not write code yet | `preflight-issue` |
| plan this issue; plan this feature; flesh out this issue; add acceptance criteria and sub-issues | `plan-next-issue` (plan **this** item, not a different backlog pick) |
| verify this; create a PR for this; open a pull request | `verify-and-create-pr` |
| refresh the docs; update the user guide for this; update the decision log | `docs-update` |
| run daily start; what's next from the board | `daily-start` |
| run a progress review | `pm-progress-review` |
| clean up status labels | `sync-status-labels` |

Bare "implement this issue" on a GitHub Issue is **`deliver-issue`**, not the split `implement-issue` path. A Cloud Agent started from GitHub is expected to open a pull request unless the comment says otherwise.

### GitHub Pull Request comment → workflow

| Human-friendly comment (after `@cursor` / `@cursoragent`) | Workflow |
|-----------------------------------------------------------|----------|
| code review this PR; review this PR for conventions | `code-review` |
| address the review comments; fix the review threads; address Copilot comments | `address-pr-review-comments` |
| fix CI; fix the failing checks | Stay on the PR branch and repair CI. This is not a named PM workflow. |
| verify / create a PR | Do **not** open a second PR. The PR already exists. |

### Ambiguous phrases

- **"Review this issue"** is not code review and not verify. Ask which workflow, or treat as read-only clarification, unless the comment also says "code review" or "conventions".
- **"Review this PR"** without "code review" or "conventions" is still `code-review` when the comment is on a pull request.
- **"Verify"** means quality gates and PR creation (`verify-and-create-pr`). It does not mean code review.
- Slash commands and Copilot prompts in chat keep the split: `/implement-issue` does not open a PR; `/deliver-issue` does.

## Routing

| User intent | Trigger phrases | Workflow | Contract |
|-------------|-----------------|----------|----------|
| Start a working session | "Run the daily start workflow", `/daily-start` | `daily-start` | `pm-orchestrator` |
| Plan the next feature | "Plan the next item", `/plan-next-issue`, GitHub: "plan this issue" | `plan-next-issue` | `pm-orchestrator` |
| Preflight before implementation | "Preflight issue #N", `/preflight-issue`, GitHub: "preflight this" | `preflight-issue` | `delivery` |
| Deliver planned work (happy path) | "Deliver issue #N", `/deliver-issue`, GitHub: "implement this issue" | `deliver-issue` | `delivery`, `verify` |
| Implement planned work (no PR) | "Implement issue #N", `/implement-issue`, GitHub: "implement without a PR" | `implement-issue` | `delivery` |
| Validate implementation and open PR | "Verify issue #N", "Create PR for issue #N", `/verify-and-create-pr` | `verify-and-create-pr` | `verify` |
| Address PR review comments | "Address PR review comments on PR #N", `/address-pr-review-comments` | `address-pr-review-comments` | `delivery` |
| Progress review since last update | "Run the PM progress review", `/pm-progress-review` | `pm-progress-review` | `pm-orchestrator` |
| Status label hygiene | "Clean up status labels", `/sync-status-labels` | `sync-status-labels` | `repo-github-gh-cli` |
| Audit code against conventions | "Code review PR #N", "Review PR #N for conventions", `/code-review` | `code-review` | `code-review` |
| Refresh documentation | "Refresh documentation for X", `/docs-update` | `docs-update` | `tech-writer` |

**Rule:** Never use bare "Review issue #N" for code-review, or bare "Review PR #N" for verify. The word **verify** = gate + PR; **code review** = conventions audit.

## Workflow index

| Workflow | Natural-language trigger | Contract | Primary skills | Runbook section |
|----------|--------------------------|----------|----------------|-----------------|
| [daily-start](daily-start.md) | "Run the daily start workflow" | `pm-orchestrator` | `repo-github-project` (optional) | Session Start |
| [plan-next-issue](plan-next-issue.md) | "Plan the next item" | `pm-orchestrator` | `breakdown-plan`, `breakdown-test`, `repo-github-issues`, `repo-github-project` | Stage 1: Planning |
| [preflight-issue](preflight-issue.md) | "Preflight issue #N" | `delivery` | `aspire`, `dotnet-best-practices`, `mudblazor` (on demand) | Stage 2: Implementation (preflight) |
| [deliver-issue](deliver-issue.md) | "Deliver issue #N"; GitHub: "implement this issue" | `delivery`, `verify` | `aspire`, `dotnet-best-practices`, `mudblazor`, etc. | Stage 2–3: Delivery happy path |
| [implement-issue](implement-issue.md) | "Implement issue #N" (chat/slash; no PR) | `delivery` | `aspire`, `dotnet-best-practices`, `mudblazor`, etc. | Stage 2: Implementation |
| [verify-and-create-pr](verify-and-create-pr.md) | "Verify issue #N", "Create PR for issue #N" | `verify` | — | Stage 3: Verify and PR |
| [address-pr-review-comments](address-pr-review-comments.md) | "Address PR review comments on PR #N" | `delivery` | `aspire` (when AppHost is running) | PR Review Comment Loop |
| [pm-progress-review](pm-progress-review.md) | "Run the PM progress review" | `pm-orchestrator` | — | Progress Review Rhythm |
| [sync-status-labels](sync-status-labels.md) | "Clean up status labels", `/sync-status-labels` | `repo-github-gh-cli` | — | Board Hygiene Audit |
| [code-review](code-review.md) | "Code review PR #N" | `code-review` | — | Stage 3b: Independent code review |
| [docs-update](docs-update.md) | "Refresh documentation for X" | `tech-writer` | `documentation-writer` | — |
