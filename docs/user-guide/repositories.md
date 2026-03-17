---
layout: page
title: Repositories
parent: User Guide
nav_order: 0
---

The Repositories page provides a central view of all repositories available to your authenticated GitHub account.

---

## Overview

The Repositories page enables you to:
- View all accessible repositories in a responsive data grid.
- Search repositories by name using the search field in the command area.
- Perform actions such as refresh, add, remove, and bulk operations from the command strip.
- See repository status at a glance with visual chips for connection (Connected/Archived) and visibility (Public/Private).
- Access row actions for each repository, including Edit and More (placeholders for future milestones).
- Receive clear feedback for loading, success, empty, and error states.

---

## Page Layout and Behaviour

- **Command Strip**: Located at the top of the page, the command strip includes buttons for Refresh, Add, Remove, and Bulk actions. On desktop, these appear as individual buttons; on mobile, they are grouped into a compact actions menu for easier access.
- **Search Field**: The search box allows you to filter repositories by name directly from the command area.
- **Data Grid**: The main grid displays repository name, status chips (Connected/Archived and Public/Private), and row actions. The Edit and More actions are placeholders for future enhancements.
- **Feedback Region**: The page provides clear feedback for loading, success, empty, and error states, helping you understand the current status of your repositories.

---

## How to Use

1. Open **Repositories** from the left navigation menu.
2. Use the search field to filter repositories by name if needed.
3. Use the command strip to refresh the list, add new repositories, remove selected ones, or perform bulk actions.
4. Review repository status using the visual chips and row actions.
5. Observe the feedback region for loading, empty, or error messages.

---

## Troubleshooting

If repositories do not appear or loading fails:
- Confirm your GitHub token is configured via user secrets or environment variables.
- Ensure the token has the required scopes for your repositories.
- Check that your network connection allows access to `api.github.com`.
- Use the Refresh button to retry loading after resolving any issues.
