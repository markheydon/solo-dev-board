# SoloDevBoard — Backlog Index

**GitHub Issues are the single source of truth for open work.** This page is a roadmap index only — it does not hold a living work queue.

For historical backlog content (pre-migration), see [archive/BACKLOG-2026-07-18.md](archive/BACKLOG-2026-07-18.md). Migration details: [BACKLOG_TO_ISSUES_MIGRATION.md](BACKLOG_TO_ISSUES_MIGRATION.md).

## Where to find work

| Resource | Purpose |
|----------|---------|
| [GitHub Issues (open)](https://github.com/markheydon/solo-dev-board/issues?q=is%3Aissue+is%3Aopen) | Canonical work items |
| [SoloDevBoard Roadmap (Project #8)](https://github.com/users/markheydon/projects/8) | Prioritisation, flow, Up Next queue |
| [Milestones](https://github.com/markheydon/solo-dev-board/milestones) | Release targeting |

## Roadmap Status (2026-07-25)

| Phase | Milestone | Status |
|-------|-----------|--------|
| Phase 1 — Foundation | v0.1.0 | Complete |
| Phase 2 — Label Manager + Audit Dashboard | v0.2.0 | Complete |
| Phase 3 — One-Click Migration + Triage UI | v0.3.0 | Complete |
| Phase 4 — Board Rules Visualiser + Workflow Templates | v0.4.0 | Complete |
| Phase 5 — Cross-Repo PM Workflow | v0.5.0 | Parked until after v1.0.0 — [#272](https://github.com/markheydon/solo-dev-board/issues/272) |
| Phase 6 — Polish, Testing, and Azure Deployment | v1.0.0 | Active — release closure in progress |

## Key epics

| Epic | Issue | Milestone |
|------|-------|-----------|
| Public Release Infrastructure and Authentication | [#101](https://github.com/markheydon/solo-dev-board/issues/101) | v1.0.0 |
| Operational hardening for public release | [#102](https://github.com/markheydon/solo-dev-board/issues/102) | v1.0.0 |
| Cross-Repo PM Workflow (Phase 5) | [#272](https://github.com/markheydon/solo-dev-board/issues/272) | v0.5.0 (parked) |
| Post-v1 Improvements | [#289](https://github.com/markheydon/solo-dev-board/issues/289) | None |

## Completed phases (summary)

Phases 1–4 delivered foundation, Label Manager, Audit Dashboard, One-Click Migration (labels + milestones), Triage UI, Board Rules Visualiser, and built-in Workflow Templates. Deferred follow-on slices (label consistency warnings, project board migration, custom template repos, Audit Dashboard polish) are tracked under [#289](https://github.com/markheydon/solo-dev-board/issues/289) and child issues.

Phase 6 (v1.0.0) remaining work is tracked on the [v1.0.0 milestone](https://github.com/markheydon/solo-dev-board/milestone/4), including issues [#110](https://github.com/markheydon/solo-dev-board/issues/110), [#247](https://github.com/markheydon/solo-dev-board/issues/247)–[#259](https://github.com/markheydon/solo-dev-board/issues/259). Operational hardening test coverage expectations are documented in [OPERATIONAL_HARDENING_TEST_COVERAGE.md](OPERATIONAL_HARDENING_TEST_COVERAGE.md).

### V1 auth polish (Epic #101 follow-on)

| Issue | Focus | Status |
|-------|-------|--------|
| [#247](https://github.com/markheydon/solo-dev-board/issues/247) | PAT-only local trusted mode documentation | Docs in [PR #324](https://github.com/markheydon/solo-dev-board/pull/324) |
| [#248](https://github.com/markheydon/solo-dev-board/issues/248) | Self-hoster deployment path (`aspire deploy` with PAT) | Docs in [PR #324](https://github.com/markheydon/solo-dev-board/pull/324) |
| [#249](https://github.com/markheydon/solo-dev-board/issues/249) | Hosted unauthenticated landing page | Done |
| [#314](https://github.com/markheydon/solo-dev-board/issues/314) | PAT/GitHub connectivity readiness (startup probe, shell status, recovery UX) | Done |

For phase sequencing and architecture milestones, see [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md). For product boundaries, see [SCOPE.md](SCOPE.md).
