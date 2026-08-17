# SoloDevBoard — Backlog Index

**GitHub Issues are the single source of truth for open work.** This page is a roadmap index only — it does not hold a living work queue.

For historical backlog content (pre-migration), see [archive/BACKLOG-2026-07-18.md](archive/BACKLOG-2026-07-18.md). Migration details: [BACKLOG_TO_ISSUES_MIGRATION.md](BACKLOG_TO_ISSUES_MIGRATION.md). Post-1.0 numbering is [DEC-024](DECISIONS.md#dec-024-post-10-milestone-numbering).

## Where to find work

| Resource | Purpose |
|----------|---------|
| [GitHub Issues (open)](https://github.com/markheydon/solo-dev-board/issues?q=is%3Aissue+is%3Aopen) | Canonical work items |
| [SoloDevBoard Roadmap (Project #8)](https://github.com/users/markheydon/projects/8) | Prioritisation, flow, Up Next queue |
| [Milestones](https://github.com/markheydon/solo-dev-board/milestones) | Release targeting |

## Roadmap Status (2026-08-17)

| Phase | Milestone | Status |
|-------|-----------|--------|
| Phase 1 — Foundation | v0.1.0 | Complete |
| Phase 2 — Label Manager + Audit Dashboard | v0.2.0 | Complete |
| Phase 3 — One-Click Migration + Triage UI | v0.3.0 | Complete |
| Phase 4 — Board Rules Visualiser + Workflow Templates | v0.4.0 | Complete |
| Phase 6 — Polish, Testing, and Azure Deployment | v1.0.0 | Ready to tag — [#257](https://github.com/markheydon/solo-dev-board/issues/257) |
| Deferred follow-ons | v1.1.0 | Not started — [#289](https://github.com/markheydon/solo-dev-board/issues/289) |
| Phase 5 — Cross-Repo PM Workflow | v1.2.0 | Parked until after v1.0.0 — [#272](https://github.com/markheydon/solo-dev-board/issues/272) |

The GitHub milestone for Phase 5 is `v1.2.0 — Cross-Repo PM Workflow` (renamed from `v0.5.0`). Deferred follow-ons use `v1.1.0 — Deferred follow-ons`.

## Key epics

| Epic | Issue | Milestone |
|------|-------|-----------|
| Public Release Infrastructure and Authentication | [#101](https://github.com/markheydon/solo-dev-board/issues/101) | v1.0.0 (closed) |
| Operational hardening for public release | [#102](https://github.com/markheydon/solo-dev-board/issues/102) | v1.0.0 (closed) |
| v1.1 deferred follow-ons | [#289](https://github.com/markheydon/solo-dev-board/issues/289) | v1.1.0 |
| Cross-Repo PM Workflow (Phase 5) | [#272](https://github.com/markheydon/solo-dev-board/issues/272) | v1.2.0 |

## Completed phases (summary)

Phases 1–4 delivered foundation, Label Manager, Audit Dashboard, One-Click Migration (labels + milestones), Triage UI, Board Rules Visualiser, and built-in Workflow Templates. Deferred follow-on slices (label consistency warnings, project board migration, custom template repos, hosted private Projects v2) are tracked under [#289](https://github.com/markheydon/solo-dev-board/issues/289) and child issues [#290](https://github.com/markheydon/solo-dev-board/issues/290)–[#293](https://github.com/markheydon/solo-dev-board/issues/293).

Phase 6 (v1.0.0) delivery is complete aside from tagging. Remaining work on the [v1.0.0 milestone](https://github.com/markheydon/solo-dev-board/milestone/4) is [#257](https://github.com/markheydon/solo-dev-board/issues/257) (tag and release notes) and [#364](https://github.com/markheydon/solo-dev-board/issues/364) (paperwork alignment). Duplicate Phase 5 issues #260–#271 were closed as duplicates of the #272-family stories. Operational hardening test coverage expectations are documented in [OPERATIONAL_HARDENING_TEST_COVERAGE.md](OPERATIONAL_HARDENING_TEST_COVERAGE.md).

### V1 auth polish (Epic #101 follow-on)

| Issue | Focus | Status |
|-------|-------|--------|
| [#247](https://github.com/markheydon/solo-dev-board/issues/247) | PAT-only local trusted mode documentation | Done |
| [#248](https://github.com/markheydon/solo-dev-board/issues/248) | Self-hoster deployment path (`aspire deploy` with PAT) | Done |
| [#249](https://github.com/markheydon/solo-dev-board/issues/249) | Hosted unauthenticated landing page | Done |
| [#314](https://github.com/markheydon/solo-dev-board/issues/314) | PAT/GitHub connectivity readiness (startup probe, shell status, recovery UX) | Done |

For phase sequencing and architecture milestones, see [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md). For product boundaries, see [SCOPE.md](SCOPE.md).
