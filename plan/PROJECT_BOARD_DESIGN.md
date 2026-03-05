# SoloDevBoard — Project Board Design

<!-- AI Collaborator Instructions: See the "AI Collaborator Instructions" section at the bottom of this file before making changes to the board structure. -->

This document defines the design of the SoloDevBoard GitHub Projects (v2) board, including columns, automation rules, and the relationship between the board and the Board Rules Visualiser feature.

---

## Board Name

**SoloDevBoard** (GitHub Projects v2, single board for the entire project)

---

## Column Structure

| Column | Purpose |
|--------|---------|
| **Backlog** | All triaged issues that are ready to be worked on but not yet scheduled for the current sprint. |
| **Todo** | Issues assigned to the current sprint/phase and ready to start. |
| **In Progress** | Issues actively being worked on. |
| **In Review** | Issues with an open pull request under review. |
| **Done** | Issues completed and closed in the current sprint. |
| **Blocked** | Issues that cannot progress due to an external dependency or decision. |

---

## Automation Rules

The following automation rules are configured on the board. These are documented here so that the **Board Rules Visualiser** feature can use them as a reference implementation.

### Issue Created
- **Trigger:** A new issue is opened in the repository.
- **Action:** Add to **Backlog** column (if the issue has `status/todo` label or no status label).

### Pull Request Opened
- **Trigger:** A pull request linked to an issue is opened.
- **Action:** Move the linked issue to **In Review**; apply `status/in-review` label.

### Pull Request Merged
- **Trigger:** A pull request is merged to `main`.
- **Action:** Move the linked issue to **Done**; close the issue; apply `status/done` label.

### Label: status/in-progress Applied
- **Trigger:** The `status/in-progress` label is applied to an issue.
- **Action:** Move the issue to **In Progress**.

### Label: status/blocked Applied
- **Trigger:** The `status/blocked` label is applied to an issue.
- **Action:** Move the issue to **Blocked**.

### Label: status/todo Applied
- **Trigger:** The `status/todo` label is applied to an issue.
- **Action:** Move the issue to **Todo** (if it was previously in **Backlog**).

### Issue Closed (Not as Duplicate)
- **Trigger:** An issue is closed without being marked as a duplicate.
- **Action:** Move the issue to **Done**; apply `status/done` label if not already present.

### Issue Closed as Duplicate
- **Trigger:** An issue is closed as a duplicate (via the Triage UI or manually).
- **Action:** Remove from the board (do not add to Done).

---

## How Issues and PRs Appear on the Board

- **Issues** appear as cards. The card displays: title, labels, assignee, and milestone.
- **Pull Requests** linked to issues (via `Closes #N` in the PR body) update the linked issue's column automatically via the rules above.
- **Unlinked PRs** (no linked issue) are tracked separately and should not appear on the main board. Use a separate view or filter.

---

## Relationship to the Board Rules Visualiser Feature

The Board Rules Visualiser feature (see [board-rules-visualiser.md](../docs/user-guide/board-rules-visualiser.md)) will:

1. Read the automation rules configured on this board via the GitHub Projects v2 GraphQL API.
2. Display them as an interactive flow diagram.
3. Use the rules defined in this document as the **expected/canonical state** for comparison.

The rules defined here serve as both the operational configuration and the reference documentation for the visualiser's expected output.

---

## Board Views

In addition to the default board view, the following saved views are useful:

| View | Filter | Purpose |
|------|--------|---------|
| **By Area** | Group by `area/` label | See progress per feature area |
| **By Phase** | Filter by milestone | Focus on the current sprint |
| **Blocked Items** | Filter by `status/blocked` | Quickly identify blockers |
| **Untracked Issues** | No milestone assigned | Identify issues that need scheduling |

---

## AI Collaborator Instructions

> When making changes to the project board structure:
>
> 1. Update the **Column Structure** table in this document to reflect any added, renamed, or removed columns.
> 2. Update the **Automation Rules** section to reflect any new or changed rules.
> 3. If the rules change affects the Board Rules Visualiser feature, update `docs/user-guide/board-rules-visualiser.md` accordingly.
> 4. Ensure label changes align with `LABEL_STRATEGY.md`.
> 5. If a new automation rule requires a new label, add that label to `LABEL_STRATEGY.md` first.
