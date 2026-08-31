# Board Rules Visualiser — Wireframe

**Purpose**

To provide a visual, interactive overview of board rules and automation logic for a selected GitHub Project v2 board, enabling users to understand, audit, and compare rule sets across repositories and projects.

**User Goals**
- Select a repository and associated GitHub Project v2 board to inspect its rules and automation logic.
- View high-level board metadata (name, description, status, last sync, etc.).
- Explore an interactive diagram of board rules, triggers, and actions.
- Inspect details of individual rules, including triggers, conditions, and actions, in a dedicated panel.
- Identify and review conflicts, warnings, or unsupported rule patterns.
- Enter a compare mode to contrast the current board’s rules with the canonical SoloDevBoard roadmap board design.
- Understand empty, loading, and error states clearly.
- Use the visualiser effectively on both desktop and mobile devices.

**Layout (ASCII Wireframe)**

```
+---------------------------------------------------------------+
| [Repository/Project Selector]  [Reload from GitHub] [Compare] |
+---------------------------------------------------------------+
| [Board Overview Metadata]                                     |
+---------------------------------------------------------------+
| [Interactive Diagram Area]                                    |
|   (nodes: rules/triggers/actions; edges: flows/conditions)    |
+---------------------+---------------------+-------------------+
| [Rule Detail Panel] | [Conflict/Warning Panel] | [Empty/Loading/Error States] |
+---------------------+---------------------+-------------------+
```

**Interaction Notes**
- Repository/project selector uses MudBlazor `MudSelect` and `MudAutocomplete` for efficient navigation.
- Compare mode is accessed via a prominent switch; when active, the diagram overlays or splits to show differences with the canonical board design (see `plan/PROJECT_BOARD_DESIGN.md`).
- **Reload from GitHub** (`MudButton`, filled secondary) sits in the selector header row with compare mode. It keeps the primary repository picker (and the comparison repository when compare mode is on), invalidates the repository catalogue when refreshing that list, refetches project boards from GitHub, and shows loading on the board-selector section only. It must not reuse a load path that clears selection.
- **Try again** on the unsupported-board empty state (`board-rules-unsupported-boards-message` and the comparison equivalent) uses the same keep-selection refetch. Error-state retries (**Try loading repositories again**, **Try loading project boards again**) remain.
- Do not add a global app-bar refresh. Do not refetch automatically when the window or tab regains focus (later [#450](https://github.com/markheydon/solo-dev-board/issues/450)).
- Interactive diagram supports zoom, pan, and node selection (MudBlazor layout primitives and utility classes for structure; custom rendering only if no MudBlazor primitive suffices).
- Selecting a rule node populates the Rule Detail Panel with full rule logic, triggers, and actions.
- Conflict/Warning Panel surfaces issues such as overlapping triggers, unsupported actions, or missing required rules, with clear, actionable descriptions.
- Empty state: prompts user to select a repository/project.
- Loading state: shows MudBlazor progress indicator in the section that is loading.
- Error state: displays error message with retry option.

**State Variants**
- No project selected: only selector and empty state prompt visible.
- Loading: disables interaction, shows progress indicator in diagram area.
- Error: disables interaction, shows error panel with retry.
- Board with no rules: diagram area shows empty state, panels hidden.
- Board with rules: all regions active; selecting nodes updates detail panel.
- Compare mode: diagram overlays or splits to highlight differences.
- Reload in progress: board-selector (and comparison selector if active) shows progress; pickers stay visible and keep their values; Reload is disabled while that refetch runs.
- Unsupported boards: info alert plus **Try again**; repository selection is unchanged.

**Accessibility Notes**
- All interactive elements are keyboard navigable and screen reader accessible.
- Diagram nodes and edges have accessible labels and focus states.
- Panels use MudBlazor’s ARIA and focus management features.

**Responsive Behaviour**
- On mobile, panels stack vertically below the diagram area.
- Diagram area adapts to available width; panels collapse to expandable accordions if space is limited.
- All controls remain accessible and usable at all viewport sizes.

**Planning Baseline**
This wireframe defines the Board Rules Visualiser, focused on GitHub Projects v2 boards, including the `v1.2` Reload from GitHub slice ([#449](https://github.com/markheydon/solo-dev-board/issues/449), epic [#447](https://github.com/markheydon/solo-dev-board/issues/447)). The canonical SoloDevBoard roadmap board design (see `plan/PROJECT_BOARD_DESIGN.md`) may be used as a comparison baseline for expected rules. Implementation must use MudBlazor layout primitives and utility classes wherever possible, with custom rendering only as a last resort. Suggested test ids: `board-rules-reload-from-github-button` for page-level Reload; `board-rules-unsupported-boards-retry-button` for unsupported Try again (keep existing error retry test ids).