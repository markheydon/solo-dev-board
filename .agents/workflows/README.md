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

## Routing

| User intent | Trigger phrases | Workflow | Contract |
|-------------|-----------------|----------|----------|
| Validate implementation and open PR | "Verify issue #N", "Create PR for issue #N", `/verify-and-create-pr` | `verify-and-create-pr` | `verify` |
| Audit code against conventions | "Code review PR #N", "Review PR #N for conventions", `/code-review` | `code-review` | `code-review` |
| Weekly milestone health | "Run the weekly PM review", `/weekly-pm-review` | `weekly-pm-review` | `pm-orchestrator` |

**Rule:** Never use bare "Review issue #N" for code-review, or bare "Review PR #N" for verify. The word **verify** = gate + PR; **code review** = conventions audit.

## Workflow index

| Workflow | Natural-language trigger | Contract | Primary skills | Runbook section |
|----------|--------------------------|----------|----------------|-----------------|
| [daily-start](daily-start.md) | "Run the daily start workflow" | `pm-orchestrator` | `repo-github-project` (optional) | Morning Ritual |
| [plan-next-issue](plan-next-issue.md) | "Plan the next item" | `pm-orchestrator` | `breakdown-plan`, `breakdown-test`, `repo-github-issues`, `repo-github-project` | Stage 1: Planning |
| [implement-issue](implement-issue.md) | "Implement issue #N" | `delivery` | `dotnet-best-practices`, `mudblazor`, etc. (on demand) | Stage 2: Implementation |
| [verify-and-create-pr](verify-and-create-pr.md) | "Verify issue #N", "Create PR for issue #N" | `verify` | — | Stage 3: Verify and PR |
| [address-pr-review-comments](address-pr-review-comments.md) | "Address PR review comments on PR #N" | `delivery` | — | PR Review Comment Loop |
| [weekly-pm-review](weekly-pm-review.md) | "Run the weekly PM review" | `pm-orchestrator` | — | Weekly Operating Rhythm |
| [code-review](code-review.md) | "Code review PR #N" | `code-review` | — | — |
| [docs-update](docs-update.md) | "Refresh documentation for X" | `tech-writer` | `documentation-writer` | — |
