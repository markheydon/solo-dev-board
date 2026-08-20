# Address PR Review Comments

**Contract:** [`.agents/contracts/delivery.md`](../contracts/delivery.md) (PR review comment loop section)
**Runbook:** [`plan/PM_RUNBOOK.md`](../../plan/PM_RUNBOOK.md) — PR Review Comment Loop
**Skills (required when the AppHost is running):** `aspire`, `aspire-orchestration`, `aspire-monitoring`

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
- If Aspire is running after those fixes, `aspire resource app rebuild` then `aspire wait app`. On exceptions, use `aspire otel logs app` and `aspire logs app`.

## Invocation

**Chat:** "Address PR review comments on PR #N".
**Slash command:** `/address-pr-review-comments [number]`.
**GitHub Pull Request comment** (mention at the start; `N` is this PR):
- `@cursor address the review comments`
- `@cursor fix the review threads`
- `@cursor address Copilot comments`

Prefer commenting on the PR, not on the linked issue. Each issue `@cursor` mention starts a new session and will not automatically check out the existing PR branch.
