# Deliver Issue

**Contracts:** [`.agents/contracts/delivery.md`](../contracts/delivery.md), [`.agents/contracts/verify.md`](../contracts/verify.md)
**Runbook:** [`plan/PM_RUNBOOK.md`](../../plan/PM_RUNBOOK.md) — Stage 2 and Stage 3 (happy path)
**Skills (on demand):** Same as `implement-issue` (via Delivery contract)
**Skills (required):** `aspire` (router), then `aspire-orchestration` and `aspire-monitoring` when running or diagnosing the AppHost

## Purpose

Orchestrate the delivery happy path in one session: preflight (when required), implementation, and verify/PR creation. Does not run code review — use a **new** agent session for `/code-review` after the PR exists.

## Easy-to-miss specifics

- Require issue number `N`. Load labels: `gh issue view <N> --json title,body,state,labels`.
- Do not implement issues with `status/blocked` or `status/ice-box` — escalate or re-queue first.
- **Preflight pause:** if `size/m`, `size/l`, `size/xl`, or `type/enabler`, and this session has not already produced **Preflight Complete**, run [`.agents/workflows/preflight-issue.md`](preflight-issue.md) only and stop. If preflight already ran in this chat, treat the proceed gate as satisfied.
- Otherwise run [`.agents/workflows/implement-issue.md`](implement-issue.md) (includes mandatory preflight; `size/xs`, `size/s`, and `type/bug` auto-continue per the Delivery contract).
- Follow the Delivery contract **Aspire runtime** section: this repo is Aspire-hosted; rebuild `app` after live C# or Razor fixes; read Aspire logs on exceptions.
- When Delivery reports **Implementation Complete**, run [`.agents/workflows/verify-and-create-pr.md`](verify-and-create-pr.md).
- Stop after verify. Do not continue into `/code-review` in this session.
- Keep `/preflight-issue`, `/implement-issue`, and `/verify-and-create-pr` available for split workflows.
- A GitHub Issue comment that says implement / fix / do / work on **this issue** is this workflow (open a PR), not the split `implement-issue` path. See [GitHub comment invocation](README.md#github-comment-invocation).

## Invocation

**Chat:** "Deliver issue #N".
**Slash command:** `/deliver-issue [number]`.
**GitHub Issue comment** (mention at the start; `N` is this issue):
- `@cursor implement this issue`
- `@cursoragent implement this issue`
- `@cursor fix this`
- `@cursor deliver this issue`
- `@cursor work on this`

Do not use this workflow for "implement without a PR". That is `implement-issue`.

## Handoff

When verify succeeds, end with:

```text
Delivery complete for issue #N.

PR #<id>: <url>

Next: open a new agent session and run `/code-review` on PR #<id>.
If unresolved review threads remain (Copilot, human, or code review), run `/address-pr-review-comments` on that PR.
```
