# Spec Kit Migration Plan

<!-- AI Collaborator Instructions: This document is a parked execution plan. Do not run specify init or create .specify/ until the user explicitly requests Spec Kit migration. When the user says "execute the Spec Kit migration" or references this file for implementation, follow the steps in order. -->

**Status:** Parked — decision log migration complete; Spec Kit scaffold and adoption remain future work.

**Created:** 2026-07-18.

**Purpose:** Migrate SoloDevBoard from the repo memory model (`AGENTS.md`, `plan/DECISIONS.md`, planning markdown) to **GitHub Spec Kit** for product specifications and constitution, while keeping PM/agent orchestration artefacts outside Spec Kit.

---

## Prerequisites

- [ ] [`plan/DECISIONS.md`](DECISIONS.md) migration complete (ADR archive + decision log).
- [ ] Constitution stable in [`AGENTS.md`](../AGENTS.md) and [`.github/instructions/`](../.github/instructions/).
- [ ] Backlog-to-issues Phase 2 complete or explicitly deferred ([`plan/BACKLOG_TO_ISSUES_MIGRATION.md`](BACKLOG_TO_ISSUES_MIGRATION.md)).

**Do not execute the steps below until this plan is explicitly approved.**

---

## Target mapping

| Repo memory today | Spec Kit target |
|-------------------|-----------------|
| `AGENTS.md` + `.github/instructions/` | `.specify/memory/constitution.md` (+ path rules as needed) |
| `plan/DECISIONS.md` (active entries) | Absorbed into constitution and per-feature specs |
| `plan/SCOPE.md` + `plan/wireframes/` | `.specify/specs/<feature>/spec.md` |
| `plan/IMPLEMENTATION_PLAN.md` | Spec Kit plan artefacts per feature |
| `adr/archive/` | Remains read-only; **not** ported to Spec Kit |
| `plan/PM_RUNBOOK.md`, `.agents/` | Stays outside Spec Kit (operator and AI orchestration) |
| `docs/` (user guides) | Stays end-user facing; link from specs where relevant |

---

## Agreed principles

1. **Spec Kit owns product intent** — what to build and why, per feature.
2. **Constitution owns non-negotiable rules** — architecture, testing bans, deploy model.
3. **PM runbook and agent contracts stay in `.agents/` and `plan/`** — Spec Kit does not replace daily delivery rituals.
4. **No dual maintenance** — after migration, do not update both `plan/SCOPE.md` feature prose and a spec for the same capability.

---

## Execution checklist (when approved)

### Step 1 — Initialise Spec Kit

- [ ] Run `specify init` (or equivalent) at repository root.
- [ ] Add `.specify/` to contributor docs; document CLI prerequisites in `docs/getting-started.md` if needed.

### Step 2 — Import constitution

- [ ] Draft `.specify/memory/constitution.md` from `AGENTS.md` architecture, testing, UK English, and security sections plus `.github/instructions/` essentials.
- [ ] Slim `AGENTS.md` to pointer: "Constitution: `.specify/memory/constitution.md`" for Spec Kit users, or keep parallel until cut-over is validated.

### Step 3 — Pilot one feature spec

- [ ] Pick a small in-scope feature (or next greenfield story).
- [ ] Create `.specify/specs/<feature>/spec.md` from existing wireframe + `plan/SCOPE.md` excerpt.
- [ ] Run Spec Kit plan/implement flow once end-to-end; capture gaps in this doc.

### Step 4 — Update agent gates

- [ ] Replace `repo-decision-log` routing for **feature-scoped** decisions with Spec Kit spec updates.
- [ ] Update [`AGENTS.md`](../AGENTS.md), [`repo-decision-log`](../.agents/skills/repo-decision-log/SKILL.md), and contracts to reference Spec Kit for new feature work.
- [ ] Update [`plan/DOCS_STRATEGY.md`](DOCS_STRATEGY.md).

### Step 5 — Retire duplicate planning prose

- [ ] Mark migrated sections of `plan/SCOPE.md` as pointers to specs.
- [ ] Freeze new entries in `plan/DECISIONS.md` except constitution-level changes until fully absorbed.

### Step 6 — Validation

- [ ] One feature delivered via Spec Kit only (spec → plan → implement).
- [ ] Agent workflows (`implement-issue`, `plan-next-issue`) reference Spec Kit paths.
- [ ] No new ADR or duplicate decision files created.

---

## Out of scope for Spec Kit migration

- GitHub Project board operations (`repo-github-project` skill).
- Verify / code-review / weekly PM workflows.
- End-user Jekyll docs in `docs/`.
- ADR archive in `adr/archive/`.

---

## Invocation

When ready to execute, give the agent:

```text
Execute plan/SPEC_KIT_MIGRATION.md — [any constraints, e.g. pilot feature name].
```
