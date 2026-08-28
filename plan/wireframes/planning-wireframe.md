# Planning Wireframe

## Purpose

Planning is a four-page area inside SoloDevBoard. It replaces the prompt-and-script operating system from [markheydon/github-workflows](https://github.com/markheydon/github-workflows) with a MudBlazor UI: a morning **Daily Focus**, a cross-repository **Backlog Review**, a guided **Iteration** session that curates **Up Next**, and **Repo Management** for which repositories and which Projects v2 board participate.

This wireframe is the planning baseline for feature [#272](https://github.com/markheydon/solo-dev-board/issues/272) (stories #273–#288 plus enablers and tests).

## User Goals

- See board occupancy and what to work on today without opening GitHub.
- Review open work across repositories grouped by urgency.
- Curate a realistic **Up Next** batch without over-committing.
- Exclude personal or irrelevant repositories from PM operations.
- Operate entirely with keyboard and MudBlazor primitives.

## Information architecture

Drawer entry: **Planning** (`Icons.Material.Filled.ViewWeek`) linking to `/planning`.

The hub is a `MudTabs` shell (or equivalent `MudNavLink` sub-nav) with four routes:

| Tab | Route | Primary stories |
|-----|-------|-----------------|
| Daily Focus | `/planning/daily-focus` | #273–#276 |
| Backlog | `/planning/backlog` | #277–#282 |
| Iteration | `/planning/iteration` | #283–#286 |
| Repos | `/planning/repos` | #287–#288 |

Shared chrome on every tab:

- Planning board selector (`MudSelect`) — required before data loads.
- Short status line: selected board title, repository count after exclusions, last refreshed time, refresh button.
- Inaccessible private Projects v2 warning (reuse Triage/Board Rules copy; see `plan/GITHUB_PROJECTS_V2_ACCESS.md` and #293).

## Shared chrome

```
+------------------------------------------------------------------+
| Planning                                                      |
+------------------------------------------------------------------+
| Board: [ SoloDevBoard Roadmap v ]   Repos: 12 included   [Refresh]
| [Daily Focus] [Backlog] [Iteration] [Repos]                       |
|------------------------------------------------------------------|
| (tab content)                                                    |
+------------------------------------------------------------------+
```

## Daily Focus (`/planning/daily-focus`)

```
+------------------------------------------------------------------+
| Board state                                                      |
| [Todo 14] [Up Next 4] [In Progress 2] [Blocked 1] [Ice Box 6]    |
| [Done 3]   Active load: 6 / 8 (Up Next + In Progress)            |
|------------------------------------------------------------------|
| Stalled                                                          |
| Up Next 3+ days:  #275 title  (4d)  [Open]                       |
| PRs awaiting review 3+ days:  owner/repo#12  (5d)  [Open]        |
|------------------------------------------------------------------|
| Recommended today (top 3 unblocked)                              |
| 1. [priority/high] owner/repo#40  Title…                         |
| 2. [priority/medium] owner/repo#41 Title…                        |
| 3. [priority/medium] owner/repo#42 Title…                        |
+------------------------------------------------------------------+
```

### Interaction notes

- Column chips are read-only counts for Status options discovered on the selected board (do not hard-code Project #8 option ids).
- **Active load** is Up Next + In Progress item count; the denominator is the configurable capacity from Repo Management (default 8).
- Stalled **Up Next** uses time in that Status (prefer Status-changed-at; fall back to item updated-at with a footnote).
- Stalled PRs: if the board has an **In Review** (or equivalent) Status, use time in that column. Otherwise use open, non-draft PRs with pending review (`reviewDecision` pending or requested reviewers) aged three or more days.
- Top three: unblocked (`status/blocked` absent, board Status not Blocked/Ice Box), ranked `priority/critical` > `high` > `medium` > `low` > unlabelled, then recency. Exclude items already In Progress.
- Empty, loading, and GitHub error states use `MudAlert` plus retry, matching Audit Dashboard.

## Backlog Review (`/planning/backlog`)

```
+------------------------------------------------------------------+
| Filter: [All types v] [All repos v]   Search [            ]      |
|------------------------------------------------------------------|
| Urgent (priority/high and critical)                              |
|  [Issue] owner/repo#10 Title  labels…                            |
|  [PR]    owner/repo#11 Title                                     |
|------------------------------------------------------------------|
| Ready to start                                                   |
|  unblocked stories/bugs not on Up Next / In Progress             |
|------------------------------------------------------------------|
| Awaiting triage (missing type/ or priority/)                     |
|------------------------------------------------------------------|
| Blocked / deferred (status/blocked, status/ice-box, board park)  |
|------------------------------------------------------------------|
| Epics near completion (open parent, all children closed)         |
|------------------------------------------------------------------|
| Neglected repositories (no issue/PR activity in 14 days)         |
+------------------------------------------------------------------+
```

### Interaction notes

- Groups are `MudExpansionPanel` sections with counts in the header.
- Issue versus pull request is a `MudChip` on every row (#280). Do not split into separate pages.
- **Urgent** is the `priority/high` and `priority/critical` surface (#282). It is the first panel, not a separate route.
- **Core labels** for #278 are `type/` and `priority/` (the same pair AGENTS.md requires on new issues). Items missing either are awaiting triage.
- Rows are read-only with an external GitHub link. Adding to Up Next happens on the Planning tab.
- Neglected repos list repository full name, last issue/PR activity date (or never), and open counts.

## Iteration Planning (`/planning/iteration`)

```
+------------------------------------------------------------------+
| Capacity  [========    ]  6 / 8   Meter only (amber at limit)    |
|------------------------------------------------------------------|
| [error] You cannot add to Up Next until stalled items are        |
| handled (2 stalled). Next: Re-commit, Mark Blocked, Ice Box,     |
| Remove. Do not mention capacity here.              (#445)        |
|  #275  4d  [Re-commit] [Mark Blocked] [Ice Box] [Remove]         |
|------------------------------------------------------------------|
| Candidate picker                                                 |
|  Add is paused until stalled Up Next is cleared.                 |
|  Search across included repos   [ ] issues  [ ] PRs              |
|  [ ] owner/repo#50  Title   [Add to Up Next] (disabled + tooltip)|
|------------------------------------------------------------------|
| This batch (Up Next)                                             |
|  1. #40  [Focus Order]   2. #41                                  |
|  Capacity 8/8 is status, not a lock.                             |
|  [Assign milestone to selected: v1.1 v] [Apply]                |
+------------------------------------------------------------------+
```

### Interaction notes

- Adding new items is disabled (with explanation) while any stalled Up Next item remains unresolved (#285). **Re-commit** clears stall by recording a fresh Status touch (move away and back, or update Focus Order) so the three-day clock restarts; document the exact GraphQL approach in the enabler notes.
- **Mark Blocked** / **Ice Box** set board Status and apply `status/blocked` or `status/ice-box` on the issue (DEC-028). **Remove** returns the item to Todo and clears Focus Order.
- New Up Next items receive sequential Focus Order (stories, enablers, and tests only; skip Feature/Epic parents).
- Capacity warning is not a hard stop: the user can exceed the limit after confirming a `MudDialog`.
- Bulk milestone (#286) is optional and applies only to selected batch items that belong to repositories where that milestone exists; skip others with a snackbar summary.
- **Stall versus capacity (#445):** stall is the only hard disable for **Add to Up Next**. Use one error-severity alert (not warning) that names the stalled count and the stall-table actions. Do not put “capacity” on that alert. Capacity N/N stays a meter; inner Up Next copy must not say the user must resolve items before adding when the meter is full. Keep the candidate list visible with one short pause line; optional tooltip on disabled Add. The stall table remains the place to act.

## Repo Management (`/planning/repos`)

```
+------------------------------------------------------------------+
| Planning board  [ SoloDevBoard Roadmap v ]                       |
| Capacity limit  [ 8 ]     Stall days  [ 3 ]   Neglect days [ 14 ]|
|------------------------------------------------------------------|
| Excluded repositories                                            |
|  Search catalogue  [ ] owner/personal-site  [Exclude]            |
|  Currently excluded: owner/dotfiles  [Include]                   |
|------------------------------------------------------------------|
| Per-repository summary                                           |
|  Repo              Issues  PRs  Last activity  Included?         |
|  owner/solo-dev-board  12    2   2 days ago     Yes              |
+------------------------------------------------------------------+
```

### Interaction notes

- Settings persist in browser `localStorage` (DEC-029), same pattern as theme preference.
- Exclusion applies to Daily Focus recommendations, Backlog, Planning candidates, and the summary table. The planning **board** itself is independent of repo exclusion (a user-owned board can span excluded repos; those cards still appear on board-state counts).
- Archived GitHub repositories are omitted from the include list by default (reuse `GetActiveRepositoriesAsync`).
- Persist: selected board node id, capacity, stall days, neglect days, excluded `owner/name` list.

## State variants

- **No board selected:** tabs show an instructional alert; data tables hidden.
- **Loading:** `MudProgressLinear` indeterminate at top of tab.
- **Partial failure:** some repos failed; show count and retry those.
- **Empty catalogue:** no active repositories after exclusion.
- **Hosted inaccessible boards:** warning plus PAT-mode guidance.

## Accessibility notes

- Tabs, selects, and tables use MudBlazor keyboard support; do not invent custom focus traps.
- Status counts and capacity are announced with `aria-live="polite"` on refresh.
- Colour is not the only stall signal; include day counts in text.
- Prefer MudBlazor layout primitives and utility `Class` attributes; no new `.razor.css` unless a documented gap remains.

## Responsive behaviour

- Below `md`, the tab strip scrolls horizontally; KPI chips wrap; tables become stacked cards (`MudHidden` / breakpoint classes).
- Capacity bar remains visible at the top of Planning on all widths.

## Out of scope for this wireframe

- Editing issue body content.
- Creating new GitHub Projects.
- Real-time multi-user collaboration.
- Making private user-owned Projects v2 work under hosted GitHub App sign-in (#293).
