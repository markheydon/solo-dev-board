---
layout: page
title: Audit Dashboard
parent: User Guide
nav_order: 2
---

# Audit Dashboard

The Audit Dashboard provides a summary of open issues, open pull requests, repository health indicators, and now features a wireframe-aligned repository selector, KPI summary cards, grouped health indicators, and a consistent feedback region across all your GitHub repositories in SoloDevBoard.

## Accessing the Audit Dashboard

- Navigate to the Audit Dashboard via the dashboard card link on the home page.
- The route is `/audit-dashboard`. The legacy `/audit` route is also supported.

## Features

- A repository selector lets you choose one or more of your active GitHub repositories before loading any data.
- Each repository is shown as a row in a sortable MudBlazor grid.
- Columns include repository full name (linked to GitHub), open issue count, and open pull request count.
- The repository selector at the top of the page provides clear access to repository selection and audit actions.
- KPI summary cards display total open issues, total open pull requests, unlabelled issues, and failing workflows across all selected repositories.
- Health indicator sections are grouped in a dedicated container below the KPI summary, making it easier to scan repository health at a glance.
- Feedback states (loading, empty, error, and prompt) are surfaced through a consistent feedback region, ensuring users always understand the current state of the dashboard.
- All UI elements follow the wireframe-aligned layout for improved clarity and usability.
- The browser page title is set to 'Audit Dashboard — SoloDevBoard'.
- The Unlabelled Issues section shows issues without any labels, including repository, issue number, title, and age.
- The Stale Pull Requests section lists pull requests with no activity in the last 14 days, including repository, pull request number, title, author, and days since update.
- The Failing Workflows section shows the latest run for each workflow where the conclusion is failure or cancelled, including repository, workflow name, branch, and run link.
- Each health indicator section shows a badge count and uses MudBlazor expansion panels for expand and collapse behaviour.
- Links in health indicator sections open in a new browser tab.

## Usage

1. Open the Audit Dashboard from the dashboard or navigation menu.
2. Use the repository selector autocomplete to search and select the repositories you want to audit.
3. Use **Select all** to include every active repository, or **Clear** to reset the selection.
4. Click **Load selected repositories** to fetch audit data for the chosen set.
5. Review the summary grid to see open issues and pull requests per repository.
6. Click a repository name to open it directly in GitHub.
7. Use the summary cards to view total open issues and pull requests.
8. Review the KPI summary cards for a quick overview of repository health.
9. Expand grouped health indicator sections to see details about unlabelled issues, stale pull requests, and failing workflows.
10. Observe feedback states in the feedback region for loading, empty, error, or prompt messages.
11. Click links in health sections to open items in GitHub (in a new tab).
12. To change the repository set, adjust the selector and click **Load selected repositories** again.

## Notes

Zero-state messages are shown when no items are found in a health indicator section:
- "No unlabelled issues — great."
- "No stale pull requests — great."
- "No failing workflows — great."
Feedback region also displays loading, error, and prompt messages to guide the user through dashboard states.

For more information about upcoming features, see the [BACKLOG](../../plan/BACKLOG.md).
