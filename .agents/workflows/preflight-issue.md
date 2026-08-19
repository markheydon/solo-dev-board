# Preflight Issue

**Contract:** [`.agents/contracts/delivery.md`](../contracts/delivery.md) (Implementation Preflight section only)
**Runbook:** [`plan/PM_RUNBOOK.md`](../../plan/PM_RUNBOOK.md) — Stage 2: Implementation (preflight sub-step)
**Skills (on demand):** `dotnet-best-practices`, `mudblazor`
**Skills (required):** `aspire` (this repo is Aspire-hosted; load during preflight)

## Easy-to-miss specifics

- Run preflight only — do not create a feature branch, write code, or change issue labels.
- Follow Implementation Preflight in the Delivery contract (load context, validate readiness, codebase discovery, sketch).
- Read the `aspire` skill during preflight. SoloDevBoard runs locally via `src/SoloDevBoard.AppHost`.
- Area label → codebase hints: see [implement-issue workflow](implement-issue.md).
- Always pause after the **Preflight Complete** output, even for `size/xs` and `size/s` issues.
- End with: "Ready to implement — run `/implement-issue` or 'Implement issue #N'."

## Invocation

Natural language: "Preflight issue #N"
