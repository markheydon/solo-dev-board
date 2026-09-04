# Actions Templates Page Wireframe

## Purpose
- Provide a central page for browsing, customising, and applying GitHub Actions workflow templates across repositories.
- Reduce friction by avoiding manual YAML copy/paste and by surfacing template application state.

## User Goals
- Discover reusable workflow templates quickly.
- Point the catalogue at one GitHub repository of organisation-specific workflow YAML (v1.2, #292).
- Preview and adjust parameters before applying a template.
- Apply a template to one or more repositories from one page.
- See which repositories already have a template applied.
- Detect repositories whose workflow file differs from the canonical template.

## Layout
```
+-------------------------------------------------------------+
| Repository selector / repository scope filter (multi)       |
+-------------------------------------------------------------+
| Custom template source                                      |
|   [ RepositorySelector — single-select, own catalogue ]       |
|   Or enter a repository not in your list:                   |
|   [ owner/name text field ]  [ Load templates ]             |
|   Caption: last-used value restored from localStorage       |
+-------------------------------------------------------------+
| Template browser: [Search] [Category chips]                 |
+-------------------------------------------------------------+
| Template list/card region                                   |
|   - Source badge: Built-in | owner/name                     |
|   - Template name, description, tags                        |
|   - Preview / details / select                              |
+-------------------------------------------------------------+
| Selected template detail panel                              |
|   - YAML summary / template fields                          |
|   - Parameter editor form (inferred {{tokens}} on custom)   |
|   - Apply to repositories control                           |
+-------------------------------------------------------------+
| Feedback region: status, errors, applied results             |
+-------------------------------------------------------------+
```

## Interaction Notes
- Repository selector sets the **apply** scope (target repositories). It is independent of the custom template source.
- Custom template source uses a **dual picker**: single-select `RepositorySelector` (same active catalogue as apply targets) plus a manual `owner/name` field. Load is explicit (button beside the manual field only), not on every keystroke or autocomplete browse.
- Selecting a catalogue repository sets the source `owner/name` and syncs the manual field; it does not load until **Load templates** is selected.
- Typing a manual `owner/name` that is not in the catalogue clears the source selector selection.
- Typing a manual `owner/name` that matches a catalogue repository selects that repository in the source selector.
- On first visit neither picker has a value. If localStorage has a last-used source, pre-select the catalogue repo when it is in the list, otherwise pre-fill the manual field, and load that catalogue on page load.
- Changing the source and loading replaces the custom catalogue for the session. Built-in templates always remain visible.
- Selecting a template loads details and parameter fields.
- Apply action is disabled until required parameters and target repositories are selected.
- Feedback region shows success, partial apply results, conflicts, and custom-source load failures.

### Load button alignment
- Do not put last-used or hint copy on `MudTextField.HelperText` in the same row as **Load templates** — MudBlazor end-aligns the button to the helper-text baseline and the button sags below the input outline.
- Place last-used / hint text as a caption **below** the manual-field row.
- Align **Load templates** to the outlined manual field (input control baseline), not beside the full `RepositorySelector` stack (chips and summary).

## State Variants
- Empty state: no templates or no repositories selected.
- Loading state: templates, custom source, or repository list loading.
- Error state: template retrieval, custom source (not found, no access, invalid `owner/name`), or apply failure. Built-in templates still render when the custom source fails.
- Custom source empty directory: info that `.github/workflows` has no `.yml` / `.yaml` files; built-ins unchanged.
- Difference state: repository workflow differs from the canonical template.

## Accessibility Notes
- Focus order: apply repository selector → custom source selector → manual source field → Load → template list → template details → apply controls → feedback.
- Use `aria-live="polite"` for result updates and source-load outcomes.
- Template cards, source selector, manual field, Load, and form controls must be keyboard-accessible.
- Source badges must not rely on colour alone (text label Built-in or `owner/name`).

## Responsive Behaviour
- Desktop: side-by-side template list and details panel. Custom source section sits in the full-width stack above the browser.
- Mobile: stacked template list above details, with collapsible details panel.

## v1.2 — Custom template source (#292)

Product decisions (also [DEC-038](../DECISIONS.md#dec-038-custom-actions-template-sources)):

- One GitHub repository at a time. Multiple concurrent sources are out of this increment.
- Source selection: catalogue `RepositorySelector` (single-select) **or** manual `owner/name` field; both resolve to one `owner/name` for load.
- Last-used `owner/name` only in browser localStorage. No application database. Server-side persistence is a later blocked chore.
- Scan `.github/workflows/*.yml` and `*.yaml` at the top level of that directory only.
- Infer `{{token}}` placeholders as required string parameters labelled with the token name. No sidecar or front-matter.
- Merge custom cards into the same grid with a source badge. Do not hide a built-in when a custom file uses the same target path.
- Private sources use the existing PAT or GitHub App token. Fail clearly; do not add a second credential.
- Out of increment: persisted parameter profiles (#436), in-app YAML authoring, publish-back, GitHub organisation starter-workflow metadata, and operator appsettings lists.
