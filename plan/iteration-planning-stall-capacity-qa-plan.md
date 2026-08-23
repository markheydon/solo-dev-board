# Iteration Planning stall versus capacity — QA plan

## Entry criteria

- Planning wireframe stall/capacity notes are approved.
- Test issue exists with Implementation References.

## Exit criteria

- #445 acceptance criteria are checked off, then its test issue.
- `dotnet test` and Playwright planning shell tests pass.
- `website/content/docs/pm-workflow.md` Iteration section states stall is a hard gate and capacity is a soft confirm.
- UK English in UI strings and docs.

## Quality gates

1. bUnit covers stall-on (Add disabled, error alert) and capacity-full with no stall (Add enabled, confirm dialog).
2. Playwright asserts stall-alert `data-testid` on the `/pm-workflow/planning` shell without requiring a live PAT.
