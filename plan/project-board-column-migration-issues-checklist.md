# Project board column migration — issues checklist

Parent: [#291](https://github.com/markheydon/solo-dev-board/issues/291). Milestone: **`v1.1 - Cross-Repo Planning & Refinement`** (number 7). Assignee: `markheydon`. Status on new/updated issues: `status/todo` (do not add an Up Next **label**).

Wireframe: `plan/wireframes/one-click-migration-wireframe.md`. Plan: `plan/project-board-column-migration-project-plan.md`. Decisions: DEC-010, DEC-032.

## Issues

| Kind | Issue | Size | Priority | Area | Blocks |
|------|-------|------|----------|------|--------|
| Feature | [#291](https://github.com/markheydon/solo-dev-board/issues/291) Project board column migration | `size/l` | low | `area/migration` | Feature close-out after children |
| Enabler | [#414](https://github.com/markheydon/solo-dev-board/issues/414) Projects v2 Status structure GraphQL | `size/m` | low | `area/migration` | #416 |
| Story | [#416](https://github.com/markheydon/solo-dev-board/issues/416) Migrate Status columns in One-Click Migration | `size/m` | low | `area/migration` | #415 |
| Test | [#415](https://github.com/markheydon/solo-dev-board/issues/415) Project board column migration coverage | `size/m` | low | `area/migration` | Feature close-out |

## Blocking relationships

| Blocking | Blocked | Type |
|----------|---------|------|
| #414 GraphQL + contracts | #416 Migration UI | blocks |
| #416 Migration UI | #415 Tests (UI / Playwright) | blocks |

Suggested delivery order (Up Next later, not this session):

1. #414, including the GitHub.com GraphQL spike.
2. #416 (page, preview, apply, user guide).
3. #415 remaining coverage and Playwright alignment.

## Do not

- Set Project **Phase** (`v1.1` leaves Phase blank).
- Set Start/Target dates at planning time.
- Put the Feature in Up Next.
- Reopen closed epic #87 or closed feature #88.
- Expand scope to `copyProjectV2`, automations, or #293.
