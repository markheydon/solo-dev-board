# Label Manager v1.1 dogfood — QA plan

## Entry criteria

- DEC-034 and the Label Manager wireframe bulk-delete / keep-`area/*` notes are in planning artefacts.
- Test issues exist with Implementation References.

## Exit criteria

- #446 and #444 acceptance criteria are checked off, then their test issues.
- `dotnet test` and Playwright `/labels` shell tests pass.
- `website/content/docs/label-manager.md` describes the nested keep option, the catalogue change, and bulk delete (confirm, cancel, partial failure).
- `plan/LABEL_STRATEGY.md` remains the source for *this* repository's `area/*` labels and is not treated as an export mandate.
- UK English in UI strings and docs.

## Quality gates

1. Unit tests: catalogue has no `area/*`; keep-on vs keep-off extra-delete matrices for Recommended taxonomy and Synchronise.
2. bUnit: nested checkbox gating; Labels multi-select; confirm/cancel; disabled bulk Delete.
3. Playwright does not require a live PAT for CI; `data-testid`s for the nested checkbox and bulk Delete are asserted on the shell.
