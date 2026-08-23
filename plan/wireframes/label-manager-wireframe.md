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
| Repository Selector: [Repository dropdown]                  |
+-------------------------------------------------------------+
| Tab Strip: [Labels] [Recommended Taxonomy] [Synchronise]    |
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

## Labels tab — multi-select bulk delete (v1.1.0, #444)

```
+-------------------------------------------------------------+
| Action Strip: [New label]  [Load selected repositories]     |
|               [Delete]  (enabled when ≥1 row selected)      |
+-------------------------------------------------------------+
| MudDataGrid (multi-select)                                  |
| [x] Name     Colour   Description   Repositories   [row Del]|
| [x] bug      #d73a4a  Something…    2 of 3                  |
| [ ] enhancement …                                           |
+-------------------------------------------------------------+
| Confirm dialog: Are you sure?                               |
| Delete labels: bug                                          |
| From repositories: owner/a, owner/b                         |
| [No]  [Yes, delete]                                         |
+-------------------------------------------------------------+
```

- Keep per-row Delete for a single name (`LabelOperationDialog`).
- Bulk Delete is disabled with no selection or no repositories in scope.
- Confirm lists selected names and in-scope repositories. No / cancel leaves labels and selection unchanged.
- GitHub deletes one label per repository; the app loops, skips repos that lack the label, continues after per-item errors, and reports counts plus failures.
- Disable repeat submit while the batch is running (same idea as Synchronise apply).
- Do not invent an in-use protection: GitHub allows deleting labels that are still on issues.

## Recommended taxonomy and Synchronise — keep `area/*` (v1.1.0, #446)

```
+-------------------------------------------------------------+
| [ ] Remove labels outside taxonomy                          |
|     [x] Keep area/* labels   (nested; default on;           |
|         disabled or hidden when the parent box is off)      |
| Those labels will not be deleted as outside the taxonomy.   |
+-------------------------------------------------------------+
```

- Built-in recommended catalogue **omits** every `area/*` entry (DEC-034). Workflow prefixes (`type/`, `priority/`, `status/`, `size/`) stay.
- Nested control copy uses **Keep**, not Ignore. Hard-code prefix `area/` for this slice.
- Preview and Confirm list matching names as kept (area prefix) when the nested box is ticked; they are true extras when it is unticked and remove-outside is on.
- Synchronise extra deletes on the target follow the same keep-versus-delete rule (matching nested copy if a remove-outside control exists there).

## Interaction Notes
- Repository selector sets page-level context and filters all tabs.
- Action strip adapts to selected tab (e.g., synchronise actions only in Synchronise tab).
- Feedback region provides real-time status and error messages.
- Labels tab bulk Delete sits on the action strip next to New label / Load; it is not a second taxonomy apply flow.

## State Variants
- Empty state: Show onboarding prompt in each tab if no data.
- Loading state: Display spinner in results region.
- Error state: Show error in feedback region.

## Accessibility Notes
- Focus order: Repository selector → tab strip → action strip → results region → feedback region.
- ARIA: Tabs use `role="tablist"`, results region uses `aria-label`.
- Live region: Feedback region uses `aria-live="polite"`.

## Responsive Behaviour
- Desktop: Repository selector remains above tabs, with tabs and action strip visible and results region adapting to width.
- Mobile: Repository selector stays first, tabs collapse into dropdown, action strip moves to bottom, and results region stacks vertically.
