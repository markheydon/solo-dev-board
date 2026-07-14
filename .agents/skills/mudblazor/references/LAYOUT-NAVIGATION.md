# MudBlazor Layout and Navigation

Use this reference when structuring pages, shell layout, and navigation affordances.

## Layout Components

| Component | When to use it. | Notes. |
|-----------|-----------------|--------|
| `MudAppBar` | Top-level application bar and command strip. | Commonly paired with drawer toggles and page-level actions. |
| `MudContainer` | Width-constrained page content area. | Helps maintain readable line lengths and consistent margins. |
| `MudDrawer` | Side navigation or secondary panel content. | Bind `@bind-Open` for predictable state handling. |
| `MudGrid` + `MudItem` | Responsive column-based layouts. | Prefer over bespoke CSS grid for standard responsive layouts. |
| `MudHidden` | Breakpoint-based conditional visibility. | Prefer over custom media-query show/hide CSS. |
| `MudPaper` | Surface container with elevation and padding support. | Use for sections and panels requiring visual separation. |
| `MudDivider` | Visual separation between content groups. | Prefer over raw `<hr>` elements. |
| `MudStack` | One-dimensional spacing and alignment layout. | Usually the quickest layout primitive for forms and card internals. |
| `MudToolBar` | Inline command toolbar in sections or cards. | Useful for local action groups and filter bars. |

## Navigation Components

| Component | When to use it. | Notes. |
|-----------|-----------------|--------|
| `MudBreadcrumbs` | Hierarchical page context and location path. | Useful for deep configuration flows. |
| `MudLink` | Styled navigation links aligned with theme typography and colours. | Use for inline links in copy and helper text. |
| `MudMenu` | Contextual menu actions anchored to a trigger. | Good for overflow actions in dense UIs. |
| `MudNavMenu` + `MudNavLink` + `MudNavGroup` | Persistent app navigation structures. | Preferred for sidebar and grouped navigation. |

## Decision Guidance

- Start page structure with `MudContainer`, `MudStack`, and `MudPaper`.
- Add `MudGrid` only when true responsive columns are needed.
- Use `MudDrawer` and `MudNavMenu` for app shell navigation, not ad hoc link lists.
- Use `MudMenu` for condensed action sets before introducing custom popovers.

## Related References

- For action components in bars and menus, see `BUTTONS.md`.
- For overlays and transient surfaces, see `FEEDBACK-OVERLAYS.md`.
- For full page shell setup, see `../SKILL.md`.
