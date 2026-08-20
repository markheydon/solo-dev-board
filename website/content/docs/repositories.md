---
weight: 20
title: Repositories
landing: true
landingIcon: folder
landingSubtitle: "View and manage repositories accessible to your GitHub account."
guideStatus: Available
---

The Repositories page provides a central view of all repositories available to your authenticated GitHub account.

![Repositories page filtered to markheydon/solo-dev-board in the data grid](/images/repositories/overview.png)

## Overview

From this page you can:

- View accessible repositories in a responsive data grid.
- Search repositories by name.
- Refresh the catalogue from the command strip.
- See connection status (Connected/Archived) and visibility (Public/Private) at a glance.
- Use row actions for each repository (some actions are placeholders for later enhancements).

## Page layout

- **Command strip** — Refresh, Add, Remove, and Bulk actions. On mobile these are grouped into a compact actions menu.
- **Search field** — Filter repositories by name.
- **Data grid** — Repository name, status chips, visibility chips, and row actions. On phone-width viewports the grid stacks each field so names wrap, chips stay fully visible, and row actions remain tappable without horizontal overflow.
- **Feedback region** — Loading, empty, success, and error messages.

## How to use

1. Open **Repositories** from the left navigation menu.
2. Use the search field to filter repositories by name if needed.
3. Use **Refresh** to reload the catalogue after connectivity changes.
4. Review status and visibility chips and row actions as needed.

## Troubleshooting

If repositories do not appear or loading fails:

- Confirm GitHub authentication is working (connected status in the app bar).
- Check that your account can access the repositories you expect.
- Use **Refresh** to retry after resolving connectivity issues.
