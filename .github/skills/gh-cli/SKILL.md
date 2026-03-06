---
name: gh-cli
description: 'GitHub CLI guidance for SoloDevBoard issue triage, label management, pull requests, workflows, and project operations.'
---

# GitHub CLI (SoloDevBoard)

Use this skill when the user asks for command-line GitHub operations. Prioritise issue and project-management workflows used by SoloDevBoard.

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
	--title "Feature: Add OAuth login" \
	--body-file issue.md \
	--label "type/feature,priority/high,status/todo,area/infrastructure"

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
Start-Process "https://github.com/users/markheydon/projects/8"

# NOTE: Updating project field values (Phase, Priority, Status, dates) requires GraphQL mutations.
# See `.github/skills/github-project/SKILL.md` for complete command patterns and all field/option IDs.
```

### Safety Rules

- Use `--repo owner/repo` for multi-repository operations to avoid acting on the wrong repository.
- Prefer `gh issue edit`/`gh pr edit` over ad-hoc API mutations for common updates.
- For bulk operations, list and review targets first before piping into mutation commands.
- Confirm authentication state with `gh auth status` before mutating operations.

Refer to the [GitHub CLI documentation](https://cli.github.com/manual/) for command details.