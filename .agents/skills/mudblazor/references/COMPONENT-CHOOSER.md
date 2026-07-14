# MudBlazor Component Chooser

Map UI patterns to the correct MudBlazor component. Never use a raw HTML element where a MudBlazor component exists.

Decision order for UI composition:
1. Pick a MudBlazor component.
2. Compose with MudBlazor layout primitives.
3. Use MudBlazor utility classes in `Class`.
4. Fall back to isolated CSS only when the first three options cannot satisfy the requirement.

## Button Semantics and Colour Hierarchy

Use button colour and variant to communicate intent consistently across pages.

- `Color.Primary` with `Variant.Filled` is for commit actions that move work forward, such as create, save, apply, or confirm.
- `Color.Secondary` is for meaningful non-commit actions, such as load, preview, open, or retry.
- `Color.Default` is for neutral actions, such as edit, close, dismiss, or low-risk utility actions.
- `Color.Error` is for destructive actions, such as delete.
- `Variant.Text` is preferred for cancel actions.
- `Variant.Outlined` is preferred for secondary and neutral actions when you want reduced visual emphasis.
- Avoid using `Color.Primary` for every action in the same section, because this weakens visual hierarchy.

| UI Pattern | MudBlazor Component | Notes |
|------------|---------------------|-------|
| Page section wrapper | `MudPaper`, `MudCard`, or `MudContainer` | Avoid decorative `<div>` wrappers. |
| Responsive page layout | `MudGrid` + `MudItem` | Prefer over custom grid CSS. |
| Single-line text input | `MudTextField<T>` | Use `Variant="Variant.Outlined"` |
| Read-only text / heading | `MudText` | Prefer over styled `<p>` / `<h*>` wrappers when typography is the main concern. |
| Multi-line text / textarea | `MudTextField<T>` with `Lines="N"` | Not `<textarea>` |
| Password input | `MudTextField<string>` with `InputType.Password` | Includes show/hide toggle |
| Search box | `MudTextField<string>` with `Adornment.End` search icon | Not `<input type="search">` |
| Number input | `MudNumericField<T>` | Handles min/max/step |
| Single select / dropdown | `MudSelect<T>` + `MudSelectItem<T>` | Not `<select>` |
| Autocomplete (type-ahead) | `MudAutocomplete<T>` | Popup via `MudPopoverProvider` |
| Multi-select checkbox list | `MudSelect<T>` with `MultiSelection="true"` | Or list of `MudCheckBox` |
| Checkbox | `MudCheckBox<bool>` | Not `<input type="checkbox">` |
| Toggle / switch | `MudSwitch<bool>` | Not `<input type="checkbox">` |
| Radio group | `MudRadioGroup<T>` + `MudRadio<T>` | Not `<input type="radio">` |
| Colour picker | `MudColorPicker` | First-class; no hand-rolling needed |
| Date picker | `MudDatePicker` | Not `<input type="date">` |
| Time picker | `MudTimePicker` | Not `<input type="time">` |
| Button (primary action) | `MudButton Variant="Variant.Filled"` | Not `<button>` |
| Button (secondary/ghost) | `MudButton Variant="Variant.Outlined"` | |
| Button (text-only link) | `MudButton Variant="Variant.Text"` | |
| Icon-only button | `MudIconButton` | Not `<button><img></button>` |
| Floating action button | `MudFab` | |
| Data table / grid | `MudDataGrid<T>` | With `PropertyColumn` / `TemplateColumn` |
| Simple read-only table | `MudTable<T>` | Lighter than DataGrid |
| Card / panel | `MudCard` or `MudPaper` | Not `<div class="card">` |
| Expandable panel | `MudExpansionPanel` | |
| Tabs | `MudTabs` + `MudTabPanel` | |
| Navigation sidebar | `MudNavMenu` + `MudNavLink` + `MudNavGroup` | |
| App bar / header | `MudAppBar` | |
| Loading spinner | `MudProgressCircular` | Indeterminate or determinate |
| Progress bar | `MudProgressLinear` | |
| Alert / info banner | `MudAlert` | Not `<div class="alert">` |
| Notification toast | `ISnackbar.Add(...)` | Service injection; `MudSnackbarProvider` required |
| Modal dialog | `IDialogService.ShowAsync<T>()` | `MudDialogProvider` required |
| Tooltip | `MudTooltip` | |
| Chip / tag | `MudChip<T>` | |
| Chip set (multi) | `MudChipSet<T>` | |
| Badge | `MudBadge` | |
| Divider | `MudDivider` | Not `<hr>` |
| Spacer (flex) | `MudSpacer` | Inside `MudAppBar` or `MudStack` |
| Horizontal stack | `MudStack Row="true"` | Not `<div class="d-flex">` |
| Vertical stack | `MudStack` | |
| Spacing / alignment adjustment | `Class` with MudBlazor utility classes | Prefer `pa-*`, `ma-*`, `d-flex`, `justify-*`, `align-*` over custom CSS. |
| Breakpoint visibility | `MudHidden` | Prefer over custom media-query CSS for show/hide behaviour. |
| Avatar / icon circle | `MudAvatar` | |
| Colour swatch display | `MudChip` with background colour style | |

## Additional Official Components

Use this section for components available in the official MudBlazor overview that are less common in current SoloDevBoard screens.

| UI Pattern | MudBlazor Component | Notes |
|------------|---------------------|-------|
| Grouped adjacent actions | `MudButtonGroup` | See `BUTTONS.md` for grouping guidance. |
| Icon toggle state control | `MudToggleIconButton` | Use for pinned, favourite, and similar icon-first booleans. |
| Long-page return affordance | `MudScrollToTop` | Prefer over custom JavaScript scrolling controls. |
| Section command strip | `MudToolBar` | Use for local command areas inside sections and cards. |
| Custom field shell around bespoke content | `MudField` | Use for consistent labels and helper text around custom content. |
| File selection and upload | `MudFileUpload<T>` | Validate file type and size constraints explicitly. |
| Coordinated validation across fields | `MudForm` | Keep form-level validity and submit behaviour centralised. |
| Focus-trapped interaction region | `MudFocusTrap` | Useful in constrained overlays and accessibility-sensitive flows. |
| Text highlight of search matches | `MudHighlighter` | Pair with filtered search result rendering. |
| Rating input or display | `MudRating` | Use for sentiment scoring interactions. |
| Range and threshold tuning | `MudSlider<T>` | Prefer over free-form numeric text for bounded ranges. |
| Segmented mode toggles | `MudToggleGroup<T>` | Good for compact option groups. |
| Rich contextual menu | `MudMenu` | Use for overflow and contextual actions. |
| Hierarchical breadcrumb path | `MudBreadcrumbs` | Use for deep navigation context. |
| Themed inline navigation link | `MudLink` | Prefer over raw styled anchors where appropriate. |
| Structured non-tabular lists | `MudList<T>` + `MudListItem<T>` | Use when table columns are unnecessary. |
| Lightweight paging controls | `MudPagination` | Pair with server or in-memory paging. |
| Anchored contextual surface | `MudPopover` | Ensure dismiss and focus behaviour are clear. |
| Lightweight static table | `MudSimpleTable` | Use for simple read-only tabular summaries. |
| Chronological event feed | `MudTimeline` | Useful for audit and history visualisation. |
| Hierarchical data explorer | `MudTreeView<T>` | Use for nested repository and rule structures. |
| Drag-and-drop target and reorder region | `MudDropZone<T>` | Useful for visual ordering workflows. |
| Rotating visual panels | `MudCarousel` | Use sparingly and avoid critical information in carousels. |
| Conversational message thread view | `MudChat` | Suitable for chat-like audit or assistant interactions. |
| Themed responsive image display | `MudImage` | Prefer over raw image tags when component features are needed. |
| Standard confirmation prompt | `MudMessageBox` | Use for straightforward confirm and acknowledge flows. |
| Loading-state placeholders | `MudSkeleton` | Improves perceived loading quality on content-heavy views. |
| Backdrop and blocking layer | `MudOverlay` | Use for blocking busy states and modal emphasis. |

For complete grouped guidance, see `INPUTS.md`, `BUTTONS.md`, `LAYOUT-NAVIGATION.md`, `DATA-DISPLAY.md`, and `FEEDBACK-OVERLAYS.md`.
