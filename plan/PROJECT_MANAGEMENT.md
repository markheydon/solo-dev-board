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

### Linking Issues to PRs

Reference the issue in your PR description using `Closes #<issue-number>` so GitHub automatically closes the issue when the PR is merged.

---

## Milestones

Milestones map to implementation phases and releases:

| Milestone | Phase | Target Release |
|-----------|-------|---------------|
| Phase 1 — Foundation | Phase 1 | v0.1.0 |
| Phase 2 — Core Features | Phase 2 | v0.2.0 |
| Phase 3 — Migration and Triage | Phase 3 | v0.3.0 |
| Phase 4 — Visualisation and Templates | Phase 4 | v0.4.0 |
| Phase 5 — Polish and v1.0 | Phase 5 | v1.0.0 |

### Milestone Workflow

1. Create the milestone on GitHub at the start of each phase.
2. Assign all issues planned for that phase to the milestone.
3. Track progress via the milestone's completion percentage.
4. Close the milestone when all issues are resolved and the release is tagged.

---

## Projects (v2)

SoloDevBoard uses a single **GitHub Projects (v2)** board called "SoloDevBoard". See [PROJECT_BOARD_DESIGN.md](PROJECT_BOARD_DESIGN.md) for the column structure and automation rules.

---

## Keeping Issues in Sync with BACKLOG.md

`plan/BACKLOG.md` is the **human-readable** planning document. GitHub Issues are the **trackable** implementation. Both should be kept in sync:

- When a new user story is added to `BACKLOG.md`, create a corresponding GitHub Issue.
- When an issue is closed, tick off the corresponding item in `BACKLOG.md`.
- When a feature is descoped, remove it from `BACKLOG.md` and close the corresponding issue with a "wontfix" label.

---

## GitHub Copilot Chat Guidance

When using Copilot Chat to manage issues:

- **"Create an issue for [task]"** → Copilot should draft the issue body using the appropriate template fields, suggest labels from `LABEL_STRATEGY.md`, and suggest the appropriate milestone.
- **"What issues are open in Phase 2?"** → Copilot should list items from `BACKLOG.md` in the Phase 2 section that are not yet ticked off.
- **"Update the backlog for the Triage UI"** → Copilot should update `BACKLOG.md` and suggest creating corresponding GitHub Issues.

---

## AI Collaborator Instructions

> When asked to create, update, or close issues:
>
> 1. Check `BACKLOG.md` for the relevant epic and user story.
> 2. Apply labels from `LABEL_STRATEGY.md` — minimum: one `type/`, one `priority/`, one `area/`.
> 3. Assign the issue to the appropriate milestone.
> 4. If creating an issue from a backlog item, use the item's user story format as the issue body.
> 5. After creating an issue, tick off the item in `BACKLOG.md` with a reference to the issue number.
> 6. Keep `BACKLOG.md` and GitHub Issues in sync — they should tell the same story.
