---
layout: page
title: Triage UI
parent: User Guide
nav_order: 5
---


## Overview

The Triage UI enables you to work through untriaged GitHub issues for a single selected repository in a focused, step-by-step session. Issues without any labels are queued and presented one at a time, allowing you to triage efficiently and track your progress throughout the session.

Key features:
- You can start a triage session for one repository at a time.
- Only unlabelled issues are included in the triage queue.
- The interface shows your current position (e.g., Item 3 of 10), remaining count, skipped count, and a progress indicator.
- You can move to the next item or skip the current item, optionally providing a reason for skipping.
- Skipped items can be revisited within the same session.
- Progress and session context are always visible, so you know how much work remains.

## How to Use

1. Select a repository to start a triage session.
2. The Triage UI queues all unlabelled issues for that repository.
3. Review each item in turn, then choose **Next** to continue or **Skip** to defer the current item with an optional reason.
4. After reaching the end of the queue, choose the revisit action to process skipped items within the same session.
5. Use the progress indicator and session context to track your position, remaining items, and skipped count.

**Note:** The Triage UI currently supports one-repository sessions and progress visibility. Additional triage actions (such as label assignment, milestone selection, and project board placement) are planned for future releases and are not yet available.

Planned configuration options include:
- Filtering which issues appear in a triage session (e.g. unlabelled only, no milestone, created within the last 7 days).
- Customising available quick-action buttons for frequently used labels or milestones.
