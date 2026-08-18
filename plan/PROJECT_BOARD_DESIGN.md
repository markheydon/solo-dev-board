# SoloDevBoard — Project Board Design

<!-- AI Collaborator Instructions: See the "AI Collaborator Instructions" section at the bottom of this file before making changes to the board structure. -->

This document defines the design of the SoloDevBoard GitHub Projects (v2) board, including columns, fields, automation rules, and the relationship between the board and the Board Rules Visualiser feature.

---

## Board Name

**SoloDevBoard Roadmap** (GitHub Projects v2, single board for the entire project)

---

## Column Structure

| Column | Purpose |
|--------|---------|
| **Todo** | Issues assigned to the current phase and ready to start, but not yet selected for the immediate execution batch. |
| **Up Next** | The next short-horizon batch of stories, enablers, and tests chosen for execution. |
| **In Progress** | Issues actively being worked on. |
| **Done** | Issues completed and closed. |

---

## Field Usage

| Field | Usage |
|-------|-------|
| **Status** | Core board state used by all main views. |
| **Phase** | Maps issues to the implementation phase via milestone. |
| **Priority** | Mirrors the issue's delivery priority. |
| **Focus Order** | Numeric sequence for the current **Up Next** batch on the Story Board only. |
| **Start Date / Target Date** | Blank while an item is still untouched in **Todo**; record the actual start plus the active forecast once work begins, then overwrite Target Date with the actual completion date when the item is done. |

Rules for **Focus Order**:
- Apply it only to stories, enablers, and tests.
- Apply it only when the item is currently in **Up Next**.
- Leave it blank for Features, Epics, and all non-queued items.
- Sort the Story Board by **Focus Order** ascending when working from the queue.

---

## Automation Rules

The following automation rules are configured on the board. These are documented here so that the **Board Rules Visualiser** feature can use them as a reference implementation.

**Operational note:** The board uses a repository-owned GitHub Actions bridge workflow (`.github/workflows/roadmap-sync.yml`) to apply and repair the roadmap metadata on the user-owned Project. This keeps the roadmap manageable even when a local agent runtime cannot mutate the Project directly.

### Issue Created
- **Trigger:** A new planned issue is created or explicitly added by an agent.
- **Action:** Add to **Todo** and set the required board metadata (`Phase`, `Priority`, and assignee).

### Daily Queue Selected
- **Trigger:** The user explicitly asks Copilot to populate today's working queue.
- **Action:** Move the selected stories, enablers, or tests to **Up Next** and set **Focus Order** in the recommended sequence.

### Pull Request Opened
- **Trigger:** A pull request linked to an issue is opened.
- **Action:** Keep the linked issue in its current execution state on the board; apply `status/in-review` label and rely on the linked pull request field for review visibility.
- **Board rule:** Do not add the pull request itself to the roadmap board as a standalone card.

### Issue Started
- **Trigger:** Delivery starts on a story, enabler, test, feature, or bug.
- **Action:** Move the issue to **In Progress**, set **Start Date** to the actual start date, and set **Target Date** using the size calibration in `.agents/skills/repo-github-project/SKILL.md`.
- **Board rule:** Leave untouched sibling items blank until they start; do not auto-forecast their dates during normal delivery.

### Pull Request Merged
- **Trigger:** A pull request is merged to `main`.
- **Action:** Move the linked issue to **Done**; close the issue; apply `status/done` label; overwrite **Target Date** with the actual completion date.
- **Board rule:** The merged pull request remains attached through the linked pull request field only; it is not kept as a separate roadmap item.

### Current Default Workflow Settings
- **Enabled:** `Auto-add sub-issues to project`, `Auto-close issue`, `Item added to project`, and `Item closed`.
- **Disabled:** `Auto-add to project`, `Pull request linked to issue`, `Pull request merged`, `Auto-archive items`, `Code changes requested`, `Code review approved`, and `Item reopened`.
- **Reason:** SoloDevBoard uses an issue-driven roadmap. Agents manage issue creation and metadata deliberately. Built-in GitHub workflows stay as narrow safety nets. **Archiving closed cards is Roadmap Sync’s job**, not GitHub’s Auto-archive workflow, because GitHub’s `updated:` filter is the project-card clock and a bulk field write resets it.

**Archive rule (Roadmap Sync, nightly and on issue events):** closed, non-duplicate issues whose **`closed_at` is at least 14 calendar days ago** are archived on Project #8 via `archiveProjectV2Item` without a preceding field-sync pass ([DEC-026](DECISIONS.md#dec-026-project-8-archive-rule-via-roadmap-sync)). Open items are never archived. Reopened issues are unarchived. Duplicate closures are still **removed**, not archived. Prefer this over GitHub **Auto-archive items**; if that workflow is on, turn it off so the two clocks do not fight.

### Label: status/in-progress Applied
- **Trigger:** The `status/in-progress` label is applied to an issue.
- **Action:** Move the issue to **In Progress**.

### Label: status/todo Applied
- **Trigger:** The `status/todo` label is applied to an issue.
- **Action:** Move the issue to **Todo** if it is not explicitly queued in **Up Next**.

### Issue Closed (Not as Duplicate)
- **Trigger:** An issue is closed without being marked as a duplicate.
- **Action:** Move the issue to **Done**; apply `status/done` label if not already present; ensure **Target Date** reflects the actual close date.

### Issue Closed as Duplicate
- **Trigger:** An issue is closed as a duplicate (via the Triage UI or manually).
- **Action:** Remove from the board (do not add to Done).

### Board Hygiene Audit
- **Trigger:** PM progress review, or any time the roadmap view appears inconsistent.
- **Action:** Backfill missing Start Date / Target Date values for active or done items, correct invalid date pairs, remove stray pull request cards, and add missing planned issues back to the roadmap board.
- **Status labels:** `.github/scripts/sync-status-labels.mjs` runs automatically before `roadmap-sync.mjs` in the Roadmap Sync workflow (nightly schedule, `workflow_dispatch`, and issue events). Use `/sync-status-labels` for on-demand inspection and ad-hoc repairs.

---

## How Issues and PRs Appear on the Board

- **Issues** appear as cards. The card displays: title, labels, assignee, and milestone.
- **Pull Requests** linked to issues (via `Closes #N` in the PR body) update the linked issue's column automatically via the rules above and appear through the **Linked pull requests** field, not as standalone roadmap cards.
- **Unlinked PRs** (no linked issue) are tracked separately and should not appear on the main board. Use a separate view or filter.
- If a pull request card appears on the roadmap board because of an accidentally enabled workflow or manual add, remove it unless you are intentionally using a separate PR review view.
- **Todo issues** may legitimately have blank Start Date and Target Date values until work begins. **In Progress** and **Done** items should not.

---

## Relationship to the Board Rules Visualiser Feature

The Board Rules Visualiser feature (see [board-rules-visualiser.md](../website/content/docs/board-rules-visualiser.md)) will:

1. Read the automation rules configured on this board via the GitHub Projects v2 GraphQL API.
2. Display them as an interactive flow diagram.
3. Use the rules defined in this document as the **expected/canonical state** for comparison.

The rules defined here serve as both the operational configuration and the reference documentation for the visualiser's expected output.

---

## Board Views

In addition to the default board view, the following saved views are useful:

| View | Layout | Purpose |
|------|--------|---------|
| **Story Board** | Board | Day-to-day execution; filter `-label:type/epic -label:type/feature`; sort by **Focus Order** ascending when using **Up Next**. This is the view that should stay useful after v1.0.0. |
| **Feature Board** | Board | Track feature-level progress (`label:type/feature`). |
| **Epic Board** | Board | Track epic-level progress (`label:type/epic`). |
| **Roadmap** | Roadmap (Gantt) | Date-range visualisation of items that already have **Start Date** and **Target Date**. See [Keeping the Roadmap layout useful](#keeping-the-roadmap-layout-useful). Filter out Done for a live queue. |

The GitHub **Roadmap** layout only draws bars for items with dates. SoloDevBoard date rules leave **Todo** items blank until work starts, then overwrite **Target Date** with the close date. After a release, a month-scale Roadmap zoomed to "today" therefore shows:

- Closed items as left-pointing arrows (their date range is in the past).
- Parked v1.1.0 / v1.2.0 items with **no bars** (no dates yet).

That is expected. Do not invent Start/Target dates on untouched items just to fill the Gantt.

### Keeping the Roadmap layout useful

Do this in the GitHub UI (saved view settings); agents cannot edit Project views via the GitHub MCP server.

1. **Filter out Done** on the Roadmap view: `status:Todo,Up Next,In Progress` (or equivalent). The live queue is 22 items, not 152 closed cards.
2. **Zoom to Quarter or Year** when you want historical bars; Month + Today will always look empty between active slices.
3. **Treat Story Board as the default working view.** Bars reappear on Roadmap automatically when Roadmap Sync sets dates at Event 2 (work started) and Event 3 (done).
4. **Leave GitHub Auto-archive off.** Roadmap Sync archives closed non-duplicate issues 14 days after `closed_at`. That is the catch-up for Phases 1–4 and the ongoing rule.

Do not put speculative dates on the parked Phase 5 queue (#272–#288) or unstarted v1.1.0 stories. The next visible Roadmap bar should be the first v1.1.0 item that actually starts.

### Agent write access (Projects v2)

GitHub MCP in Cursor can list issues and pull requests. It cannot mutate a **user-owned** Project. The Cloud Agent `gh` token is typically a GitHub App installation token, which can **read** Project #8 (`gh project item-list`) but often cannot **edit** README, short description, views, or item fields.

Item-field writes already go through the Actions bridge and `ROADMAP_PROJECT_TOKEN` (ADR-0017). That is enough for Status/Phase/Priority/dates. A second PAT in Cursor Cloud (`SDB_ROADMAP_PROJECT_TOKEN`, classic, `project` plus `repo` scopes) is optional and only worth it if you want agents to update the info pane README, short description, or repair fields without waiting for the workflow. Reuse the same token family as `ROADMAP_PROJECT_TOKEN`; do not mint a broader token.

---

## Info pane README

The Project #8 info pane (https://github.com/users/markheydon/projects/8?pane=info) is a short public description. Keep it aligned with live milestones, not with a stale phase name.

Canonical copy: [`plan/PROJECT_README.md`](PROJECT_README.md). Refresh that file during each PM progress review, then paste the **Info pane README** block into the project (this repository cannot always write the user-owned Project README from an agent runtime).

Leave the Project **short description** as a current one-liner (GitHub may refuse to save an empty value). Update it in the same pass as the README so it does not lag.

Do not treat [`plan/BACKLOG.md`](BACKLOG.md) as the work queue in that README. Link GitHub Issues and this project instead.

---

## AI Collaborator Instructions

> When making changes to the project board structure:
>
> 1. Update the **Column Structure** table in this document to reflect any added, renamed, or removed columns.
> 2. Update the **Field Usage** section when a field is added or its operational meaning changes.
> 3. Update the **Automation Rules** section to reflect any new or changed rules.
> 4. If the rules change affects the Board Rules Visualiser feature, update `website/content/docs/board-rules-visualiser.md` accordingly.
> 5. Ensure label changes align with `LABEL_STRATEGY.md`.
> 6. If a new automation rule requires a new label, add that label to `LABEL_STRATEGY.md` first.
> 7. Keep project-only workflow states such as **Up Next** out of the issue label taxonomy unless there is a deliberate lifecycle change.
> 8. When phase or milestone status changes, update [`plan/PROJECT_README.md`](PROJECT_README.md) so the info pane can be refreshed.
