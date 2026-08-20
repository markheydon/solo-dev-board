# Cross-Repo PM Workflow — issues checklist

Parent: [#272](https://github.com/markheydon/solo-dev-board/issues/272). Milestone: **v1.1.0** (number 7). Do **not** assign new issues at creation — Roadmap Sync assigns `markheydon` when Status is **Up Next** or **In Progress**. Status on new/updated children: `status/todo` (do not add an Up Next **label**).

Wireframe: `plan/wireframes/pm-workflow-wireframe.md`. Plan: `plan/cross-repo-pm-workflow-project-plan.md`. Decisions: DEC-027, DEC-028, DEC-029.

## New issues (created during planning)

| Kind | Issue | Size | Priority | Area | Blocks |
|------|-------|------|----------|------|--------|
| Enabler | [#382](https://github.com/markheydon/solo-dev-board/issues/382) Projects v2 item catalogue | `size/l` | high | `area/infrastructure` | #273, #275, #283–#285 |
| Enabler | [#383](https://github.com/markheydon/solo-dev-board/issues/383) PM settings persistence | `size/s` | high | `area/infrastructure` | #273, #274, #277, #284, #287 |
| Enabler | [#384](https://github.com/markheydon/solo-dev-board/issues/384) Cross-repo work-item catalogue | `size/l` | high | `area/infrastructure` | #274, #276, #277, #279, #281, #288 |
| Test | [#385](https://github.com/markheydon/solo-dev-board/issues/385) Daily Focus | `size/m` | medium | `area/dashboard` | Feature close-out |
| Test | [#386](https://github.com/markheydon/solo-dev-board/issues/386) Backlog Review | `size/m` | medium | `area/dashboard` | Feature close-out |
| Test | [#387](https://github.com/markheydon/solo-dev-board/issues/387) Iteration Planning | `size/m` | medium | `area/dashboard` | Feature close-out |
| Test | [#388](https://github.com/markheydon/solo-dev-board/issues/388) Repo Management | `size/s` | medium | `area/dashboard` | Feature close-out |

## Existing stories (spec upgrade)

| # | Page | Size after spec | Depends on |
|---|------|-----------------|------------|
| 273 | Daily Focus — board state | `size/m` | Item catalogue, settings |
| 274 | Daily Focus — top 3 | `size/m` | Work-item catalogue, settings |
| 275 | Daily Focus — Up Next stall | `size/s` | Item catalogue, 273 |
| 276 | Daily Focus — PR review stall | `size/s` | Work-item catalogue, 273 |
| 277 | Backlog — grouped view | `size/l` | Work-item catalogue, settings |
| 278 | Backlog — missing core labels | `size/s` | 277 |
| 279 | Backlog — epics near complete | `size/m` | Work-item catalogue, 277 |
| 280 | Backlog — issue vs PR chips | `size/xs` | 277 |
| 281 | Backlog — neglected repos | `size/s` | Work-item catalogue, 277 |
| 282 | Backlog — priority/high surface | `size/s` | 277 |
| 283 | Planning — add to Up Next | `size/l` | Item catalogue, 274 or 277 for candidates |
| 284 | Planning — capacity | `size/m` | Settings, 283 |
| 285 | Planning — resolve stalled first | `size/m` | 275, 283 |
| 286 | Planning — bulk milestone | `size/s` | 283 |
| 287 | Repos — exclusions | `size/m` | Settings enabler |
| 288 | Repos — counts summary | `size/s` | Work-item catalogue, 287 |

## Blocking relationships (set in GitHub)

Sub-issues: all rows below are children of **#272**.

| Blocking | Blocked | Type |
|----------|---------|------|
| #382 item catalogue | #273, #275, #283, #284, #285 | blocks |
| #383 settings | #273, #274, #277, #284, #287 | blocks |
| #384 work-item catalogue | #274, #276, #277, #279, #281, #288 | blocks |
| #287 | #274, #277, #283, #288 (query scope; board occupancy on #273 does not wait) | blocks |
| #273 | #275, #276 | blocks |
| #277 | #278, #279, #280, #281, #282 | blocks |
| #283 | #284, #285, #286 | blocks |
| #275 | #285 | blocks |
| Each area test | corresponding stories (tests blocked by stories) | blocks |

Suggested delivery order (Up Next later, not this session):

1. Three enablers (item catalogue and work-item catalogue can proceed in parallel after settings is started).
2. #287 then #288.
3. #273 then #274, #275, #276.
4. #277 then #278–#282.
5. #283 then #284–#286.
6. Four test issues in parallel with the last story in each area.

## Do not

- Set Project **Phase** (v1.1.0 leaves Phase blank).
- Set Start/Target dates at planning time.
- Put Features in Up Next.
- Merge #280/#282 into #277 on the board; keep slices, same route.
