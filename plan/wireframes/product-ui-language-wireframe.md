# Product visual language — page chrome

**Status:** Planning baseline for the UX language feature (sibling of branding [#397](https://github.com/markheydon/solo-dev-board/issues/397)).  
**Related:** [DEC-039](../DECISIONS.md#dec-039-mudblazor-visual-language-for-page-chrome), [UX-LANGUAGE.md](../../.agents/skills/mudblazor/references/UX-LANGUAGE.md).

This is a **substantive refresh of existing pages**, not a new route. Implementation must not invent a second app shell.

## User goals

- Recognise the same product on every feature page.
- Find the primary action without scanning equally loud buttons.
- Understand loading, empty, and error without a blank paper.

## Layout (desktop)

```
+--------------------------------------------------------------+
| MudAppBar  [menu]  SoloDevBoard  [spacer]  [theme] [overflow] |
+----------+---------------------------------------------------+
| Nav      | h5  Page title                                    |
|          | body2 purpose line                                |
|          | ToolBar  [Primary] [Secondary]     [Filter field] |
|          | Paper elevation 1                                 |
|          |   content / MudSkeleton / MudAlert                |
+----------+---------------------------------------------------+
```

## Layout (narrow)

Nav collapses to the existing drawer. ToolBar wraps. Filter field goes full width under actions. No FAB.

## MudBlazor first

- Header: `MudText` only.
- Commands: `MudToolBar` or `MudStack` row with the colour hierarchy in the chooser.
- Two panes (Board Rules, Actions Templates detail): consider `MudSplitPanel` only if the current stack is cramped; do not split every page.
- Loading: `MudSkeleton` for grids and KPI rows; small circular progress on selectors.
- Empty/error: `MudAlert` with one action.

## Out of scope

- New logo, favicon, or GitHub App listing art (that is #397).
- Charts, carousels, FABs, chat components.
- Rewriting workflow behaviour or routes.

## Accessibility

- Page `h5` is the main heading inside `#main-content` after the skip link.
- Icon-only controls have `MudTooltip` and `aria-label`.
- Empty and error alerts are `role="status"` or `role="alert"` as today.

## Test ids

Keep existing `data-testid` values. Add `*-page-header` only if a page has no stable heading hook today.
