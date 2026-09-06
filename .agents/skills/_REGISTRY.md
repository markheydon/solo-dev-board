# Skill Registry

This registry defines active and companion skills for SoloDevBoard.

**Canonical location:** `.agents/skills/` (formerly `.github/skills/`). Both GitHub Copilot and Cursor discover skills from this directory.

Skills prefixed with `repo-` are SoloDevBoard-specific (GitHub project board, issue lifecycle, PM workflow, and related repository operations).

## Active Skills

- `breakdown-plan`: planning and decomposition
- `repo-github-issues`: issue lifecycle operations (SoloDevBoard)
- `repo-github-gh-cli`: bulk GitHub CLI operations (SoloDevBoard)
- `repo-github-project`: SoloDevBoard Roadmap project board operations
- `breakdown-test`: test planning and QA workflow
- `repo-decision-log`: architectural and technical decision routing (SoloDevBoard)
- `documentation-writer`: user and technical documentation
- `dotnet-best-practices`: implementation quality baseline
- `mudblazor`: MudBlazor Blazor component library implementation guidance
- `repo-pm-feature-workflow`: end-to-end orchestration for high-level PM prompts (SoloDevBoard)
- `aspire`: Aspire CLI orchestration and distributed application operations

## Optional Companion Skills

- `csharp-xunit`: xUnit patterns and fixtures
- `csharp-docs`: XML documentation depth guidance
- `playwright-cli`: browser automation and Playwright test authoring for AI-driven UI validation
- `aspire-init`: bootstrap Aspire in a greenfield or existing repo
- `aspireify`: wire AppHost resources after `aspire init`
- `aspire-orchestration`: AppHost lifecycle, start/stop, port conflicts, hot reload
- `aspire-deployment`: publish and deploy to Docker Compose, Kubernetes, Azure, or AWS
- `aspire-monitoring`: logs, traces, metrics, and dashboard diagnostics

## Deactivated Policy

Skills not listed as active or optional companion should be removed from `.agents/skills/` to reduce context noise.

## Role contracts

SoloDevBoard defines specialised role contracts for PM workflows. These orchestrate skills and enforce quality gates.

- `pm-orchestrator`: GitHub Issues / Project #8 selection → scope validation → technical planning (breakdown-plan) → GitHub issue setup
- `delivery`: Implementation preflight → Aspire runtime (logs, resource rebuild) → code → tests → docs → decision log (if needed) → PR review comment loop
- `verify`: Quality validation (clean rebuild, package hygiene) → PR creation → issue closure → release impact assessment

**Role contracts:** `.agents/contracts/*.md`  
**Workflow entry points:** `.agents/workflows/*.md` (canonical)  
**Invocation:** Natural language, GitHub Issue/PR comments (`@cursor` / `@cursoragent` — see [workflow README](../../.agents/workflows/README.md#github-comment-invocation)), [`.github/prompts/`](../../.github/prompts/) (Copilot), [`.cursor/commands/`](../../.cursor/commands/) (Cursor) — all thin mirrors pointing to `.agents/workflows/`  
**Orchestration:** `plan/PM_RUNBOOK.md` (session and progress-review workflow guide)

## Workflow Library

Reusable workflows for PM operations (canonical definitions in `.agents/workflows/`):

- `daily-start`: Session orientation → issue and project board health → blocker identification → next action recommendation
- `plan-next-issue`: GitHub Issues / Project #8 selection → scope validation → breakdown-plan → GitHub issue creation
- `preflight-issue`: Implementation preflight only (context, codebase discovery, sketch) — no coding
- `deliver-issue`: Preflight (when required) → implementation → verify/PR (happy path orchestration)
- `implement-issue`: Preflight → implementation → tests → docs → decision log
- `verify-and-create-pr`: Quality gates (clean rebuild, package hygiene) → PR creation → issue closure
- `address-pr-review-comments`: PR review feedback (any reviewer) → implement → thread replies → resolved conversations
- `pm-progress-review`: Milestone health since last review → velocity trends → release confidence → top 3 next-session priorities
- `technical-debt-review`: On-demand (typically monthly) read-only codebase deep dive → candidate debt list for human review (no auto-filed issues)
- `code-review`: PR and branch review against repository conventions (GitHub review with resolvable threads)
- `docs-update`: Documentation refresh and decision log updates

**Workflow definitions:** [`.agents/workflows/`](../../.agents/workflows/)  
**Usage guide:** `plan/PM_RUNBOOK.md`

## Tool entry points

Cursor slash commands and Copilot prompts are thin discovery layers only. Examples: `/preflight-issue` → [`.agents/workflows/preflight-issue.md`](../../.agents/workflows/preflight-issue.md) → [`.agents/contracts/delivery.md`](../../.agents/contracts/delivery.md); `/deliver-issue` → [`.agents/workflows/deliver-issue.md`](../../.agents/workflows/deliver-issue.md) → Delivery + Verify contracts; `/implement-issue` → [`.agents/workflows/implement-issue.md`](../../.agents/workflows/implement-issue.md) → [`.agents/contracts/delivery.md`](../../.agents/contracts/delivery.md)

## Canonical Sources

- Stack and testing baseline: [`AGENTS.md`](../../AGENTS.md)
- Decision log: `plan/DECISIONS.md`
- Issue labels: `plan/LABEL_STRATEGY.md`
- Feature workflow gates: `.agents/skills/repo-pm-feature-workflow/SKILL.md`
- Daily PM workflow: `plan/PM_RUNBOOK.md`
- GitHub issue templates: `.github/ISSUE_TEMPLATE/*.yml` (YAML forms are canonical)
- Template synchronisation: `.agents/skills/repo-github-issues/references/TEMPLATE_SYNC.md`
