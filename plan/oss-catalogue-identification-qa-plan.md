# OSS catalogue identification — QA plan

## Entry criteria

- DEC-032 and the Repositories wireframe filter strip are approved in planning artefacts.
- Child issues exist with Implementation References.

## Exit criteria

- Enabler, story, and test acceptance criteria are checked off.
- `dotnet test` and Playwright repositories shell tests pass.
- User Guide describes the built-in filters and that public is not the same as open source.
- UK English in UI strings and docs.

## Quality gates

1. Classifier unit tests include the `oss` non-match and mixed-case match cases.
2. bUnit covers default All, Open source, Not open source, and empty filtered sets.
3. Playwright does not require a live PAT for CI; filter `data-testid` is asserted on the shell.
