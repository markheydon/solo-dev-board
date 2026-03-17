# Repositories Page Wireframe

## Purpose
- Provide a clear overview of all repositories managed by SoloDevBoard.
- Enable efficient repository selection, filtering, and bulk actions.

## User Goals
- View repository details in a structured data grid.
- Perform actions such as refresh, add, or remove repositories.
- Access overflow row actions for advanced management (future enhancement).

## Layout
```
+-------------------------------------------------------------+
| Command Strip: [Refresh] [Add] [Remove] [Bulk Actions]      |
+-------------------------------------------------------------+
| Data Grid:                                                  |
| +-------------------+-------------------+----------------+ |
| | Repository Name   | Status            | Actions        | |
| +-------------------+-------------------+----------------+ |
| | repo-1            | Connected         | [Edit] [More]  | |
| | repo-2            | Disconnected      | [Edit] [More]  | |
| ...                                                     ...|
+-------------------------------------------------------------+
| Feedback Region: [Status messages, errors, confirmations]   |
+-------------------------------------------------------------+
```

## Interaction Notes
- Command strip actions trigger immediate feedback in the feedback region.
- Row actions include Edit and More (overflow menu for future extensibility).
- Bulk actions operate on selected repositories.

## State Variants
- Empty state: Show onboarding prompt when no repositories are connected.
- Loading state: Display spinner overlay on data grid.
- Error state: Show error message in feedback region.

## Accessibility Notes
- Focus order: Command strip → data grid header → data grid rows → feedback region.
- ARIA: Data grid uses `aria-rowindex`, command strip uses `aria-label`.
- Live region: Feedback region uses `aria-live="polite"` for status updates.

## Responsive Behaviour
- Desktop: Data grid expands to fill available width, command strip remains visible.
- Mobile: Command strip collapses into overflow menu, data grid stacks vertically.
