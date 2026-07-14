# AI Collaboration Guide

SoloDevBoard supports **GitHub Copilot** and **Cursor** with shared canonical content and thin platform adapters.

## Shared canonical layer

| Asset | Location | Used by |
|-------|----------|---------|
| Project standards | [`AGENTS.md`](../AGENTS.md) | Both |
| Skills | [`.agents/skills/`](../.agents/skills/) | Both (open Agent Skills standard) |
| Skill registry | [`.agents/skills/_REGISTRY.md`](../.agents/skills/_REGISTRY.md) | Both |
| Agents | [`.agents/agents/`](../.agents/agents/) | Both |
| Prompts / workflows | [`.agents/prompts/`](../.agents/prompts/) | Both |
| PM runbook | [`plan/PM_RUNBOOK.md`](../plan/PM_RUNBOOK.md) | Human + both |
| Path-scoped rules | [`.github/instructions/`](../.github/instructions/) | Copilot native; Cursor via [`.cursor/rules/`](../.cursor/rules/) |

## GitHub Copilot

| What | How to invoke |
|------|----------------|
| Base instructions | Automatic via [`.github/copilot-instructions.md`](../.github/copilot-instructions.md) → [`AGENTS.md`](../AGENTS.md) |
| Custom agents | VS Code agent picker → [`.github/agents/`](../.github/agents/) (adapters to `.agents/agents/`) |
| Workflow prompts | Copilot prompt library → [`.github/prompts/`](../.github/prompts/) (adapters to `.agents/prompts/`) |
| Skills | Auto-discovered from `.agents/skills/` |

**Example:** Run the daily start workflow via the `daily-start` prompt or ask Copilot to act as PM Orchestrator.

## Cursor

| What | How to invoke |
|------|----------------|
| Base standards | Automatic via [`.cursor/rules/solodevboard-core.mdc`](../.cursor/rules/solodevboard-core.mdc) → [`AGENTS.md`](../AGENTS.md) |
| File-scoped rules | Auto-applied from [`.cursor/rules/`](../.cursor/rules/) when matching files are open |
| Workflow commands | Type `/` in Agent chat → [`.cursor/commands/`](../.cursor/commands/) |
| Named agents | Type `/pm-orchestrator`, `/delivery`, etc., or ask explicitly; skills in `.agents/skills/<agent>/` |
| Skills | Auto-discovered from `.agents/skills/` |

**Example:** Type `/daily-start` or `/plan-next-issue` in Cursor Agent chat.

## Workflow quick reference

| Goal | Copilot | Cursor |
|------|---------|--------|
| Start the day | `daily-start` prompt | `/daily-start` |
| Plan next feature | `plan-next-issue` prompt | `/plan-next-issue` |
| Implement issue | `execute-feature` prompt | `/execute-feature` |
| Review and PR | `review-and-close` prompt | `/review-and-close` |
| PR comment fixes | `address-pr-review-comments` prompt | `/address-pr-review-comments` |
| Weekly review | `weekly-pm-review` prompt | `/weekly-pm-review` |

Full operating rhythm: [`plan/PM_RUNBOOK.md`](../plan/PM_RUNBOOK.md).

## DRY layout principle

- **Write once:** agent bodies, prompt workflows, and skill content live under `.agents/` and `AGENTS.md`.
- **Adapt twice:** `.github/` files add Copilot frontmatter (model, `agent:` binding); `.cursor/` files add rules and slash commands.
- **Do not duplicate** skill reference trees; both platforms read `.agents/skills/`.
