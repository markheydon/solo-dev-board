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

## Roadmap status (2026-11-15)

| Release | GitHub milestone | Release tag | Status |
|---------|------------------|-------------|--------|
| Foundation | `v0.1 - Foundation` | `v0.1.0` | Complete |
| Core features | `v0.2 - Label Manager + Audit Dashboard` | `v0.2.0` | Complete |
| Migration + Triage | `v0.3 - One-Click Migration + Triage UI` | `v0.3.0` | Complete |
| Visualisation + Templates | `v0.4 - Board Rules Visualiser + Workflow Templates` | `v0.4.0` | Complete |
| Production Ready | `v1.0 - Production Ready` | `v1.0.0` | Complete — [`v1.0.0`](https://github.com/markheydon/solo-dev-board/releases/tag/v1.0.0) (2026-08-18) |
| Cross-Repo Planning & Refinement | `v1.1 - Cross-Repo Planning & Refinement` | `v1.1.0` | Complete — [`v1.1.0`](https://github.com/markheydon/solo-dev-board/releases/tag/v1.1.0) (2026-08-31) |
| Planning polish, Reload & Templates | `v1.2 - Planning polish, Reload & Templates` | `v1.2.0` | Complete — [`v1.2.0`](https://github.com/markheydon/solo-dev-board/releases/tag/v1.2.0) (2026-09-07) |
| Label remap onto the recommended taxonomy | `v1.3 - Label remap onto the recommended taxonomy` | `v1.3.0` | Complete — [`v1.3.0`](https://github.com/markheydon/solo-dev-board/releases/tag/v1.3.0) (2026-11-15) |
| Product chrome, branding, and narrow viewports | `v1.4 - Product chrome, branding, and narrow viewports` | `v1.4.0` | Complete — [`v1.4.0`](https://github.com/markheydon/solo-dev-board/releases/tag/v1.4.0) (2026-11-15). Milestone [#10](https://github.com/markheydon/solo-dev-board/milestone/10) closed. |

Per [DEC-027](adr/adr-0027-post-1-0-milestone-and-work-item-hierarchy.md), there is **no open milestone** until the next release is declared. **New or promoted features** use **Spec Kit** ([`SPEC_KIT_MIGRATION.md`](SPEC_KIT_MIGRATION.md)), not Copilot-era issue-tree prompts. Do not declare a separate **`v1.5`** for branding (folded into `v1.4`).

**Unmilestoned backlog:** [#293](https://github.com/markheydon/solo-dev-board/issues/293) (platform-blocked private user-owned Projects v2 under hosted sign-in); [#391](https://github.com/markheydon/solo-dev-board/issues/391) (Aspire PM settings store); [#498](https://github.com/markheydon/solo-dev-board/issues/498) (persist Actions template source — blocked by #391); ice-box catalogue and hygiene work ([#381](https://github.com/markheydon/solo-dev-board/issues/381), [#435](https://github.com/markheydon/solo-dev-board/issues/435)–[#439](https://github.com/markheydon/solo-dev-board/issues/439), [#450](https://github.com/markheydon/solo-dev-board/issues/450), [#470](https://github.com/markheydon/solo-dev-board/issues/470), [#534](https://github.com/markheydon/solo-dev-board/issues/534)–[#537](https://github.com/markheydon/solo-dev-board/issues/537), [#571](https://github.com/markheydon/solo-dev-board/issues/571)). Dispositions: [Ice-box review](#ice-box-review-2026-11-15). Closed ice-box direction: [#475](https://github.com/markheydon/solo-dev-board/issues/475) (see [`PRODUCT_OPERATING_SYSTEM.md`](PRODUCT_OPERATING_SYSTEM.md)). See [open issues](https://github.com/markheydon/solo-dev-board/issues?q=is%3Aissue+is%3Aopen).

## Shipped summary

The six core tools (Label Manager, Audit Dashboard, One-Click Migration, Triage UI, Board Rules Visualiser, Actions Templates) plus hosted and self-host hardening shipped in v1.0.0. Planning, OSS catalogue identification, project-board Status migration, and related refinements shipped in `v1.1.0`. Reload, custom templates, and Triage Save and next shipped on `v1.2`. Label remap ([#491](https://github.com/markheydon/solo-dev-board/issues/491)–[#521](https://github.com/markheydon/solo-dev-board/issues/521)) closed on the `v1.3` milestone.

For product boundaries, see [SCOPE.md](SCOPE.md). For release process and tag history, see [RELEASE_PLAN.md](RELEASE_PLAN.md).

## Ice-box review (2026-11-15)

Most items were ice-boxed because they were unscheduled, not because they were rejected. Branding ([#397](https://github.com/markheydon/solo-dev-board/issues/397), [#529](https://github.com/markheydon/solo-dev-board/issues/529), [#531](https://github.com/markheydon/solo-dev-board/issues/531)) was promoted onto **`v1.4`** on 2026-11-15. **After `v1.4.0`**, further promoted ice-box features should enter **Spec Kit**, not Copilot-era issue-tree prompts ([`SPEC_KIT_MIGRATION.md`](SPEC_KIT_MIGRATION.md)).

| Issue | Disposition | Notes |
|-------|-------------|-------|
| [#381](https://github.com/markheydon/solo-dev-board/issues/381) Repository groups | Intended later | Natural follow-on after OSS filters; needs design before a milestone. |
| [#397](https://github.com/markheydon/solo-dev-board/issues/397) Branding / logo | On **`v1.4`** | Promoted 2026-11-15 with [#529](https://github.com/markheydon/solo-dev-board/issues/529) and [#531](https://github.com/markheydon/solo-dev-board/issues/531). No separate `v1.5`. |
| [#435](https://github.com/markheydon/solo-dev-board/issues/435) Repositories catalogue actions | Intended later | Stubs called out in release notes; catalogue slice when prioritised. |
| [#436](https://github.com/markheydon/solo-dev-board/issues/436) Template parameter profiles | Intended later | Complements Actions Templates after localStorage source ships. |
| [#437](https://github.com/markheydon/solo-dev-board/issues/437) Full Project v2 automation rules | Intended later | Board Rules depth; GraphQL and API surface to scope. |
| [#438](https://github.com/markheydon/solo-dev-board/issues/438) Community Standards health | Intended later | Cross-repo hygiene; fits catalogue theme with #435–#439. |
| [#439](https://github.com/markheydon/solo-dev-board/issues/439) FUNDING.yml check | Intended later | Same catalogue theme. |
| [#450](https://github.com/markheydon/solo-dev-board/issues/450) Window-focus refetch | Intended later | Deliberate follow-up after Reload; not a CD or auth concern. |
| [#470](https://github.com/markheydon/solo-dev-board/issues/470) Least-privilege CI credentials | Intended later | Security hygiene when CI tokens are revisited. |
| [#529](https://github.com/markheydon/solo-dev-board/issues/529) Theme tokens enabler | On **`v1.4`** | Child of #397. |
| [#531](https://github.com/markheydon/solo-dev-board/issues/531) Branding test assets | On **`v1.4`** | Child of #397. |
| [#534](https://github.com/markheydon/solo-dev-board/issues/534) Dependabot management | Intended later | Cross-repo ops; after catalogue actions mature. |
| [#535](https://github.com/markheydon/solo-dev-board/issues/535) Active vs dormant repos | Intended later | Catalogue insight slice. |
| [#536](https://github.com/markheydon/solo-dev-board/issues/536) Testing-standard migration | Intended later | Cross-repo standards tracking. |
| [#537](https://github.com/markheydon/solo-dev-board/issues/537) Shared filters for large lists | Intended later | Repositories UX at scale. |
| [#571](https://github.com/markheydon/solo-dev-board/issues/571) Security and Quality roll-up | Intended later | Audit Dashboard expansion; large slice. |

**Still blocked (unchanged):** [#293](https://github.com/markheydon/solo-dev-board/issues/293) hosted private user Projects v2; [#391](https://github.com/markheydon/solo-dev-board/issues/391) Aspire PM settings store; [#498](https://github.com/markheydon/solo-dev-board/issues/498) server-backed template source (blocked by #391).