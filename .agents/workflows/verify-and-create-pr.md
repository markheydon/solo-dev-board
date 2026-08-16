# Verify and Create PR

**Contract:** [`.agents/contracts/verify.md`](../contracts/verify.md)
**Runbook:** [`plan/PM_RUNBOOK.md`](../../plan/PM_RUNBOOK.md) — Stage 3: Verify and PR Creation

## Easy-to-miss specifics

- Follow [`plan/PULL_REQUEST_POLICY.md`](../../plan/PULL_REQUEST_POLICY.md) for title, body, labels, linking, draft state, assignee, and milestone.
- Use `gh pr create --fill` so the repository PR template is applied; do not bypass with a custom `--body` unless the platform cannot apply the template — then copy the template headings into the body.
- Always use the repo's label strategy for the relevant labels. See the [label strategy](../../plan/LABEL_STRATEGY.md).

## Invocation

Natural language: "Verify issue #N" or "Create PR for issue #N"
