# Address PR Review Comments

**Contract:** [`.agents/contracts/delivery.md`](../contracts/delivery.md) (PR review comment loop section)
**Runbook:** [`plan/PM_RUNBOOK.md`](../../plan/PM_RUNBOOK.md) — PR Review Comment Loop

## Easy-to-miss specifics

- Stay on the existing pull request branch; do not create a replacement PR.
- Checkout the PR head branch before making changes.
- Fetch **every** unresolved review thread on the PR (Copilot, human, agent, or bot) via GraphQL `reviewThreads` or equivalent — do not stop after the first reviewer.
- Merge policy blocks merge while **unresolved review conversations** remain. Top-level issue comments are not review threads and cannot be resolved; ignore them except the workflow's own closing summary.
- **Thread disposition:**
  - In-scope and valid: implement, reply on the thread, then resolve.
  - Invalid, duplicate, or out of scope: reply with the reason, then resolve so merge is not blocked.
  - Needs maintainer decision (security, scope, product): reply, leave unresolved, and stop with a short list for the user.
- If no unresolved review threads exist, no-op with "No unresolved review conversations" — do not invent work.
- Do not treat this workflow's closing summary issue comment as a finding on a subsequent pass.
- Post one final summary issue comment on the pull request once all addressed threads are handled.
- This path is exempt from the Delivery Testing Phase (commit and push without waiting for manual test acceptance).
- Re-run scoped tests after fixes; push to the existing PR branch.

## Invocation

Natural language: "Address PR review comments on PR #N"
