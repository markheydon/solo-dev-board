---
name: gh-cli
description: 'GitHub CLI guidance for SoloDevBoard issue triage, label management, pull requests, workflows, and project operations.'
---

# GitHub CLI (SoloDevBoard)

Use this skill when the user asks for command-line GitHub operations. Prioritise issue and project-management workflows used by SoloDevBoard.

## Tool Selection

- Prefer GitHub MCP tools for issue search, pull requests, labels, and notifications when those tools cover the requested operation.
- Prefer `gh` for GitHub Projects v2 item operations, field edits, and ad-hoc project inspection because those workflows are not fully covered by MCP in this repository.
- Do not fall back to `gh` after an MCP failure unless the missing capability or limitation is clear.
- Before mutating project items, confirm authentication state with `gh auth status` and ensure the token includes the `project` scope.

## Shell Selection

- Default to bash examples for this repository when the current shell is WSL or Linux.
- Do not use PowerShell backtick escaping or `Get-Date` syntax in WSL bash sessions.
- Use cross-platform commands where possible. For example, prefer `gh project view 8 --owner markheydon --web` over shell-specific browser launch commands.

## Scope

- Issue creation, updates, and triage
- Label taxonomy operations aligned to `plan/LABEL_STRATEGY.md`
- Pull request status and checks
- CI/CD workflow triggering and monitoring
- Basic project board item operations

Avoid broad, low-value commands (for example gists, codespaces, SSH/GPG key management) unless explicitly requested.

## Usage

### Label Taxonomy

Always align labels to `plan/LABEL_STRATEGY.md` groups:

- `type/`
- `priority/`
- `status/`
- `area/`
- `size/`

Always apply at least one `type/` and one `priority/` label on issue creation.

### Vetted Command Patterns

```bash
# Create an issue with repo taxonomy labels
gh issue create \
	--repo owner/repo \
	--title "[Story] Add OAuth login" \
	--body-file issue.md \
	--label "type/story,priority/high,status/todo,area/infrastructure"

# List open todo issues for a work area
gh issue list \
	--repo owner/repo \
	--state open \
	--label "status/todo,area/dashboard" \
	--limit 50

# Transition issue state labels
gh issue edit 123 \
	--repo owner/repo \
	--remove-label "status/todo" \
	--add-label "status/in-progress"

# Inspect pull request checks
gh pr checks 123 --repo owner/repo

# Trigger and monitor workflow runs
gh workflow run ci.yml --repo owner/repo --ref main
gh run list --repo owner/repo --workflow ci.yml --limit 10

# Add an issue to a project (requires project permissions)
gh project item-add 5 --owner owner --url https://github.com/owner/repo/issues/123

# === SoloDevBoard Roadmap (Project #8) ===

# List all project items and their current state
gh project item-list 8 --owner markheydon --format json

# Add an issue to the SoloDevBoard Roadmap
gh project item-add 8 --owner markheydon --url https://github.com/markheydon/solo-dev-board/issues/123

# View the roadmap board in the browser
gh project view 8 --owner markheydon --web

# Set a single-select project field on an existing item
gh project item-edit \
	--id "$item_id" \
	--project-id "PVT_kwHOAJefG84BQ6bh" \
	--field-id "PVTSSF_lAHOAJefG84BQ6bhzg-5WGY" \
	--single-select-option-id "df9275ed"

# Set a number field on an existing item
gh project item-edit \
	--id "$item_id" \
	--project-id "PVT_kwHOAJefG84BQ6bh" \
	--field-id "PVTF_lAHOAJefG84BQ6bhzg_Lx34" \
	--number 1

# NOTE: Prefer `gh project item-edit` for project field updates.
# Use raw GraphQL only when `gh project` does not expose the required operation.
# See `.github/skills/github-project/SKILL.md` for the SoloDevBoard-specific field IDs and queue workflow.
```

### Safety Rules

- Use `--repo owner/repo` for multi-repository operations to avoid acting on the wrong repository.
- Prefer `gh issue edit`/`gh pr edit` over ad-hoc API mutations for common updates.
- Prefer `gh project item-edit` over raw `gh api graphql` mutations for project field updates.
- For bulk operations, list and review targets first before piping into mutation commands.
- Confirm authentication state with `gh auth status` before mutating operations.
- In WSL or Linux, keep jq filters in single quotes and avoid PowerShell-specific quoting patterns.

Refer to the [GitHub CLI documentation](https://cli.github.com/manual/) for command details.