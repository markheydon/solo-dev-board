---
title: Cross-Repo PM Workflow
draft: true
weight: 110
guideStatus: Partially Available
---

> ⚠️ **Partial delivery** — Repo Management is available in the app under **PM Workflow** in the navigation drawer. Daily Focus, Backlog Review, and Iteration Planning are not shipped yet. This guide remains a draft until all four tabs match the wireframe; only the sections below describe current behaviour.

---

## Overview

The Cross-Repo PM Workflow brings a structured, two-mode operating system into SoloDevBoard, replacing the manual AI prompts and scripts in [markheydon/github-workflows](https://github.com/markheydon/github-workflows) with a visual interface.

The system is built around two modes of operation:

- **PM Mode** (weekly or fortnightly) — Active curation: review your backlog across all repositories, resolve stalled work, and populate your project board with a realistic set of committed items for the next few days.
- **Work Mode** (daily) — Execution: the project board is the single pane of glass. Open it, pick the next item, and get things done. The optional Daily Focus view will provide a quick morning nudge on what is most urgent today.

### What is available now

| Tab | Route | Status |
|-----|-------|--------|
| **Repos** | `/pm-workflow/repos` | **Available** — planning board selection, thresholds, and repository exclusions. |
| Daily Focus | `/pm-workflow/daily-focus` | Placeholder — story [#273](https://github.com/markheydon/solo-dev-board/issues/273). |
| Backlog | `/pm-workflow/backlog` | Placeholder — story [#277](https://github.com/markheydon/solo-dev-board/issues/277). |
| Planning | `/pm-workflow/planning` | Placeholder — story [#283](https://github.com/markheydon/solo-dev-board/issues/283). |

Open **PM Workflow** from the navigation drawer. The hub redirects to **Repos** until Daily Focus ships.

---

## Repo Management (available)

Repo Management controls which repositories and which Projects v2 board participate in PM operations. Settings persist in your browser (see [Configuration](#configuration)).

### Accessing

1. Open the navigation drawer.
2. Select **PM Workflow**.
3. You land on the **Repos** tab (`/pm-workflow/repos`).

Shared chrome on every PM Workflow tab includes:

- **Planning board** selector — choose a Projects v2 board discovered from your active repositories (same discovery model as Triage and Board Rules).
- **Status line** — `Repos: N included`, selected board title (when chosen), last refreshed time, and a **Refresh** control to reload board options and the repository catalogue.
- **Tab strip** — Daily Focus, Backlog, Planning, and Repos (only Repos is functional today).

If GitHub reports linked project boards that cannot be read with your current sign-in (common for private user-owned boards under GitHub App sign-in), a warning appears at the top of the page. See [plan/GITHUB_PROJECTS_V2_ACCESS.md](https://github.com/markheydon/solo-dev-board/blob/main/plan/GITHUB_PROJECTS_V2_ACCESS.md) in the repository.

### Select a planning board

1. Open the **Planning board** dropdown in the shared chrome.
2. Choose a board title from the list (boards are discovered across active, non-archived repositories).
3. The selection is saved automatically.

Until a board is selected, an informational alert explains that future tabs need a board. You can still edit Repo Management settings below.

Board occupancy on the selected board will count **every** linked card when Daily Focus ships. Recommendations, backlog queries, and planning candidates will honour repository exclusions.

### Set planning thresholds

Under **Planning thresholds**:

| Field | Default | Purpose |
|-------|---------|---------|
| **Capacity limit** | 8 | Maximum combined Up Next + In Progress items for planning warnings (used on the Planning tab when shipped). |
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

> **Scope note** — Open issue and PR counts plus last-activity columns for each repository are tracked on story [#288](https://github.com/markheydon/solo-dev-board/issues/288) and are not shown yet. The included repositories table lists participation only today.

---

## Key Capabilities (planned)

The following sections describe the full v1.1.0 feature set. They are **not** available in the app yet.

### Daily Focus

A quick morning summary that answers "what should I work on right now?":

- Project board state: item count per status column and current active load.
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
