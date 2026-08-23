# Repositories Page Wireframe

## Purpose
- Provide a clear overview of all repositories managed by SoloDevBoard.
- Enable efficient repository selection, filtering, and bulk actions.
- Identify which catalogue members are open-source project repositories (and which are not) without waiting for repository groups ([#440](https://github.com/markheydon/solo-dev-board/issues/440), [DEC-032](../DECISIONS.md#dec-032-oss-catalogue-identification-from-the-github-open-source-topic)).

## User Goals
- View repository details in a structured data grid.
- Perform actions such as refresh, add, or remove repositories.
- Filter the loaded catalogue to **Open source** or **Not open source** using the GitHub topic `open-source`.
- Access overflow row actions for advanced management (future enhancement).

## Layout
```
+-------------------------------------------------------------+
| Command Strip: [Refresh] [Add] [Remove] [Bulk Actions]      |
|                [Search repositories]                        |
+-------------------------------------------------------------+
| Catalogue filter (MudToggleGroup, exclusive):               |
|   ( All )  [ Open source ]  [ Not open source ]             |
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
- **All** is the default catalogue filter. It shows every loaded repository (subject to name search).
- **Open source** limits the grid to catalogue rows whose GitHub `topics` include `open-source`.
- **Not open source** shows the complement (loaded catalogue minus that set). Use this to find repositories that should be tagged on GitHub but are not.
- Name search combines with the catalogue filter (logical AND). Changing the toggle does not clear the search field.
- Do not persist the filter. A page reload returns to **All**.
- Public / Private visibility chips remain independent of the OSS filter. Public is not the same as open source.
- Prefer `MudToggleGroup<T>` for the exclusive All / Open source / Not open source control, with MudBlazor spacing utilities on the existing command-strip `MudStack`. Do not add a second search field or a new page.

## State Variants
- Empty state: Show onboarding prompt when no repositories are connected.
- Loading state: Display spinner overlay on data grid.
- Error state: Show error message in feedback region.
- Open source filter empty: Explain that no catalogue repositories currently have the `open-source` topic.
- Not open source filter empty: Explain that every catalogue repository currently has the `open-source` topic.

## Accessibility Notes
- Focus order: Command strip → catalogue filter toggle group → data grid header → data grid rows → feedback region.
- ARIA: Data grid uses `aria-rowindex`, command strip uses `aria-label`, catalogue filter uses `aria-label="Catalogue filter"`.
- Live region: Feedback region uses `aria-live="polite"` for status updates.

## Responsive Behaviour
- Desktop: Data grid expands to fill available width, command strip remains visible.
- Mobile: Command strip collapses into overflow menu, data grid stacks vertically. Catalogue filter remains visible above the grid (wrap the toggle group; do not hide it inside the actions menu).
