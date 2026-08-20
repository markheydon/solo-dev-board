# Plan Next Issue

**Contract:** [`.agents/contracts/pm-orchestrator.md`](../contracts/pm-orchestrator.md)
**Runbook:** [`plan/PM_RUNBOOK.md`](../../plan/PM_RUNBOOK.md) — Stage 1: Planning
**Skills (on demand):** `breakdown-plan`, `breakdown-test`, `repo-github-issues`, `repo-github-project`

## Easy-to-miss specifics

- A wireframe in `plan/wireframes/` is required before page-producing UI planning is considered complete.
- Set parent/child sub-issues (MCP `sub_issue_write`) and blocking relationships (`gh api` REST issue-dependencies) as part of planning. Do not leave them for the user. Report a **Manual fallback** table only if those APIs fail.
- Sync each created issue to Project #8 per the `repo-github-project` skill.
- A GitHub Issue comment "plan this issue" plans **that** issue. Do not pick a different backlog item unless the comment says "plan the next item".

## Invocation

**Chat:** "Plan the next item" or "Plan the [feature name]".
**Slash command:** `/plan-next-issue`.
**GitHub Issue comment** (mention at the start). When the comment is on an issue, plan **that** item rather than picking a different backlog candidate:
- `@cursor plan this issue`
- `@cursor plan this feature`
- `@cursor flesh out acceptance criteria and sub-issues`
- `@cursor plan the next item` (board pick; ignore the current thread unless it is the selected item)
