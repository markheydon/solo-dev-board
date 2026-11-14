# Reload from GitHub, keep selection — test strategy

## Testing scope

Validate that Reload from GitHub and Try again keep Board Rules pickers, refetch project boards, bust the repository catalogue when force-reload is requested, and that remaining surfaces later match the same pattern. CI stays on placeholder auth; the Status-field dogfood path is bUnit (mock GitHub reads), not live Playwright.

## Quality objectives

- Unsupported-board Try again does not clear the repository picker.
- Page-level Reload keeps primary and comparison pickers and shows section loading.
- Force-reload drops cached repository catalogues before refetch.
- Existing error-state retries still work.
- Docs and Playwright shells stay aligned.

## Risks

| Risk | Test response |
|------|----------------|
| Delivery reuses `LoadRepositoriesAsync` and wipes selection | bUnit: assert selected full name after Reload. |
| Force-reload is a no-op and TTL serves stale repos | Application/Infrastructure unit tests on invalidate-then-fetch. |
| Playwright tries to add a GitHub Status field | Keep CI on shell controls; use mocks in bUnit for empty-to-supported transition. |
| Roll-out #451 ships without per-page picker tests | #487 checklist per in-scope surface. |

## Test design

- **Equivalence:** no selection; repository selected with unsupported boards; repository selected with boards; compare mode on.
- **State transition:** unsupported empty → Try again → supported options (mock second call).
- **Error vs empty:** repository load failure retry vs unsupported-board Try again (different `data-testid`s).
- **Experience-based:** do not interrupt imagined future writes; Board Rules is read-only now.

## Coverage by layer

| Layer | Focus |
|-------|--------|
| Infrastructure / Application | `GitHubResponseCache` repository invalidation; `IRepositoryService` force-reload default false. |
| App (bUnit) | Board Rules Reload and unsupported Try again; comparison pickers. |
| Playwright | `/board-rules` Reload control visible with selector region; existing error retry remains. |

## Out of scope

- Live GitHub Project Status field mutation in CI.
- Window-focus refetch (#450).
- Planning Refresh (already covered).
