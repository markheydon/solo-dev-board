# Workflow Templates Page Wireframe

## Purpose
- Provide a central page for browsing, customising, and applying GitHub Actions workflow templates across repositories.
- Reduce friction by avoiding manual YAML copy/paste and by surfacing template application state.

## User Goals
- Discover reusable workflow templates quickly.
- Preview and adjust parameters before applying a template.
- Apply a template to one or more repositories from one page.
- See which repositories already have a template applied.
- Detect repositories whose workflow file differs from the canonical template.

## Layout
```
+-------------------------------------------------------------+
| Repository selector / repository scope filter               |
+-------------------------------------------------------------+
| Template browser: [Search] [Category chips] [Sort]          |
+-------------------------------------------------------------+
| Template list/card region                                   |
|   - Template name, description, tags                        |
|   - Preview / details / select                              |
+-------------------------------------------------------------+
| Selected template detail panel                              |
|   - YAML summary / template fields                          |
|   - Parameter editor form                                   |
|   - Apply to repositories control                           |
+-------------------------------------------------------------+
| Feedback region: status, errors, applied results             |
+-------------------------------------------------------------+
```

## Interaction Notes
- Repository selector sets the page scope.
- Selecting a template loads details and parameter fields.
- Apply action is disabled until required parameters and target repositories are selected.
- Feedback region shows success, partial apply results, and conflicts.

## State Variants
- Empty state: no templates or no repositories selected.
- Loading state: templates or repository list loading.
- Error state: template retrieval or apply failure.
- Difference state: repository workflow differs from canonical template.

## Accessibility Notes
- Focus order: repository selector → template list → template details → apply controls → feedback.
- Use `aria-live="polite"` for result updates.
- Template cards and form controls must be keyboard-accessible.

## Responsive Behaviour
- Desktop: side-by-side template list and details panel.
- Mobile: stacked template list above details, with collapsible details panel.
