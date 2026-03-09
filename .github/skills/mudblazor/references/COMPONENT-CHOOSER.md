# MudBlazor Component Chooser

Map UI patterns to the correct MudBlazor component. Never use a raw HTML element where a MudBlazor component exists.

| UI Pattern | MudBlazor Component | Notes |
|------------|---------------------|-------|
| Single-line text input | `MudTextField<T>` | Use `Variant="Variant.Outlined"` |
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
| Avatar / icon circle | `MudAvatar` | |
| Colour swatch display | `MudChip` with background colour style | |
