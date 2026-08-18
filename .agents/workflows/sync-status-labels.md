# Sync Status Labels

**Contract:** [`.agents/skills/repo-github-gh-cli/SKILL.md`](../skills/repo-github-gh-cli/SKILL.md) (label safety rules)
**Runbook:** [`plan/PROJECT_BOARD_DESIGN.md`](../../plan/PROJECT_BOARD_DESIGN.md) — Board Hygiene Audit

## Easy-to-miss specifics

- **Dry-run first** — always run `node .github/scripts/sync-status-labels.mjs --dry-run` and present the report.
- **Apply only on explicit user confirmation** — then re-run with `--apply`.
- **Re-run dry-run after apply** — confirm zero remaining violations.
- **Do not touch** `type/`, `priority/`, `area/`, or `size/` labels.
- **Scope:** `markheydon/solo-dev-board` only unless the user names another repo.
- **CI already auto-applies** — the Roadmap Sync workflow runs `sync-status-labels.mjs --apply` before `roadmap-sync.mjs` on schedule, `workflow_dispatch`, and issue events. Manual invocation is for on-demand inspection and ad-hoc fixes; do not trigger CI from the agent unless the user explicitly asks for `workflow_dispatch`.

## Repair rules

Canonical `status/*` labels are defined in [`plan/LABEL_STRATEGY.md`](../../plan/LABEL_STRATEGY.md).

### Closed issues and PRs

| Condition | Action |
|-----------|--------|
| Closed, not duplicate | Remove every `status/*` except `status/done`; add `status/done` if missing. |
| Closed as duplicate | Remove all `status/*` labels; do not add `status/done`. |

### Open issues and PRs

| Condition | Action |
|-----------|--------|
| Open with `status/done` | Remove `status/done`; set `status/in-review` if the item has an open linked PR, otherwise `status/todo`. |
| Open with multiple `status/*` labels | Keep the most advanced label (`in-review` > `in-progress` > `blocked` > `todo`); remove the rest. |
| Open with no `status/*` label | Report only — do not auto-add. |

## Invocation

Natural language: "Clean up status labels", "Fix status/done drift on closed issues", `/sync-status-labels`
