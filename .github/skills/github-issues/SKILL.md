---
name: github-issues
description: 'Create, update, and manage GitHub issues using MCP tools. Use this skill when users want to create bug reports, feature requests, or task issues, update existing issues, add labels/assignees/milestones, or manage issue workflows. Triggers on requests like "create an issue", "file a bug", "request a feature", "update issue X", or any GitHub issue management task.'
---

# GitHub Issues

Manage GitHub issues using the `@modelcontextprotocol/server-github` MCP server.

## Available MCP Tools

| Tool | Purpose |
|------|---------|
| `mcp__github__create_issue` | Create new issues |
| `mcp__github__update_issue` | Update existing issues |
| `mcp__github__get_issue` | Fetch issue details |
| `mcp__github__search_issues` | Search issues |
| `mcp__github__add_issue_comment` | Add comments |
| `mcp__github__list_issues` | List repository issues |

## Workflow

1. **Determine action**: Create, update, or query?
2. **Gather context**: Get repo info, existing labels, milestones if needed
3. **Structure content**: Use appropriate template from [references/templates.md](references/templates.md)
   - These markdown templates mirror the YAML issue forms in `.github/ISSUE_TEMPLATE/`
   - YAML templates are canonical structure (used by humans via GitHub UI)
   - Markdown templates are for AI programmatic creation (via MCP tools)
   - See [references/TEMPLATE_SYNC.md](references/TEMPLATE_SYNC.md) for synchronisation governance
4. **Execute**: Call the appropriate MCP tool
5. **Confirm**: Report the issue URL to user

For SoloDevBoard, labels must follow `plan/LABEL_STRATEGY.md` using `type/`, `priority/`, `status/`, `area/`, and optionally `size/`.

## Creating Issues

### Required Parameters

```
owner: repository owner (org or user)
repo: repository name  
title: clear, actionable title
body: structured markdown content
```

### Optional Parameters

```
labels: ["type/bug", "priority/medium", "status/todo", "area/dashboard", ...]
assignees: ["username1", "username2"]
milestone: milestone number (integer)
```

### Title Guidelines

- Start with type prefix when useful: `[Bug]`, `[Feature]`, `[Docs]`
- Be specific and actionable
- Keep under 72 characters
- Examples:
  - `[Bug] Login fails with SSO enabled`
  - `[Feature] Add dark mode support`
  - `Add unit tests for auth module`

### Body Structure

Always use the templates in [references/templates.md](references/templates.md). Choose based on issue type:

| User Request | Template | YAML Form Mirror |
|--------------|----------|------------------|
| Bug, error, broken, not working | Bug Report | `.github/ISSUE_TEMPLATE/bug.yml` |
| Feature, enhancement, add, new | Feature / User Story | `.github/ISSUE_TEMPLATE/feature.yml` |
| Task, chore, refactor, update | Chore / Technical Debt | `.github/ISSUE_TEMPLATE/chore.yml` |

**Note:** Markdown templates mirror YAML issue forms which humans use via GitHub UI. They must stay synchronised — see [references/TEMPLATE_SYNC.md](references/TEMPLATE_SYNC.md).

## Updating Issues

Use `mcp__github__update_issue` with:

```
owner, repo, issue_number (required)
title, body, state, labels, assignees, milestone (optional - only changed fields)
```

State values: `open`, `closed`

## Examples

### Example 1: Bug Report

**User**: "Create a bug issue - the login page crashes when using SSO"

**Action**: Call `mcp__github__create_issue` with:
```json
{
  "owner": "github",
  "repo": "awesome-copilot",
  "title": "[Bug] Login page crashes when using SSO",
  "body": "## Description\nThe login page crashes when users attempt to authenticate using SSO.\n\n## Steps to Reproduce\n1. Navigate to login page\n2. Click 'Sign in with SSO'\n3. Page crashes\n\n## Expected Behaviour\nSSO authentication should complete and redirect to dashboard.\n\n## Actual Behaviour\nPage becomes unresponsive and displays error.\n\n## Environment\n- Browser: [To be filled]\n- OS: [To be filled]\n\n## Additional Context\nReported by user.",
  "labels": ["bug"]
}
```

### Example 2: Feature Request

**User**: "Create a feature request for dark mode with high priority"

**Action**: Call `mcp__github__create_issue` with:
```json
{
  "owner": "github",
  "repo": "awesome-copilot",
  "title": "[Feature] Add dark mode support",
  "body": "## Summary\nAdd dark mode theme option for improved user experience and accessibility.\n\n## Motivation\n- Reduces eye strain in low-light environments\n- Increasingly expected by users\n- Improves accessibility\n\n## Proposed Solution\nImplement theme toggle with system preference detection.\n\n## Acceptance Criteria\n- [ ] Toggle switch in settings\n- [ ] Persists user preference\n- [ ] Respects system preference by default\n- [ ] All UI components support both themes\n\n## Alternatives Considered\nNone specified.\n\n## Additional Context\nHigh priority request.",
  "labels": ["enhancement", "high-priority"]
}
```

## SoloDevBoard Label Rules

Always apply at least one `type/` and one `priority/` label.

| Group | Examples |
|-------|----------|
| `type/` | `type/epic`, `type/feature`, `type/story`, `type/enabler`, `type/test`, `type/bug`, `type/chore`, `type/documentation` |
| `priority/` | `priority/critical`, `priority/high`, `priority/medium`, `priority/low` |
| `status/` | `status/todo`, `status/in-progress`, `status/blocked`, `status/in-review`, `status/done` |
| `area/` | `area/dashboard`, `area/migration`, `area/labels`, `area/board-rules`, `area/triage`, `area/workflows`, `area/infrastructure`, `area/docs` |
| `size/` | `size/xs`, `size/s`, `size/m`, `size/l`, `size/xl` |

## Tips

- Always confirm the repository context before creating issues
- Ask for missing critical information rather than guessing
- Link related issues when known: `Related to #123`
- For updates, fetch current issue first to preserve unchanged fields
