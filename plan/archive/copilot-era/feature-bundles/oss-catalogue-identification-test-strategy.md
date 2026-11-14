# OSS catalogue identification — test strategy

## Testing scope

Validate that catalogue repositories are classified from GitHub `topics` containing `open-source`, that the Repositories page filters apply that rule without changing the default list, and that Application helpers are the reuse point for later scanners.

## Quality objectives

- Classification is deterministic and case-insensitive on the canonical topic slug.
- Public-without-topic and private-with-topic partitions are both covered.
- UI default remains the unfiltered catalogue.
- CI Playwright continues to assert the unauthenticated shell; populated filter behaviour is covered by bUnit (and docs-capture when a real PAT is used).

## Risks

| Risk | Test response |
|------|----------------|
| Topics dropped in JSON mapping | Infrastructure unit test with a fixture payload that includes `topics`. |
| Filter combined incorrectly with name search | bUnit cases for filter-only and filter-plus-search. |
| CI has no live catalogue | Playwright asserts the filter control is present on the shell; populated grids stay in bUnit / docs-capture. |

## Test design

- **Equivalence partitioning:** OSS (topic present), non-OSS (topic absent or other topics only), empty catalogue.
- **Boundary:** empty `topics`, mixed-case `Open-Source` (must still match), `oss` alone (must not match).
- **State transition:** All → Open source → Not open source → All, with search term retained.

## Coverage by layer

| Layer | Focus |
|-------|--------|
| Domain | Canonical topic matcher. |
| Infrastructure | `topics` mapped onto `Repository`. |
| Application | DTO `IsOpenSource` / filter helpers; RepositoryService mapping. |
| App (bUnit) | Toggle group, filtered rows, empty-filter copy, search AND filter. |
| Playwright | Filter control visible on `/repositories`; existing error-state and overflow tests remain. |

## Out of scope for this test issue

- Overnight scan jobs (#438, #439).
- Group selector behaviour (#381).
- Live GitHub Search cross-checks.
