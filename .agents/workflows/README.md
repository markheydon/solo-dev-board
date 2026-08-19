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
| Start a working session | "Run the daily start workflow", `/daily-start` | `daily-start` | `pm-orchestrator` |
| Plan the next feature | "Plan the next item", `/plan-next-issue` | `plan-next-issue` | `pm-orchestrator` |
| Preflight before implementation | "Preflight issue #N", `/preflight-issue` | `preflight-issue` | `delivery` |
| Deliver planned work (happy path) | "Deliver issue #N", `/deliver-issue` | `deliver-issue` | `delivery`, `verify` |
| Implement planned work | "Implement issue #N", `/implement-issue` | `implement-issue` | `delivery` |
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
| [deliver-issue](deliver-issue.md) | "Deliver issue #N" | `delivery`, `verify` | `aspire`, `dotnet-best-practices`, `mudblazor`, etc. | Stage 2–3: Delivery happy path |
| [implement-issue](implement-issue.md) | "Implement issue #N" | `delivery` | `aspire`, `dotnet-best-practices`, `mudblazor`, etc. | Stage 2: Implementation |
| [verify-and-create-pr](verify-and-create-pr.md) | "Verify issue #N", "Create PR for issue #N" | `verify` | — | Stage 3: Verify and PR |
| [address-pr-review-comments](address-pr-review-comments.md) | "Address PR review comments on PR #N" | `delivery` | `aspire` (when AppHost is running) | PR Review Comment Loop |
| [pm-progress-review](pm-progress-review.md) | "Run the PM progress review" | `pm-orchestrator` | — | Progress Review Rhythm |
| [sync-status-labels](sync-status-labels.md) | "Clean up status labels", `/sync-status-labels` | `repo-github-gh-cli` | — | Board Hygiene Audit |
| [code-review](code-review.md) | "Code review PR #N" | `code-review` | — | Stage 3b: Independent code review |
| [docs-update](docs-update.md) | "Refresh documentation for X" | `tech-writer` | `documentation-writer` | — |
