---
title: Cross-Repo PM Workflow
draft: true
weight: 110
guideStatus: Partially Available
---

> ⚠️ **Partial delivery** — Daily Focus board occupancy and Repo Management are available in the app under **PM Workflow**. Backlog Review, Iteration Planning, and Daily Focus stall/recommendation panels are not shipped yet. This guide remains a draft until all four tabs match the wireframe; only the sections below describe current behaviour.

---

## Overview

The Cross-Repo PM Workflow brings a structured, two-mode operating system into SoloDevBoard, replacing the manual AI prompts and scripts in [markheydon/github-workflows](https://github.com/markheydon/github-workflows) with a visual interface.

The system is built around two modes of operation:

- **PM Mode** (weekly or fortnightly) — Active curation: review your backlog across all repositories, resolve stalled work, and populate your project board with a realistic set of committed items for the next few days.
- **Work Mode** (daily) — Execution: the project board is the single pane of glass. Open it, pick the next item, and get things done. Daily Focus currently shows board occupancy and active load so you can start the day without opening GitHub.

### What is available now

| Tab | Route | Status |
|-----|-------|--------|
| **Daily Focus** | `/pm-workflow/daily-focus` | **Partial** — Status occupancy chips and active load. Stalled items and recommendations are still planned. |
| Backlog | `/pm-workflow/backlog` | Placeholder — story [#277](https://github.com/markheydon/solo-dev-board/issues/277). |
| Planning | `/pm-workflow/planning` | Placeholder — story [#283](https://github.com/markheydon/solo-dev-board/issues/283). |
| **Repos** | `/pm-workflow/repos` | **Available** — planning board selection, thresholds, repository exclusions, and per-repository open-work counts. |

Open **PM Workflow** from the Home card or the navigation drawer. The hub redirects to **Daily Focus**.

---

## Daily Focus (partial)

Daily Focus is a read-only morning snapshot of the selected planning board. It answers how busy the board is today; stall clocks and ranked recommendations will follow in later stories.

### Accessing

1. Open the navigation drawer, or open **PM Workflow** from Home.
2. Select **PM Workflow** if you used the drawer.
3. You land on the **Daily Focus** tab (`/pm-workflow/daily-focus`).

Shared chrome on every PM Workflow tab includes:

- **Planning board** selector — choose a Projects v2 board discovered from your active repositories (same discovery model as Triage and Board Rules).
- **Status line** — `Repos: N included`, selected board title (when chosen), last refreshed time, and a **Refresh** control to reload board options and the repository catalogue.
- **Tab strip** — Daily Focus, Backlog, Planning, and Repos.

If GitHub reports linked project boards that cannot be read with your current sign-in (common for private user-owned boards under GitHub App sign-in), a warning appears at the top of the page. See [plan/GITHUB_PROJECTS_V2_ACCESS.md](https://github.com/markheydon/solo-dev-board/blob/main/plan/GITHUB_PROJECTS_V2_ACCESS.md) in the repository.

Until a board is selected, an informational alert points at the **Planning board** dropdown. Occupancy loads as soon as you open Daily Focus when a board is already stored, or as soon as you choose one. Use the **Repos** tab to edit exclusions and thresholds.

A progress bar at the top of the page shows while planning boards are discovered. Switching tabs does not restart that load.

### Board occupancy and active load

After a board is selected, Daily Focus shows:

- **Status chips** — one chip per Status option discovered on that board (including empty columns), with the item count. Option identifiers are not hard-coded to any one GitHub project.
- **Active load** — `Up Next` plus `In Progress` item counts over the persisted **Capacity limit** (default 8).

Board occupancy counts mapped **Issue** and **Pull Request** cards on the selected board. Notes, draft issues, and redacted items are omitted. Repository exclusions do not change these counts; they will apply to recommendations when that panel ships.

If the catalogue cannot be loaded, an error alert includes **Retry**. An empty board still lists Status chips at zero and explains that there are no items.

> **Scope note** — Stalled Up Next items, stalled review pull requests, and the top-three recommended list are tracked on stories [#274](https://github.com/markheydon/solo-dev-board/issues/274)–[#276](https://github.com/markheydon/solo-dev-board/issues/276) and are not shown yet.

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

Board occupancy on Daily Focus counts mapped Issue and Pull Request cards. Recommendations, backlog queries, and planning candidates will honour repository exclusions.

### Set planning thresholds

Under **Planning thresholds**:

| Field | Default | Purpose |
|-------|---------|---------|
| **Capacity limit** | 8 | Denominator for Daily Focus active load, and the planned ceiling for Planning warnings. |
| **Stall days** | 3 | Days before an Up Next item is treated as stalled. |
| **Neglect days** | 14 | Days before a repository is treated as neglected in backlog views. |

1. Adjust the numeric fields.
2. Select **Save thresholds**.

### Manage repository participation

The **Repository participation** section shows how many repositories are included and excluded. Every active repository participates in PM queries by default. Archived repositories are never offered.

Excluded repositories are omitted from Daily Focus recommendations, Backlog Review, and Planning candidates.

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

Counts reuse the Cross-Repo PM work-item catalogue (the same read model used by later Daily Focus and Backlog views). They do not call GitHub a second time for issues and pull requests.

If some repositories fail to load, a warning lists them and **Retry** reloads the catalogue. A complete load failure shows an error with **Retry**.

Use this table to see load at a glance before excluding a repository or planning the next iteration.

---

## Key Capabilities (planned)

The following sections describe the full v1.1.0 feature set. They are **not** available in the app yet.

### Daily Focus (remaining)

A quick morning summary that will also answer "what should I work on right now?":

- Stalled items: anything in your Up Next column for three or more days.
- Stalled PR reviews: pull requests in review for three or more days that need a merge, close, or return-to-progress decision.
- Top three recommended work items, ranked by priority across included repositories.

### Backlog Review

A cross-repository prioritised view of all your open work:

- Urgent items (high-priority issues and PRs surfaced at the top).
- Items ready to start (unblocked stories and bugs across included repos).
- Blocked and deferred items (parked in Blocked or Ice Box).
- Neglected repositories (no issue or PR activity within the neglect threshold).
- Items awaiting triage (missing `type/` or `priority/` labels).

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
| Stall days | Up Next stall threshold (default 3). |
| Neglect days | Repository neglect threshold (default 14). |

Clearing site data for SoloDevBoard resets PM settings to defaults. Settings do not sync across browsers or devices.

There are no `appsettings.json` entries for PM Workflow user preferences.

---

## Related documentation

- Wireframe: [`plan/wireframes/pm-workflow-wireframe.md`](https://github.com/markheydon/solo-dev-board/blob/main/plan/wireframes/pm-workflow-wireframe.md)
- Decision: [DEC-029](https://github.com/markheydon/solo-dev-board/blob/main/plan/DECISIONS.md#dec-029-cross-repo-pm-workflow-board-selection-and-local-settings) (board selection and local settings)
