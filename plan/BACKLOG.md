# SoloDevBoard — Backlog Index

**GitHub Issues are the single source of truth for open work.** This page is a roadmap index only — it does not hold a living work queue.

For historical backlog content (pre-migration), see [archive/BACKLOG-2026-07-18.md](archive/BACKLOG-2026-07-18.md). Migration details: [BACKLOG_TO_ISSUES_MIGRATION.md](BACKLOG_TO_ISSUES_MIGRATION.md). Post-1.0 planning is [DEC-027](DECISIONS.md#dec-027-post-10-milestone-and-work-item-hierarchy).

## Where to find work

| Resource | Purpose |
|----------|---------|
| [GitHub Issues (open)](https://github.com/markheydon/solo-dev-board/issues?q=is%3Aissue+is%3Aopen) | Canonical work items |
| [SoloDevBoard Roadmap (Project #8)](https://github.com/users/markheydon/projects/8) | Prioritisation, flow, Up Next queue |
| [Project info pane README](PROJECT_README.md) | Canonical copy of the Project #8 info pane text |
| [Milestones](https://github.com/markheydon/solo-dev-board/milestones) | Release targeting |

## Roadmap status (2026-08-31)

| Release | GitHub milestone | Release tag | Status |
|---------|------------------|-------------|--------|
| Foundation | `v0.1 - Foundation` | `v0.1.0` | Complete |
| Core features | `v0.2 - Label Manager + Audit Dashboard` | `v0.2.0` | Complete |
| Migration + Triage | `v0.3 - One-Click Migration + Triage UI` | `v0.3.0` | Complete |
| Visualisation + Templates | `v0.4 - Board Rules Visualiser + Workflow Templates` | `v0.4.0` | Complete |
| Production Ready | `v1.0 - Production Ready` | `v1.0.0` | Complete — [`v1.0.0`](https://github.com/markheydon/solo-dev-board/releases/tag/v1.0.0) (2026-08-18) |
| Cross-Repo Planning & Refinement | `v1.1 - Cross-Repo Planning & Refinement` | `v1.1.0` | Complete — [`v1.1.0`](https://github.com/markheydon/solo-dev-board/releases/tag/v1.1.0) (2026-08-31) |
| Planning polish, Reload & Templates | `v1.2 - Planning polish, Reload & Templates` | `v1.2.0` | Open — [milestone](https://github.com/markheydon/solo-dev-board/milestone/8) |

The open milestone is `v1.2 - Planning polish, Reload & Templates`. Further work stays unmilestoned until the next release is declared.

**Unmilestoned backlog:** [#293](https://github.com/markheydon/solo-dev-board/issues/293) (platform-blocked private user-owned Projects v2 under hosted sign-in); [#391](https://github.com/markheydon/solo-dev-board/issues/391) (Aspire PM settings store); [#450](https://github.com/markheydon/solo-dev-board/issues/450) (window-focus refetch — ice-box, later Reload follow-up); ice-box catalogue and hygiene work ([#381](https://github.com/markheydon/solo-dev-board/issues/381), [#397](https://github.com/markheydon/solo-dev-board/issues/397), [#411](https://github.com/markheydon/solo-dev-board/issues/411), [#435](https://github.com/markheydon/solo-dev-board/issues/435)–[#439](https://github.com/markheydon/solo-dev-board/issues/439), [#470](https://github.com/markheydon/solo-dev-board/issues/470)). Closed ice-box direction: [#475](https://github.com/markheydon/solo-dev-board/issues/475) (see [`PRODUCT_OPERATING_SYSTEM.md`](PRODUCT_OPERATING_SYSTEM.md)). See [open issues](https://github.com/markheydon/solo-dev-board/issues?q=is%3Aissue+is%3Aopen).

Implementation phases in [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) are historical sequencing for the v1.0 release only.

## Key features (remaining open work)

Locked-in `v1.2` delivery is the open queue. Remaining work stays **unmilestoned**.

| Work | Issues | Milestone |
|------|--------|-----------|
| Optional board-scoped Daily Focus | [#403](https://github.com/markheydon/solo-dev-board/issues/403) | `v1.2` |
| Reload from GitHub (keep selection) | [#447](https://github.com/markheydon/solo-dev-board/issues/447), [#449](https://github.com/markheydon/solo-dev-board/issues/449), [#451](https://github.com/markheydon/solo-dev-board/issues/451) | `v1.2` |
| Hosted GitHub HttpClient split | [#453](https://github.com/markheydon/solo-dev-board/issues/453) | `v1.2` |
| Custom template repositories | [#292](https://github.com/markheydon/solo-dev-board/issues/292) | `v1.2` |
| Window-focus refetch | [#450](https://github.com/markheydon/solo-dev-board/issues/450) | Unmilestoned (ice-box; later follow-up) |
| Hosted private Projects v2 | [#293](https://github.com/markheydon/solo-dev-board/issues/293) | Unmilestoned (platform-blocked) |

Deferred slices ([#290](https://github.com/markheydon/solo-dev-board/issues/290)–[#291](https://github.com/markheydon/solo-dev-board/issues/291)) and Planning ([#272](https://github.com/markheydon/solo-dev-board/issues/272)–[#288](https://github.com/markheydon/solo-dev-board/issues/288)) shipped on `v1.1`. [#292](https://github.com/markheydon/solo-dev-board/issues/292) is now on `v1.2`.

## Shipped summary

The six core tools (Label Manager, Audit Dashboard, One-Click Migration, Triage UI, Board Rules Visualiser, Actions Templates) plus hosted and self-host hardening shipped in v1.0.0. Planning, OSS catalogue identification, project-board Status migration, and related refinements ship in `v1.1.0`. Duplicate Planning issues #260–#271 were closed as duplicates of the #272–#288 family.

For phase sequencing history and architecture milestones, see [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md). For product boundaries, see [SCOPE.md](SCOPE.md).
