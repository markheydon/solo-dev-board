# Daily Start

**Contract:** [`.agents/contracts/pm-orchestrator.md`](../contracts/pm-orchestrator.md) (read-only orientation mode)
**Runbook:** [`plan/PM_RUNBOOK.md`](../../plan/PM_RUNBOOK.md) — Session Start

## Purpose

Read-only orientation at the **start of a working session** — not tied to a calendar day. Use whenever you sit down to work on the project after a gap or at the beginning of a focused block.

## Easy-to-miss specifics

- Read-only by default — do not update Project #8 unless the user explicitly asks.
- Query `gh issue list` and Project #8 for work selection; do not use `plan/BACKLOG.md` as a work queue.
- May identify candidates for **Up Next** and **Focus Order**, but only apply board changes when requested.
- Do not recommend **Blocked** or **Ice Box** items for implementation; skip issues with `status/blocked` or `status/ice-box`.

## Invocation

**Chat:** "Run the daily start workflow".
**Slash command:** `/daily-start`.
**GitHub Issue comment** (mention at the start; the current issue is context only, not the work item to implement):
- `@cursor run daily start`
- `@cursor what's next from the board`

Do not treat this as "implement this issue".
