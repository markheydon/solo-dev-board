---
name: github-project
description: 'Manage the SoloDevBoard Roadmap GitHub Project (Project #8). Add issues to the board, set Phase/Priority/Status/Date fields, manage the Up Next queue, and keep the project in sync with the issue lifecycle. Use this skill whenever creating or updating GitHub issues for SoloDevBoard.'
---

# GitHub Project Board — SoloDevBoard Roadmap

Centralised reference and command patterns for maintaining the **SoloDevBoard Roadmap** GitHub Projects v2 board in sync with GitHub issues throughout the planning, delivery, and review lifecycle.

## Tool and Shell Preference

- Use GitHub MCP tools for issue and pull request operations when the capability exists there.
- Use `gh project` for SoloDevBoard Roadmap item operations and field updates.
- Prefer `gh project item-edit` over raw GraphQL mutations when the CLI supports the field update directly.
- Default to bash-safe command patterns in WSL or Linux terminals.
- Do not use PowerShell backtick escaping, `Get-Date`, `Out-Null`, or `ConvertFrom-Json` in bash sessions.

---

## Project Reference

| Property | Value |
|----------|-------|
| **Project name** | SoloDevBoard Roadmap |
| **Project number** | `8` |
| **Project ID** | `PVT_kwHOAJefG84BQ6bh` |
| **Owner** | `markheydon` |
| **URL** | https://github.com/users/markheydon/projects/8 |

### Phase Model Note

As of 2026-03-17, the live GitHub Project board and the repository planning artefacts are aligned on a six-phase roadmap. `v0.5.0` maps to **Phase 5 — Cross-Repo PM Workflow** and `v1.0.0` maps to **Phase 6 — Polish and v1.0**.

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
| Phase 5 — Cross-Repo PM Workflow | `495afaf1` | v0.5.0 |
| Phase 6 — Polish and v1.0 | `dfa36cee` | v1.0.0 |

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
| `v0.5.0` | `area/dashboard` | Phase 5 — Cross-Repo PM Workflow |
| `v1.0.0` | any | Phase 6 — Polish and v1.0 |

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

```bash
# Step 1: Find the project item ID for the issue.
item_id=$(gh project item-list 8 --owner markheydon --format json --jq ".items[] | select(.content.number == $issueNumber) | .id" | head -n1)

# Step 2: Set Status → Up Next.
gh project item-edit \
    --id "$item_id" \
    --project-id "PVT_kwHOAJefG84BQ6bh" \
    --field-id "PVTSSF_lAHOAJefG84BQ6bhzg-5WGY" \
    --single-select-option-id "df9275ed"

# Step 3: Set Focus Order to the execution sequence number.
gh project item-edit \
    --id "$item_id" \
    --project-id "PVT_kwHOAJefG84BQ6bh" \
    --field-id "PVTF_lAHOAJefG84BQ6bhzg_Lx34" \
    --number "$focusOrder"
```

### Event 1: Issue Created (PM Orchestrator responsibility)

After creating a new issue, add it to the project and set Status, Phase, and Priority. **Do not set Start Date or Target Date** — dates are calculated and set only when work actually begins (Event 2).

```bash
# Step 1: Add the issue to the project.
issue_url="https://github.com/markheydon/solo-dev-board/issues/$issueNumber"
gh project item-add 8 --owner markheydon --url "$issue_url" >/dev/null

# Step 2: Find the new project item ID.
item_id=$(gh project item-list 8 --owner markheydon --format json --jq ".items[] | select(.content.number == $issueNumber) | .id" | head -n1)

# Step 3: Set Status → Todo.
gh project item-edit \
    --id "$item_id" \
    --project-id "PVT_kwHOAJefG84BQ6bh" \
    --field-id "PVTSSF_lAHOAJefG84BQ6bhzg-5WGY" \
    --single-select-option-id "f75ad846"

# Step 4: Set Phase. Replace "$phase_option_id" with the value from the Phase Options table above.
gh project item-edit \
    --id "$item_id" \
    --project-id "PVT_kwHOAJefG84BQ6bh" \
    --field-id "PVTSSF_lAHOAJefG84BQ6bhzg-5WLw" \
    --single-select-option-id "$phase_option_id"

# Step 5: Set Priority. Replace "$priority_option_id" with the value from the Priority Options table above.
gh project item-edit \
    --id "$item_id" \
    --project-id "PVT_kwHOAJefG84BQ6bh" \
    --field-id "PVTSSF_lAHOAJefG84BQ6bhzg-5WMc" \
    --single-select-option-id "$priority_option_id"

# Step 6: Assign the issue to markheydon.
gh issue edit "$issueNumber" --repo markheydon/solo-dev-board --add-assignee markheydon

# NOTE: Start Date and Target Date are intentionally left blank at this stage.
# They are set when work begins (Event 2), calculated from the actual start date
# and the issue's size label per the Size-to-Effort Calibration table above.
```

---

### Event 2: Implementation Started (Delivery Agent responsibility)

When beginning work on an issue, set Status to "In Progress", set Start Date to today, and set Target Date using the size calibration table above. This transition can start from **Todo** or **Up Next**.

```bash
# Step 1: Find the project item ID for the issue.
item_id=$(gh project item-list 8 --owner markheydon --format json --jq ".items[] | select(.content.number == $issueNumber) | .id" | head -n1)

# Step 2: Update Status → In Progress.
gh project item-edit \
    --id "$item_id" \
    --project-id "PVT_kwHOAJefG84BQ6bh" \
    --field-id "PVTSSF_lAHOAJefG84BQ6bhzg-5WGY" \
    --single-select-option-id "47fc9ee4"

# Optional: Clear Focus Order once work starts so the queue only shows unstarted items.
gh project item-edit --id "$item_id" --project-id "PVT_kwHOAJefG84BQ6bh" --field-id "PVTF_lAHOAJefG84BQ6bhzg_Lx34" --clear

# Step 3: Set Start Date = today.
today=$(date +%F)
gh project item-edit \
    --id "$item_id" \
    --project-id "PVT_kwHOAJefG84BQ6bh" \
    --field-id "PVTF_lAHOAJefG84BQ6bhzg-5WQE" \
    --date "$today"

# Step 4: Set Target Date = today + calendar days from size calibration table.
#   xs → +1 day, s → +1 day, m → +3 days, l → +7 days, xl → +14 days.
size_label=$(gh api "repos/markheydon/solo-dev-board/issues/$issueNumber" --jq '[.labels[].name | select(startswith("size/"))] | first')
case "$size_label" in
    "size/xs"|"size/s") calendar_days=1 ;;
    "size/m") calendar_days=3 ;;
    "size/l") calendar_days=7 ;;
    "size/xl") calendar_days=14 ;;
    *) calendar_days=3 ;;
esac
target_date=$(date -d "+$calendar_days day" +%F)
gh project item-edit \
    --id "$item_id" \
    --project-id "PVT_kwHOAJefG84BQ6bh" \
    --field-id "PVTF_lAHOAJefG84BQ6bhzg-5WQw" \
    --date "$target_date"
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

```bash
# For each parent issue number ($parent_issue_number = Feature or Epic issue number):

# Step 1: Find the parent project item ID if the parent is still in Todo.
parent_item_id=$(gh project item-list 8 --owner markheydon --format json --jq ".items[] | select(.content.number == $parent_issue_number and .status == \"Todo\") | .id" | head -n1)

# Step 2: If the parent is still Todo, update Status → In Progress and Start Date → today.
if [ -n "$parent_item_id" ]; then
	actual_start_date=$(date +%F)
	gh project item-edit --id "$parent_item_id" --project-id "PVT_kwHOAJefG84BQ6bh" --field-id "PVTSSF_lAHOAJefG84BQ6bhzg-5WGY" --single-select-option-id "47fc9ee4"
	gh project item-edit --id "$parent_item_id" --project-id "PVT_kwHOAJefG84BQ6bh" --field-id "PVTF_lAHOAJefG84BQ6bhzg-5WQE" --date "$actual_start_date"
	gh issue edit "$parent_issue_number" --repo markheydon/solo-dev-board --remove-label "status/todo" --add-label "status/in-progress"
fi
```

**Apply this for both the immediate parent Feature and the grandparent Epic.** In practice for SoloDevBoard, the hierarchy is always Epic → Feature → Story/Enabler/Test, so at most two cascade checks are needed per delivery start.

---

### Event 3: Issue Closed (Review Agent responsibility, post-merge)

When a PR is merged and the issue is closed, update Status to "Done" and **overwrite Target Date with today's actual completion date**. This replaces the planned estimate set at Event 1, giving a true record of when the work finished.

```bash
# Step 1: Find the project item ID for the issue.
item_id=$(gh project item-list 8 --owner markheydon --format json --jq ".items[] | select(.content.number == $issueNumber) | .id" | head -n1)

# Step 2: Update Status → Done.
gh project item-edit \
    --id "$item_id" \
    --project-id "PVT_kwHOAJefG84BQ6bh" \
    --field-id "PVTSSF_lAHOAJefG84BQ6bhzg-5WGY" \
    --single-select-option-id "98236657"

# Step 3: Overwrite Target Date with today's actual completion date.
actual_end_date=$(date +%F)
gh project item-edit \
    --id "$item_id" \
    --project-id "PVT_kwHOAJefG84BQ6bh" \
    --field-id "PVTF_lAHOAJefG84BQ6bhzg-5WQw" \
    --date "$actual_end_date"
```

---

### Event 3a: Cascade "Done" to Parent Feature and Epic (Review Agent responsibility, post-merge)

After closing a Story, Enabler, or Test via Event 3, check whether **all sibling issues** under the same parent Feature are now closed. If so, apply Event 3 to the Feature (close it, `status/done`, board Status→Done, Target Date→today). Then repeat: if all Features under the parent Epic are also closed, apply Event 3 to the Epic too.

**How to check:** In the GitHub UI, open the parent Feature issue and inspect the Sub-issues widget — if all sub-issues are marked closed, the cascade applies. Repeat for the Epic.

```bash
# For the parent Feature ($feature_issue_number) — run after each child closure.
feature_item_id=$(gh project item-list 8 --owner markheydon --format json --jq ".items[] | select(.content.number == $feature_issue_number) | .id" | head -n1)

# Close the Feature issue only if all children are closed.
gh issue edit "$feature_issue_number" --repo markheydon/solo-dev-board --remove-label "status/in-progress" --add-label "status/done"
gh issue close "$feature_issue_number" --repo markheydon/solo-dev-board --comment "All child issues are complete. Closing Feature as done."

# Update project board — Status → Done and Target Date → today.
gh project item-edit --id "$feature_item_id" --project-id "PVT_kwHOAJefG84BQ6bh" --field-id "PVTSSF_lAHOAJefG84BQ6bhzg-5WGY" --single-select-option-id "98236657"
actual_end_date=$(date +%F)
gh project item-edit --id "$feature_item_id" --project-id "PVT_kwHOAJefG84BQ6bh" --field-id "PVTF_lAHOAJefG84BQ6bhzg-5WQw" --date "$actual_end_date"

# Repeat identically for the parent Epic ($epic_issue_number) if all its Features are now closed.
```

---

## Checking Project State

List all items and their current state:

```bash
gh project item-list 8 --owner markheydon --format json --jq '.items[] | {id, title: .title, number: .content.number, status}'
```

View the roadmap in the browser:

```bash
gh project view 8 --owner markheydon --web
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
- In WSL or Linux terminals, prefer the bash patterns in this file over PowerShell syntax.
- If a project update can be expressed with `gh project item-edit`, do that before reaching for raw GraphQL.
- **Never** set Status to "Done" before the PR is merged to `main`.
- The **Linked pull requests** field updates automatically when a PR is created referencing the issue — no manual action needed.
- The **Milestone** and **Labels** fields sync automatically from the issue — no manual action needed.
- If an issue is split into sub-issues, add all sub-issues to the project as well.
- The **Sub-issues progress** field updates automatically from GitHub's sub-issue tracking.
