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

## Roadmap status (2026-08-18)

| Release | Milestone | Status |
|---------|-----------|--------|
| Foundation | v0.1.0 | Complete |
| Core features | v0.2.0 | Complete |
| Migration + Triage | v0.3.0 | Complete |
| Visualisation + Templates | v0.4.0 | Complete |
| Production Ready | v1.0.0 | Complete — [`v1.0.0`](https://github.com/markheydon/solo-dev-board/releases/tag/v1.0.0) (2026-08-18) |
| **Next release** | **v1.1.0** | **Not started — 20 open issues** |

The sole open milestone is **v1.1.0**. It includes deferred v1.0 slices ([#290](https://github.com/markheydon/solo-dev-board/issues/290)–[#292](https://github.com/markheydon/solo-dev-board/issues/292)), Planning ([#272](https://github.com/markheydon/solo-dev-board/issues/272)–[#288](https://github.com/markheydon/solo-dev-board/issues/288)), and dogfood fixes as they are raised.

**Unmilestoned backlog:** [#293](https://github.com/markheydon/solo-dev-board/issues/293) (platform-blocked private user-owned Projects v2 under hosted sign-in); [#397](https://github.com/markheydon/solo-dev-board/issues/397) (product branding including a logo — ice-box).

Implementation phases in [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) are historical sequencing for the v1.0 release only.

## Key features (open work)

| Feature | Issue | Milestone |
|---------|-------|-----------|
| Planning | [#272](https://github.com/markheydon/solo-dev-board/issues/272) | v1.1.0 |

Deferred slices ([#290](https://github.com/markheydon/solo-dev-board/issues/290)–[#292](https://github.com/markheydon/solo-dev-board/issues/292)) are milestone stories extending shipped features, not children of a bucket epic.

## Shipped summary

The six core tools (Label Manager, Audit Dashboard, One-Click Migration, Triage UI, Board Rules Visualiser, Actions Templates) plus hosted and self-host hardening shipped in v1.0.0. Duplicate Planning issues #260–#271 were closed as duplicates of the #272–#288 family.

For phase sequencing history and architecture milestones, see [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md). For product boundaries, see [SCOPE.md](SCOPE.md).
