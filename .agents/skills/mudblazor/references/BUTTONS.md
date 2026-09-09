# MudBlazor Buttons

Use this reference when choosing between action-oriented MudBlazor components.

## Decision Guidance

- Use `MudButton` for most text-labelled actions.
- Use `MudIconButton` for compact icon-only actions where the icon is unambiguous.
- Use `MudButtonGroup` when multiple related actions should read as one control set.
- Use `MudFab` / `MudFabMenu` only if a future design explicitly calls for a floating action; current SoloDevBoard pages must not add them.
- Use `MudToggleIconButton` when a boolean state needs icon-based on/off feedback.
- Use `MudScrollToTop` for long pages where users need a fast return to top.
- Wrap every `MudIconButton` in `MudTooltip` and set `aria-label`.

## Component Coverage

| Component | When to use it. | Notes. |
|-----------|-----------------|--------|
| `MudButton` | Primary, secondary, neutral, and destructive actions with a text label. | Prefer `Variant` and `Color` over custom CSS styling. |
| `MudButtonGroup` | Grouped actions where visual adjacency communicates relationship. | Useful for segmented controls and grouped command bars. |
| `MudFab` | Prominent page-level call to action. | Do not add to current SoloDevBoard pages. |
| `MudFabMenu` | Speed-dial around a FAB. | Do not add to current SoloDevBoard pages. |
| `MudIconButton` | Small icon-only actions in tables, lists, cards, or toolbars. | Pair with tooltip text when icon meaning may be ambiguous. |
| `MudToggleIconButton` | Toggled states such as favourite, pinned, muted, or enabled/disabled. | Use stateful icon and colour differences to make state clear. |
| `MudScrollToTop` | Long vertically scrolling content. | Prefer this over custom JavaScript scroll controls. |

## Related References

- For button colour and intent hierarchy, see `COMPONENT-CHOOSER.md`.
- For top bar and command placement, see `LAYOUT-NAVIGATION.md`.
