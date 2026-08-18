# Code Review

**Contract:** [`.agents/contracts/code-review.md`](../contracts/code-review.md)

## Easy-to-miss specifics

- Review only — do not modify code; escalate required fixes to Delivery.
- Do not use for post-implementation PR creation — use `verify-and-create-pr` instead.
- When reviewing an open PR, submit a **GitHub Pull Request Review** with resolvable inline threads (`gh pr review` / GraphQL). Do not post actionable findings as a lone issue comment.
- Use `--request-changes` for Critical/High findings; `--comment` otherwise. Prefer `--comment` plus text recommendation over `--approve` to preserve the manual merge gate.
- A clean review may submit `COMMENT` with no inline threads.

## Invocation

Natural language: "Code review PR #N" or "Code review branch feature/X"
