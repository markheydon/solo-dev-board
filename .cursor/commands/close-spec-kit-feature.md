# Spec Kit feature close-out (after delivery)

Mark the active Spec Kit increment as **delivered** in `spec.md` when implementation and optional converge are complete, without rewriting delivery artefacts or moving files. Report remaining documentation and ADR gaps for the maintainer.

Honor any extra user message in this chat as scope (for example a different feature directory); default to the active feature from prerequisites.

## When to run (workflow gate)

Run this command when you choose to close out a feature increment after implement (and converge, if used):

```text
/speckit-constitution → /speckit-specify → /speckit-plan → /speckit-tasks
  → (optional) do-analyse-after-tasks → do-implement → (optional) do-converge-after-implement
  → **this command** → (optional) loop-code-review
```

**Required before starting**

- `tasks.md` and `spec.md` exist for the active feature.
- From the repo root, prerequisites succeed:

  ```shell
  .specify/scripts/bash/check-prerequisites.sh --json --require-spec --require-tasks --include-tasks
  ```

  Parse `FEATURE_DIR` from the JSON. Abort with clear next steps if any required artefact is missing.

- Policy for what to retain and maintain lives in [`.specify/memory/constitution.md`](../../.specify/memory/constitution.md) (Delivery Workflow: Post-delivery persistence). Follow that section; do not invent a second persistence policy in this command.

**Do not run this command when**

- `tasks.md` does not exist yet — run `/speckit-tasks` first.
- Implementation is not finished — run [do-implement.md](./do-implement.md) first.
- The goal is gap analysis against code — run [do-converge-after-implement.md](./do-converge-after-implement.md) or `/speckit-converge` first.
- The goal is PR merge-readiness review — use [loop-code-review.md](./loop-code-review.md).

Do **not** edit `.specify/` or Spec Kit skill files unless the user explicitly asks (see [AGENTS.md](../../AGENTS.md)).

## Your job

1. **Task gate** — In `FEATURE_DIR/tasks.md`, find every unchecked task (`- [ ]`). If any remain, **stop**. List each unchecked task with its ID and description. Do **not** change `spec.md` status or treat the increment as delivered.

2. **Delivered status** — When all tasks are checked (`[X]` or `[x]`):
   - Update **`FEATURE_DIR/spec.md` only**: set **Status** to a delivered value (for example `Delivered` with the ship or merge date if the user supplied one). Do not remove or rewrite user scenarios, requirements, or acceptance criteria.
   - Do **not** edit `plan.md`, `tasks.md`, `research.md`, `quickstart.md`, checklists, `data-model.md`, or `contracts/` to match the code as part of close-out.
   - Do **not** move, rename, delete, or gitignore any file under `FEATURE_DIR`.

3. **Report gaps (do not auto-fix unless the user asks in this chat)**:
   - **User guide and screenshots** — Per [plan/DOCS_STRATEGY.md](../../plan/DOCS_STRATEGY.md) and constitution Principle III, note whether `website/content/docs/` and `tests/SoloDevBoard.E2E.Tests/USER_DOCS_ALIGNMENT.md` appear satisfied for operator-visible behaviour from this feature.
   - **Binding ADR** — If material architecture from `research.md` or `plan.md` should be in `adr/` but is not yet accepted there, name the gap and suggest the ADR skill; do not draft an ADR unless the user asks.

4. **Next steps** — Summarise what you changed in `spec.md`. Suggest [loop-code-review.md](./loop-code-review.md) if the branch may not be merge-ready. Do **not** open or update a PR unless the user explicitly asked in this chat.

Invoking this command is explicit permission to **commit** the `spec.md` status update on the feature branch when step 2 ran (message should describe close-out, not implementation).

## Stop condition

- **Blocked** — Unchecked tasks remain; no `spec.md` status change.
- **Done** — All tasks complete; `spec.md` marked delivered; gap report delivered; optional commit as above.