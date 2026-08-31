# Reload from GitHub, keep selection — QA plan

## Entry criteria

- Board Rules wireframe Reload and Try again notes are in `plan/wireframes/board-rules-visualiser-wireframe.md`.
- Enabler #485 and story #449 have Implementation References and sizes.
- Test issues #486 and #487 exist.

## Exit criteria

- #485 acceptance criteria checked off, then #449, then #486.
- #451 and #487 remain open until the roll-out slice; they are not a Board Rules close-out gate.
- `dotnet test` passes for cache and Board Rules tests.
- Playwright `board-rules.spec.ts` asserts the page-level Reload control (and existing error retry).
- `website/content/docs/board-rules-visualiser.md` describes Reload and unsupported-board Try again.
- `tests/E2E/USER_DOCS_ALIGNMENT.md` Board Rules row notes those controls.
- UK English in UI strings and docs.

## Quality gates

1. bUnit: selected repository survives Reload and unsupported Try again; second service call returns boards.
2. Unit: force-reload invalidates repository catalogue keys.
3. Playwright CI: Reload `data-testid` present on `/board-rules` without requiring a live PAT.
4. No global app-bar refresh control added.
