---
layout: page
title: Triage UI
parent: User Guide
nav_order: 5
---


## Overview

The Triage UI enables you to work through untriaged GitHub items for a single selected repository in a focused, step-by-step session. Unlabelled issues are always included, and you can optionally include pull requests in the same queue.

Key features:
- You can start a triage session for one repository at a time.
- The queue includes unlabelled issues, with an option to include pull requests.
- The interface shows your current position (e.g., Item 3 of 10), a remaining count, and a progress indicator.
- Use the **Quick Label** field to search and select a label, then click **Apply + next** to apply it and move on.
- Click **Next** to advance without making any change to the current item.
- Progress and session context are always visible, so you know how much work remains.

## How to Use

1. Select a repository to start a triage session.
2. The Triage UI queues all unlabelled issues for that repository.
3. Review each item in turn, then either:
   - Search for a label in the **Quick Label** field and click **Apply + next** to label the issue and advance, or
   - Click **Next** to advance to the next item without applying a label.
4. The session completes once all queued items have been processed.
5. Use the progress indicator and session context to track your position and the number of remaining items.



## Quick Label and Duplicate Actions with Keyboard Shortcuts

You can apply labels or close issues as duplicates directly from the Triage UI without leaving the triage view.

- Use the **Quick Label** search field to find and select a repository label. The field accepts free-text search so you can type any part of a label name.
- Click **Apply + next** (or press **L**) to apply the selected label and immediately advance to the next item.
- Click **Next** (or press **N**) to move to the next item without applying a label.
- To close an issue as a duplicate, use the **Duplicate reference** input to enter the reference number of the original issue, then click **Close as duplicate** (or press **D**) to close the current issue and advance.
- The **Duplicate reference** input is shown inline when the duplicate action is selected, allowing you to specify the related issue number before confirming the closure.
- Keyboard shortcuts work when the action button row is focused. Typing in the Quick Label or Duplicate reference fields does not trigger shortcuts.
- Primary success and failure feedback appears inline in the triage view via the operation alert, with additional snackbar notifications used for selected error conditions.

The action model now supports three flows: label and advance, close as duplicate and advance, or advance without changes. This keeps the triage rhythm straightforward and efficient.

## Assigning Milestones from the Triage UI

You can assign a milestone to an issue directly within the Triage UI, without leaving your triage session.

- In the **Planning Actions** section, use the milestone dropdown to select the desired milestone for the current issue.
- Click the **Assign milestone** button to apply your selection.
- A feedback message will confirm whether the milestone was assigned successfully or if an error occurred.
- You can continue triaging without interruption after assigning a milestone.

Milestone assignment is available for all issues where you have permission to edit milestones. If the operation fails, an error message will be shown with guidance to retry or check your permissions.

## Adding Issues to Project Boards from the Triage UI

The Triage UI allows you to add issues to a GitHub project board and set their status column in context.

- In the **Planning Actions** section, select a project board from the available list.
- Choose the desired project status (column) for the issue.
- Click the **Add to project board** button to place the issue in the selected board and status.
- Success or failure feedback will appear in the user feedback region, confirming the result of the operation.

If the issue is already on the selected board, you can update its status column directly. Any errors encountered during project board placement will be surfaced with actionable feedback.

All planning actions use MudBlazor controls for a consistent and accessible experience, and the workflow aligns with the [triage wireframe](../../plan/wireframes/triage-ui-wireframe.md).
