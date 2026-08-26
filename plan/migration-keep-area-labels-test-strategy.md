# One-Click Migration keep-`area/*` overwrite — test strategy

**Parent enabler:** [#464](https://github.com/markheydon/solo-dev-board/issues/464)  
**Standards:** xUnit v3 `Assert.*` and NSubstitute only ([DEC-006](DECISIONS.md#dec-006-no-fluentassertions--xunit-built-in-assertions-only), [DEC-016](DECISIONS.md#dec-016-formalised-testing-standard--xunit-v3-nsubstitute-playwright-e2e)).

## Scope

Validate that label Overwrite keep-versus-delete behaviour matches Label Manager extra deletes for the `area/` prefix, without regressing Skip, Merge, milestones, or Status columns.

## Test design

- **Equivalence partitioning:** keep on (default) versus keep off; Overwrite versus Skip/Merge; `area/*` versus other target-only labels.
- **Decision table:** Overwrite + keep on → area names in `KeptAreaLabels` and not in deletes; Overwrite + keep off → area names in deletes; Skip/Merge → no label deletes regardless of keep.
- **Change-related:** existing Overwrite create/update/delete tests still pass when fixtures have no `area/*` orphans.

## Coverage

| Layer | Focus |
|-------|--------|
| Application | `MigrationService` preview and apply with `area/*` target-only labels. |
| App (bUnit) | Nested checkbox visibility for Overwrite + Labels; default checked; preview kept caption versus delete rows. |
| Playwright | `/migrate` shell: control exists when Overwrite and Labels are selected. CI remains placeholder-auth. |
| Docs alignment | `website/content/docs/one-click-migration.md` plus `tests/E2E/USER_DOCS_ALIGNMENT.md` if the control list changes. |

## Quality gates

- Functional suitability is critical: accidental `area/*` deletes on Overwrite are the defect this slice prevents.
- Usability is high: the nested control must be labelled **Keep `area/*` labels** and must not use the word “Ignore”.
- Entry: #464 acceptance criteria and DEC-036 are recorded.
- Exit: paired test issue acceptance criteria are ticked and `dotnet test` plus Playwright migrate shell pass.
