# Label Manager Page Wireframe

## Purpose
- Enable users to create, edit, synchronise, and manage labels across repositories.
- Reduce cognitive load by separating modes and use cases.

## User Goals
- Organise labels efficiently using a clear, tabbed interface.
- Access recommended taxonomy and synchronisation tools.
- Receive actionable feedback for label operations.

## Layout
```
+-------------------------------------------------------------+
| Tab Strip: [Labels] [Recommended Taxonomy] [Synchronise]    |
+-------------------------------------------------------------+
| Scope Selector: [Repository dropdown]                       |
+-------------------------------------------------------------+
| Action Strip: [Create] [Edit] [Delete] [Bulk Actions]       |
+-------------------------------------------------------------+
| Results Region:                                             |
|   - Labels: List/grid of labels with colour, description    |
|   - Recommended: Taxonomy suggestions, import options       |
|   - Synchronise: Sync status, controls, progress indicator  |
+-------------------------------------------------------------+
| Feedback Region: [Status, errors, confirmations]            |
+-------------------------------------------------------------+
```

## Mode Separation Rationale
- Tabs separate distinct use cases: label management, taxonomy guidance, and synchronisation.
- Reduces confusion and cognitive load by isolating workflows.
- Each tab presents relevant actions and feedback, minimising context switching.

## Interaction Notes
- Scope selector filters results and actions by repository.
- Action strip adapts to selected tab (e.g., synchronise actions only in Synchronise tab).
- Feedback region provides real-time status and error messages.

## State Variants
- Empty state: Show onboarding prompt in each tab if no data.
- Loading state: Display spinner in results region.
- Error state: Show error in feedback region.

## Accessibility Notes
- Focus order: Tab strip → scope selector → action strip → results region → feedback region.
- ARIA: Tabs use `role="tablist"`, results region uses `aria-label`.
- Live region: Feedback region uses `aria-live="polite"`.

## Responsive Behaviour
- Desktop: Tabs and action strip remain visible, results region adapts to width.
- Mobile: Tabs collapse into dropdown, action strip moves to bottom, results region stacks vertically.
