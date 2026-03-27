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


## Quick Label Actions and Keyboard Shortcuts

You can apply labels to issues directly from the Triage UI without leaving the triage view.

- Use the **Quick Label** search field to find and select a repository label. The field accepts free-text search so you can type any part of a label name.
- Click **Apply + next** (or press **L**) to apply the selected label and immediately advance to the next item.
- Click **Next** (or press **N**) to move to the next item without applying a label.
- Keyboard shortcuts work when the action button row is focused. Typing in the Quick Label field does not trigger shortcuts.
- Primary success and failure feedback appears inline in the triage view via the operation alert, with additional snackbar notifications used for selected error conditions.

The two-action model keeps the triage rhythm straightforward: every item is either labelled and advanced, or advanced without changes.
