# Backlog-to-Issues Migration Plan

<!-- AI Collaborator Instructions: This document is a parked execution plan. Do not begin migration until the agent/workflow review for Cursor is complete. When the user says "execute the backlog migration" or references this file for implementation, follow the steps in order and tick off the checklist as you go. -->

**Status:** Parked — Phase 1 governance neutralisation complete; behavioural migration (issues as source of truth) remains Phase 2.

**Created:** 2026-07-18.

**Purpose:** Migrate SoloDevBoard from a dual-source planning model (`plan/BACKLOG.md` + GitHub Issues) to **GitHub Issues as the single source of truth for work items**, while keeping markdown files for scope, sequencing, and process.

---

## Prerequisite

**Phase 1 (complete):** Governance consolidated into `AGENTS.md`, role contracts relocated to `.agents/contracts/`, and cross-references updated for tool-agnostic AI collaboration.

**Phase 2 (this plan):** Execute the behavioural migration so that:

- "Add to backlog" means **create or update a GitHub Issue** and sync the project board.
- Role contracts do not continue writing open work items to `plan/BACKLOG.md`.
- All AI tools share the same canonical workflow via `AGENTS.md` and `.agents/contracts/`.

**Do not execute Steps 1–8 below until Phase 2 is explicitly approved.**

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

- [ ] Agent/workflow review for Cursor is complete and signed off.
- [ ] Confirm `gh` CLI is authenticated and can create issues and update Project #8.
- [ ] Run `gh issue list --state open --limit 200` and `gh api repos/markheydon/solo-dev-board/milestones` to capture current issue/milestone baseline.
- [ ] Note the highest existing issue number to avoid duplicate creation.

### Step 1 — Audit existing issues against open backlog

- [ ] For each item in the **Current Open Work Inventory** above, check whether a GitHub Issue already exists (search by title, `#106`, `#107`, `#108`, etc.).
- [ ] Record a mapping table: `BACKLOG item → Issue #N (exists | create new)`.
- [ ] Close or update any stale issues that are already done but still open.
- [ ] Do **not** retroactively create issues for completed Phases 1–4 work.

### Step 2 — Create v1.0.0 issue hierarchy

- [ ] Create or confirm parent epic: `[Epic] v1.0.0 — Production Ready` (or use milestone #4 as the grouping mechanism).
- [ ] Create missing Phase 6 issues from the inventory (items without existing issue numbers).
- [ ] Apply labels per `plan/LABEL_STRATEGY.md` (at minimum `type/` + `priority/` + `area/`).
- [ ] Assign all v1.0.0 issues to milestone `v1.0.0 — Production Ready`.
- [ ] Link child issues to parent epic in issue bodies (`Part of #<epic>`).
- [ ] Add all issues to GitHub Project #8 with `status/todo`.

### Step 3 — Create parked Phase 5 issues

- [ ] Create `[Epic] Cross-Repo PM Workflow (Phase 5)` parent issue.
- [ ] Create 16 story issues from Epic 7 backlog entries.
- [ ] Assign milestone `v0.5.0 — Cross-Repo PM Workflow` (milestone #6).
- [ ] Set `status/todo` on all; do **not** place in Up Next.
- [ ] Add to Project #8.

### Step 4 — Create post-v1 improvement issues

- [ ] Create `[Epic] Post-v1 Improvements` parent issue.
- [ ] Create four deferred improvement issues (see inventory).
- [ ] Apply `priority/low`; leave **unmilestoned**.
- [ ] Mark the blocked Projects v2 item with `status/blocked`.
- [ ] Add to Project #8.

### Step 5 — Retire `plan/BACKLOG.md` as a living backlog

Choose **Option A** (recommended) or **Option B**:

**Option A — Slim index (recommended):**

- [ ] Replace `plan/BACKLOG.md` body with:
  - Brief statement that GitHub Issues are the source of truth.
  - Roadmap status table (copy from current header).
  - Links to: GitHub Project #8, open milestones, key epics.
  - A "Completed phases" section listing what was delivered (no open checkboxes).
  - Pointer to this migration doc for history.

**Option B — Archive:**

- [ ] Move current `plan/BACKLOG.md` to `plan/archive/BACKLOG-2026-07-18.md`.
- [ ] Replace `plan/BACKLOG.md` with a short redirect to GitHub Issues/Project.

### Step 6 — Update planning and agent artefacts

Update every file that references `BACKLOG.md` as a living work queue. At minimum:

| File | Change required |
|------|-----------------|
| `AGENTS.md` | Replace "Update `plan/BACKLOG.md`" with "Create/update GitHub Issue + sync Project #8". |
| `CONTRIBUTING.md` | Point contributors to GitHub Issues/Project, not BACKLOG.md. |
| `README.md` | Update plan/ directory description. |
| `plan/IMPLEMENTATION_PLAN.md` | Open tasks reference issue numbers; remove "see BACKLOG.md" for work items. |
| `plan/RELEASE_PLAN.md` | Release notes pull from closed GitHub Issues/milestone, not BACKLOG.md. |
| `plan/PM_RUNBOOK.md` | Daily/weekly rituals query GitHub Issues/Project, not BACKLOG.md. |
| `plan/PROJECT_MANAGEMENT.md` | Confirm Issues as canonical; note BACKLOG.md retirement. |
| `.agents/skills/repo-pm-feature-workflow/SKILL.md` | Step 1: select from GitHub Issues/Project, not BACKLOG.md. |
| `.agents/skills/_REGISTRY.md` | Update workflow description. |
| `.agents/skills/breakdown-plan/SKILL.md` | Output is GitHub Issues, not BACKLOG.md entries. |
| `.agents/skills/breakdown-test/SKILL.md` | Link test issues to parent feature issues. |
| `.agents/contracts/pm-orchestrator.md` | Read Project/Issues, not BACKLOG.md. |
| `.agents/contracts/delivery.md` | Remove BACKLOG.md sync; issue closure is sufficient. |
| `.agents/contracts/verify.md` | Confirm issue labels/milestone on PR, not BACKLOG.md. |
| `.agents/workflows/daily-start.md` | Query `gh issue list` / Project #8. |
| `.agents/workflows/plan-next-issue.md` | Select from open issues or create new. |
| `.agents/workflows/weekly-pm-review.md` | Milestone/issue health, not BACKLOG.md. |
| `.agents/workflows/implement-issue.md` | Aligned with unified workflow. |

**Note:** The agent/workflow review may add, remove, or restructure files in this table. Reconcile this list against the post-review agent layout before executing Step 6.

### Step 7 — Verify project board hygiene

- [ ] All new issues appear on Project #8.
- [ ] v1.0.0 issues have correct Phase/Priority/Status fields per `plan/PROJECT_BOARD_DESIGN.md`.
- [ ] Phase 5 issues are on the board but not in Up Next.
- [ ] Post-v1 issues are on the board with no milestone.
- [ ] Run roadmap-sync workflow manually if needed (`.github/workflows/roadmap-sync.yml`).
- [ ] Close completed milestones (v0.1.0–v0.4.0) on GitHub if still open.

### Step 8 — Validation and sign-off

- [ ] `gh issue list --state open` returns all expected open work; no orphaned BACKLOG.md checkboxes remain for open items.
- [ ] A new contributor can find open work via README → Project #8 without reading BACKLOG.md.
- [ ] Agent smoke test: run daily-start equivalent and confirm it reads from Issues/Project.
- [ ] Agent smoke test: run plan-next-issue equivalent and confirm it creates/updates Issues, not BACKLOG.md.
- [ ] Update `plan/SCOPE.md` changelog with migration completion entry.
- [ ] Delete or archive this file's "Parked" status — mark **Complete** with date.

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
