# Label Manager v1.1 dogfood — test strategy

## Testing scope

Validate that the built-in SoloDevBoard recommended catalogue no longer includes `area/*`, that the nested keep option changes extra-delete behaviour for those names, that Synchronise follows the same rule, and that Labels-tab bulk delete confirms, cancels, continues after partial GitHub failures, and refreshes the grid.

## Quality objectives

- Catalogue apply never creates `area/*` from the built-in list.
- Default keep-on does not delete `area/*` extras; keep-off does.
- Bulk delete never runs on cancel; confirmed deletes loop per repository and do not abort the whole batch on the first error.
- CI Playwright keeps asserting the `/labels` shell; destructive GitHub deletes are unit/bUnit (and docs-capture when a real PAT is used).

## Risks

| Risk | Test response |
|------|----------------|
| Catalogue still listed in Audit Dashboard helpers | Assert `RecommendedLabelTaxonomyCatalog.SoloDevBoard` has no `area/` prefix; check Audit consumers that assume those names exist. |
| Nested checkbox enabled when parent remove-outside is off | bUnit: nested control not actionable unless remove-outside is on. |
| Single-label `DeleteLabelAsync` still fail-fast | Application tests for the bulk path must continue after a per-repo failure. |
| Multi-select missing in Playwright shell | Assert bulk Delete control exists and is disabled with no selection. |

## Test design

- **Equivalence:** catalogue names with and without `area/` prefix; extras that are `area/*` vs other orphans.
- **Decision table:** remove-outside off / on × keep-areas on / off.
- **Bulk delete:** empty selection, cancel confirm, confirm success, missing label on a subset of repos, one GitHub error mid-batch.

## Coverage by layer

| Layer | Focus |
|-------|--------|
| Application | Catalogue contents; keep-`area/*` filter on extra deletes; bulk delete continue-on-error. |
| App (bUnit) | Nested checkbox enablement; preview rows marked kept vs delete; Labels grid multi-select and confirm dialog. |
| Playwright | `/labels` shell: new controls present; bulk Delete disabled with no rows selected. |

## Out of scope for these test issues

- Configurable ignore-prefix settings.
- Protecting GitHub default labels from delete.
- Live PAT deletes in CI.
