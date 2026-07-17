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
- `create-architectural-decision-record`: architecture decision capture
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

## Custom Agents

SoloDevBoard defines specialised agents for daily PM workflows. These orchestrate skills and enforce quality gates.

- `pm-orchestrator`: Backlog selection → scope validation → technical planning (breakdown-plan) → GitHub issue setup
- `delivery`: Implementation execution → tests → docs → ADR creation (if needed) → backlog sync
- `review`: Quality validation → PR creation → issue closure → release impact assessment

**Agent definitions:** `.github/agents/*.agent.md`  
**Agent invocation:** Via prompts in `.github/prompts/` or direct "Invoke [agent name]" requests  
**Agent orchestration:** `plan/PM_RUNBOOK.md` (daily/weekly workflow guide)

## Prompt Library

Reusable workflow prompts for daily PM operations:

- `daily-start`: Morning status check → backlog health → blocker identification → next action recommendation
- `plan-next-issue`: Backlog selection → scope validation → breakdown-plan → GitHub issue creation (invokes PM Orchestrator)
- `implement-issue`: Implementation → tests → docs → ADR (invokes Delivery Agent)
- `review-and-close`: Quality gates → PR creation → issue closure (invokes Review Agent)
- `weekly-pm-review`: Milestone health → velocity trends → release confidence → top 3 priorities

**Prompt definitions:** `.github/prompts/*.prompt.md`  
**Prompt usage guide:** `plan/PM_RUNBOOK.md`

## Cursor entry points

- **Implementation:** `/implement-issue` in Agent chat → Delivery Agent contract in [`.github/agents/delivery.agent.md`](../../.github/agents/delivery.agent.md)

## Canonical Sources

- Stack and testing baseline: `.github/copilot-instructions.md`
- Issue labels: `plan/LABEL_STRATEGY.md`
- Feature workflow gates: `.agents/skills/repo-pm-feature-workflow/SKILL.md`
- Daily PM workflow: `plan/PM_RUNBOOK.md`
- GitHub issue templates: `.github/ISSUE_TEMPLATE/*.yml` (YAML forms are canonical)
- Template synchronisation: `.agents/skills/repo-github-issues/references/TEMPLATE_SYNC.md`
