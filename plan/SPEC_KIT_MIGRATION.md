# Spec Kit Migration Plan

<!-- AI Collaborator Instructions: This document is a parked execution plan. Do not run specify init or create .specify/ until the user explicitly requests Spec Kit migration. When the user says "execute the Spec Kit migration" or references this file for implementation, follow the steps in order. -->

**Status:** Parked — ADR migration complete (2026-11-14); Spec Kit scaffold and adoption remain future work.

**Created:** 2026-07-18.

**Purpose:** Migrate SoloDevBoard from repository markdown (`AGENTS.md`, `CONVENTIONS.md`, `plan/SCOPE.md`, `adr/`) toward **GitHub Spec Kit** for product specifications and constitution where that reduces duplication.

---

## Prerequisites

- [x] Active decisions live under `adr/` (former `plan/DECISIONS.md` retired).
- [ ] Constitution stable in [`AGENTS.md`](../AGENTS.md) and [`CONVENTIONS.md`](../CONVENTIONS.md).
- [ ] Backlog-to-issues Phase 2 complete or explicitly deferred ([`plan/BACKLOG_TO_ISSUES_MIGRATION.md`](BACKLOG_TO_ISSUES_MIGRATION.md)).

**Do not execute the steps below until this plan is explicitly approved.**

---

## Target mapping

| Repo memory today | Spec Kit target |
| --- | --- |
| `AGENTS.md` + `CONVENTIONS.md` | `.specify/memory/constitution.md` (+ path rules as needed) |
| `adr/` (product decisions) | Absorbed into constitution and per-feature specs where appropriate |
| `plan/SCOPE.md` + `plan/wireframes/` | `.specify/specs/<feature>/spec.md` |
| `plan/IMPLEMENTATION_PLAN.md` | Spec Kit plan artefacts per feature |
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

- [ ] Pick a small in-scope feature (or next greenfield story).
- [ ] Create `.specify/specs/<feature>/spec.md` from existing wireframe + `plan/SCOPE.md` excerpt.
- [ ] Run Spec Kit plan/implement flow once end-to-end; capture gaps in this doc.

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