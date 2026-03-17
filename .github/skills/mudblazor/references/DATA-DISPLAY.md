# MudBlazor Data Display

Use this reference for read-heavy views, structured information, and visual presentation components.

## Component Coverage

| Component | When to use it. | Notes. |
|-----------|-----------------|--------|
| `MudAvatar` | User or entity avatar visuals. | Use in lists, cards, and activity streams. |
| `MudCard` | Information grouped as card content. | Suitable for dashboard summaries and compact records. |
| `MudCarousel` | Rotating set of visual slides. | Use sparingly; avoid for critical information. |
| `MudChat` | Chat-style message thread presentation. | Suitable for conversational or timeline-style interactions. |
| `MudChip<T>` | Compact labels, facets, or status indicators. | Works well with filters and taxonomy badges. |
| `MudChipSet<T>` | Coordinated chip selection groups. | Useful for filter bars and quick selectors. |
| `MudDataGrid<T>` | Rich tabular data with filtering, sorting, and templates. | Prefer for feature-rich interactive tables. |
| `MudDropZone<T>` | Drag-and-drop target and reorder interactions. | Useful for visual ordering and workflow mapping interfaces. |
| `MudExpansionPanels` | Collapsible grouped sections. | Ideal for advanced settings and dense forms. |
| `MudImage` | Responsive and themed image rendering. | Prefer over raw `<img>` when MudBlazor behaviours are useful. |
| `MudList<T>` + `MudListItem<T>` | Lightweight list presentation and selection. | Choose over table when columns are unnecessary. |
| `MudPagination` | Page navigation controls for paged content. | Pair with server-side paging logic where needed. |
| `MudPopover` | Anchored content surface for contextual detail. | Use with care; ensure focus and dismiss behaviour are clear. |
| `MudSimpleTable` | Low-friction static table rendering. | Good for read-only compact tabular details. |
| `MudTable<T>` | Data table with moderate capabilities. | Prefer when `MudDataGrid` features are unnecessary. |
| `MudTabs` + `MudTabPanel` | Segmented content areas. | Useful for mode switches and detail partitioning. |
| `MudTimeline` + timeline items | Chronological event display. | Good fit for audit and activity histories. |
| `MudTreeView<T>` + `MudTreeViewItem<T>` | Hierarchical data and nested structures. | Suitable for file-like trees and rule hierarchies. |

## Table and Grid Guidance

- Use `MudSimpleTable` for static data with no interactive operations.
- Use `MudTable<T>` for moderate interaction needs.
- Use `MudDataGrid<T>` when advanced filtering, template columns, or richer interactions are required.

For concrete grid patterns, see `DATAGRID.md`.

## Related References

- For action controls within displayed data, see `BUTTONS.md`.
- For notifications and transient UI tied to display actions, see `FEEDBACK-OVERLAYS.md`.
