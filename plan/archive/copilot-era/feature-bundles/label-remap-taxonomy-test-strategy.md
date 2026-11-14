# Remap existing labels onto the recommended taxonomy — test strategy

## Testing scope

Validate that extras can be mapped onto taxonomy (or other existing) labels, that issues and pull requests keep other labels, that the source is deleted only after successful retag, and that Keep `area/*` still excludes those names. CI stays on placeholder auth; live retag of private issues is not a Playwright CI path.

## Quality objectives

- Leaf suggestions pre-select destinations; skip remains available.
- Apply reports per-repository success and failure counts.
- Failed retag does not delete the source for that repository.
- One-Click Migration behaviour is unchanged.
- Docs and Playwright shells stay aligned.

## Risks

| Risk | Test response |
|------|----------------|
| Delivery uses rename onto an existing destination | Unit tests: destination already exists; assert add-then-delete, not update-name. |
| Source deleted after a partial retag failure | Unit tests: one item add fails; source still present. |
| Playwright tries to retag live GitHub issues | CI asserts mapping UI and preview rows with mocks; docs-capture may use a public catalogue. |

## Test design

- **Equivalence:** skip row; map to existing taxonomy name; map to another existing non-taxonomy name; destination missing (create then retag).
- **Decision table:** remove-outside off (no remap rows); remove-outside on with keep-area on vs off.
- **State transition:** preview mapping → confirm apply → result counts.
- **Experience-based:** do not wipe other labels on the item.

## Coverage by layer

| Layer | Focus |
|-------|--------|
| Infrastructure / Application | List items by label; add destination; remove source; delete source only on full success; pagination. |
| App (bUnit) | Remap rows, suggestion pre-fill, skip, keep-area exclusion, apply disabled until preview. |
| Playwright | Recommended Taxonomy remap controls visible; User Guide alignment. |

## Out of scope

- Live merge of labels on private repositories in CI.
- Migration overwrite retagging.
- Projects v2 field remapping.
