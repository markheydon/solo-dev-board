# SoloDevBoard — Project Management Guide

<!-- AI Collaborator Instructions: See the "AI Collaborator Instructions" section at the bottom of this file before making changes to the project structure or issue workflow. -->

This document describes how GitHub Issues, Milestones, and Projects are used to manage the SoloDevBoard project.

---

## Overview

SoloDevBoard uses GitHub's native project management tools:

| Tool | Maps To |
|------|---------|
| **GitHub Issues** | User stories, tasks, bugs, chores |
| **GitHub Milestones** | Phases / releases (e.g. "Phase 1 — Foundation") |
| **GitHub Projects (v2)** | The project board (see [PROJECT_BOARD_DESIGN.md](PROJECT_BOARD_DESIGN.md)) |
| **Labels** | Classification, priority, status, area (see [LABEL_STRATEGY.md](LABEL_STRATEGY.md)) |
| **Epics** | Represented as parent Issues with `type/epic` label; child issues reference the parent |

---

## Issues

### Creating Issues

- Use the appropriate issue template:
  - `.github/ISSUE_TEMPLATE/feature.yml` for features and user stories
  - `.github/ISSUE_TEMPLATE/bug.yml` for bugs
  - `.github/ISSUE_TEMPLATE/chore.yml` for maintenance tasks
- Always apply at least one `type/` label and one `priority/` label.
- Apply the relevant `area/` label.
- Assign the issue to the current milestone if it is planned for the current phase.
- Link epics by mentioning the parent issue (e.g. "Part of #12") in the issue body.

### Issue Lifecycle

```
Created → [status/todo] → [status/in-progress] → [status/in-review] → [status/done] → Closed
                                                        ↑
                                               PR opened (linked)
```

The issue lifecycle labels remain the source of truth for GitHub Issues. The project board may show an additional planning state, **Up Next**, but that state is board-only and does not have a matching GitHub issue label.

Roadmap date handling follows the same lifecycle:

- **Todo** items stay blank until work actually starts.
- **In Progress** items must have a **Start Date** and a size-based **Target Date** forecast.
- **Done** items keep their **Start Date** and replace **Target Date** with the actual completion date.
- Parent Features and Epics inherit their first **Start Date** when the first child starts; untouched sibling issues remain blank until they begin.

### Linking Issues to PRs

Follow [`PULL_REQUEST_POLICY.md`](PULL_REQUEST_POLICY.md). Reference the issue in the PR description using `Closes #<issue-number>` so GitHub automatically closes the issue when the PR is merged.

### Automated Dependency PRs

- Dependabot pull requests are treated as maintenance work. [`.github/dependabot.yml`](../.github/dependabot.yml) applies `type/chore`, `priority/low`, `status/in-review`, `area/infrastructure`, and `dependencies`.
- Dependabot pull requests should normally use `priority/low`, `status/in-review`, and `area/infrastructure` unless the update affects a different area or becomes urgent.
- Dependabot pull requests do not need a separate tracking issue unless the update uncovers breaking changes, follow-up work, or a broader remediation task.
- Dependabot updates should be grouped by related package area where practical so each pull request stays reviewable.
- Low-risk Dependabot patch updates may use GitHub auto-merge, but only after the standard CI workflow has passed successfully.
- Merge Dependabot pull requests only after the standard CI workflow passes and the package change has been reviewed for relevance and release risk.

---

## Milestones

Milestones map to implementation phases and releases:

| Milestone | Phase | Target Release |
|-----------|-------|---------------|
| Phase 1 — Foundation | Phase 1 | v0.1.0 |
| Phase 2 — Core Features | Phase 2 | v0.2.0 |
| Phase 3 — Migration and Triage | Phase 3 | v0.3.0 |
| Phase 4 — Visualisation and Templates | Phase 4 | v0.4.0 |
| Phase 5 — Cross-Repo PM Workflow | Phase 5 | v1.2.0 (GitHub milestone may still show the former `v0.5.0` title until renamed) |
| Phase 6 — Polish and v1.0 | Phase 6 | v1.0.0 |
| Deferred follow-ons | Phase 6 field | v1.1.0 |

### Milestone Workflow

1. Create the milestone on GitHub at the start of each phase.
2. Assign all issues planned for that phase to the milestone.
3. Track progress via the milestone's completion percentage.
4. Close the milestone when all issues are resolved and the release is tagged.

---

## Projects (v2)

SoloDevBoard uses a single **GitHub Projects (v2)** board called "SoloDevBoard". See [PROJECT_BOARD_DESIGN.md](PROJECT_BOARD_DESIGN.md) for the column structure and automation rules.

### Workflow Ownership

- The roadmap board is **issue-driven**. Issues are the primary planning and delivery records.
- Copilot agents own deliberate board changes such as adding planned issues, setting `Phase` and `Priority`, moving items through `Todo` / `Up Next` / `In Progress` / `Done`, and maintaining dates.
- `.github/workflows/roadmap-sync.yml` is the operational bridge for the user-owned roadmap board. It reconciles board metadata from issue lifecycle events plus scheduled and manual hygiene runs when direct project-board credentials are unavailable to the current agent runtime.
- Weekly governance must also audit board hygiene: missing dates on active or done items, invalid date pairs, roadmap issues missing from the board, and stray pull request cards.
- GitHub project workflows are kept intentionally narrow and issue-centric so they act as safety nets rather than competing sources of truth.
- Standalone pull request cards do not belong on the main roadmap board. A linked pull request should be visible through the board's **Linked pull requests** field on the issue item instead.
- `Auto-add to project` should remain disabled for this board, because it adds raw issues and pull requests before the planned metadata is applied.

### Project-Only Execution Queue

- **Up Next** is a project board status used to queue the next short-horizon batch of stories, enablers, and tests.
- **Up Next** is not a GitHub issue label and must not be added to the issue taxonomy in [LABEL_STRATEGY.md](LABEL_STRATEGY.md).
- **Focus Order** is a project number field used to order the current **Up Next** batch on the Story Board.
- Apply **Focus Order** only to stories, enablers, and tests that are currently in **Up Next**.
- Leave **Focus Order** blank for Features, Epics, and all non-queued items.
- New issues still enter the board in **Todo** unless there is an explicit instruction to build an **Up Next** queue.
- Linked pull requests should not be added to the roadmap board as separate items; they remain attached through the board's pull request field on the linked issue.

---

## GitHub Issues as Source of Truth

GitHub Issues are the **canonical** store for all open, deferred, and in-progress work. [`plan/BACKLOG.md`](BACKLOG.md) is a slim roadmap index linking to Issues, milestones, and Project #8 — it is not a living work queue.

- When new work is identified, create or update a GitHub Issue and sync Project #8.
- When an issue is closed, no markdown backlog tick-off is required.
- When a feature is descoped, close the corresponding issue with a `wontfix` label and update `plan/SCOPE.md` if needed.

---

## GitHub Copilot Chat Guidance

When using Copilot Chat to manage issues:

- **"Create an issue for [task]"** → Copilot should draft the issue body using the appropriate template fields, suggest labels from `LABEL_STRATEGY.md`, and suggest the appropriate milestone.
- **"What issues are open in Phase 2?"** → Copilot should query GitHub Issues filtered by milestone or labels.
- **"Plan the Triage UI follow-on"** → Copilot should create or update GitHub Issues and sync Project #8.
- **"Populate Up Next for today"** → Copilot should move the selected stories, enablers, or tests to the board-only **Up Next** state and assign **Focus Order** values in the recommended sequence.

---

## AI Collaborator Instructions

> When asked to create, update, or close issues:
>
> 1. Query GitHub Issues or Project #8 for the relevant work item.
> 2. Apply labels from `LABEL_STRATEGY.md` — minimum: one `type/`, one `priority/`, one `area/`.
> 3. Assign the issue to the appropriate milestone.
> 4. Use the user story format in the issue body when creating from planning input.
> 5. Sync the issue to Project #8 per the `repo-github-project` skill.
