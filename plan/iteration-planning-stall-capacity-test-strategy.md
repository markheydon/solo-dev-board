# Iteration Planning stall versus capacity — test strategy

## Testing scope

Validate that stalled Up Next is the only hard disable for Add to Up Next, that the stall alert is error severity and does not mention capacity, that capacity-at-limit still opens the exceed dialog when there is no stall, and that the candidate picker stays visible with a pause line when stalled.

## Quality objectives

- Stall-on: Add disabled; one error alert; candidate list visible.
- Stall-off and capacity full: Add enabled; confirm dialog still used.
- Stall-off and capacity under limit: Add enabled with no stall alert.
- Copy never blends stall and capacity into one brake.

## Risks

| Risk | Test response |
|------|----------------|
| Regression that treats 8/8 as a hard disable | bUnit: capacity full, empty stalled list, Add still enabled. |
| Warning-severity stall banner remains | Assert error severity and `data-testid` on the stall alert. |
| Candidate section hidden when paused | Assert picker markup present with pause line. |

## Test design

- **State combinations:** stall empty/non-empty × capacity under/at/over limit.
- **Copy:** stall alert must not contain “capacity”; capacity strip must not require resolving items before add.

## Coverage by layer

| Layer | Focus |
|-------|--------|
| App (bUnit) | `IsAddToUpNextDisabled` vs capacity; alert severity; pause line; exceed dialog still shown. |
| Playwright | `/pm-workflow/planning` shell: stall alert test id; Add buttons present. |

## Out of scope

- Changing stall-day maths.
- Live GitHub board mutations in CI.
