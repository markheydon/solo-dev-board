# Spec Kit Migration Plan

<!-- AI Collaborator Instructions: Use Spec Kit for new feature work. Legacy v1.4 delivery is complete; shipped bundles live under plan/archive/. -->

**Status:** **Cut-over active** (2026-11-15, with [`v1.4.0`](https://github.com/markheydon/solo-dev-board/releases/tag/v1.4.0)). New capabilities use **Spec Kit** by default. Scaffold: `.specify/`, feature dirs: `specs/<feature>/`.

**Created:** 2026-07-18.

**Purpose:** Migrate SoloDevBoard from repository markdown (`AGENTS.md`, `CONVENTIONS.md`, `plan/SCOPE.md`, `adr/`) toward **GitHub Spec Kit** for product specifications and constitution where that reduces duplication.

---

## Prerequisites

- [x] Active decisions live under `adr/` (former `plan/DECISIONS.md` retired).
- [ ] Constitution stable in [`AGENTS.md`](../AGENTS.md) and [`CONVENTIONS.md`](../CONVENTIONS.md) (parallel `.specify/memory/constitution.md` exists; slimming `AGENTS.md` deferred).
- [x] Backlog-to-issues Phase 2 complete ([`plan/archive/BACKLOG_TO_ISSUES_MIGRATION.md`](archive/BACKLOG_TO_ISSUES_MIGRATION.md)).
- [x] `plan/` slimmed for Spec Kit readiness (2026-11-14): Copilot-era bundles and phased plan archived; see [`plan/README.md`](README.md).
- [x] Pre–Spec Kit cut-over agreed (2026-11-15): finish **`v1.4`**, then adopt Spec Kit for new work.
- [x] **`v1.4.0` tagged** (2026-11-15); re-archive review recorded below.

---

## Pre–Spec Kit delivery cut-over (2026-11-15)

| Phase | Milestone | Delivery mode | Product intent |
| --- | --- | --- | --- |
| **Complete** | **`v1.4 - Product chrome, branding, and narrow viewports`** | Legacy Issues + `plan/archive/product-chrome-ui-quality/` | Shipped [`v1.4.0`](https://github.com/markheydon/solo-dev-board/releases/tag/v1.4.0) |
| **Now** | Next promoted feature | **Spec Kit default:** `/speckit-specify` → plan → tasks → implement | `specs/<feature>/`; at most one tracking issue, not a generated subtree |

There is no `v1.5` milestone. Do not declare one for branding.

### Rules for agents

1. **Do** use Spec Kit for **new** or **ice-box promoted** capabilities.
2. **Do not** use Copilot-era planning prompts to mint new Epic → Feature → Story trees.
3. **Do not** maintain duplicate feature prose in `plan/SCOPE.md` and a spec for the same capability.
4. **Historical `v1.4`:** do not re-open Spec Kit specify/plan/tasks for chrome/branding unless the user explicitly requests a post-ship archival spec.

### Re-archive review (2026-11-15, with `v1.4.0`)

| Artefact | Outcome |
| --- | --- |
| [`archive/product-chrome-ui-quality/`](archive/product-chrome-ui-quality/README.md) | Archived (shipped `v1.4.0`) |
| [`archive/label-remap-taxonomy-project-plan.md`](archive/label-remap-taxonomy-project-plan.md) | Archived (shipped `v1.3.0`) |
| [`wireframes/`](wireframes/) | Retained as implemented UI references |
| New specs under `specs/` | Deferred until first post-`v1.4.0` feature pilot |

---

## Target mapping

| Repo memory today | Spec Kit target |
| --- | --- |
| `AGENTS.md` + `CONVENTIONS.md` | `.specify/memory/constitution.md` (+ path rules as needed) |
| `adr/` (product decisions) | Absorbed into constitution and per-feature specs where appropriate |
| `plan/SCOPE.md` + `plan/wireframes/` | `specs/<feature>/spec.md` |
| `plan/archive/IMPLEMENTATION_PLAN.md` (retired) | Spec Kit plan artefacts per feature |
| `.agents/skills/` | Stays outside Spec Kit (agent orchestration) |
| `docs/` (operator guides) | Stays end-user facing; link from specs where relevant |

---

## Agreed principles

1. **Spec Kit owns product intent** — what to build and why, per feature.
2. **Constitution owns non-negotiable rules** — architecture, testing bans, deploy model.
3. **Agent skills stay in `.agents/`** — Spec Kit does not replace Cursor skills.
4. **No dual maintenance** — after migration, do not update both `plan/SCOPE.md` feature prose and a spec for the same capability.

---

## Execution checklist

### Step 1 — Initialise Spec Kit

- [x] Scaffold present (`.specify/` on `main`, PR #590).
- [x] Contributor pointers in [`docs/getting-started.md`](../docs/getting-started.md) and [`docs/README.md`](../docs/README.md).
- [x] Maintainer pointers in [`plan/README.md`](README.md), [`plan/DOCS_STRATEGY.md`](DOCS_STRATEGY.md).

### Step 2 — Import constitution

- [x] `.specify/memory/constitution.md` drafted (PR #590).
- [ ] Slim `AGENTS.md` to a pointer-only constitution gate — **deferred** (explicit follow-up).

### Step 3 — Pilot one feature spec

- [ ] First **post–`v1.4.0`** promoted feature — **deferred** until backlog promotion.

### Step 4 — Update agent gates

- [ ] Update [`AGENTS.md`](../AGENTS.md) and skills — **deferred** (out of scope for release doc PR).
- [x] Update [`plan/DOCS_STRATEGY.md`](DOCS_STRATEGY.md).

### Step 5 — Retire duplicate planning prose

- [x] Shipped increments archived under `plan/archive/`; [`plan/SCOPE.md`](SCOPE.md) keeps boundaries only.

### Step 6 — Validation

- [ ] One feature delivered via Spec Kit only — **deferred** (pilot feature).

---

## Out of scope for Spec Kit migration

- GitHub Project board automation (`.github/workflows/roadmap-sync.yml`).
- End-user Hugo site in `website/`.
- **Active `adr/`** remains the audit trail for architecture unless explicitly absorbed into Spec Kit.

---

## Invocation

When ready to execute the remaining checklist (pilot feature, `AGENTS.md` slimming):

```text
Execute plan/SPEC_KIT_MIGRATION.md — [constraints, e.g. pilot feature name].
```