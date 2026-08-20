# Verify and Create PR

**Contract:** [`.agents/contracts/verify.md`](../contracts/verify.md)
**Runbook:** [`plan/PM_RUNBOOK.md`](../../plan/PM_RUNBOOK.md) — Stage 3: Verify and PR Creation

## Easy-to-miss specifics

- Follow [`plan/PULL_REQUEST_POLICY.md`](../../plan/PULL_REQUEST_POLICY.md) for title, body, labels, linking, draft state, assignee, and milestone.
- Use `gh pr create --fill` so the repository PR template is applied; do not bypass with a custom `--body` unless the platform cannot apply the template — then copy the template headings into the body.
- Always use the repo's label strategy for the relevant labels. See the [label strategy](../../plan/LABEL_STRATEGY.md).
- Test gate: `dotnet clean SoloDevBoard.slnx && dotnet test SoloDevBoard.slnx` before PR creation.
- Package gate: `dotnet list SoloDevBoard.slnx package --outdated` before PR creation. Escalate to Delivery if a direct `PackageReference` added or bumped on this branch is not the latest listed version. Do not block on unrelated pre-existing outdated packages.
- After PR creation, hand off to a **new** session for `/code-review` (GitHub review with resolvable threads, not chat only).

## Invocation

**Chat:** "Verify issue #N" or "Create PR for issue #N".
**Slash command:** `/verify-and-create-pr [number]`.
**GitHub Issue comment** (mention at the start; `N` is this issue):
- `@cursor verify this issue`
- `@cursor create a PR for this`
- `@cursor open a pull request`

Do not run this from a Pull Request comment to open a second PR. If the PR already exists, stay on that branch.
