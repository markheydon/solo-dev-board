---
weight: 30
title: Audit Dashboard
landing: true
landingSubtitle: "Consolidated view of issues, open PRs, label consistency, and workflow health across all repositories."
guideStatus: Available
---

The Audit Dashboard summarises open issues, open pull requests, and repository health across the repositories you select.

![Audit Dashboard showing KPI summary and health indicators for markheydon/solo-dev-board](/images/audit-dashboard/overview.png)

## Accessing the Audit Dashboard

- Open **Audit Dashboard** from the app home page or the left navigation.
- The route is `/audit-dashboard`. The legacy `/audit` route is also supported.

## Features

- Choose one or more active repositories with the repository selector before loading data.
- Review a sortable summary grid with repository name (linked to GitHub), open issue count, and open pull request count.
- KPI summary cards show total open issues, total open pull requests, unlabelled issues, and failing workflows for the selection.
- Health sections cover unlabelled issues, stale pull requests, and failing workflows, each with a badge count and expandable detail.
- Loading, empty, error, and prompt states appear in a consistent feedback region.
- Auto-refresh can be set to off, every 1 minute, every 5 minutes, or every 15 minutes (default: every 5 minutes).
- **Export Markdown** copies the current audit summary to the clipboard, respecting the selected repository filter.
- Links in health sections open in a new browser tab.

## How to use

1. Open the Audit Dashboard from the app home page or navigation menu.
2. Search and select the repositories you want to audit.
3. Use **Select all** to include every active repository, or **Clear** to reset the selection.
4. Click **Load selected repositories** to fetch audit data.
5. Review the summary grid and KPI cards.
6. Expand health sections for unlabelled issues, stale pull requests, and failing workflows.
7. Optionally set **Auto-refresh** to keep the summary up to date.
8. Click **Export Markdown** to copy the current summary for planning notes.
9. To change the repository set, adjust the selector and load again.

## Empty states

When a health category has no items, the dashboard shows a positive empty-state message, for example:

- "No unlabelled issues — great."
- "No stale pull requests — great."
- "No failing workflows — great."
