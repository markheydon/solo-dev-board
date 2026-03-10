# SoloDevBoard — Project Board Design

<!-- AI Collaborator Instructions: See the "AI Collaborator Instructions" section at the bottom of this file before making changes to the board structure. -->

This document defines the design of the SoloDevBoard GitHub Projects (v2) board, including columns, fields, automation rules, and the relationship between the board and the Board Rules Visualiser feature.

---

## Board Name

**SoloDevBoard Roadmap** (GitHub Projects v2, single board for the entire project)

---

## Column Structure

| Column | Purpose |
|--------|---------|
| **Todo** | Issues assigned to the current phase and ready to start, but not yet selected for the immediate execution batch. |
| **Up Next** | The next short-horizon batch of stories, enablers, and tests chosen for execution. |
| **In Progress** | Issues actively being worked on. |
| **Done** | Issues completed and closed. |

---

## Field Usage

| Field | Usage |
|-------|-------|
| **Status** | Core board state used by all main views. |
| **Phase** | Maps issues to the implementation phase via milestone. |
| **Priority** | Mirrors the issue's delivery priority. |
| **Focus Order** | Numeric sequence for the current **Up Next** batch on the Story Board only. |
| **Start Date / Target Date** | Actual start and completion tracking plus size-based estimates once work begins. |

Rules for **Focus Order**:
- Apply it only to stories, enablers, and tests.
- Apply it only when the item is currently in **Up Next**.
- Leave it blank for Features, Epics, and all non-queued items.
- Sort the Story Board by **Focus Order** ascending when working from the queue.

---

## Automation Rules

The following automation rules are configured on the board. These are documented here so that the **Board Rules Visualiser** feature can use them as a reference implementation.

### Issue Created
- **Trigger:** A new issue is opened in the repository.
- **Action:** Add to **Todo**.

### Daily Queue Selected
- **Trigger:** The user explicitly asks Copilot to populate today's working queue.
- **Action:** Move the selected stories, enablers, or tests to **Up Next** and set **Focus Order** in the recommended sequence.

### Pull Request Opened
- **Trigger:** A pull request linked to an issue is opened.
- **Action:** Keep the linked issue in its current execution state on the board; apply `status/in-review` label and rely on the linked pull request field for review visibility.

### Pull Request Merged
- **Trigger:** A pull request is merged to `main`.
- **Action:** Move the linked issue to **Done**; close the issue; apply `status/done` label.

### Label: status/in-progress Applied
- **Trigger:** The `status/in-progress` label is applied to an issue.
- **Action:** Move the issue to **In Progress**.

### Label: status/todo Applied
- **Trigger:** The `status/todo` label is applied to an issue.
- **Action:** Move the issue to **Todo** if it is not explicitly queued in **Up Next**.

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
| **Story Board** | `-label:type/epic -label:type/feature` | Day-to-day execution view; sort by **Focus Order** ascending when using **Up Next**. |
| **Feature Board** | `label:type/feature` | Track feature-level progress without execution sequencing. |
| **Epic Board** | `label:type/epic` | Track epic-level progress and phase coverage. |
| **By Phase** | Filter by milestone | Focus on the current phase. |
| **Untracked Issues** | No milestone assigned | Identify issues that need scheduling. |

---

## AI Collaborator Instructions

> When making changes to the project board structure:
>
> 1. Update the **Column Structure** table in this document to reflect any added, renamed, or removed columns.
> 2. Update the **Field Usage** section when a field is added or its operational meaning changes.
> 3. Update the **Automation Rules** section to reflect any new or changed rules.
> 4. If the rules change affects the Board Rules Visualiser feature, update `docs/user-guide/board-rules-visualiser.md` accordingly.
> 5. Ensure label changes align with `LABEL_STRATEGY.md`.
> 6. If a new automation rule requires a new label, add that label to `LABEL_STRATEGY.md` first.
> 7. Keep project-only workflow states such as **Up Next** out of the issue label taxonomy unless there is a deliberate lifecycle change.
