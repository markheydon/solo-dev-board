---
layout: page
title: Audit Dashboard
parent: User Guide
nav_order: 2
---

# Audit Dashboard

The Audit Dashboard provides a summary of open issues and open pull requests across all your GitHub repositories in SoloDevBoard.

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

## Usage

1. Open the Audit Dashboard from the dashboard or navigation menu.
2. Review the grid to see open issues and pull requests for each repository.
3. Click a repository name to open it directly in GitHub.
4. Use the summary cards to view total open issues and pull requests.

## Notes

- Health indicators (such as label consistency, stale PRs, workflow status) and filtering by repository are planned for future stories and are not included in the current release.

For more information about upcoming features, see the [BACKLOG](../../plan/BACKLOG.md).
