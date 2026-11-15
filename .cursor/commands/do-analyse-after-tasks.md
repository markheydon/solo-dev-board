# Spec Kit analyze loop (after tasks)

Automate the optional read-only **analyze** gate: cross-check `spec.md`, `plan.md`, and `tasks.md` for the active feature, then fix paperwork only until the audit is clean enough to start implementation. You do not have to run analyze on every feature; invoke this command when you want that gate.

## When to run (workflow gate)

Run this command **only** after task generation and **before** implementation (when you choose to use analyze):

```text
/speckit-constitution → /speckit-specify → /speckit-plan → /speckit-tasks → (optional) **this command** → /speckit-implement → (optional) /speckit-converge
```

**Required before starting**

- `/speckit-tasks` has produced a complete `tasks.md` for the current feature (see [AGENTS.md](../../AGENTS.md); active feature directory from `.specify/feature.json` or `check-prerequisites.sh` output).
- From the repo root, prerequisites succeed:

  ```shell
  .specify/scripts/bash/check-prerequisites.sh --json --require-spec --require-tasks --include-tasks
  ```

  Parse `FEATURE_DIR` from the JSON. Abort with clear next steps if any required artifact is missing (run the missing `/speckit-*` step first).

**Do not run this command when**

- `tasks.md` does not exist yet — run `/speckit-tasks` first.
- You are already executing or finishing `/speckit-implement` for this feature — analyze is a **pre-implementation** artifact audit, not a code review or `/speckit-converge` pass.
- The goal is to change application code under `src/`, `tests/`, or AppHost projects — use `/speckit-implement` (and [loop-code-review.md](./loop-code-review.md) later for code).

Constitution conflicts are **CRITICAL** per [.specify/memory/constitution.md](../../.specify/memory/constitution.md); resolve them in spec/plan/tasks (or an explicit constitution update), never by weakening principles in passing.

## Your job

Repeat this cycle until the analyze pass reports no **CRITICAL** or **HIGH** issues (or you hit the cap):

1. **Analyze** — In a **new subagent** (Task), read and follow [.cursor/skills/speckit-analyze/SKILL.md](../skills/speckit-analyze/SKILL.md) (same as `/speckit-analyze`). The subagent must produce the structured analysis report only; it must **not** modify any files.
2. **Remediate** — In this session, fix every finding you agree with by editing **only** artifacts under `FEATURE_DIR` (`spec.md`, `plan.md`, `tasks.md`, and other files that step owns). Route work to the owning Spec Kit step instead of improvising:
   - Requirements gaps, ambiguity, or story problems → `/speckit-specify` or `/speckit-clarify`
   - Architecture, data model, or plan contradictions → `/speckit-plan`
   - Task coverage, ordering, or task-list structure → `/speckit-tasks` (or targeted edits to `tasks.md` when appropriate)
3. **Re-check** — After remediation, ensure the three core artifacts still exist and remain internally consistent before starting the next loop.

Start the next loop only after step 2 is done (or step 1 reported no significant issues).

This is a **tasks and paperwork gap-filling** exercise only. **Do not** implement product code, change `SoloDevBoard.slnx` projects, or run build/test gates meant for implementation.

Do **not** edit `.specify/` or Spec Kit skill files unless the user explicitly asks (see [AGENTS.md](../../AGENTS.md)).

## Stop condition

- **All clear** — `/speckit-analyze` reports no CRITICAL or HIGH issues; only LOW/MEDIUM remain that you choose not to block on. Summarise coverage metrics and tell the user they may proceed with [do-implement.md](./do-implement.md) (`/speckit-implement`).
- **Cap** — After **8** complete analyze cycles, stop. Summarise remaining findings, what you fixed in artifacts, and whether another loop is worthwhile or accepting the current state is reasonable before implement.

## Isolation

Run **each analyze cycle in its own subagent** so audit context does not leak across loops. Artifact remediation stays in the parent session so each fix is applied once and the next analyze sees the updated files.