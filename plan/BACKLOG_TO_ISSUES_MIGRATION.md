# Backlog-to-Issues Migration Plan

<!-- AI Collaborator Instructions: Migration complete 2026-07-18. This document is retained for history. -->

**Status:** Complete — executed 2026-07-18.

**Created:** 2026-07-18.

**Completed:** 2026-07-18.

**Purpose:** Migrate SoloDevBoard from a dual-source planning model (`plan/BACKLOG.md` + GitHub Issues) to **GitHub Issues as the single source of truth for work items**, while keeping markdown files for scope, sequencing, and process.

---

## Prerequisite

**Phase 1 (complete):** Governance consolidated into `AGENTS.md`, role contracts relocated to `.agents/contracts/`, and cross-references updated for tool-agnostic AI collaboration.

**Phase 2 (this plan):** Execute the behavioural migration so that:

- "Add to backlog" means **create or update a GitHub Issue** and sync the project board.
- Role contracts do not continue writing open work items to `plan/BACKLOG.md`.
- All AI tools share the same canonical workflow via `AGENTS.md` and `.agents/contracts/`.

**Do not re-execute Steps 1–8 below unless a future migration is required.**

---

## Agreed Target State

### Mental model

```
SCOPE.md                 → product boundaries (what is / isn't in scope)
IMPLEMENTATION_PLAN.md   → phase sequencing and architecture milestones
RELEASE_PLAN.md          → versioning and release process
LABEL_STRATEGY.md        → label taxonomy contract
PM_RUNBOOK.md            → daily/weekly operating rhythm
GitHub Issues            → work items (source of truth)
GitHub Project #8        → prioritisation, flow, Up Next queue
GitHub Milestones        → release targeting
```

### What changes

| Artefact | Current role | Target role |
|----------|--------------|-------------|
| `plan/BACKLOG.md` | Living backlog of open user stories | **Retired as a work queue.** Slim index page linking to GitHub milestones/project board, or archived with a redirect notice. |
| GitHub Issues | Created during planning; often duplicates backlog | **Canonical store** for all open, deferred, and in-progress work. |
| `plan/IMPLEMENTATION_PLAN.md` | Phase tasks (some stale) | High-level phase status only; open tasks link to issue numbers, not duplicate prose. |
| Agent skills / prompts | Mandate `BACKLOG.md` updates | Mandate issue creation + project board sync. |

### Release strategy (agreed 2026-07-18)

- **Phases 1–4 are complete.** Deferred slices are post-v1 improvements, not blockers.
- **v1.0.0 is the immediate goal** (Phase 6 closure).
- **Phase 5 (Cross-Repo PM Workflow)** is parked until after v1.0.0 ships.
- **Deferred improvements** (label consistency, custom template repos, project board migration, etc.) are post-v1 backlog items with no milestone.

---

## Current Open Work Inventory

The following items are open in `plan/BACKLOG.md` and/or `plan/IMPLEMENTATION_PLAN.md` as of 2026-07-18. Each must become a GitHub Issue (or be linked to an existing issue) during migration.

### Phase 6 — v1.0.0 (immediate; milestone: `v1.0.0 — Production Ready`)

**Authentication and self-hosting (Epic 101 follow-on):**

| # | Summary | Existing issue? | Suggested labels |
|---|---------|-----------------|------------------|
| 1 | Formalise PAT-only local trusted mode documentation | New issue | `type/documentation`, `priority/medium`, `area/infrastructure`, `size/s` |
| 2 | Self-hoster deployment path (PAT on own Azure subscription) | New issue | `type/documentation`, `priority/medium`, `area/infrastructure`, `size/m` |
| 3 | Dedicated unauthenticated landing page for hosted deployments | New issue | `type/story`, `priority/medium`, `area/infrastructure`, `size/s` |

**Operational hardening:**

| # | Summary | Existing issue? | Suggested labels |
|---|---------|-----------------|------------------|
| 4 | GitHub API response caching | **#108** (verify open) | `type/enabler`, `priority/medium`, `area/infrastructure`, `size/m` |
| 5 | Health check endpoints for Azure Container Apps | **#106** (verify open) | `type/story`, `priority/medium`, `area/infrastructure`, `size/s` |
| 6 | Structured logging and Application Insights telemetry | **#107** (verify open) | `type/enabler`, `priority/medium`, `area/infrastructure`, `size/m` |
| 7 | Persist hosted auth material via Key Vault-backed patterns | New issue | `type/enabler`, `priority/medium`, `area/infrastructure`, `size/m` |
| 8 | CD pipeline with production environment gate (`aspire deploy`) | New issue | `type/chore`, `priority/high`, `area/infrastructure`, `size/m` |

**Quality and release:**

| # | Summary | Existing issue? | Suggested labels |
|---|---------|-----------------|------------------|
| 9 | ≥80% unit test coverage (`Application` + `Domain`) | New issue | `type/test`, `priority/medium`, `area/infrastructure`, `size/l` |
| 10 | Accessibility audit (WCAG 2.1 AA) | New issue | `type/test`, `priority/medium`, `area/infrastructure`, `size/l` |
| 11 | Performance review (caching, pagination) | New issue | `type/chore`, `priority/medium`, `area/infrastructure`, `size/m` |
| 12 | End-to-end tests for critical user journeys | New issue | `type/test`, `priority/medium`, `area/infrastructure`, `size/l` |
| 13 | Comprehensive `docs/` content for all features | New issue | `type/documentation`, `priority/medium`, `area/docs`, `size/m` |
| 14 | Tag v1.0.0 release with release notes | New issue | `type/chore`, `priority/high`, `area/infrastructure`, `size/s` |

**Phase 6 polish (reclassified from Phase 2):**

| # | Summary | Existing issue? | Suggested labels |
|---|---------|-----------------|------------------|
| 15 | Audit Dashboard auto-refresh | New issue | `type/story`, `priority/low`, `area/dashboard`, `size/s` |
| 16 | Audit Dashboard Markdown export | New issue | `type/story`, `priority/low`, `area/dashboard`, `size/s` |

### Phase 5 — v0.5.0 (parked; milestone: `v0.5.0 — Cross-Repo PM Workflow`)

Create issues now or at v1 kickoff, but **do not assign v1.0.0 milestone**. Group under a parent epic issue.

**Parent epic (create if not exists):** `[Epic] Cross-Repo PM Workflow (Phase 5)`

| Group | Stories (from BACKLOG.md Epic 7) | Count |
|-------|----------------------------------|-------|
| Daily Focus | Board state overview; top-3 priorities; Up Next stall alerts; PR In Review stall alerts | 4 |
| Backlog Review | Priority-grouped view; unlabelled flagging; epic completion detection; issue/PR distinction; neglected repos; `priority-high` surfacing | 6 |
| Iteration Planning | Up Next curation; capacity warnings; stale resolution; milestone assignment | 4 |
| Repo Management | Excluded repos config; per-repo issue/PR summary | 2 |

**Total:** 16 story-level issues + 1 epic + enabler/test issues as appropriate during breakdown.

### Post-v1 improvements (no milestone)

Create a parent epic: `[Epic] Post-v1 Improvements`

| # | Summary | Notes | Suggested labels |
|---|---------|-------|------------------|
| 1 | Label consistency warnings (Audit Dashboard) | Originally in SCOPE.md; never built | `type/story`, `priority/low`, `area/dashboard`, `size/m` |
| 2 | Project board column migration | Deferred; ADR-0013 | `type/story`, `priority/low`, `area/migration`, `size/l` |
| 3 | Custom workflow template repositories | Built-in templates done | `type/story`, `priority/low`, `area/workflows`, `size/m` |
| 4 | Private user-owned Projects v2 via hosted sign-in | **Blocked** by GitHub platform | `type/story`, `priority/low`, `status/blocked`, `area/board-rules` |

---

## Execution Steps

Execute in order. Each step has acceptance criteria.

### Step 0 — Pre-flight

- [x] Agent/workflow review for Cursor is complete and signed off.
- [x] Confirm `gh` CLI is authenticated and can create issues and update Project #8.
- [x] Run `gh issue list --state open --limit 200` and `gh api repos/markheydon/solo-dev-board/milestones` to capture current issue/milestone baseline.
- [x] Note the highest existing issue number to avoid duplicate creation.

### Step 1 — Audit existing issues against open backlog

- [x] For each item in the **Current Open Work Inventory** above, check whether a GitHub Issue already exists (search by title, `#106`, `#107`, `#108`, etc.).
- [x] Record a mapping table: `BACKLOG item → Issue #N (exists | create new)`.
- [x] Close or update any stale issues that are already done but still open. (#106 title updated for ACA.)
- [x] Do **not** retroactively create issues for completed Phases 1–4 work.

### Step 2 — Create v1.0.0 issue hierarchy

- [x] Reused Epic #101 and Feature #102 as parents (milestone #4 grouping).
- [x] Created Phase 6 issues #247–#259.
- [x] Applied labels per `plan/LABEL_STRATEGY.md`.
- [x] Assigned all v1.0.0 issues to milestone `v1.0.0 — Production Ready`.
- [x] Linked child issues to parent epics in issue bodies (`Part of #101` / `#102`).
- [x] Added v1 issues to GitHub Project #8 with `status/todo` (Phase 6).

### Step 3 — Create parked Phase 5 issues

- [x] Created `[Epic] Cross-Repo PM Workflow (Phase 5)` — #272.
- [x] Created 16 story issues #273–#288.
- [x] Assigned milestone `v0.5.0 — Cross-Repo PM Workflow` (milestone #6).
- [x] Set `status/todo` on all; not placed in Up Next.
- [ ] Add to Project #8 (pending GraphQL rate-limit reset or roadmap-sync).

### Step 4 — Create post-v1 improvement issues

- [x] Created `[Epic] Post-v1 Improvements` — #289.
- [x] Created four deferred improvement issues #290–#293.
- [x] Applied `priority/low`; left **unmilestoned**.
- [x] Marked #293 with `status/blocked`.
- [ ] Add to Project #8 (pending GraphQL rate-limit reset or roadmap-sync).

### Step 5 — Retire `plan/BACKLOG.md` as a living backlog

**Option A — Slim index (executed):**

- [x] Replaced `plan/BACKLOG.md` body with slim index.
- [x] Archived prior content to `plan/archive/BACKLOG-2026-07-18.md`.
- [x] Removed "Planned migration — parked" banner.

### Step 6 — Update planning and agent artefacts

- [x] Updated all files in the table below (plus contextual pointer updates).

### Step 7 — Verify project board hygiene

- [x] v1.0.0 issues (#247–#259) synced to Project #8 with Phase/Priority/Status.
- [ ] Phase 5 (#272–#288) and post-v1 (#289–#293) on Project #8 — run `roadmap-sync` after GraphQL rate limit resets.
- [x] v0.1.0–v0.4.0 milestones already closed on GitHub (only #4 and #6 remain open).

### Step 8 — Validation and sign-off

- [x] `gh issue list --state open` returns 53 open issues covering all former open BACKLOG items.
- [x] Contributor path: README → Project #8 / Issues links in slim BACKLOG index.
- [x] Agent workflows updated to read Issues/Project, not BACKLOG.md.
- [x] Updated `plan/SCOPE.md` changelog with migration completion entry.
- [x] Marked this document **Complete** with date.

---

## What Not To Do

- Do **not** create issues for already-completed Phases 1–4 work (hundreds of `[x]` items).
- Do **not** big-bang migrate every historical backlog mention — only **open** work.
- Do **not** execute before the agent/workflow review — agents will keep writing to BACKLOG.md and recreate the dual-source problem.
- Do **not** assign Phase 5 or post-v1 issues to the v1.0.0 milestone.
- Do **not** delete `plan/BACKLOG.md` without a redirect — external links and agent training may reference it.

---

## Invocation Prompt

When ready to execute after the agent review, give the agent:

```
Execute plan/BACKLOG_TO_ISSUES_MIGRATION.md in full.
The agent/workflow review is complete — [brief summary of any changes].
Start at Step 0 and work through Step 8.
```

---

## Related Documents

| Document | Role |
|----------|------|
| [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) | Phase sequencing (updated 2026-07-18) |
| [SCOPE.md](SCOPE.md) | Product boundaries |
| [RELEASE_PLAN.md](RELEASE_PLAN.md) | Version strategy |
| [PROJECT_MANAGEMENT.md](PROJECT_MANAGEMENT.md) | Issue/milestone/project conventions |
| [LABEL_STRATEGY.md](LABEL_STRATEGY.md) | Label taxonomy |
| [PM_RUNBOOK.md](PM_RUNBOOK.md) | Daily operating rhythm |
| [PROJECT_BOARD_DESIGN.md](PROJECT_BOARD_DESIGN.md) | Project #8 field definitions |
| `.agents/skills/repo-github-issues/SKILL.md` | Issue creation guidance |
| `.agents/skills/repo-github-project/SKILL.md` | Project board sync guidance |
