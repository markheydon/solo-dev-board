---
layout: page
title: Audit Dashboard
parent: User Guide
nav_order: 2
---

# Audit Dashboard

The Audit Dashboard provides a summary of open issues, open pull requests, and repository health indicators across all your GitHub repositories in SoloDevBoard.

## Accessing the Audit Dashboard

- Navigate to the Audit Dashboard via the dashboard card link on the home page.
- The route is `/audit-dashboard`. The legacy `/audit` route is also supported.

## Features

- Each repository is shown as a row in a sortable MudBlazor grid.
- Columns include repository full name (linked to GitHub), open issue count, and open pull request count.
- Total summary cards display aggregate open issues and open pull requests across all repositories.
- A loading skeleton is shown while data is being fetched.
- An empty state is displayed if no repositories are returned.
- The browser page title is set to 'Audit Dashboard — SoloDevBoard'.
- A repository filter allows you to focus the dashboard on a single repository.
- The Unlabelled Issues section shows issues without any labels, including repository, issue number, title, and age.
- The Stale Pull Requests section lists pull requests with no activity in the last 14 days, including repository, pull request number, title, author, and days since update.
- The Failing Workflows section shows the latest run for each workflow where the conclusion is failure or cancelled, including repository, workflow name, branch, and run link.
- Each health indicator section shows a badge count and uses MudBlazor expansion panels for expand and collapse behaviour.
- Links in health indicator sections open in a new browser tab.

## Usage

1. Open the Audit Dashboard from the dashboard or navigation menu.
2. Use the repository filter to select a specific repository or view all.
3. Review the grid to see open issues and pull requests for each repository.
4. Click a repository name to open it directly in GitHub.
5. Use the summary cards to view total open issues and pull requests.
6. Expand the health indicator panels to review unlabelled issues, stale pull requests, and failing workflows.
7. Click links in health sections to open items in GitHub (in a new tab).

## Notes

Zero-state messages are shown when no items are found in a health indicator section:
- "No unlabelled issues — great!"
- "No stale pull requests — great!"
- "No failing workflows  great!"

For more information about upcoming features, see the [BACKLOG](../../plan/BACKLOG.md).
