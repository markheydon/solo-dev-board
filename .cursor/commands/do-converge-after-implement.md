# Spec Kit converge loop (after implement)

Automate the optional **converge** pass after implementation: compare the codebase to the feature’s `spec.md`, `plan.md`, and `tasks.md`, append any remaining work to `tasks.md`, then implement that work until converge reports clean—or you hit the loop cap.

Honor any extra user message in this chat as `$ARGUMENTS` to the converge skill (scope or focus areas).

## When to run (workflow gate)

Run this command **only** after an implement pass, when you choose to use converge:

```text
/speckit-constitution → /speckit-specify → /speckit-plan → /speckit-tasks → (optional) /speckit-analyze → /speckit-implement → (optional) **this command**
```

**Required before starting**

- `tasks.md` exists for the active feature and `/speckit-implement` has already been run against the current task list (see [do-implement.md](./do-implement.md)). Converge assesses **present code vs artifacts**; it is not a substitute for implement.
- From the repo root, prerequisites succeed:

  ```shell
  .specify/scripts/bash/check-prerequisites.sh --json --require-spec --require-tasks --include-tasks
  ```

  Parse `FEATURE_DIR` from the JSON. Abort with clear next steps if any required artifact is missing.

**Do not run this command when**

- `tasks.md` does not exist yet — run `/speckit-tasks` first.
- You have not implemented the feature yet — run [do-implement.md](./do-implement.md) (`/speckit-implement`) first.
- The goal is only to cross-check artifacts without reading code — use [do-analyse-after-tasks.md](./do-analyse-after-tasks.md) (`/speckit-analyze`) before implement instead.
- The goal is PR merge-readiness review of a diff — use [loop-code-review.md](./loop-code-review.md).

Do **not** edit `.specify/` or Spec Kit skill files unless the user explicitly asks (see [AGENTS.md](../../AGENTS.md)).

## Your job

Repeat this cycle until converge reports **converged** (no new tasks appended) or you hit the cap:

1. **Converge** — In a **new subagent** (Task), read and follow [.cursor/skills/speckit-converge/SKILL.md](../skills/speckit-converge/SKILL.md) (same as `/speckit-converge`). The subagent may **only** append new `## Phase N: Convergence` tasks to `FEATURE_DIR/tasks.md` per the skill; it must not change `spec.md`, `plan.md`, existing tasks, or application code. Return the findings summary and whether the outcome was `converged` or `tasks_appended`.
2. **Implement** — In this session, when the outcome was `tasks_appended`, complete every new unchecked task (typically the latest Convergence phase). Follow [.cursor/skills/speckit-implement/SKILL.md](../skills/speckit-implement/SKILL.md) for execution rules, mark items `[X]` in `tasks.md`, and respect phase dependencies. Delegate `[P]` tasks to subagents when they do not conflict on the same files, as in [do-implement.md](./do-implement.md).
3. **Verify** — From the repo root, run per [AGENTS.md](../../AGENTS.md):

   ```shell
   dotnet build SoloDevBoard.slnx -c Release -warnaserror
   dotnet test SoloDevBoard.UnitTests.slnf -c Release --no-build
   ```

   Fix failures before the next converge loop.

When the outcome was `converged`, skip step 2 and finish.

Invoking this command is explicit permission to **commit** implementation work on the feature branch when a loop produced code changes (messages should describe the convergence tasks addressed).

Start the next loop only after steps 2–3 are done (or step 1 reported `converged`).

## Stop condition

- **All clear** — `/speckit-converge` reports **converged** (no append to `tasks.md`). Summarise what was checked and suggest [loop-code-review.md](./loop-code-review.md) or PR polish if the branch is not merge-ready yet.
- **Cap** — After **6** complete converge cycles (converge → implement when needed → verify), stop. Summarise remaining gaps, what you implemented, and whether another loop is worthwhile or accepting the current state is reasonable.

## Isolation

Run **each converge pass in its own subagent** so assessment context does not leak across loops. Task implementation, build/test, and commits stay in the parent session so fixes land once and the next converge sees the updated code and `tasks.md`.