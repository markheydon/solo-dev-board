# GitHub Copilot Instructions — SoloDevBoard

<!-- Copilot adapter: canonical standards live in AGENTS.md -->

**Follow all project standards in [`AGENTS.md`](../AGENTS.md).** This file adds Copilot-specific entry points only.

---

## Path-Scoped Instructions

Copilot loads additional rules from `.github/instructions/` when matching files are in context:

| File | Scope |
|------|-------|
| `blazor.instructions.md` | `**/*.razor`, `**/*.razor.cs`, `**/*.razor.css` |
| `dotnet-framework.instructions.md` | `**/*.cs`, `**/*.csproj` |
| `github-actions-ci-cd-best-practices.instructions.md` | `.github/workflows/**` |

---

## Skills

Skills are canonical in **`.agents/skills/`**. See [`.agents/skills/_REGISTRY.md`](../.agents/skills/_REGISTRY.md) for the active set, workflow order, and companion skills.

---

## Custom Agents

Invoke via the VS Code agent picker or prompts that reference an agent:

| Agent | File | Role |
|-------|------|------|
| PM Orchestrator | `.github/agents/pm-orchestrator.agent.md` | Planning, issue setup |
| Delivery | `.github/agents/delivery.agent.md` | Implementation, tests |
| Tech Writer | `.github/agents/tech-writer.agent.md` | Documentation prose |
| Review | `.github/agents/review.agent.md` | PR creation, closure |
| Code Review | `.github/agents/code-review.agent.md` | PR diff review |

Canonical agent bodies: `.agents/agents/`

---

## Workflow Prompts

| Prompt | File |
|--------|------|
| Daily start | `.github/prompts/daily-start.prompt.md` |
| Plan next issue | `.github/prompts/plan-next-issue.prompt.md` |
| Execute feature | `.github/prompts/execute-feature.prompt.md` |
| Review and close | `.github/prompts/review-and-close.prompt.md` |
| Address PR review comments | `.github/prompts/address-pr-review-comments.prompt.md` |
| Weekly PM review | `.github/prompts/weekly-pm-review.prompt.md` |
| Code review | `.github/prompts/code-review.prompt.md` |
| Docs update | `.github/prompts/docs-update.prompt.md` |

Canonical prompt bodies: `.agents/prompts/`

**Workflow reference:** [`plan/PM_RUNBOOK.md`](../plan/PM_RUNBOOK.md)
