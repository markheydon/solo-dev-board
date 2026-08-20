---
weight: 20
title: Repositories
landing: true
landingIcon: folder
landingSubtitle: "View and manage repositories accessible to your GitHub account."
guideStatus: Partially Available
---

{{< callout type="info" >}}
**Scope note** — Viewing, searching, and refreshing the GitHub repository catalogue are available now. Add, Remove, Bulk actions, Edit, and More row actions are visible in the UI but not yet implemented; they show a future-milestone message when used.
{{< /callout >}}

## Overview

The Repositories page provides a central view of all repositories available to your authenticated GitHub account.

![Repositories page filtered to markheydon/solo-dev-board in the data grid](/images/repositories/overview.png)

Key goals of the Repositories page:
- Make accessible repositories visible in one responsive catalogue.
- Support quick search and refresh after connectivity changes.
- Provide a foundation for later catalogue management (add, remove, bulk, and row actions).

---

## How to Use

{{% steps %}}

### View and search repositories

1. Open **Repositories** from the left navigation menu.
2. Use the search field to filter repositories by name if needed.
3. Use **Refresh** to reload the catalogue after connectivity changes.
4. Review status chips (Connected or Archived) and visibility chips (Public or Private).

### What you can do now

- View accessible repositories in a responsive data grid.
- Search repositories by name.
- Refresh the catalogue from the command strip.
- See connection status and visibility at a glance.
- Use the phone-width stacked grid layout without horizontal overflow.

### What is coming later

- **Add** — add repositories to a managed SoloDevBoard catalogue beyond the live GitHub listing.
- **Remove** — remove selected repositories from that catalogue.
- **Bulk actions** — run multi-repository operations from the command strip.
- **Edit** (row action) — edit repository metadata or catalogue settings for a single repository.
- **More** (row action) — overflow menu for additional per-repository management.

Until those ship, each control opens an informational snackbar and feedback message stating that the action will be available in a future milestone.

{{% /steps %}}

### Page layout

- **Command strip** — Refresh (live), plus Add, Remove, and Bulk actions (stubs). On mobile the stub actions are grouped into a compact actions menu.
- **Search field** — Filter repositories by name.
- **Data grid** — Repository name, status chips, visibility chips, and stub row actions. On phone-width viewports the grid stacks each field so names wrap, chips stay fully visible, and row actions remain tappable without horizontal overflow.
- **Feedback region** — Loading, empty, success, error, and placeholder messages.

### Empty and error states

- **No repositories found:** The page explains that none were returned for your account.
- **Loading:** A progress indicator appears while repositories load.
- **Errors:** If GitHub cannot be reached, an error message appears with a retry action.

## Troubleshooting

If repositories do not appear or loading fails:

- Confirm GitHub authentication is working (connected status in the app bar).
- Check that your account can access the repositories you expect.
- Use **Refresh** to retry after resolving connectivity issues.
