---
title: Cross-Repo PM Workflow
draft: true
weight: 110
guideStatus: Partially Available
---

> ⚠️ **Partial delivery** — Daily Focus, Backlog Review grouping, and Repo Management are available in the app under **PM Workflow**. Iteration Planning remains a placeholder. This guide remains a draft until all four tabs match the wireframe; only the sections below describe current behaviour.

---

## Overview

The Cross-Repo PM Workflow brings a structured, two-mode operating system into SoloDevBoard so you can plan and execute work across your own GitHub repositories from one visual interface.

The system is built around two modes of operation:

- **PM Mode** (weekly or fortnightly) — Active curation: review your backlog across all repositories, resolve stalled work, and populate your project board with a realistic set of committed items for the next few days.
- **Work Mode** (daily) — Execution: the project board is the single pane of glass. Open it, pick the next item, and get things done. Daily Focus currently shows occupancy and active load for the selected planning board, stalled Up Next items, the top three unblocked items from all included repositories, and pull requests that have been waiting on review for the configured **Stall days** threshold (default 3).

### What is available now

| Tab | Route | Status |
|-----|-------|--------|
| **Daily Focus** | `/pm-workflow/daily-focus` | **Partial** — Status occupancy chips, active load, stalled Up Next, top-three recommendations, and pull requests awaiting review for the configured Stall days threshold. |
| **Backlog** | `/pm-workflow/backlog` | **Partial** — search, type and repository filters, Urgent, Ready to start, Awaiting triage, Blocked/deferred, Epics near completion, and Neglected repositories panels. Issue and pull request kind chips appear on grouped rows. Urgent items are omitted from Ready to start. |
| Planning | `/pm-workflow/planning` | Placeholder — story [#283](https://github.com/markheydon/solo-dev-board/issues/283). |
| **Repos** | `/pm-workflow/repos` | **Available** — planning board selection, thresholds, repository exclusions, and per-repository open-work counts. |

Open **PM Workflow** from the Home card or the navigation drawer. The hub redirects to **Daily Focus**.

### Opinionated conventions

PM Workflow is intentionally opinionated about **how** work is labelled and how a planning board is shaped. You choose which repositories and which Projects v2 board to use; SoloDevBoard does not hard-code a maintainer project or repository.

For best results, align your board and labels with these product conventions:

- **SoloDevBoard label taxonomy** — prefixes such as `type/`, `priority/`, and `status/` (including `status/blocked` and `status/ice-box`). The Label Manager **SoloDevBoard** recommended strategy applies this set.
- **Status display names** — columns matched by name, including `Up Next`, `In Progress`, `Blocked`, and `Ice Box` (plus review-equivalent names where Daily Focus looks for stalled reviews).
- **Focus Order** — optional Projects v2 number field used when Iteration Planning writes ordered Up Next items.

These conventions are product behaviour, not upstream dogfood. They mirror the published labelling guidance for this project but apply to any GitHub user who adopts the same taxonomy on their own boards.

---

## Daily Focus (partial)

Daily Focus is a read-only morning snapshot. Occupancy and active load describe the selected planning board. Stalled Up Next items surface when the stall threshold is reached. **Recommended today** lists the top three unblocked items from all included repositories, not only cards on that board. Stalled review pull requests show which PRs have been waiting on review for too long.

### Accessing

1. Open the navigation drawer, or open **PM Workflow** from Home.
2. Select **PM Workflow** if you used the drawer.
3. You land on the **Daily Focus** tab (`/pm-workflow/daily-focus`).

Shared chrome on every PM Workflow tab includes:

- **Planning board** selector — choose a Projects v2 board discovered from your active repositories (same discovery model as Triage and Board Rules).
- **Status line** — `Repos: N included`, selected board title (when chosen), last refreshed time, and a **Refresh** control to reload board options and the repository catalogue.
- **Tab strip** — Daily Focus, Backlog, Planning, and Repos.

If GitHub reports linked project boards that cannot be read with your current sign-in (common for private user-owned boards under GitHub App sign-in), a warning appears at the top of the page. See [plan/GITHUB_PROJECTS_V2_ACCESS.md](https://github.com/markheydon/solo-dev-board/blob/main/plan/GITHUB_PROJECTS_V2_ACCESS.md) in the repository.

Until a board is selected, an informational alert points at the **Planning board** dropdown. Occupancy, stalled Up Next items, and recommendations load as soon as you open Daily Focus when a board is already stored, or as soon as you choose one. Backlog Review uses the same board selection. Use the **Repos** tab to edit exclusions and thresholds.

A progress bar at the top of the page shows while planning boards are discovered. Switching tabs does not restart that load.

### Board occupancy, active load, and stalled Up Next

After a board is selected, Daily Focus shows:

- **Status chips** — one chip per Status option discovered on that board (including empty columns), with the item count. Option identifiers are not hard-coded to any one GitHub project.
- **Active load** — `Up Next` plus `In Progress` item counts over the persisted **Capacity limit** (default 8).
- **Stalled Up Next** — items whose Status name is `Up Next` and whose stall clock is at or beyond the persisted **Stall days** threshold (default 3, inclusive). Each row shows the title, age in whole days, and an **Open** link to GitHub. The link text stays **Open**; each control has a distinct accessible name that includes the item title.

Age prefers Status-changed-at. When that timestamp is missing, Daily Focus uses the item last-updated time and shows a footnote. Items with neither timestamp are omitted from the stalled list.

If no Up Next items meet the threshold, the stalled section still appears with a short none-stalled sentence.

Board occupancy counts mapped **Issue** and **Pull Request** cards on the selected board. Notes, draft issues, and redacted items are omitted. Repository exclusions do not change occupancy or stalled Up Next counts; they do apply to the recommended-today list and stalled review pull requests.

If the occupancy catalogue cannot be loaded, an error alert includes **Retry**. An empty board still lists Status chips at zero and explains that there are no items.

### Recommended today (all included repositories)

After a board is selected, Daily Focus also lists up to three unblocked work items from all included repositories, not only cards on that board:

1. Items labelled `status/blocked` or `status/ice-box` are omitted.
2. Items whose board Status is **Blocked**, **Ice Box**, or **In Progress** are omitted.
3. Remaining items are ranked `priority/critical`, then `priority/high`, `priority/medium`, `priority/low`, then unlabelled, and then by most recently updated.
4. Each row shows rank, a priority chip, an `owner/name#number` link that opens GitHub in a new tab, and the title.

Excluded repositories (see [Repo Management](#manage-repository-participation)) are omitted from this list. Items already in **Up Next** can still appear so you know what is queued.

If no unblocked items remain, an informational alert explains that there is nothing to recommend today. If every included repository fails to load, a separate error alert includes **Retry** rather than an empty list. If some repositories fail but others succeed, Daily Focus still ranks the remaining items and shows a warning with **Retry**. Occupancy can still show on its own while recommendations continue to load. GitHub 404 or 410 responses for issues or pull requests on a repository that is already in the catalogue (for example a profile README repository whose `/pulls` endpoint is not found) are treated as no items, not as a load failure.

### Stalled review pull requests

After a board is selected, Daily Focus also lists pull requests that have been waiting on review for the configured **Stall days** threshold (default 3, inclusive).

Detection uses two modes:

1. **Board column** — if the selected board has an **In Review** Status, or a clearly equivalent name such as **Waiting on review** or **Code Review**, SoloDevBoard uses time in that column (Status-changed time when GitHub provides it).
2. **Pending review fallback** — if there is no such column, it uses open, non-draft pull requests with a pending review (`REVIEW_REQUIRED` or requested reviewers) that are at least the stall threshold old.

Each row shows the repository (`owner/name`), pull request number, age in days, and an **Open** link to GitHub. Excluded repositories are omitted from this list.

If no pull requests meet the threshold, an informational alert says so. If the list cannot be loaded, an error alert includes **Retry**.

---

## Backlog Review (partial)

Backlog Review is a read-only weekly PM pass across included repositories. It groups open issues and pull requests into urgency panels. Rows open GitHub in a new tab. Adding work to **Up Next** remains on the Planning tab.

### Accessing

1. Open **PM Workflow**.
2. Select the **Backlog** tab (`/pm-workflow/backlog`).

The same shared chrome described under Daily Focus appears on this tab. Until a board is selected, an informational alert points at the **Planning board** dropdown.

### Filters

Above the panels:

- **Type** — All types, Issues, or Pull requests.
- **Repository** — All repositories, or one `owner/name` value from the loaded catalogue.
- **Search** — Matches title, repository name, or item number.

Filters apply in the browser after the catalogue loads. They do not reload GitHub.

### Groups

After a board is selected, expansion panels list:

1. **Urgent** — items labelled `priority/high` or `priority/critical`.
2. **Ready to start** — unblocked items (`status/blocked` and `status/ice-box` absent, board Status not Blocked or Ice Box) that are not already **Up Next** or **In Progress**. Items already listed under **Urgent** are omitted here.
3. **Awaiting triage** — open issues or pull requests missing a `type/` or `priority/` label.
4. **Blocked / deferred** — items labelled `status/blocked` or `status/ice-box`, or whose joined board Status is **Blocked** or **Ice Box**.
5. **Epics near completion** — open epics or features whose sub-issues are all closed (requires GitHub sub-issue counts).
6. **Neglected repositories** — included repositories with no open issue or pull request activity within the persisted **Neglect days** threshold (default 14).

Each grouped row shows an **Issue** or **PR** chip, a `owner/name#number` link that opens GitHub in a new tab, the title, and the priority label when present.

An item can appear in more than one panel (for example an urgent item that is also blocked). Each panel header includes a count. Empty panels say there are no items in that group.

If there are no open issues or pull requests in included repositories, an informational alert says so. If the current filters hide every row, a separate alert says no items match. If every included repository fails to load, an error alert includes **Retry**. If some repositories fail but others succeed, grouping still proceeds and a warning lists the failed repositories with **Retry**. When GitHub does not return sub-issue counts, **Epics near completion** explains that near-complete parents cannot be listed.

> **Scope note** — Iteration Planning (adding items to **Up Next**) remains on story [#283](https://github.com/markheydon/solo-dev-board/issues/283).

---

## Repo Management (available)

Repo Management controls which repositories and which Projects v2 board participate in PM operations. Settings persist in your browser (see [Configuration](#configuration)).

### Accessing

1. Open **PM Workflow**.
2. Select the **Repos** tab (`/pm-workflow/repos`).

The same shared chrome described under Daily Focus appears on this tab.

### Select a planning board

1. Open the **Planning board** dropdown in the shared chrome.
2. Choose a board title from the list (boards are discovered across active, non-archived repositories).
3. The selection is saved automatically.

Until a board is selected, an informational alert explains that other tabs need a board. You can still edit Repo Management settings below.

Board occupancy on Daily Focus counts mapped Issue and Pull Request cards. Recommendations, stalled Up Next items, stalled review pull requests, and Backlog Review honour repository exclusions. Planning candidates will also honour exclusions when that panel ships.

### Set planning thresholds

Under **Planning thresholds**:

| Field | Default | Purpose |
|-------|---------|---------|
| **Capacity limit** | 8 | Denominator for Daily Focus active load, and the planned ceiling for Planning warnings. |
| **Stall days** | 3 | Inclusive days before Daily Focus treats an Up Next item or pull request awaiting review as stalled. |
| **Neglect days** | 14 | Days before a repository is treated as neglected in backlog views. |

1. Adjust the numeric fields.
2. Select **Save thresholds**.

### Manage repository participation

The **Repository participation** section shows how many repositories are included and excluded. Every active repository participates in PM queries by default. Archived repositories are never offered.

Excluded repositories are omitted from Daily Focus recommendations, stalled Up Next, stalled review pull requests, and Backlog Review, and, when it ships, Planning candidates.

**Included repositories** lists active `owner/name` values that currently participate:

1. Optionally narrow the list with **Filter included repositories**.
2. Select **Exclude** on the row you want to remove.

**Quick exclude** under **Excluded repositories** offers the same action via search:

1. Type an `owner/name` value in **Quick exclude**.
2. Select **Exclude**.

**To include a repository again:**

1. Find the repository in the **Excluded repositories** list.
2. Select **Include again**.

Exclusions persist immediately in browser storage.

### Per-repository summary

The **Per-repository summary** table lists included repositories only:

| Column | Meaning |
|--------|---------|
| **Repository** | Full `owner/name`. |
| **Open issues** | Open issues in the PM work-item catalogue. |
| **Open PRs** | Open pull requests in the same catalogue. |
| **Last activity** | Latest catalogue item update, or the repository update time when there are no open items. |
| **Included** | Always **Yes** in this table, because excluded repositories are omitted. |

Counts are aggregated in memory from the Cross-Repo PM work-item catalogue (the same read model later Daily Focus and Backlog views use). There is no separate GitHub count or search endpoint. Opening Repo Management still loads that catalogue from GitHub (issues, pull requests, review metadata, and sub-issue summaries) when it runs. Daily Focus occupancy uses the project board catalogue, so this visit is often the first work-item catalogue load rather than a reuse of an already-fetched snapshot.

If some repositories fail to load, a warning lists them. Failed repositories are omitted from the table so unavailable counts are not shown as zero open work. **Retry** reloads the catalogue. A complete load failure shows an error with **Retry**.

Use this table to see load at a glance before excluding a repository or planning the next iteration.

---

## Key Capabilities (planned)

The following sections describe the full v1.1.0 feature set. They are **not** available in the app yet.

### Daily Focus (remaining)

A quick morning summary that will also answer stalled-work questions:

- Stalled items: anything in your Up Next column for three or more days (shipped on Daily Focus as described above).
- Stalled PR reviews: shipped on Daily Focus as described above.
- Top three recommended work items: shipped on Daily Focus as described above.

### Backlog Review (remaining)

The grouped view is shipped as described above. Still planned:

- Adding selected items to **Up Next** from Backlog Review (Iteration Planning tab).

### Iteration Planning

A guided planning session that populates your project board's Up Next column:

- Shows current board capacity (Up Next + In Progress combined).
- Surfaces stalled Up Next items and asks you to resolve them before adding new work.
- Lets you select issues and pull requests from across included repositories.
- Optionally assigns a milestone to planned items in a single step.
- Configurable capacity limit with a confirmation dialog when exceeded.

---

## Configuration

PM Workflow preferences are stored in **browser localStorage** on the device you use (same pattern as theme preference). There is no server-side database for these settings.

Persisted fields:

| Setting | Description |
|---------|-------------|
| Planning board node id | GitHub GraphQL node id for the selected Projects v2 board. |
| Excluded repositories | `owner/name` values omitted from PM queries. |
| Capacity limit | Active load ceiling for planning (default 8). |
| Stall days | Stall threshold for Up Next and review pull requests on Daily Focus (default 3). |
| Neglect days | Repository neglect threshold (default 14). |

Clearing site data for SoloDevBoard resets PM settings to defaults. Settings do not sync across browsers or devices.

There are no `appsettings.json` entries for PM Workflow user preferences.

---

## Related documentation

- Wireframe: [`plan/wireframes/pm-workflow-wireframe.md`](https://github.com/markheydon/solo-dev-board/blob/main/plan/wireframes/pm-workflow-wireframe.md)
- Decision: [DEC-029](https://github.com/markheydon/solo-dev-board/blob/main/plan/DECISIONS.md#dec-029-cross-repo-pm-workflow-board-selection-and-local-settings) (board selection and local settings)
- Hardcoding audit: [`plan/HARDCODING_AUDIT_v1.1.md`](https://github.com/markheydon/solo-dev-board/blob/main/plan/HARDCODING_AUDIT_v1.1.md) (issue [#423](https://github.com/markheydon/solo-dev-board/issues/423))
