# SoloDevBoard — Backlog Index

**GitHub Issues are the single source of truth for open work.** This page is the canonical roadmap index; it does not hold a living work queue.

For historical backlog content (pre-migration), see [archive/BACKLOG-2026-07-18.md](archive/BACKLOG-2026-07-18.md). Migration details: [archive/BACKLOG_TO_ISSUES_MIGRATION.md](archive/BACKLOG_TO_ISSUES_MIGRATION.md). Post-1.0 planning is [DEC-027](adr/adr-0027-post-1-0-milestone-and-work-item-hierarchy.md). Phased v0.x–v1.2 delivery history: [archive/IMPLEMENTATION_PLAN.md](archive/IMPLEMENTATION_PLAN.md).

## Where to find work

| Resource | Purpose |
|----------|---------|
| [GitHub Issues (open)](https://github.com/markheydon/solo-dev-board/issues?q=is%3Aissue+is%3Aopen) | Canonical work items |
| [SoloDevBoard Roadmap (Project #8)](https://github.com/users/markheydon/projects/8) | Prioritisation, flow, Up Next queue |
| [Project info pane README](PROJECT_README.md) | Canonical copy of the Project #8 info pane text |
| [Milestones](https://github.com/markheydon/solo-dev-board/milestones) | Release targeting |

## Roadmap status (2026-11-14)

| Release | GitHub milestone | Release tag | Status |
|---------|------------------|-------------|--------|
| Foundation | `v0.1 - Foundation` | `v0.1.0` | Complete |
| Core features | `v0.2 - Label Manager + Audit Dashboard` | `v0.2.0` | Complete |
| Migration + Triage | `v0.3 - One-Click Migration + Triage UI` | `v0.3.0` | Complete |
| Visualisation + Templates | `v0.4 - Board Rules Visualiser + Workflow Templates` | `v0.4.0` | Complete |
| Production Ready | `v1.0 - Production Ready` | `v1.0.0` | Complete — [`v1.0.0`](https://github.com/markheydon/solo-dev-board/releases/tag/v1.0.0) (2026-08-18) |
| Cross-Repo Planning & Refinement | `v1.1 - Cross-Repo Planning & Refinement` | `v1.1.0` | Complete — [`v1.1.0`](https://github.com/markheydon/solo-dev-board/releases/tag/v1.1.0) (2026-08-31) |
| Planning polish, Reload & Templates | `v1.2 - Planning polish, Reload & Templates` | `v1.2.0` | Complete — [`v1.2.0`](https://github.com/markheydon/solo-dev-board/releases/tag/v1.2.0) (2026-09-07) |
| Usable solo workflow across repositories | `v1.3 - Usable solo workflow across repositories` | `v1.3.0` | Ready to tag — [milestone #9](https://github.com/markheydon/solo-dev-board/milestone/9): 14 closed, 0 open |

There is **no newly declared open milestone** after `v1.3` until you scope the next release and open one on GitHub ([DEC-027](adr/adr-0027-post-1-0-milestone-and-work-item-hierarchy.md)). Tag `v1.3.0` after staging smoke test and release hygiene; then close milestone #9.

**Unmilestoned backlog:** [#293](https://github.com/markheydon/solo-dev-board/issues/293) (platform-blocked private user-owned Projects v2 under hosted sign-in); [#391](https://github.com/markheydon/solo-dev-board/issues/391) (Aspire PM settings store); [#498](https://github.com/markheydon/solo-dev-board/issues/498) (persist Actions template source — blocked by #391); [#450](https://github.com/markheydon/solo-dev-board/issues/450) (window-focus refetch — ice-box, later Reload follow-up); ice-box catalogue and hygiene work ([#381](https://github.com/markheydon/solo-dev-board/issues/381), [#397](https://github.com/markheydon/solo-dev-board/issues/397), [#411](https://github.com/markheydon/solo-dev-board/issues/411) (mobile layout story under [#527](https://github.com/markheydon/solo-dev-board/issues/527)), [#435](https://github.com/markheydon/solo-dev-board/issues/435)–[#439](https://github.com/markheydon/solo-dev-board/issues/439), [#470](https://github.com/markheydon/solo-dev-board/issues/470)); planned MudBlazor visual language ([#527](https://github.com/markheydon/solo-dev-board/issues/527), DEC-039). Closed ice-box direction: [#475](https://github.com/markheydon/solo-dev-board/issues/475) (see [`PRODUCT_OPERATING_SYSTEM.md`](PRODUCT_OPERATING_SYSTEM.md)). See [open issues](https://github.com/markheydon/solo-dev-board/issues?q=is%3Aissue+is%3Aopen).

## Shipped summary

The six core tools (Label Manager, Audit Dashboard, One-Click Migration, Triage UI, Board Rules Visualiser, Actions Templates) plus hosted and self-host hardening shipped in v1.0.0. Planning, OSS catalogue identification, project-board Status migration, and related refinements shipped in `v1.1.0`. Reload, custom templates, and Triage Save and next shipped on `v1.2`. Label remap ([#491](https://github.com/markheydon/solo-dev-board/issues/491)–[#521](https://github.com/markheydon/solo-dev-board/issues/521)) closed on the `v1.3` milestone.

For product boundaries, see [SCOPE.md](SCOPE.md). For release process and tag history, see [RELEASE_PLAN.md](RELEASE_PLAN.md).