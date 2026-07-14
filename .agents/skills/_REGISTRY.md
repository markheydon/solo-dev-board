# Skill Registry

This registry defines active and companion skills for SoloDevBoard. Skills live in **`.agents/skills/`** and are discovered by both GitHub Copilot and Cursor.

## Active Skills

- `breakdown-plan`: planning and decomposition
- `github-issues`: issue lifecycle operations
- `gh-cli`: bulk GitHub operations
- `breakdown-test`: test planning and QA workflow
- `create-architectural-decision-record`: architecture decision capture
- `documentation-writer`: user and technical documentation
- `dotnet-best-practices`: implementation quality baseline
- `mudblazor`: MudBlazor Blazor component library implementation guidance
- `pm-feature-workflow`: end-to-end orchestration for high-level PM prompts
- `aspire`: Aspire CLI orchestration and distributed application operations

## Optional Companion Skills

- `csharp-xunit`: xUnit patterns and fixtures
- `csharp-docs`: XML documentation depth guidance
- `playwright-cli`: Browser automation and Playwright test authoring for AI-driven UI validation
- `dotnet-inspect`: .NET API inspection across packages and local code

## Agent Skills (explicit invocation)

- `pm-orchestrator`: planning and issue setup agent
- `delivery`: implementation agent
- `tech-writer`: documentation prose agent
- `review`: PR and closure agent
- `code-review`: PR diff review agent

## Deactivated Policy

Skills not listed as active or optional companion should be removed from this repository to reduce context noise.

## Custom Agents

SoloDevBoard defines specialised agents for daily PM workflows. These orchestrate skills and enforce quality gates.

- `pm-orchestrator`: Backlog selection → scope validation → technical planning (breakdown-plan) → GitHub issue setup
- `delivery`: Implementation execution → tests → docs → ADR creation (if needed) → backlog sync
- `review`: Quality validation → PR creation → issue closure → release impact assessment
- `tech-writer`: Planning and user-facing documentation in UK English
- `code-review`: Pull request diff review

**Canonical agent definitions:** `.agents/agents/`  
**Copilot adapters:** `.github/agents/*.agent.md`  
**Cursor invocation:** `.cursor/commands/` or agent skills above  
**Agent orchestration:** `plan/PM_RUNBOOK.md` (daily/weekly workflow guide)

## Prompt Library

Reusable workflow prompts for daily PM operations:

- `daily-start`: Morning status check → backlog health → blocker identification → next action recommendation
- `plan-next-issue`: Backlog selection → scope validation → breakdown-plan → GitHub issue creation (invokes PM Orchestrator)
- `execute-feature`: Implementation → tests → docs → ADR (invokes Delivery Agent)
- `review-and-close`: Quality gates → PR creation → issue closure (invokes Review Agent)
- `weekly-pm-review`: Milestone health → velocity trends → release confidence → top 3 priorities

**Canonical prompt definitions:** `.agents/prompts/`  
**Copilot adapters:** `.github/prompts/*.prompt.md`  
**Cursor commands:** `.cursor/commands/`  
**Prompt usage guide:** `plan/PM_RUNBOOK.md` and `docs/ai-collaboration.md`

## Canonical Sources

- Stack and testing baseline: `AGENTS.md`
- Copilot adapter index: `.github/copilot-instructions.md`
- Issue labels: `plan/LABEL_STRATEGY.md`
- Feature workflow gates: `.agents/skills/pm-feature-workflow/SKILL.md`
- Daily PM workflow: `plan/PM_RUNBOOK.md`
- GitHub issue templates: `.github/ISSUE_TEMPLATE/*.yml` (YAML forms are canonical)
- Template synchronization: `.agents/skills/github-issues/references/TEMPLATE_SYNC.md`
