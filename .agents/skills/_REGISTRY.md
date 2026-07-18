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

## Deactivated Policy

Skills not listed as active or optional companion should be removed from `.agents/skills/` to reduce context noise.

## Role contracts

SoloDevBoard defines specialised role contracts for daily PM workflows. These orchestrate skills and enforce quality gates.

- `pm-orchestrator`: Backlog selection → scope validation → technical planning (breakdown-plan) → GitHub issue setup
- `delivery`: Implementation execution → tests → docs → decision log (if needed) → backlog sync
- `verify`: Quality validation → PR creation → issue closure → release impact assessment

**Role contracts:** `.agents/contracts/*.md`  
**Workflow entry points:** `.agents/workflows/*.md` (canonical)  
**Invocation:** Natural language, [`.github/prompts/`](../../.github/prompts/) (Copilot), [`.cursor/commands/`](../../.cursor/commands/) (Cursor) — all thin mirrors pointing to `.agents/workflows/`  
**Orchestration:** `plan/PM_RUNBOOK.md` (daily/weekly workflow guide)

## Workflow Library

Reusable workflows for daily PM operations (canonical definitions in `.agents/workflows/`):

- `daily-start`: Morning status check → backlog health → blocker identification → next action recommendation
- `plan-next-issue`: Backlog selection → scope validation → breakdown-plan → GitHub issue creation
- `implement-issue`: Implementation → tests → docs → decision log
- `verify-and-create-pr`: Quality gates → PR creation → issue closure
- `address-pr-review-comments`: PR review feedback → thread replies → resolved conversations
- `weekly-pm-review`: Milestone health → velocity trends → release confidence → top 3 priorities
- `code-review`: PR and branch review against repository conventions
- `docs-update`: Documentation refresh and decision log updates

**Workflow definitions:** [`.agents/workflows/`](../../.agents/workflows/)  
**Usage guide:** `plan/PM_RUNBOOK.md`

## Tool entry points

Cursor slash commands and Copilot prompts are thin discovery layers only. Example: `/implement-issue` → [`.agents/workflows/implement-issue.md`](../../.agents/workflows/implement-issue.md) → [`.agents/contracts/delivery.md`](../../.agents/contracts/delivery.md)

## Canonical Sources

- Stack and testing baseline: [`AGENTS.md`](../../AGENTS.md)
- Decision log: `plan/DECISIONS.md`
- Issue labels: `plan/LABEL_STRATEGY.md`
- Feature workflow gates: `.agents/skills/repo-pm-feature-workflow/SKILL.md`
- Daily PM workflow: `plan/PM_RUNBOOK.md`
- GitHub issue templates: `.github/ISSUE_TEMPLATE/*.yml` (YAML forms are canonical)
- Template synchronisation: `.agents/skills/repo-github-issues/references/TEMPLATE_SYNC.md`
