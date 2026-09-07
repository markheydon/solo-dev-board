# Remap existing labels onto the recommended taxonomy — QA plan

## Entry criteria

- Remap section is in `plan/wireframes/label-manager-wireframe.md`.
- Feature #491 and children [#519](https://github.com/markheydon/solo-dev-board/issues/519), [#520](https://github.com/markheydon/solo-dev-board/issues/520), and [#521](https://github.com/markheydon/solo-dev-board/issues/521) have Implementation References and sizes.
- Milestone `v1.3 - Usable solo workflow across repositories` is open.

## Exit criteria

- Enabler acceptance criteria checked off, then the story, then the test.
- `dotnet test` passes for remap unit and bUnit tests.
- Playwright Label Manager spec asserts remap preview controls (placeholder auth).
- `website/content/docs/label-manager.md` describes remap; `tests/E2E/USER_DOCS_ALIGNMENT.md` Label Manager row notes those controls.
- UK English in UI strings and docs.

## Quality gates

1. Unit: retag then delete; no delete after a failed item; other labels preserved.
2. bUnit: mapping table when remove-outside is on; keep-area excludes `area/*` sources; skip row is not applied.
3. Playwright CI: remap `data-testid`s present on `/labels` Recommended Taxonomy without requiring a live PAT merge.
4. Migration page behaviour unchanged.
