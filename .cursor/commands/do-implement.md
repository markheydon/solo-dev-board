# Spec Kit implement

Run `/speckit-implement` for the active feature with repo-specific branch, parallelism, verification, and PR conventions—so you do not have to repeat the same instructions each time.

Honor any extra user message in this chat as `$ARGUMENTS` to the implement skill (scope, priorities, or constraints).

## When to run (workflow gate)

Run this command after `/speckit-tasks` when you are ready to write product code. Analyze and converge are **optional** in the usual Spec Kit flow; this command does not require them.

```text
/speckit-constitution → /speckit-specify → /speckit-plan → /speckit-tasks → (optional) /speckit-analyze → **this command** → (optional) /speckit-converge
```

**Required before starting**

- `tasks.md` exists for the active feature (from `/speckit-tasks`). This is the hard gate; do not run implement without it.
- If you ran `/speckit-analyze` or [do-analyse-after-tasks.md](./do-analyse-after-tasks.md) for this feature, do not start until **CRITICAL** and **HIGH** findings are resolved or explicitly accepted by the user.
- From the repo root, prerequisites succeed:

  ```shell
  .specify/scripts/bash/check-prerequisites.sh --json --require-tasks --include-tasks
  ```

  Parse `FEATURE_DIR` from the JSON. Read `FEATURE_DIR/spec.md` for the **Feature Branch** name and any issue/milestone hints.

**Do not run this command when**

- `tasks.md` is missing or incomplete — run `/speckit-tasks` first.
- The goal is only to edit `spec.md` / `plan.md` / `tasks.md` — use [do-analyse-after-tasks.md](./do-analyse-after-tasks.md) (optional) or the relevant `/speckit-*` step instead.
- The goal is only post-implementation gap analysis — use `/speckit-converge` (optional); that is separate from this command.

Do **not** edit `.specify/` or Spec Kit skill files unless the user explicitly asks (see [AGENTS.md](../../AGENTS.md)).

## Your job

Read and follow [.cursor/skills/speckit-implement/SKILL.md](../skills/speckit-implement/SKILL.md) end to end (checklists, task phases, marking `[X]` in `tasks.md`, hooks, and completion report). The sections below are **repository additions** on top of that skill; where they conflict, these repo rules win for branch/PR/build behaviour.

### Feature branch and clean starting point

1. If the current branch is `main` (or the repository default branch), create and check out the feature branch named in `spec.md` (for example `001-feature-slug`).
2. Commit or stash any outstanding work so implementation starts from a clean, feature-branch baseline. Invoking this command is explicit permission to **commit** preparatory changes on the feature branch when needed (messages should describe the prep work, not implementation yet).

### Implementation execution

1. Execute `tasks.md` per the skill: phase order, dependencies, TDD where tasks require it, and mark completed items `[X]` in `FEATURE_DIR/tasks.md`.
2. **Parallel tasks** — For each task marked `[P]` that can run without conflicting on the same files, delegate work to a **new subagent** (Task). Sequential tasks and tasks that touch the same files stay in this session (or one subagent at a time) to avoid merge conflicts.
3. After meaningful batches of work (and before declaring done), from the repo root run per [AGENTS.md](../../AGENTS.md):

   ```shell
   dotnet build SoloDevBoard.slnx -c Release -warnaserror
   dotnet test SoloDevBoard.UnitTests.slnf -c Release --no-build
   ```

   Fix failures before continuing or opening/updating a PR.

### Finish: PR or push

When implementation is complete per the skill’s completion criteria:

- If no PR exists for this branch, open one using [plan/PULL_REQUEST_POLICY.md](../../plan/PULL_REQUEST_POLICY.md) and [`.github/PULL_REQUEST_TEMPLATE.md`](../../.github/PULL_REQUEST_TEMPLATE.md): title, labels, linked issue (from the feature issue or `spec.md` if present), and milestone when applicable.
- If a PR already exists, commit and push remaining work to that branch so CI can run.

Invoking this command is explicit permission to **commit and push** implementation work on the feature branch.

For merge readiness after implementation, the user may run [loop-code-review.md](./loop-code-review.md) separately; do not substitute that loop unless asked.

## Stop condition

- **Done** — All required tasks in `tasks.md` are `[X]`, build/test pass, skill completion report delivered, and branch is pushed with a PR opened or updated as above. Optionally run [do-converge-after-implement.md](./do-converge-after-implement.md) to close spec/plan vs code gaps before review.
- **Blocked** — Halt per the skill (checklist gate, failed task, missing prerequisite) and report what the user must decide or which `/speckit-*` step to rerun.