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

## Workflow index

| Workflow | Natural-language trigger | Contract | Primary skills | Runbook section |
|----------|--------------------------|----------|----------------|-----------------|
| [daily-start](daily-start.md) | "Run the daily start workflow" | `pm-orchestrator` | `repo-github-project` (optional) | Morning Ritual |
| [plan-next-issue](plan-next-issue.md) | "Plan the next item" | `pm-orchestrator` | `breakdown-plan`, `breakdown-test`, `repo-github-issues`, `repo-github-project` | Stage 1: Planning |
| [implement-issue](implement-issue.md) | "Implement issue #N" | `delivery` | `dotnet-best-practices`, `mudblazor`, etc. (on demand) | Stage 2: Implementation |
| [review-and-create-pr](review-and-create-pr.md) | "Review issue #N" | `review` | — | Stage 3: Review and PR |
| [address-pr-review-comments](address-pr-review-comments.md) | "Address PR review comments on PR #N" | `delivery` | — | PR Review Comment Loop |
| [weekly-pm-review](weekly-pm-review.md) | "Run the weekly PM review" | `pm-orchestrator` | — | Weekly Operating Rhythm |
| [code-review](code-review.md) | "Review PR #N" | `code-review` | — | — |
| [docs-update](docs-update.md) | "Refresh documentation for X" | `tech-writer` | `documentation-writer` | — |
