---
name: repo-github-project
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
- If direct project authentication is unavailable to the agent, rely on the `roadmap-sync.yml` GitHub Actions bridge by updating the underlying issue or pull request state and then letting the workflow reconcile the user-owned roadmap board.

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

As of 2026-08-18, the Project board **Phase** field is **legacy** for closed pre-1.0 milestones only. Roadmap Sync does not set Phase on `v1.1.0` or unmilestoned issues ([DEC-027](DECISIONS.md#dec-027-post-10-milestone-and-work-item-hierarchy)). Post-1.0 delivery uses **one open GitHub milestone** at a time (`v1.1.0`).

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

| Phase | Option ID | Milestone | Notes |
|-------|-----------|-----------|-------|
| Phase 1 — Foundation | `1fbac877` | v0.1.0 | Legacy — closed releases only |
| Phase 2 — Label Manager + Audit | `0f90ba94` | v0.2.0 | Legacy |
| Phase 3 — Migration + Triage | `f3de38ba` | v0.3.0 | Legacy |
| Phase 4 — Board Rules + Workflows | `f5bc6726` | v0.4.0 | Legacy |
| Phase 5 — Cross-Repo PM Workflow | `495afaf1` | v0.5.0 (historical) | Legacy |
| Phase 6 — Polish and v1.0 | `dfa36cee` | v1.0.0 | Legacy |

`v1.1.0` and unmilestoned issues: **do not set Phase** (Roadmap Sync leaves the field blank).

### Priority Options

| Priority | Option ID |
|----------|-----------|
| Critical | `8d63dbb3` |
| High | `e89555ab` |
| Medium | `90261711` |
| Low | `0f0afb94` |

---

## Phase Assignment Rules

**Legacy only.** Roadmap Sync sets Phase for closed pre-1.0 milestone titles (`v0.1.0`–`v0.5.0`, `v1.0.0`). It does **not** set Phase for `v1.1.0` or unmilestoned issues ([DEC-027](DECISIONS.md#dec-027-post-10-milestone-and-work-item-hierarchy)).

| Milestone assigned | → Phase (legacy) |
|--------------------|------------------|
| `v0.1.0` | Phase 1 — Foundation |
| `v0.2.0` | Phase 2 — Label Manager + Audit |
| `v0.3.0` | Phase 3 — Migration + Triage |
| `v0.4.0` | Phase 4 — Board Rules + Workflows |
| `v0.5.0` | Phase 5 — Cross-Repo PM Workflow |
| `v1.0.0` | Phase 6 — Polish and v1.0 |
| `v1.1.0` or none | **Leave Phase blank** |

---

## Roadmap Date Guidelines

**Principle:** Dates are never estimated at planning time. The roadmap is a record of actuals enriched by size-derived forward estimates that are recalculated from the moment work actually starts — not from a speculative calendar.

| Lifecycle Event | Start Date | Target Date |
|-----------------|------------|-------------|
| Event 1: Issue Created | **Not set** | **Not set** |
| Event 2: Work Started (issue being delivered now) | Today (actual) | Today + size estimate (see table below) |
| Event 2a: Parent Feature/Epic first child started | Today (inherited) | Latest dated child Target Date currently known |
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

### Date Discipline

Roadmap dates are intentionally conservative and should be easy to keep correct:

1. **Started issue** — Set Start Date = today and Target Date = today + size estimate.
2. **Parent Feature / Epic** — When the first child starts, set parent Start Date = today if blank and set Target Date = the latest dated child Target Date currently known.
3. **Unstarted siblings** — Leave Start Date and Target Date blank until that sibling actually starts. Do not auto-forecast untouched siblings as part of normal delivery.
4. **Done items** — Preserve Start Date and replace Target Date with the actual completion date when the issue closes.

This keeps the roadmap aligned with actual delivery signals and avoids speculative sibling forecasts drifting out of date.

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

When beginning work on an issue, apply `status/in-progress` to the issue. **Preferred path:** label only — the Roadmap Sync workflow moves the item to **In Progress** and sets Start Date and Target Date from the label event and `size/` label. Do not call `gh project` commands unless the user explicitly requests manual board repair.

```bash
gh issue edit "$issueNumber" --repo markheydon/solo-dev-board --remove-label "status/todo" --add-label "status/in-progress"
```

Skip if the issue already has `status/in-progress`. Escalate if the issue has `status/blocked`.

**Manual board path (fallback only):** If Roadmap Sync is unavailable and the user requests immediate board repair, use the `gh project item-edit` sequence from Event 2a patterns to set Status, Start Date, and Target Date on the implementing issue.

**Parent roll-up:** Roadmap Sync updates parent Feature and Epic board Status and dates when a child receives `status/in-progress`. Do not edit parent issue labels during normal delivery.

**Manual fallback (Event 2a):** If Roadmap Sync is unavailable and the user requests immediate parent repair, apply the sequence below for each parent Feature and Epic still in **Todo**.

---

### Event 2a: Cascade "In Progress" to Parent Feature and Epic (manual fallback)

When starting work on a Story, Enabler, or Test, and Roadmap Sync cannot run, check whether the parent Feature and Epic are still "Todo" on the project board. If so:

1. Move them to "In Progress" and set their Start Date = today.
2. Set their Target Date = the latest Target Date among child issues that already have dates.

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

### Event 3: Issue Closed (Verify Agent responsibility, post-merge)

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

### Event 3a: Cascade "Done" to Parent Feature and Epic (Verify Agent responsibility, post-merge)

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

### Event 4: Board Hygiene Audit (PM Orchestrator responsibility)

Run this during the PM progress review, after unusual manual board edits, or whenever the roadmap view looks wrong.

Audit for:

1. **In Progress / Done items missing Start Date** — backfill Start Date from the earliest verifiable implementation signal:
   - issue timeline status change, or
   - linked PR open date, or
   - first implementation commit date, or
   - issue close date as a conservative fallback.
2. **Done items missing Target Date** — set Target Date = actual issue close date.
3. **Impossible date pairs** — if `Start Date > Target Date`, correct Target Date to the actual close date for done items or recalculate from the issue's current size label for active items.
4. **Standalone PR cards** — remove pull requests accidentally added as roadmap items.
5. **Planned issues missing from the board** — add the issue, then apply the appropriate lifecycle state and dates.

Historical backfills should prefer accurate actuals, but a conservative same-day Start/Target pair is acceptable when the close date is the only reliable evidence left.

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

### GitHub Actions bridge

This repository also carries `.github/workflows/roadmap-sync.yml`, which exists specifically because user-owned Projects v2 boards cannot always be updated directly from every agent runtime. The bridge workflow runs on issue lifecycle changes, scheduled audits, and manual dispatch, then reconciles:

- missing roadmap items,
- Status / Phase / Priority field drift,
- Start Date / Target Date drift,
- parent Feature / Epic roll-up dates,
- stray standalone pull request cards, and
- archiving closed non-duplicate issues 14 days after `closed_at` (and unarchiving if reopened).

Prefer direct project updates when you have working credentials. Use the bridge as the reliability layer and fallback path.

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
- **Always** treat date hygiene as part of workflow completion. Do not consider delivery or review complete while the linked roadmap item still has missing or invalid dates that should already be populated.
- Use **Up Next** only when the user explicitly wants a visible short-horizon execution queue.
- Use **Focus Order** only on Story Board items in **Up Next**.
- In WSL or Linux terminals, prefer the bash patterns in this file over PowerShell syntax.
- If a project update can be expressed with `gh project item-edit`, do that before reaching for raw GraphQL.
- **Never** set Status to "Done" before the PR is merged to `main`.
- The **Linked pull requests** field updates automatically when a PR is created referencing the issue — no manual action needed.
- The **Milestone** and **Labels** fields sync automatically from the issue — no manual action needed.
- If an issue is split into sub-issues, add all sub-issues to the project as well.
- The **Sub-issues progress** field updates automatically from GitHub's sub-issue tracking.
- Leave untouched **Todo** siblings blank unless the user explicitly asks for a forecasting exercise; the normal workflow records actual starts, active forecasts, and actual finishes only.
