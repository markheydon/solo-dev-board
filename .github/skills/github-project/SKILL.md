---
name: github-project
description: 'Manage the SoloDevBoard Roadmap GitHub Project (Project #8). Add issues to the board, set Phase/Priority/Status/Date fields, and keep the project in sync with the issue lifecycle. Use this skill whenever creating or updating GitHub issues for SoloDevBoard.'
---

# GitHub Project Board — SoloDevBoard Roadmap

Centralised reference and command patterns for maintaining the **SoloDevBoard Roadmap** GitHub Projects v2 board in sync with GitHub issues throughout the planning, delivery, and review lifecycle.

---

## Project Reference

| Property | Value |
|----------|-------|
| **Project name** | SoloDevBoard Roadmap |
| **Project number** | `8` |
| **Project ID** | `PVT_kwHOAJefG84BQ6bh` |
| **Owner** | `markheydon` |
| **URL** | https://github.com/users/markheydon/projects/8 |

---

## Field IDs

| Field | ID | Type |
|-------|----|------|
| Title | `PVTF_lAHOAJefG84BQ6bhzg-5WGQ` | Text |
| Assignees | `PVTF_lAHOAJefG84BQ6bhzg-5WGU` | Assignees |
| Status | `PVTSSF_lAHOAJefG84BQ6bhzg-5WGY` | Single select |
| Labels | `PVTF_lAHOAJefG84BQ6bhzg-5WGc` | Labels |
| Linked pull requests | `PVTF_lAHOAJefG84BQ6bhzg-5WGg` | Pull requests |
| Milestone | `PVTF_lAHOAJefG84BQ6bhzg-5WGk` | Milestone |
| Repository | `PVTF_lAHOAJefG84BQ6bhzg-5WGo` | Repository |
| Phase | `PVTSSF_lAHOAJefG84BQ6bhzg-5WLw` | Single select |
| Priority | `PVTSSF_lAHOAJefG84BQ6bhzg-5WMc` | Single select |
| Start Date | `PVTF_lAHOAJefG84BQ6bhzg-5WQE` | Date |
| Target Date | `PVTF_lAHOAJefG84BQ6bhzg-5WQw` | Date |

---

## Option IDs

### Status Options

| Option | ID |
|--------|----|
| Todo | `f75ad846` |
| In Progress | `47fc9ee4` |
| Done | `98236657` |

### Phase Options

| Phase | Option ID | Milestone |
|-------|-----------|-----------|
| Phase 1 — Foundation | `1fbac877` | v0.1.0 |
| Phase 2 — Label Manager + Audit | `0f90ba94` | v0.2.0 |
| Phase 3 — Migration + Triage | `f3de38ba` | v0.3.0 |
| Phase 4 — Board Rules + Workflows | `f5bc6726` | v0.4.0 |
| Phase 5 — Polish + Release | `dfa36cee` | v1.0.0 |

### Priority Options

| Priority | Option ID |
|----------|-----------|
| Critical | `8d63dbb3` |
| High | `e89555ab` |
| Medium | `90261711` |
| Low | `0f0afb94` |

---

## Phase Assignment Rules

Determine the correct **Phase** option from the issue's milestone or area label:

| Milestone assigned | Area labels | → Phase |
|--------------------|-------------|---------|
| `v0.1.0` | `area/infrastructure` | Phase 1 — Foundation |
| `v0.2.0` | `area/labels`, `area/dashboard` | Phase 2 — Label Manager + Audit |
| `v0.3.0` | `area/migration`, `area/triage` | Phase 3 — Migration + Triage |
| `v0.4.0` | `area/board-rules`, `area/workflows` | Phase 4 — Board Rules + Workflows |
| `v1.0.0` | any | Phase 5 — Polish + Release |

**Precedence:** Milestone > area label. When a milestone is assigned, use the milestone to determine the phase.

---

## Roadmap Date Guidelines

Dates evolve through the issue lifecycle from **planned estimates** to **actual dates**:

| Lifecycle Event | Start Date | Target Date |
|-----------------|------------|-------------|
| Event 1: Issue Created | Planned start (from Phase table below) | Planned end (from Phase table below) |
| Event 2: Implementation Started | **Overwritten with actual start date (today)** | Unchanged (still planned end) |
| Event 3: Issue Closed | Unchanged (actual start intact) | **Overwritten with actual completion date (today)** |

This means the roadmap chart naturally transitions from planned estimates into a true record of when work actually happened. The planned dates are not preserved once overwritten — they serve only as initial placeholders.

### Planned Phase Dates (used at Event 1)

When creating new issues, set Start Date and Target Date based on the issue's **role within the phase**, not a flat phase-wide date. This produces a realistic dependency-ordered Gantt chart from day one.

The ordering within any phase follows: **Epic/Feature → Enablers → Stories → Tests**

#### Phase 1 — Foundation (v0.1.0, March 2026)

| Issue type | Role | Planned Start | Planned Target |
|------------|------|--------------|----------------|
| Epic | Spans full phase | `2026-03-05` | `2026-03-31` |
| Feature | Spans most of phase | `2026-03-05` | `2026-03-28` |
| Enabler | First (unblocks stories) | `2026-03-06` | `2026-03-13` |
| Story (REST API) | After enabler | `2026-03-16` | `2026-03-20` |
| Story (Dashboard shell) | After enabler (parallel) | `2026-03-16` | `2026-03-24` |
| Story (Repositories page) | After REST API story | `2026-03-23` | `2026-03-27` |
| Test (Unit tests) | After enabler + REST story | `2026-03-19` | `2026-03-27` |
| Test (Component tests) | After UI stories | `2026-03-25` | `2026-03-31` |

#### Phase 2 — Label Manager + Audit (v0.2.0, April 2026)

| Issue type | Role | Planned Start | Planned Target |
|------------|------|--------------|----------------|
| Epic | Spans full phase | `2026-04-01` | `2026-04-30` |
| Feature | Spans most of phase | `2026-04-01` | `2026-04-25` |
| Enabler(s) | First (unblocks stories) | `2026-04-01` | `2026-04-07` |
| Stories | After enablers | `2026-04-08` | `2026-04-22` |
| Tests | After stories | `2026-04-18` | `2026-04-30` |

#### Phase 3 — Migration + Triage (v0.3.0, May 2026)

| Issue type | Role | Planned Start | Planned Target |
|------------|------|--------------|----------------|
| Epic | Spans full phase | `2026-05-01` | `2026-05-31` |
| Feature | Spans most of phase | `2026-05-01` | `2026-05-27` |
| Enabler(s) | First (unblocks stories) | `2026-05-01` | `2026-05-08` |
| Stories | After enablers | `2026-05-11` | `2026-05-25` |
| Tests | After stories | `2026-05-20` | `2026-05-31` |

#### Phase 4 — Board Rules + Workflows (v0.4.0, June 2026)

| Issue type | Role | Planned Start | Planned Target |
|------------|------|--------------|----------------|
| Epic | Spans full phase | `2026-06-01` | `2026-06-30` |
| Feature | Spans most of phase | `2026-06-01` | `2026-06-26` |
| Enabler(s) | First (unblocks stories) | `2026-06-01` | `2026-06-08` |
| Stories | After enablers | `2026-06-09` | `2026-06-23` |
| Tests | After stories | `2026-06-18` | `2026-06-30` |

#### Phase 5 — Polish + Release (v1.0.0, July 2026)

| Issue type | Role | Planned Start | Planned Target |
|------------|------|--------------|----------------|
| Epic | Spans full phase | `2026-07-01` | `2026-07-31` |
| Feature | Spans most of phase | `2026-07-01` | `2026-07-28` |
| Stories | As sequenced | `2026-07-01` | `2026-07-22` |
| Tests | After stories | `2026-07-18` | `2026-07-31` |

---

## Lifecycle Events

### Event 1: Issue Created (PM Orchestrator responsibility)

After creating a new issue, add it to the project and set all relevant fields. Dates set here are **planned estimates** from the Phase table above — they will be overwritten with actuals as the issue progresses through the lifecycle.

```powershell
# Step 1: Get the issue node ID
$nodeId = gh api "repos/markheydon/solo-dev-board/issues/$issueNumber" --jq '.node_id'

# Step 2: Add issue to project — capture the returned project item ID
$itemId = gh api graphql --field query="mutation { addProjectV2ItemById(input: { projectId: `"PVT_kwHOAJefG84BQ6bh`" contentId: `"$nodeId`" }) { item { id } } }" --jq '.data.addProjectV2ItemById.item.id'

# Step 3: Set Status → Todo
gh api graphql --field query="mutation { updateProjectV2ItemFieldValue(input: { projectId: `"PVT_kwHOAJefG84BQ6bh`" itemId: `"$itemId`" fieldId: `"PVTSSF_lAHOAJefG84BQ6bhzg-5WGY`" value: { singleSelectOptionId: `"f75ad846`" } }) { projectV2Item { id } } }" | Out-Null

# Step 4: Set Phase (replace $phaseOptionId with value from Phase Options table above)
gh api graphql --field query="mutation { updateProjectV2ItemFieldValue(input: { projectId: `"PVT_kwHOAJefG84BQ6bh`" itemId: `"$itemId`" fieldId: `"PVTSSF_lAHOAJefG84BQ6bhzg-5WLw`" value: { singleSelectOptionId: `"$phaseOptionId`" } }) { projectV2Item { id } } }" | Out-Null

# Step 5: Set Priority (replace $priorityOptionId with value from Priority Options table above)
gh api graphql --field query="mutation { updateProjectV2ItemFieldValue(input: { projectId: `"PVT_kwHOAJefG84BQ6bh`" itemId: `"$itemId`" fieldId: `"PVTSSF_lAHOAJefG84BQ6bhzg-5WMc`" value: { singleSelectOptionId: `"$priorityOptionId`" } }) { projectV2Item { id } } }" | Out-Null

# Step 6: Set planned Start Date (ISO 8601 from Phase table, e.g. "2026-03-05")
# Will be overwritten with actual start date when implementation begins (Event 2)
gh api graphql --field query="mutation { updateProjectV2ItemFieldValue(input: { projectId: `"PVT_kwHOAJefG84BQ6bh`" itemId: `"$itemId`" fieldId: `"PVTF_lAHOAJefG84BQ6bhzg-5WQE`" value: { date: `"$startDate`" } }) { projectV2Item { id } } }" | Out-Null

# Step 7: Set planned Target Date (ISO 8601 from Phase table, e.g. "2026-03-31")
# Will be overwritten with actual completion date when issue is closed (Event 3)
gh api graphql --field query="mutation { updateProjectV2ItemFieldValue(input: { projectId: `"PVT_kwHOAJefG84BQ6bh`" itemId: `"$itemId`" fieldId: `"PVTF_lAHOAJefG84BQ6bhzg-5WQw`" value: { date: `"$targetDate`" } }) { projectV2Item { id } } }" | Out-Null

# Step 8: Assign the issue to markheydon
gh issue edit $issueNumber --repo markheydon/solo-dev-board --add-assignee markheydon
```

---

### Event 2: Implementation Started (Delivery Agent responsibility)

When beginning implementation of an issue, update Status to "In Progress" and **overwrite Start Date with today's actual start date**. This replaces the planned estimate set at Event 1.

```powershell
# Step 1: Find the project item ID for the issue
$itemId = gh api graphql --field query='query { repository(owner: "markheydon", name: "solo-dev-board") { issue(number: '"$issueNumber"') { projectItems(first: 10) { nodes { id project { number } } } } } }' --jq '.data.repository.issue.projectItems.nodes[] | select(.project.number == 8) | .id'

# Step 2: Update Status → In Progress
gh api graphql --field query="mutation { updateProjectV2ItemFieldValue(input: { projectId: `"PVT_kwHOAJefG84BQ6bh`" itemId: `"$itemId`" fieldId: `"PVTSSF_lAHOAJefG84BQ6bhzg-5WGY`" value: { singleSelectOptionId: `"47fc9ee4`" } }) { projectV2Item { id } } }" | Out-Null

# Step 3: Overwrite Start Date with today's actual start date
$actualStartDate = (Get-Date -Format 'yyyy-MM-dd')
gh api graphql --field query="mutation { updateProjectV2ItemFieldValue(input: { projectId: `"PVT_kwHOAJefG84BQ6bh`" itemId: `"$itemId`" fieldId: `"PVTF_lAHOAJefG84BQ6bhzg-5WQE`" value: { date: `"$actualStartDate`" } }) { projectV2Item { id } } }" | Out-Null
```

Also update the issue label at the same time:

```powershell
gh issue edit $issueNumber --repo markheydon/solo-dev-board --remove-label "status/todo" --add-label "status/in-progress"
```

---

### Event 2a: Cascade "In Progress" to Parent Feature and Epic (Delivery Agent responsibility)

When starting work on a Story, Enabler, or Test, check whether the parent Feature and Epic are still "Todo" on the project board. If so, move them to "In Progress" and overwrite their Start Date. This is a **one-time transition** — once a parent is "In Progress" it remains so until all children are done and it is closed.

This rule exists because Features and Epics have no direct implementation start — they transition to "In Progress" when the **first child issue** begins work.

```powershell
# For each parent issue number ($parentIssueNumber = Feature or Epic issue number):

# Step 1: Check current project status — only proceed if still "Todo"
$parentItemId = gh api graphql --field query='query { repository(owner: "markheydon", name: "solo-dev-board") { issue(number: '$parentIssueNumber') { projectItems(first: 10) { nodes { id status: fieldValueByName(name: "Status") { ... on ProjectV2ItemFieldSingleSelectValue { name } } project { number } } } } } }' --jq '.data.repository.issue.projectItems.nodes[] | select(.project.number == 8) | select(.status.name == "Todo") | .id'

# Step 2: If $parentItemId is non-empty, update Status → In Progress and Start Date → today
if ($parentItemId) {
    $actualStartDate = (Get-Date -Format 'yyyy-MM-dd')
    gh api graphql --field query="mutation { updateProjectV2ItemFieldValue(input: { projectId: `"PVT_kwHOAJefG84BQ6bh`" itemId: `"$parentItemId`" fieldId: `"PVTSSF_lAHOAJefG84BQ6bhzg-5WGY`" value: { singleSelectOptionId: `"47fc9ee4`" } }) { projectV2Item { id } } }" | Out-Null
    gh api graphql --field query="mutation { updateProjectV2ItemFieldValue(input: { projectId: `"PVT_kwHOAJefG84BQ6bh`" itemId: `"$parentItemId`" fieldId: `"PVTF_lAHOAJefG84BQ6bhzg-5WQE`" value: { date: `"$actualStartDate`" } }) { projectV2Item { id } } }" | Out-Null
    # Also update the issue label
    gh issue edit $parentIssueNumber --repo markheydon/solo-dev-board --remove-label "status/todo" --add-label "status/in-progress"
}
```

**Apply this for both the immediate parent Feature and the grandparent Epic.** In practice for SoloDevBoard, the hierarchy is always Epic → Feature → Story/Enabler/Test, so at most two cascade checks are needed per delivery start. (Review Agent responsibility, post-merge)

When a PR is merged and the issue is closed, update Status to "Done" and **overwrite Target Date with today's actual completion date**. This replaces the planned estimate set at Event 1, giving a true record of when the work finished.

```powershell
# Step 1: Find the project item ID for the issue
$itemId = gh api graphql --field query='query { repository(owner: "markheydon", name: "solo-dev-board") { issue(number: '"$issueNumber"') { projectItems(first: 10) { nodes { id project { number } } } } } }' --jq '.data.repository.issue.projectItems.nodes[] | select(.project.number == 8) | .id'

# Step 2: Update Status → Done
gh api graphql --field query="mutation { updateProjectV2ItemFieldValue(input: { projectId: `"PVT_kwHOAJefG84BQ6bh`" itemId: `"$itemId`" fieldId: `"PVTSSF_lAHOAJefG84BQ6bhzg-5WGY`" value: { singleSelectOptionId: `"98236657`" } }) { projectV2Item { id } } }" | Out-Null

# Step 3: Overwrite Target Date with today's actual completion date
$actualEndDate = (Get-Date -Format 'yyyy-MM-dd')
gh api graphql --field query="mutation { updateProjectV2ItemFieldValue(input: { projectId: `"PVT_kwHOAJefG84BQ6bh`" itemId: `"$itemId`" fieldId: `"PVTF_lAHOAJefG84BQ6bhzg-5WQw`" value: { date: `"$actualEndDate`" } }) { projectV2Item { id } } }" | Out-Null
```

---

## Checking Project State

List all items and their current state:

```powershell
gh project item-list 8 --owner markheydon --format json | ConvertFrom-Json | Select-Object -ExpandProperty items | Select-Object id, title | Format-Table -AutoSize
```

View the roadmap in the browser:

```powershell
Start-Process "https://github.com/users/markheydon/projects/8"
```

---

## Priority Mapping

Map `priority/` labels to project Priority option IDs:

| Label | Option ID |
|-------|-----------|
| `priority/critical` | `8d63dbb3` |
| `priority/high` | `e89555ab` |
| `priority/medium` | `90261711` |
| `priority/low` | `0f0afb94` |

---

## Important Notes

- **Always** add new issues to the project board immediately after creation — never leave issues untracked.
- **Never** set Status to "Done" before the PR is merged to `main`.
- The **Linked pull requests** field updates automatically when a PR is created referencing the issue — no manual action needed.
- The **Milestone** and **Labels** fields sync automatically from the issue — no manual action needed.
- If an issue is split into sub-issues, add all sub-issues to the project as well.
- The **Sub-issues progress** field updates automatically from GitHub's sub-issue tracking.
