---
name: github-project
description: 'Manage the SoloDevBoard Roadmap GitHub Project (Project #8). Add issues to the board, set Phase/Priority/Status/Date fields, manage the Up Next queue, and keep the project in sync with the issue lifecycle. Use this skill whenever creating or updating GitHub issues for SoloDevBoard.'
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
| Focus Order | `PVTF_lAHOAJefG84BQ6bhzg_Lx34` | Number |

---

## Option IDs

### Status Options

| Option | ID |
|--------|----|
| Todo | `f75ad846` |
| Up Next | `df9275ed` |
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

**Principle:** Dates are never estimated at planning time. The roadmap is a record of actuals enriched by size-derived forward estimates that are recalculated from the moment work actually starts — not from a speculative calendar.

| Lifecycle Event | Start Date | Target Date |
|-----------------|------------|-------------|
| Event 1: Issue Created | **Not set** | **Not set** |
| Event 2: Work Started (first story in hierarchy) | Today (actual) | Today + size estimate (see table below) |
| Event 2 cascade: Parent Feature/Epic (first start) | Today (inherited) | Latest sibling Target Date (recalculated from today) |
| Event 3: Issue Closed | Unchanged | Today (actual completion) |
| Event 3a: Cascade closure of Feature/Epic | Unchanged | Today (actual completion) |

### Size-to-Effort Calibration

`size/` labels express relative complexity, not calendar-day mandates. The calibration below is tuned to a solo developer who knows this codebase; adjust if your recent delivery pace differs.

| Size label | Estimated working days | Calendar days to add to Start Date |
|------------|------------------------|-------------------------------------|
| `size/xs` | 0.5 | 1 |
| `size/s` | 1 | 1 |
| `size/m` | 3 | 3 |
| `size/l` | 5 | 7 |
| `size/xl` | 10 | 14 |

**Target Date rule:** `Target Date = Start Date + calendar days` from the table above. For an `xs` or `s` item starting on a Monday, Target Date = Tuesday. For an `m` item starting Monday, Target Date = Thursday.

### Date Cascade on First Start

When the **first story/enabler/test** within a Feature is started, apply the full date cascade:

1. **Started issue** — Start Date = today; Target Date = today + size_estimate
2. **Each unstarted sibling** (in blocking/dependency order, ascending issue number as tiebreak):
   - Start Date = previous issue's Target Date + 1 day
   - Target Date = Start Date + size_estimate
3. **Parent Feature** — Start Date = today (if not already set); Target Date = latest sibling Target Date
4. **Parent Epic** — Start Date = today (if not already set); Target Date = latest Feature Target Date

Skip any sibling already "In Progress" or "Done" — do not alter its dates. If a second story in the same Feature starts independently (parallel work), apply Event 2 to that story only; the Feature already has dates set.

---

## Queue and Lifecycle Events

### Execution Queue Rules

- **Up Next** is a project-only planning state for the next short-horizon batch of stories, enablers, and tests.
- **Up Next** is not a GitHub issue label and must not be added to issues.
- **Focus Order** is used only on Story Board items that are currently in **Up Next**.
- Leave **Focus Order** blank for Features, Epics, and all non-queued items.

### Event 1a: Daily Queue Populated (PM Orchestrator responsibility, optional)

After the daily-start workflow recommends a short execution batch and the user explicitly asks for board updates, move the selected stories, enablers, or tests to **Up Next** and assign sequential **Focus Order** values.

```powershell
# Step 1: Find the project item ID for the issue.
$itemId = gh api graphql --field query='query { repository(owner: "markheydon", name: "solo-dev-board") { issue(number: '"$issueNumber"') { projectItems(first: 10) { nodes { id project { number } } } } } }' --jq '.data.repository.issue.projectItems.nodes[] | select(.project.number == 8) | .id'

# Step 2: Set Status → Up Next.
gh api graphql --field query="mutation { updateProjectV2ItemFieldValue(input: { projectId: `"PVT_kwHOAJefG84BQ6bh`" itemId: `"$itemId`" fieldId: `"PVTSSF_lAHOAJefG84BQ6bhzg-5WGY`" value: { singleSelectOptionId: `"df9275ed`" } }) { projectV2Item { id } } }" | Out-Null

# Step 3: Set Focus Order to the execution sequence number.
gh api graphql --field query="mutation { updateProjectV2ItemFieldValue(input: { projectId: `"PVT_kwHOAJefG84BQ6bh`" itemId: `"$itemId`" fieldId: `"PVTF_lAHOAJefG84BQ6bhzg_Lx34`" value: { number: $focusOrder } }) { projectV2Item { id } } }" | Out-Null
```

### Event 1: Issue Created (PM Orchestrator responsibility)

After creating a new issue, add it to the project and set Status, Phase, and Priority. **Do not set Start Date or Target Date** — dates are calculated and set only when work actually begins (Event 2).

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

# Step 6: Assign the issue to markheydon
gh issue edit $issueNumber --repo markheydon/solo-dev-board --add-assignee markheydon

# NOTE: Start Date and Target Date are intentionally left blank at this stage.
# They are set when work begins (Event 2), calculated from the actual start date
# and the issue's size label per the Size-to-Effort Calibration table above.
```

---

### Event 2: Implementation Started (Delivery Agent responsibility)

When beginning work on an issue, set Status to "In Progress", set Start Date to today, and set Target Date using the size calibration table above. This transition can start from **Todo** or **Up Next**.

```powershell
# Step 1: Find the project item ID for the issue
$itemId = gh api graphql --field query='query { repository(owner: "markheydon", name: "solo-dev-board") { issue(number: '"$issueNumber"') { projectItems(first: 10) { nodes { id project { number } } } } } }' --jq '.data.repository.issue.projectItems.nodes[] | select(.project.number == 8) | .id'

# Step 2: Update Status → In Progress
gh api graphql --field query="mutation { updateProjectV2ItemFieldValue(input: { projectId: `"PVT_kwHOAJefG84BQ6bh`" itemId: `"$itemId`" fieldId: `"PVTSSF_lAHOAJefG84BQ6bhzg-5WGY`" value: { singleSelectOptionId: `"47fc9ee4`" } }) { projectV2Item { id } } }" | Out-Null

# Optional: Clear Focus Order once work starts so the queue only shows unstarted items.
gh project item-edit --id "$itemId" --project-id "PVT_kwHOAJefG84BQ6bh" --field-id "PVTF_lAHOAJefG84BQ6bhzg_Lx34" --clear

# Step 3: Set Start Date = today
$today = (Get-Date -Format 'yyyy-MM-dd')
gh api graphql --field query="mutation { updateProjectV2ItemFieldValue(input: { projectId: `"PVT_kwHOAJefG84BQ6bh`" itemId: `"$itemId`" fieldId: `"PVTF_lAHOAJefG84BQ6bhzg-5WQE`" value: { date: `"$today`" } }) { projectV2Item { id } } }" | Out-Null

# Step 4: Set Target Date = today + calendar days from size calibration table
#   xs → +1 day, s → +1 day, m → +3 days, l → +7 days, xl → +14 days
$sizeLabel = gh api "repos/markheydon/solo-dev-board/issues/$issueNumber" --jq '[.labels[].name | select(startswith("size/"))] | first'
$calendarDays = switch ($sizeLabel) { "size/xs" { 1 } "size/s" { 1 } "size/m" { 3 } "size/l" { 7 } "size/xl" { 14 } default { 3 } }
$targetDate = (Get-Date).AddDays($calendarDays).ToString('yyyy-MM-dd')
gh api graphql --field query="mutation { updateProjectV2ItemFieldValue(input: { projectId: `"PVT_kwHOAJefG84BQ6bh`" itemId: `"$itemId`" fieldId: `"PVTF_lAHOAJefG84BQ6bhzg-5WQw`" value: { date: `"$targetDate`" } }) { projectV2Item { id } } }" | Out-Null
```

Also update the issue label:

```powershell
gh issue edit $issueNumber --repo markheydon/solo-dev-board --remove-label "status/todo" --add-label "status/in-progress"
```

**If this is the first story started within its Feature**, also apply the sibling and parent date cascade (see "Date Cascade on First Start" above). For each unstarted sibling, calculate its Start Date as the previous issue's Target Date + 1 day, then its Target Date from its own size label. Then apply Event 2a to set the parent Feature and Epic Start Date = today and Target Date = latest sibling Target Date.

---

### Event 2a: Cascade "In Progress" to Parent Feature and Epic (Delivery Agent responsibility)

When starting work on a Story, Enabler, or Test, check whether the parent Feature and Epic are still "Todo" on the project board. If so:
1. Move them to "In Progress" and set their Start Date = today
2. Set their Target Date = the latest Target Date among all child issues (after applying Event 2 size estimates to unstarted siblings)

This is a **one-time transition** — once a parent is "In Progress" it remains so until all children are done and it is closed. This rule exists because Features and Epics have no direct implementation start — they transition when the **first child issue** begins work.

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

**Apply this for both the immediate parent Feature and the grandparent Epic.** In practice for SoloDevBoard, the hierarchy is always Epic → Feature → Story/Enabler/Test, so at most two cascade checks are needed per delivery start.

---

### Event 3: Issue Closed (Review Agent responsibility, post-merge)

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

### Event 3a: Cascade "Done" to Parent Feature and Epic (Review Agent responsibility, post-merge)

After closing a Story, Enabler, or Test via Event 3, check whether **all sibling issues** under the same parent Feature are now closed. If so, apply Event 3 to the Feature (close it, `status/done`, board Status→Done, Target Date→today). Then repeat: if all Features under the parent Epic are also closed, apply Event 3 to the Epic too.

**How to check:** In the GitHub UI, open the parent Feature issue and inspect the Sub-issues widget — if all sub-issues are marked closed, the cascade applies. Repeat for the Epic.

```powershell
# For the parent Feature ($featureIssueNumber) — run after each child closure:
$featureItemId = gh api graphql --field query='query { repository(owner: "markheydon", name: "solo-dev-board") { issue(number: '$featureIssueNumber') { projectItems(first: 10) { nodes { id project { number } } } } } }' --jq '.data.repository.issue.projectItems.nodes[] | select(.project.number == 8) | .id'

# Close the Feature issue (only if ALL children are closed):
gh issue edit $featureIssueNumber --repo markheydon/solo-dev-board --remove-label "status/in-progress" --add-label "status/done"
gh issue close $featureIssueNumber --repo markheydon/solo-dev-board --comment "All child issues are complete. Closing Feature as done."

# Update project board — Status → Done and Target Date → today
gh api graphql --field query="mutation { updateProjectV2ItemFieldValue(input: { projectId: `"PVT_kwHOAJefG84BQ6bh`" itemId: `"$featureItemId`" fieldId: `"PVTSSF_lAHOAJefG84BQ6bhzg-5WGY`" value: { singleSelectOptionId: `"98236657`" } }) { projectV2Item { id } } }" | Out-Null
$actualEndDate = (Get-Date -Format 'yyyy-MM-dd')
gh api graphql --field query="mutation { updateProjectV2ItemFieldValue(input: { projectId: `"PVT_kwHOAJefG84BQ6bh`" itemId: `"$featureItemId`" fieldId: `"PVTF_lAHOAJefG84BQ6bhzg-5WQw`" value: { date: `"$actualEndDate`" } }) { projectV2Item { id } } }" | Out-Null

# Repeat identically for the parent Epic ($epicIssueNumber) if all its Features are now closed.
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
- Use **Up Next** only when the user explicitly wants a visible short-horizon execution queue.
- Use **Focus Order** only on Story Board items in **Up Next**.
- **Never** set Status to "Done" before the PR is merged to `main`.
- The **Linked pull requests** field updates automatically when a PR is created referencing the issue — no manual action needed.
- The **Milestone** and **Labels** fields sync automatically from the issue — no manual action needed.
- If an issue is split into sub-issues, add all sub-issues to the project as well.
- The **Sub-issues progress** field updates automatically from GitHub's sub-issue tracking.
