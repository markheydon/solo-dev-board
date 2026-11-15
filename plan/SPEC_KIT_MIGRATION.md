# Spec Kit Migration Plan

<!-- AI Collaborator Instructions: Follow the pre–Spec Kit cut-over below before running specify/plan/tasks for new features. Execute the migration checklist when the user requests full Spec Kit adoption after v1.5.0 (or follow Step 3 as a pilot on v1.5 if approved). -->

**Status:** Scaffold present (`.specify/`); **full adoption starts after `v1.5.0`**. Until then, deliver **`v1.4`** and **`v1.5`** via legacy Issues + `plan/` bundles, not Spec Kit.

**Created:** 2026-07-18.

**Purpose:** Migrate SoloDevBoard from repository markdown (`AGENTS.md`, `CONVENTIONS.md`, `plan/SCOPE.md`, `adr/`) toward **GitHub Spec Kit** for product specifications and constitution where that reduces duplication.

---

## Prerequisites

- [x] Active decisions live under `adr/` (former `plan/DECISIONS.md` retired).
- [ ] Constitution stable in [`AGENTS.md`](../AGENTS.md) and [`CONVENTIONS.md`](../CONVENTIONS.md).
- [x] Backlog-to-issues Phase 2 complete ([`plan/archive/BACKLOG_TO_ISSUES_MIGRATION.md`](archive/BACKLOG_TO_ISSUES_MIGRATION.md)).
- [x] `plan/` slimmed for Spec Kit readiness (2026-11-14): Copilot-era bundles and phased plan archived; see [`plan/README.md`](README.md).
- [x] Pre–Spec Kit cut-over agreed (2026-11-15): finish **`v1.4`** and **`v1.5`** on existing GitHub Issue trees before Spec Kit becomes the default for new work.

**Execute the migration checklist below after `v1.5.0` is tagged** (or when the user explicitly approves an earlier pilot, for example constitution import during `v1.5`).

---

## Pre–Spec Kit delivery cut-over (2026-11-15)

| Phase | Milestone | Delivery mode | Product intent |
| --- | --- | --- | --- |
| **Now** | **`v1.4 - Product chrome and narrow viewports`** | Normal Plan mode; **existing** epic [#526](https://github.com/markheydon/solo-dev-board/issues/526) and children ([#527](https://github.com/markheydon/solo-dev-board/issues/527), [#528](https://github.com/markheydon/solo-dev-board/issues/528), [#530](https://github.com/markheydon/solo-dev-board/issues/530), [#411](https://github.com/markheydon/solo-dev-board/issues/411)) | [`product-chrome-ui-quality/`](product-chrome-ui-quality/README.md) bundle + wireframes |
| **Next** | **`v1.5 - Product branding`** (declare when `v1.4` closes) | Same legacy mode; **do not** re-run prompts that split new child issues | Branding [#397](https://github.com/markheydon/solo-dev-board/issues/397), enabler [#529](https://github.com/markheydon/solo-dev-board/issues/529), test [#531](https://github.com/markheydon/solo-dev-board/issues/531) |
| **After `v1.5.0`** | Next promoted feature | **Spec Kit default:** `/speckit-specify` → plan → tasks → implement | `.specify/specs/<feature>/`; at most one tracking issue, not a generated subtree |

### Rules for agents

1. **Do not** run Spec Kit specify/plan/tasks for **`v1.4`** or **`v1.5`** scope unless the user explicitly overrides (for example a post-ship archival spec).
2. **Do not** use Copilot-era planning prompts to mint new Epic → Feature → Story trees for work that already has issues.
3. **Do** use Spec Kit for **new** or **ice-box promoted** capabilities after cut-over, typically a single Feature or Story worth of intent, not prompt-driven decomposition.
4. **Re-archive review:** After **`v1.5.0`**, review active `plan/` feature bundles ([`product-chrome-ui-quality/`](product-chrome-ui-quality/README.md), [`label-remap-taxonomy-project-plan.md`](label-remap-taxonomy-project-plan.md), wireframes) and move superseded prose to [`archive/`](archive/README.md) or into `.specify/specs/` as appropriate. Record outcome in this file.

---

## Target mapping

| Repo memory today | Spec Kit target |
| --- | --- |
| `AGENTS.md` + `CONVENTIONS.md` | `.specify/memory/constitution.md` (+ path rules as needed) |
| `adr/` (product decisions) | Absorbed into constitution and per-feature specs where appropriate |
| `plan/SCOPE.md` + `plan/wireframes/` | `.specify/specs/<feature>/spec.md` |
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

## Execution checklist (when approved)

### Step 1 — Initialise Spec Kit

- [ ] Run `specify init` (or equivalent) at repository root.
- [ ] Add `.specify/` to contributor docs; document CLI prerequisites in `docs/getting-started.md` if needed.

### Step 2 — Import constitution

- [ ] Draft `.specify/memory/constitution.md` from `AGENTS.md`, `CONVENTIONS.md`, and essential `adr/` entries.
- [ ] Slim `AGENTS.md` to pointer: "Constitution: `.specify/memory/constitution.md`" for Spec Kit users, or keep parallel until cut-over is validated.

### Step 3 — Pilot one feature spec

- [ ] Pick the first **post–`v1.5.0`** promoted feature (or an explicitly approved pilot).
- [ ] Create `.specify/specs/<feature>/spec.md` from `plan/SCOPE.md`, wireframes, or a slim issue body.
- [ ] Run Spec Kit plan/implement flow once end-to-end; capture gaps in this doc.
- [ ] Optional: optional Spec Kit pilot **during `v1.5`** for branding only if the user wants validation before cut-over; still close #397/#529/#531 on Issues.

### Step 4 — Update agent gates

- [ ] Update [`AGENTS.md`](../AGENTS.md) and skills to reference Spec Kit for new feature work.
- [ ] Update [`plan/DOCS_STRATEGY.md`](DOCS_STRATEGY.md).

### Step 5 — Retire duplicate planning prose

- [ ] Mark migrated sections of `plan/SCOPE.md` as pointers to specs.

### Step 6 — Validation

- [ ] One feature delivered via Spec Kit only (spec → plan → implement).
- [ ] No duplicate decision files outside Spec Kit and `adr/` for the same rule.

---

## Out of scope for Spec Kit migration

- GitHub Project board automation (`.github/workflows/roadmap-sync.yml`).
- End-user Hugo site in `website/`.
- **Active `adr/`** remains the audit trail for architecture unless explicitly absorbed into Spec Kit.

---

## Invocation

When ready to execute, give the agent:

```text
Execute plan/SPEC_KIT_MIGRATION.md — [any constraints, e.g. pilot feature name].
```